#!/usr/bin/env bash
#
# build-release.sh
#
# Build a Nomaos release artifact on the staging EC2 server.
#
# Usage:
#   ./build-release.sh
#   ./build-release.sh <commit-sha>
#   ./build-release.sh origin/dev
#
# Examples:
#   ./build-release.sh
#   ./build-release.sh 8005d0329dbd3ad9b70f18b6bea442811f4b60d6
#   ./build-release.sh origin/dev
#
# The script:
#   1. Reads the required SDK from global.json
#   2. Installs that exact SDK locally if missing
#   3. Fetches Git history
#   4. Creates an isolated worktree at the requested commit
#   5. Restores NuGet packages
#   6. Publishes the API
#   7. Writes release metadata
#   8. Atomically moves the release into releases/<SHA>
#
# It DOES NOT deploy the release.
#
# Deploy separately with:
#   ./deploy-staging.sh <commit-sha>
#

set -Eeuo pipefail

###############################################################################
# Configuration
###############################################################################

BASE="/var/www/Nomaos"
REPO="$BASE"

RELEASES_DIR="$BASE/releases"

PROJECT="Agrovesto.Backend.API/Agrovesto.Backend.API.csproj"

# Keep the exact SDK installation isolated from the system SDK.
# This avoids changing the server's globally installed dotnet version.
DEFAULT_DOTNET_ROOT="$HOME/.dotnet"

###############################################################################
# Helpers
###############################################################################

log() {
    printf '\n[%s] %s\n' "$(date -u '+%Y-%m-%dT%H:%M:%SZ')" "$*"
}

die() {
    echo
    echo "ERROR: $*" >&2
    exit 1
}

cleanup() {
    if [[ -n "${WORKTREE:-}" && -d "${WORKTREE:-}" ]]; then
        log "Removing temporary Git worktree"
        git -C "$REPO" worktree remove --force "$WORKTREE" >/dev/null 2>&1 || true
    fi

    if [[ -n "${STAGING_RELEASE:-}" && -d "${STAGING_RELEASE:-}" ]]; then
        log "Removing incomplete staging release"
        rm -rf "$STAGING_RELEASE"
    fi
}

trap cleanup EXIT

###############################################################################
# Validate repository
###############################################################################

[[ -d "$REPO/.git" ]] ||
    die "Git repository not found at $REPO"

[[ -f "$REPO/global.json" ]] ||
    die "global.json not found at $REPO"

[[ -f "$REPO/$PROJECT" ]] ||
    die "Project file not found: $REPO/$PROJECT"

mkdir -p "$RELEASES_DIR"

###############################################################################
# Read required .NET SDK version from global.json
###############################################################################

SDK_VERSION="$(
    python3 - "$REPO/global.json" <<'PY'
import json
import sys

with open(sys.argv[1], "r", encoding="utf-8") as f:
    data = json.load(f)

version = data.get("sdk", {}).get("version")

if not version:
    raise SystemExit("sdk.version is missing from global.json")

print(version)
PY
)"

ROLL_FORWARD="$(
    python3 - "$REPO/global.json" <<'PY'
import json
import sys

with open(sys.argv[1], "r", encoding="utf-8") as f:
    data = json.load(f)

print(data.get("sdk", {}).get("rollForward", "patch"))
PY
)"

log "Required .NET SDK: $SDK_VERSION"
log "SDK rollForward:    $ROLL_FORWARD"

###############################################################################
# Configure local .NET SDK
###############################################################################

DOTNET_ROOT="${DOTNET_ROOT:-$DEFAULT_DOTNET_ROOT}"

export DOTNET_ROOT
export PATH="$DOTNET_ROOT:$PATH"

SDK_DIR="$DOTNET_ROOT/sdk/$SDK_VERSION"

if [[ ! -x "$DOTNET_ROOT/dotnet" || ! -d "$SDK_DIR" ]]; then

    log "Required SDK $SDK_VERSION is not installed."
    log "Installing .NET SDK $SDK_VERSION into $DOTNET_ROOT"

    mkdir -p "$DOTNET_ROOT"

    INSTALL_SCRIPT="/tmp/dotnet-install.sh"

    curl -fsSL \
        https://dot.net/v1/dotnet-install.sh \
        -o "$INSTALL_SCRIPT"

    chmod +x "$INSTALL_SCRIPT"

    "$INSTALL_SCRIPT" \
        --version "$SDK_VERSION" \
        --install-dir "$DOTNET_ROOT" \
        --no-path

    rm -f "$INSTALL_SCRIPT"
fi

###############################################################################
# Verify SDK selection
###############################################################################

INSTALLED_SDKS="$("$DOTNET_ROOT/dotnet" --list-sdks)"

if ! grep -Eq "^${SDK_VERSION//./\\.}[[:space:]]" <<< "$INSTALLED_SDKS"; then
    echo "$INSTALLED_SDKS"
    die "Required SDK $SDK_VERSION is not available under $DOTNET_ROOT"
fi

SELECTED_SDK="$("$DOTNET_ROOT/dotnet" --version)"

if [[ "$SELECTED_SDK" != "$SDK_VERSION" ]]; then
    echo
    echo "Installed SDKs:"
    echo "$INSTALLED_SDKS"
    echo
    die "Expected SDK $SDK_VERSION but dotnet selected $SELECTED_SDK"
fi

log "Using .NET SDK: $SELECTED_SDK"

###############################################################################
# Resolve commit
###############################################################################

INPUT="${1:-}"

log "Fetching repository updates..."

git -C "$REPO" fetch --all --prune --quiet

if [[ -z "$INPUT" ]]; then
    log "No commit supplied; using origin/dev"
    COMMIT_SHA="$(git -C "$REPO" rev-parse origin/dev)"
