#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# render-secrets-from-vault.sh — Vault KV -> /opt/nomaos/.env.api
#
# The ONLY place application secrets are materialised on the host. Nothing
# sensitive is baked into the image, committed to git, or passed on a `docker
# run` command line (argv is world-readable through /proc and `docker inspect`).
#
#   1. AppRole login  (role_id + secret_id -> short-lived token)
#   2. Read kv/<path> (KV v2 first, transparent fallback to KV v1)
#   3. Flatten JSON   ({"Jwt":{"Key":"x"}} -> Jwt__Key=x, the .NET convention)
#   4. Write atomically as 0600, owned by the compose user
#   5. Revoke the token — it is not needed after this point
#
# Note on layering: the application ALSO reads Vault at startup
# (VaultSharp.Extensions.Configuration in Program.cs) and that provider is
# registered after the environment-variable provider, so Vault values win over
# anything rendered here. Rendering still matters because a) values are
# available to the container before managed code runs, b) compose can restart
# the container during a Vault outage, and c) `docker inspect` shows which keys
# exist without showing their values.
#
# Usage:
#   render-secrets-from-vault.sh [--output /opt/nomaos/.env.api] [--path nomaos/prod/appsettings]
#
# Auth inputs, in priority order:
#   VAULT_ROLE_ID / VAULT_SECRET_ID environment variables
#   VAULT_APPROLE_FILE (default /opt/nomaos/vault-approle), a 0600 file with:
#       VAULT_ROLE_ID=...
#       VAULT_SECRET_ID=...
#   VAULT_TOKEN (skips AppRole login entirely — for break-glass only)
#
# Exit codes: 0 rendered · 1 failure (previous file left untouched) · 2 usage
# ─────────────────────────────────────────────────────────────────────────────
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=_lib.sh
source "$SCRIPT_DIR/_lib.sh"

OUTPUT="${ENV_API_FILE:-/opt/nomaos/.env.api}"
VAULT_ADDR="${VAULT_ADDR:-}"
VAULT_KV_MOUNT="${VAULT_KV_MOUNT:-kv}"
VAULT_SECRET_PATH="${VAULT_SECRET_PATH:-nomaos/prod/appsettings}"
VAULT_APPROLE_FILE="${VAULT_APPROLE_FILE:-/opt/nomaos/vault-approle}"
OWNER="${ENV_FILE_OWNER:-}"

usage() {
  sed -n '2,40p' "$0" | sed 's/^# \{0,1\}//'
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --output)  OUTPUT="${2:?}"; shift 2 ;;
    --path)    VAULT_SECRET_PATH="${2:?}"; shift 2 ;;
    --mount)   VAULT_KV_MOUNT="${2:?}"; shift 2 ;;
    --addr)    VAULT_ADDR="${2:?}"; shift 2 ;;
    --owner)   OWNER="${2:?}"; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    *) usage >&2; die "unknown argument: $1" ;;
  esac
done

need_cmd curl
need_cmd jq

[[ -n "$VAULT_ADDR" ]] || die "VAULT_ADDR is not set (env, .env, or --addr)."
VAULT_ADDR="${VAULT_ADDR%/}"

# ── 1. Authenticate ──────────────────────────────────────────────────────────
token="${VAULT_TOKEN:-}"
revoke_token=false

if [[ -z "$token" ]]; then
  role_id="${VAULT_ROLE_ID:-}"
  secret_id="${VAULT_SECRET_ID:-}"

  if [[ -z "$role_id" || -z "$secret_id" ]] && [[ -r "$VAULT_APPROLE_FILE" ]]; then
    require_mode_600 "$VAULT_APPROLE_FILE"
    # Read without sourcing: the file holds opaque values, not shell code.
    role_id="${role_id:-$(awk -F= '/^VAULT_ROLE_ID=/{sub(/^VAULT_ROLE_ID=/,"");print;exit}' "$VAULT_APPROLE_FILE")}"
    secret_id="${secret_id:-$(awk -F= '/^VAULT_SECRET_ID=/{sub(/^VAULT_SECRET_ID=/,"");print;exit}' "$VAULT_APPROLE_FILE")}"
  fi

  [[ -n "$role_id"   ]] || die "no VAULT_ROLE_ID (env or $VAULT_APPROLE_FILE)."
  [[ -n "$secret_id" ]] || die "no VAULT_SECRET_ID (env or $VAULT_APPROLE_FILE)."

  info "authenticating to $VAULT_ADDR via AppRole"
  login_payload="$(jq -nc --arg r "$role_id" --arg s "$secret_id" '{role_id:$r, secret_id:$s}')"

  login_response="$(curl -sS --fail-with-body --max-time 15 \
      -X POST \
      -H 'Content-Type: application/json' \
      -d "$login_payload" \
      "$VAULT_ADDR/v1/auth/approle/login" 2>&1)" \
    || die "AppRole login failed: $(scrub "$login_response")"

  token="$(printf '%s' "$login_response" | jq -r '.auth.client_token // empty')"
  [[ -n "$token" ]] || die "AppRole login returned no client_token."
  revoke_token=true
fi

cleanup() {
  if [[ "$revoke_token" == true && -n "${token:-}" ]]; then
    curl -sS --max-time 10 -X POST \
      -H "X-Vault-Token: $token" \
      "$VAULT_ADDR/v1/auth/token/revoke-self" >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

# ── 2. Read the secret (KV v2, then KV v1) ───────────────────────────────────
read_kv() {
  local url="$1"
  curl -sS --fail-with-body --max-time 15 -H "X-Vault-Token: $token" "$url"
}

info "reading ${VAULT_KV_MOUNT}/${VAULT_SECRET_PATH}"
secret_json=""
if secret_json="$(read_kv "$VAULT_ADDR/v1/$VAULT_KV_MOUNT/data/$VAULT_SECRET_PATH" 2>/dev/null)" \
   && [[ "$(printf '%s' "$secret_json" | jq -r 'has("data") and (.data|has("data"))')" == "true" ]]; then
  payload="$(printf '%s' "$secret_json" | jq '.data.data')"
elif secret_json="$(read_kv "$VAULT_ADDR/v1/$VAULT_KV_MOUNT/$VAULT_SECRET_PATH" 2>/dev/null)"; then
  payload="$(printf '%s' "$secret_json" | jq '.data')"
else
  die "could not read $VAULT_KV_MOUNT/$VAULT_SECRET_PATH (KV v2 or v1). Check the AppRole policy."
fi

[[ "$(printf '%s' "$payload" | jq -r 'type')" == "object" ]] \
  || die "secret payload is not a JSON object."

key_count="$(printf '%s' "$payload" | jq -r 'length')"
(( key_count > 0 )) || die "secret at $VAULT_KV_MOUNT/$VAULT_SECRET_PATH is empty."

# ── 3. Flatten to .NET environment-variable keys ─────────────────────────────
# {"ConnectionStrings":{"DefaultConnection":"..."}} -> ConnectionStrings__DefaultConnection=...
# A key that already uses ':' (Vault flat style) becomes '__' too.
rendered="$(printf '%s' "$payload" | jq -r '
  def envkey: gsub("[:.]"; "__");
  [ paths(scalars) as $p
    | { k: ($p | map(tostring) | join("__") | envkey),
        v: (getpath($p) | if type == "string" then . else tostring end) } ]
  | sort_by(.k)[]
  | select(.v | test("\n") | not)
  | "\(.k)=\(.v)"
')"

dropped="$(printf '%s' "$payload" | jq -r '
  [ paths(scalars) as $p
    | select((getpath($p) | if type == "string" then . else tostring end) | test("\n")) ]
  | length')"

if [[ "$dropped" != "0" ]]; then
  # Multi-line values (PEM keys) cannot be represented in an env_file. They stay
  # in Vault and are picked up by the in-process Vault configuration provider.
  warn "$dropped multi-line value(s) skipped — the app reads those directly from Vault."
fi

# ── 4. Write atomically, 0600 ────────────────────────────────────────────────
mkdir -p "$(dirname "$OUTPUT")"
tmp="$(mktemp "${OUTPUT}.XXXXXX")"
chmod 600 "$tmp"

{
  echo "# Generated by render-secrets-from-vault.sh at $(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo "# Source: ${VAULT_ADDR}/v1/${VAULT_KV_MOUNT}/${VAULT_SECRET_PATH}"
  echo "# DO NOT COMMIT. DO NOT EDIT BY HAND — the next deploy overwrites it."
  echo
  printf '%s\n' "$rendered"
  echo
  echo "# AppRole passthrough: Program.cs re-reads Vault at startup so config"
  echo "# hot-reload (reloadOnChange) keeps working inside the container."
  echo "VAULT_ADDR=${VAULT_ADDR}"
  echo "VAULT_KV_MOUNT=${VAULT_KV_MOUNT}"
  echo "VAULT_SECRET_PATH=${VAULT_SECRET_PATH}"
  if [[ -n "${role_id:-}" && -n "${secret_id:-}" ]]; then
    echo "VAULT_ROLE_ID=${role_id}"
    echo "VAULT_SECRET_ID=${secret_id}"
  fi
} > "$tmp"

if [[ -n "$OWNER" ]]; then
  chown "$OWNER" "$tmp" || warn "could not chown $OUTPUT to $OWNER"
fi

mv -f "$tmp" "$OUTPUT"
chmod 600 "$OUTPUT"

ok "wrote $OUTPUT ($(grep -cv '^\s*\(#\|$\)' "$OUTPUT") variables, mode 0600)"