else
    log "Resolving requested revision: $INPUT"
    COMMIT_SHA="$(git -C "$REPO" rev-parse "$INPUT^{commit}")"
fi

[[ -n "$COMMIT_SHA" ]] ||
    die "Unable to resolve commit"

###############################################################################
# Display commit information
###############################################################################

COMMIT_MESSAGE="$(
    git -C "$REPO" show -s \
        --format='%s' \
        "$COMMIT_SHA"
)"

COMMIT_DATE="$(
    git -C "$REPO" show -s \
        --format='%ci' \
        "$COMMIT_SHA"
)"

log "Resolved commit:"
echo "  SHA:     $COMMIT_SHA"
echo "  Date:    $COMMIT_DATE"
echo "  Message: $COMMIT_MESSAGE"

###############################################################################
# Check whether release already exists
###############################################################################

FINAL_RELEASE="$RELEASES_DIR/$COMMIT_SHA"

if [[ -d "$FINAL_RELEASE" ]]; then
    die "Release already exists: $FINAL_RELEASE"
fi

###############################################################################
# Create isolated Git worktree
###############################################################################

WORKTREE="$(mktemp -d /tmp/nomaos-build.XXXXXX)"

log "Creating isolated worktree:"
echo "  $WORKTREE"

git -C "$REPO" worktree add \
    --detach \
    "$WORKTREE" \
    "$COMMIT_SHA" \
    --quiet

###############################################################################
# Validate source
###############################################################################

[[ -f "$WORKTREE/global.json" ]] ||
    die "global.json missing from commit $COMMIT_SHA"

[[ -f "$WORKTREE/$PROJECT" ]] ||
    die "Project missing from commit $COMMIT_SHA"

WORKTREE_SDK_VERSION="$(
    python3 - "$WORKTREE/global.json" <<'PY'
import json
import sys

with open(sys.argv[1], "r", encoding="utf-8") as f:
    data = json.load(f)

print(data["sdk"]["version"])
PY
)"

if [[ "$WORKTREE_SDK_VERSION" != "$SDK_VERSION" ]]; then
    die "global.json SDK changed in requested commit: expected $SDK_VERSION, found $WORKTREE_SDK_VERSION"
fi

###############################################################################
# Create staging release directory
###############################################################################

STAGING_RELEASE="$RELEASES_DIR/.staging-$COMMIT_SHA"

rm -rf "$STAGING_RELEASE"
mkdir -p "$STAGING_RELEASE"

###############################################################################
# Build
###############################################################################

cd "$WORKTREE"

log "Restoring NuGet packages..."

"$DOTNET_ROOT/dotnet" restore \
    "$PROJECT"

log "Publishing application..."

"$DOTNET_ROOT/dotnet" publish \
    "$PROJECT" \
    -c Release \
    -o "$STAGING_RELEASE" \
    --no-restore \
    /p:UseAppHost=false \
    /p:PublishTrimmed=false

###############################################################################
# Write release metadata
###############################################################################

BUILD_TIMESTAMP="$(date -u '+%Y-%m-%dT%H:%M:%SZ')"

printf '%s\n' "$COMMIT_SHA" \
    > "$STAGING_RELEASE/.commit-sha"

printf '%s\n' "manual-${BUILD_TIMESTAMP//[-:TZ]/}" \
    > "$STAGING_RELEASE/.version"

printf '%s\n' "$BUILD_TIMESTAMP" \
    > "$STAGING_RELEASE/.deployed-at"

printf '%s\n' "$BUILD_TIMESTAMP" \
    > "$STAGING_RELEASE/.built-at"

###############################################################################
# Validate artifact
###############################################################################

log "Validating release artifact..."

[[ -s "$STAGING_RELEASE/.commit-sha" ]] ||
    die ".commit-sha was not created"

if ! grep -Fxq "$COMMIT_SHA" "$STAGING_RELEASE/.commit-sha"; then
    die ".commit-sha does not match requested commit"
fi

[[ -f "$STAGING_RELEASE/Agrovesto.Backend.API.dll" ]] ||
    die "Agrovesto.Backend.API.dll was not produced"

###############################################################################
# Display release information
###############################################################################

ARTIFACT_SIZE="$(
    du -sh "$STAGING_RELEASE" |
        awk '{print $1}'
)"

FILE_COUNT="$(
    find "$STAGING_RELEASE" -type f |
        wc -l
)"

echo
echo "=============================================="
echo "Release artifact ready"
echo "=============================================="
echo "Commit:       $COMMIT_SHA"
echo "Date:         $COMMIT_DATE"
echo "Message:      $COMMIT_MESSAGE"
echo "SDK:          $SELECTED_SDK"
echo "Artifact:     $STAGING_RELEASE"
echo "Size:         $ARTIFACT_SIZE"
echo "Files:        $FILE_COUNT"
echo "=============================================="

###############################################################################
# Atomically publish release
###############################################################################

log "Publishing release directory..."

mv "$STAGING_RELEASE" "$FINAL_RELEASE"

# Staging directory is now gone, so don't try to remove it in cleanup.
STAGING_RELEASE=""

###############################################################################
# Final verification
###############################################################################

echo
echo "Release installed at:"
echo "  $FINAL_RELEASE"
echo

echo "Commit metadata:"
cat "$FINAL_RELEASE/.commit-sha"

echo
echo "Version metadata:"
cat "$FINAL_RELEASE/.version"

echo
echo "Build timestamp:"
cat "$FINAL_RELEASE/.built-at"

echo
echo "=============================================="
echo "BUILD SUCCESSFUL"
echo "=============================================="
echo
echo "Deploy with:"
echo
echo "  ./deploy-staging.sh $COMMIT_SHA"
echo

