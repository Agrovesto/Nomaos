#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# deploy.sh — containerized blue/green deployment for NomaOS on EC2
#
# Usage:
#   deploy.sh --app nomaos-api --image <ecr-ref> [options]
#
# Deployment flow:
#   1. Validate configuration
#   2. Pull immutable image
#   3. Start idle slot alongside live slot
#   4. Docker healthcheck
#   5. Application-local HTTP healthcheck inside candidate container
#   6. Nginx-container -> candidate HTTP healthcheck
#   7. Optional EC2-host -> candidate HTTP healthcheck
#   8. Flip Nginx atomically
#   9. Verify traffic through Nginx
#  10. Stop old slot
#  11. Prune dangling images
#
# IMPORTANT:
#   Application health failures are NON-FATAL by default.
#
#   The deployment continues even when:
#     - Docker HEALTHCHECK fails
#     - candidate-local /health/ready fails
#     - Nginx-container -> candidate /health/ready fails
#     - optional EC2-host health check fails
#     - post-flip /health fails
#
#   Infrastructure failures remain FATAL:
#     - invalid arguments/config
#     - ECR/image pull failure
#     - Docker Compose startup failure
#     - nginx configuration failure
#     - nginx reload failure
#
# Exit codes:
#   0 = deployment completed
#   1 = deployment failed / rolled back
#   2 = usage error
# ─────────────────────────────────────────────────────────────────────────────

set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# shellcheck source=lib.sh
source "$SCRIPT_DIR/lib.sh"

# ─────────────────────────────────────────────────────────────────────────────
# Configuration
# ─────────────────────────────────────────────────────────────────────────────

APP_NAME=""
IMAGE=""
COMMIT_SHA=""

HEALTH_TIMEOUT="${HEALTH_TIMEOUT:-10}"
KEEP_OLD="${KEEP_OLD:-false}"

# Application health failures are warnings only by default.
CONTINUE_ON_HEALTH_FAILURE="${CONTINUE_ON_HEALTH_FAILURE:-true}"

# Health checks.
CHECK_DOCKER_HEALTH="${CHECK_DOCKER_HEALTH:-true}"
CHECK_CONTAINER_LOCAL="${CHECK_CONTAINER_LOCAL:-true}"
CHECK_NGINX_NETWORK="${CHECK_NGINX_NETWORK:-true}"

# Optional EC2 host check.
#
# Example:
#   HOST_HEALTH_URL=http://127.0.0.1:5000/health/ready
#
# Leave empty to skip.
HOST_HEALTH_URL="${HOST_HEALTH_URL:-}"

CONTAINER_HEALTH_PATH="${CONTAINER_HEALTH_PATH:-/health/ready}"
EDGE_HEALTH_PATH="${EDGE_HEALTH_PATH:-/health}"

HEALTH_RETRIES="${HEALTH_RETRIES:-3}"
HEALTH_RETRY_INTERVAL="${HEALTH_RETRY_INTERVAL:-1}"

EDGE_RETRIES="${EDGE_RETRIES:-2}"
EDGE_RETRY_INTERVAL="${EDGE_RETRY_INTERVAL:-1}"

DRAIN_SECONDS="${DRAIN_SECONDS:-2}"

NGINX_CONTAINER="${NGINX_CONTAINER:-nomaos-nginx}"

# ─────────────────────────────────────────────────────────────────────────────
# Usage
# ─────────────────────────────────────────────────────────────────────────────

usage() {
  cat <<'EOF'
Usage: deploy.sh --app <name> --image <ref> [options]

Required:
  --app <name>
      App key, e.g. nomaos-api

  --image <ref>
      Full immutable image reference.
      Examples:
        1234.dkr.ecr.eu-west-1.amazonaws.com/nomaos:abc123
        1234.dkr.ecr.eu-west-1.amazonaws.com/nomaos@sha256:abc123...

Options:
  --commit <sha>
      Commit SHA recorded for the deployment.

  --timeout <sec>
      Docker health gate timeout.
      Default: 10

  --keep-old
      Leave previous slot running after the traffic flip.

  --dry-run
      Print deployment plan and exit without changing anything.

  -h, --help
      Show this help.

Environment overrides:
  CONTINUE_ON_HEALTH_FAILURE=true|false
  CHECK_DOCKER_HEALTH=true|false
  CHECK_CONTAINER_LOCAL=true|false
  CHECK_NGINX_NETWORK=true|false
  HOST_HEALTH_URL=http://127.0.0.1:<port>/health/ready
  HEALTH_RETRIES=3
  HEALTH_RETRY_INTERVAL=1
  EDGE_RETRIES=2
  EDGE_RETRY_INTERVAL=1
  DRAIN_SECONDS=2
EOF
}

# ─────────────────────────────────────────────────────────────────────────────
# Parse arguments
# ─────────────────────────────────────────────────────────────────────────────

while [[ $# -gt 0 ]]; do
  case "$1" in
    --app)
      APP_NAME="${2:?Missing value for --app}"
      shift 2
      ;;

    --image)
      IMAGE="${2:?Missing value for --image}"
      shift 2
      ;;

    --commit)
      COMMIT_SHA="${2:?Missing value for --commit}"
      shift 2
      ;;

    --timeout)
      HEALTH_TIMEOUT="${2:?Missing value for --timeout}"
      shift 2
      ;;

    --keep-old)
      KEEP_OLD=true
      shift
      ;;

    --dry-run)
      DRY_RUN=true
      shift
      ;;

    -h|--help)
      usage
      exit 0
      ;;

    *)
      usage >&2
      die "unknown argument: $1"
      ;;
  esac
done

[[ -n "$APP_NAME" ]] || {
  usage >&2
  exit 2
}

[[ -n "$IMAGE" ]] || {
  usage >&2
  exit 2
}

# Accept:
#   repository:tag
#   repository@sha256:digest
if [[ "$IMAGE" != *:* && "$IMAGE" != *@* ]]; then
  die "--image must contain an explicit tag or digest; :latest is not allowed"
fi

# ─────────────────────────────────────────────────────────────────────────────
# Validate dependencies/configuration
# ─────────────────────────────────────────────────────────────────────────────

require_docker
load_app_env "$APP_NAME"

APP_PORT="${APP_PORT:-5000}"

UPSTREAM_NAME="${UPSTREAM_NAME:-${APP_NAME//-/_}}"

UPSTREAM_DIR="$NOMAOS_STATE_DIR/nginx/upstreams"
UPSTREAM_FILE="$UPSTREAM_DIR/${UPSTREAM_NAME}.conf"

COMMIT_SHA="${COMMIT_SHA:-unknown}"

mkdir -p \
  "$UPSTREAM_DIR" \
  "$NOMAOS_STATE_DIR/logs/$APP_NAME/$TARGET"

# ─────────────────────────────────────────────────────────────────────────────
# Determine active and target slots
# ─────────────────────────────────────────────────────────────────────────────

ACTIVE="$(get_active_slot "$APP_NAME")"

case "$ACTIVE" in
  blue|green)
    ;;
  *)
    die "invalid active slot '$ACTIVE'; expected blue or green"
    ;;
esac

TARGET="$(other_slot "$ACTIVE")"

ACTIVE_CONTAINER="${APP_NAME}-${ACTIVE}"
TARGET_CONTAINER="${APP_NAME}-${TARGET}"

# ─────────────────────────────────────────────────────────────────────────────
# Display deployment plan
# ─────────────────────────────────────────────────────────────────────────────

cat <<EOF

===============================================================
 NomaOS Blue/Green Deployment
===============================================================

  app             : $APP_NAME
  image           : $IMAGE
  commit          : $COMMIT_SHA

  live slot       : $ACTIVE
  live container  : $ACTIVE_CONTAINER
  live state      : $(container_state "$ACTIVE_CONTAINER")

  target slot     : $TARGET
  target container: $TARGET_CONTAINER

  app port        : $APP_PORT
  upstream        : $UPSTREAM_FILE
  nginx container : $NGINX_CONTAINER

  docker health   : $CHECK_DOCKER_HEALTH
  local container : $CHECK_CONTAINER_LOCAL
  nginx network   : $CHECK_NGINX_NETWORK
  EC2 host check  : ${HOST_HEALTH_URL:-disabled}

  continue on
  health failure  : $CONTINUE_ON_HEALTH_FAILURE

===============================================================

EOF

if [[ "${DRY_RUN:-false}" == true ]]; then
  info "dry run — nothing changed"
  exit 0
fi

# ─────────────────────────────────────────────────────────────────────────────
# Compose helper
# ─────────────────────────────────────────────────────────────────────────────

compose_target() {
  IMAGE="$IMAGE" \
  APP_NAME="$APP_NAME" \
  SLOT="$TARGET" \
  APP_PORT="$APP_PORT" \
  COMMIT_SHA="$COMMIT_SHA" \
  NOMAOS_STATE_DIR="$NOMAOS_STATE_DIR" \
  docker compose \
    -p "${APP_NAME}-${TARGET}" \
    -f "$COMPOSE_DIR/docker-compose.app.yml" \
    "$@"
}

# ─────────────────────────────────────────────────────────────────────────────
# Retry helper
# ─────────────────────────────────────────────────────────────────────────────

retry_command() {
  local label="$1"
  local attempts="$2"
  local interval="$3"

  shift 3

  local attempt

  for attempt in $(seq 1 "$attempts"); do

    if "$@"; then
      return 0
    fi

    log "   $label attempt $attempt/$attempts failed"

    if (( attempt < attempts )); then
      sleep "$interval"
    fi
  done

  return 1
}

# ─────────────────────────────────────────────────────────────────────────────
# HTTP probe inside a container
#
# Supports either curl or wget.
# ─────────────────────────────────────────────────────────────────────────────

container_http_probe() {
  local container="$1"
  local url="$2"

  if docker exec "$container" \
      sh -c 'command -v curl >/dev/null 2>&1'; then

    docker exec "$container" \
      curl \
        --fail \
        --silent \
        --show-error \
        --max-time 5 \
        "$url" \
        >/dev/null 2>&1

    return $?
  fi

  if docker exec "$container" \
      sh -c 'command -v wget >/dev/null 2>&1'; then

    docker exec "$container" \
      wget \
        -q \
        -O /dev/null \
        --timeout=5 \
        "$url" \
        >/dev/null 2>&1

    return $?
  fi

  return 2
}

# ─────────────────────────────────────────────────────────────────────────────
# HTTP probe from EC2 host
# ─────────────────────────────────────────────────────────────────────────────

host_http_probe() {
  local url="$1"

  if command -v curl >/dev/null 2>&1; then

    curl \
      --fail \
      --silent \
      --show-error \
      --max-time 5 \
      "$url" \
      >/dev/null 2>&1

    return $?
  fi

  if command -v wget >/dev/null 2>&1; then

    wget \
      -q \
      -O /dev/null \
      --timeout=5 \
      "$url" \
      >/dev/null 2>&1

    return $?
  fi

  return 2
}

# ─────────────────────────────────────────────────────────────────────────────
# Nginx reload
# ─────────────────────────────────────────────────────────────────────────────

reload_nginx() {

  info "validating nginx configuration"

  if ! docker exec "$NGINX_CONTAINER" \
      nginx -t >/dev/null 2>&1; then

    warn "nginx -t failed:"

    docker exec "$NGINX_CONTAINER" \
      nginx -t 2>&1 |
      sed 's/^/    /' >&2 || true

    return 1
  fi

  info "reloading nginx"

  docker exec "$NGINX_CONTAINER" \
    nginx -s reload
}

# ─────────────────────────────────────────────────────────────────────────────
# Write upstream atomically
# ─────────────────────────────────────────────────────────────────────────────

write_upstream() {
  local slot="$1"
  local tmp

  # Create temp file on the SAME filesystem as the final file.
  # This guarantees mv is an atomic replacement.
  tmp="$(mktemp "$UPSTREAM_DIR/.${UPSTREAM_NAME}.XXXXXX")"

  cat > "$tmp" <<EOF
# Generated by deploy.sh
# generated_at=$(date -u +%Y-%m-%dT%H:%M:%SZ)
# app=$APP_NAME
# slot=$slot
# commit=$COMMIT_SHA

upstream ${UPSTREAM_NAME} {
    server ${APP_NAME}-${slot}:${APP_PORT} max_fails=2 fail_timeout=5s;
    keepalive 32;
}
EOF

  chmod 644 "$tmp"

  mv -f "$tmp" "$UPSTREAM_FILE"
}

# ─────────────────────────────────────────────────────────────────────────────
# Failure cleanup / rollback
# ─────────────────────────────────────────────────────────────────────────────

FLIPPED=false
UPSTREAM_BACKUP=""

cleanup_on_failure() {
  local rc=$?

  (( rc == 0 )) && return 0

  echo ""
  warn "deployment failed with exit code $rc"

  # If traffic was already flipped, restore the previous upstream.
  if [[ "$FLIPPED" == true &&
        -n "$UPSTREAM_BACKUP" &&
        -r "$UPSTREAM_BACKUP" ]]; then

    warn "restoring previous nginx upstream"
    cp "$UPSTREAM_BACKUP" "$UPSTREAM_FILE"

    if ! reload_nginx; then
      warn "CRITICAL: nginx rollback reload failed"
      warn "CHECK THE EDGE IMMEDIATELY"
    fi

    set_active_slot "$APP_NAME" "$ACTIVE"
  fi

  # Candidate is never left around after a genuine fatal failure.
  warn "tearing down candidate slot: $TARGET"

  compose_target down --remove-orphans \
    >/dev/null 2>&1 || true

  warn "live slot remains: $ACTIVE"

  exit "$rc"
}

trap cleanup_on_failure EXIT

# ─────────────────────────────────────────────────────────────────────────────
# 1. Pull immutable image
# ─────────────────────────────────────────────────────────────────────────────

info "pulling image"

docker pull --quiet "$IMAGE" >/dev/null || {
  die "could not pull image: $IMAGE"
}

ok "image present locally"

# ─────────────────────────────────────────────────────────────────────────────
# 2. Start target slot
# ─────────────────────────────────────────────────────────────────────────────

info "starting target slot: $TARGET"

# Remove stale candidate from previous failed deployments.
compose_target down --remove-orphans \
  >/dev/null 2>&1 || true

compose_target up -d --no-build

ok "$TARGET_CONTAINER started"

# ─────────────────────────────────────────────────────────────────────────────
# 3. Docker HEALTHCHECK
# ─────────────────────────────────────────────────────────────────────────────

DOCKER_HEALTH_OK=false

if [[ "$CHECK_DOCKER_HEALTH" == true ]]; then

  info "waiting for Docker healthcheck: $TARGET_CONTAINER"

  if wait_for_health "$TARGET_CONTAINER" "$HEALTH_TIMEOUT"; then

    DOCKER_HEALTH_OK=true

    ok "Docker healthcheck passed"

  else

    warn "$TARGET_CONTAINER did not become healthy within ${HEALTH_TIMEOUT}s"

    warn "last 60 log lines from $TARGET_CONTAINER:"

    docker logs \
      --tail 60 \
      "$TARGET_CONTAINER" 2>&1 |
      sed 's/^/    /' >&2 || true

    if [[ "$CONTINUE_ON_HEALTH_FAILURE" == true ]]; then

      warn "Docker healthcheck failed — CONTINUING deployment"

    else

      die "candidate Docker healthcheck failed"
    fi
  fi

else

  warn "Docker healthcheck disabled"
fi

# ─────────────────────────────────────────────────────────────────────────────
# 4. Candidate-container LOCAL healthcheck
#
# Runs INSIDE the candidate container:
#
#   http://127.0.0.1:<APP_PORT>/health/ready
#
# This checks the application without depending on:
#   - nginx
#   - Docker DNS
#   - host port publishing
#   - public DNS/domain
# ─────────────────────────────────────────────────────────────────────────────

LOCAL_HEALTH_OK=false

if [[ "$CHECK_CONTAINER_LOCAL" == true ]]; then

  info "checking candidate locally from inside $TARGET_CONTAINER"

  LOCAL_URL="http://127.0.0.1:${APP_PORT}${CONTAINER_HEALTH_PATH}"

  if retry_command \
      "local health" \
      "$HEALTH_RETRIES" \
      "$HEALTH_RETRY_INTERVAL" \
      container_http_probe \
      "$TARGET_CONTAINER" \
      "$LOCAL_URL"; then

    LOCAL_HEALTH_OK=true

    ok "candidate local healthcheck passed"

  else

    warn "candidate local healthcheck failed"
    warn "URL: $LOCAL_URL"

    if [[ "$CONTINUE_ON_HEALTH_FAILURE" == true ]]; then
      warn "local healthcheck failed — CONTINUING deployment"
    else
      die "candidate local healthcheck failed"
    fi
  fi

else

  warn "candidate local healthcheck disabled"
fi

# ─────────────────────────────────────────────────────────────────────────────
# 5. Candidate reachability from Nginx
#
# Runs INSIDE nginx container:
#
#   http://nomaos-api-blue:<APP_PORT>/health/ready
#
# This verifies Docker-network connectivity between nginx and the candidate.
# It is NOT the public/domain URL.
# ─────────────────────────────────────────────────────────────────────────────

NGINX_NETWORK_OK=false

if [[ "$CHECK_NGINX_NETWORK" == true ]]; then

  info "checking candidate from nginx container"

  NGINX_TARGET_URL="http://${TARGET_CONTAINER}:${APP_PORT}${CONTAINER_HEALTH_PATH}"

  if retry_command \
      "nginx network probe" \
      "$HEALTH_RETRIES" \
      "$HEALTH_RETRY_INTERVAL" \
      container_http_probe \
      "$NGINX_CONTAINER" \
      "$NGINX_TARGET_URL"; then

    NGINX_NETWORK_OK=true

    ok "nginx can reach candidate"

  else

    warn "nginx could not reach candidate"
    warn "URL: $NGINX_TARGET_URL"

    if [[ "$CONTINUE_ON_HEALTH_FAILURE" == true ]]; then
      warn "nginx network healthcheck failed — CONTINUING deployment"
    else
      die "nginx cannot reach candidate"
    fi
  fi

else

  warn "nginx network healthcheck disabled"
fi

# ─────────────────────────────────────────────────────────────────────────────
# 6. Optional EC2 HOST healthcheck
#
# Only performed when HOST_HEALTH_URL is supplied.
#
# Example:
#   HOST_HEALTH_URL=http://127.0.0.1:5002/health/ready
#
# This requires the candidate port to be published on the EC2 host.
# ─────────────────────────────────────────────────────────────────────────────

HOST_HEALTH_OK=true

if [[ -n "$HOST_HEALTH_URL" ]]; then

  HOST_HEALTH_OK=false

  info "checking candidate from EC2 host"

  if retry_command \
      "EC2 host health" \
      "$HEALTH_RETRIES" \
      "$HEALTH_RETRY_INTERVAL" \
      host_http_probe \
      "$HOST_HEALTH_URL"; then

    HOST_HEALTH_OK=true

    ok "EC2 host healthcheck passed"

  else

    warn "EC2 host healthcheck failed"
    warn "URL: $HOST_HEALTH_URL"

    if [[ "$CONTINUE_ON_HEALTH_FAILURE" == true ]]; then
      warn "EC2 host healthcheck failed — CONTINUING deployment"
    else
      die "EC2 host healthcheck failed"
    fi
  fi

else

  info "EC2 host healthcheck skipped"
fi

# ─────────────────────────────────────────────────────────────────────────────
# Health summary before traffic flip
# ─────────────────────────────────────────────────────────────────────────────

echo ""
echo "───────────────────────────────────────────────────────────────"
echo " Candidate health summary"
echo "───────────────────────────────────────────────────────────────"
echo " Docker health       : $([ "$DOCKER_HEALTH_OK" = true ] && echo "PASS" || echo "FAIL")"
echo " Container-local     : $([ "$LOCAL_HEALTH_OK" = true ] && echo "PASS" || echo "FAIL")"
echo " Nginx network       : $([ "$NGINX_NETWORK_OK" = true ] && echo "PASS" || echo "FAIL")"
echo " EC2 host            : $([ "$HOST_HEALTH_OK" = true ] && echo "PASS" || echo "FAIL/SKIPPED")"
echo " Continue on failure: $CONTINUE_ON_HEALTH_FAILURE"
echo "───────────────────────────────────────────────────────────────"
echo ""

# ─────────────────────────────────────────────────────────────────────────────
# 7. Prepare Nginx flip
# ─────────────────────────────────────────────────────────────────────────────

info "preparing traffic flip to $TARGET"

if [[ -r "$UPSTREAM_FILE" ]]; then

  UPSTREAM_BACKUP="$(
    mktemp "$UPSTREAM_DIR/.${UPSTREAM_NAME}.backup.XXXXXX"
  )"

  cp "$UPSTREAM_FILE" "$UPSTREAM_BACKUP"

else

  warn "no existing upstream file found"
  warn "rollback may not be possible if nginx reload fails"
fi

write_upstream "$TARGET"

# Mark as flipped BEFORE reload so the failure trap knows that the upstream
# file was modified and must be restored if nginx reload fails.
FLIPPED=true

# ─────────────────────────────────────────────────────────────────────────────
# 8. Nginx validation + reload
#
# THIS REMAINS FATAL.
#
# An invalid nginx configuration must never be silently ignored.
# ─────────────────────────────────────────────────────────────────────────────

if ! reload_nginx; then
  die "nginx reload failed — previous upstream will be restored"
fi

set_active_slot "$APP_NAME" "$TARGET"

ok "traffic switched to $TARGET"

# ─────────────────────────────────────────────────────────────────────────────
# 9. Post-flip edge healthcheck
#
# This checks nginx itself.
#
# It uses:
#   http://127.0.0.1/health
#
# from INSIDE the nginx container.
#
# This is intentionally NON-FATAL.
# ─────────────────────────────────────────────────────────────────────────────

EDGE_OK=false

info "verifying traffic through nginx"

EDGE_URL="http://127.0.0.1${EDGE_HEALTH_PATH}"

for attempt in $(seq 1 "$EDGE_RETRIES"); do

  if docker exec "$NGINX_CONTAINER" \
      wget \
        -q \
        -O /dev/null \
        --timeout=5 \
        "$EDGE_URL" \
        --header="Host: ${EDGE_HEALTH_HOST:-localhost}" \
        2>/dev/null; then

    EDGE_OK=true

    ok "post-flip edge healthcheck passed"

    break
  fi

  log "   edge health attempt $attempt/$EDGE_RETRIES failed"

  if (( attempt < EDGE_RETRIES )); then
    sleep "$EDGE_RETRY_INTERVAL"
  fi
done

if [[ "$EDGE_OK" != true ]]; then

  warn "post-flip edge /health check failed"

  warn "This check is NON-FATAL."

  warn "The candidate was already switched into service."

  warn "Direct candidate URL:"
  warn "http://${TARGET_CONTAINER}:${APP_PORT}${CONTAINER_HEALTH_PATH}"

  warn "Candidate logs:"
  docker logs \
    --tail 100 \
    "$TARGET_CONTAINER" 2>&1 |
    sed 's/^/    /' >&2 || true
fi

# ─────────────────────────────────────────────────────────────────────────────
# Deployment has passed the traffic flip.
#
# From this point onward, cleanup must NOT trigger the rollback trap.
# ─────────────────────────────────────────────────────────────────────────────

trap - EXIT

# ─────────────────────────────────────────────────────────────────────────────
# 10. Stop old slot
# ─────────────────────────────────────────────────────────────────────────────

if [[ "$KEEP_OLD" == true ]]; then

  info "--keep-old enabled"
  info "leaving $ACTIVE_CONTAINER running for rollback"

elif [[ "$(container_state "$ACTIVE_CONTAINER")" != "absent" ]]; then

  info "draining old slot: $ACTIVE"

  if (( DRAIN_SECONDS > 0 )); then
    sleep "$DRAIN_SECONDS"
  fi

  IMAGE="$IMAGE" \
  APP_NAME="$APP_NAME" \
  SLOT="$ACTIVE" \
  APP_PORT="$APP_PORT" \
  COMMIT_SHA="$COMMIT_SHA" \
  NOMAOS_STATE_DIR="$NOMAOS_STATE_DIR" \
  docker compose \
    -p "${APP_NAME}-${ACTIVE}" \
    -f "$COMPOSE_DIR/docker-compose.app.yml" \
    down --remove-orphans || {

      warn "could not cleanly stop $ACTIVE_CONTAINER"
      warn "the new slot is already live"
    }

  ok "old slot retired"

else

  info "old slot already absent"
fi

# ─────────────────────────────────────────────────────────────────────────────
# 11. Cleanup dangling images
# ─────────────────────────────────────────────────────────────────────────────

info "retaining Docker image cache for faster future deployments"

# ─────────────────────────────────────────────────────────────────────────────
# 12. Cleanup backup
# ─────────────────────────────────────────────────────────────────────────────

if [[ -n "$UPSTREAM_BACKUP" ]]; then
  rm -f "$UPSTREAM_BACKUP" || true
fi

# ─────────────────────────────────────────────────────────────────────────────
# 13. Final status
# ─────────────────────────────────────────────────────────────────────────────

echo ""
echo "==============================================================="
echo " Deployment completed"
echo "==============================================================="
echo ""
echo "  App                 : $APP_NAME"
echo "  Previous slot       : $ACTIVE"
echo "  Active slot         : $TARGET"
echo "  Active container    : $TARGET_CONTAINER"
echo "  Image               : $IMAGE"
echo "  Commit              : $COMMIT_SHA"
echo ""
echo "  Docker health       : $([ "$DOCKER_HEALTH_OK" = true ] && echo "PASS" || echo "FAIL/WARNING")"
echo "  Container local     : $([ "$LOCAL_HEALTH_OK" = true ] && echo "PASS" || echo "FAIL/WARNING")"
echo "  Nginx network       : $([ "$NGINX_NETWORK_OK" = true ] && echo "PASS" || echo "FAIL/WARNING")"
echo "  EC2 host             : $([ "$HOST_HEALTH_OK" = true ] && echo "PASS" || echo "FAIL/WARNING/SKIPPED")"
echo "  Edge /health        : $([ "$EDGE_OK" = true ] && echo "PASS" || echo "FAIL/WARNING")"
echo ""
echo "  Health failures     : NON-FATAL"
echo "  Nginx/config errors : FATAL"
echo ""
echo "==============================================================="

if [[ "$DOCKER_HEALTH_OK" != true ||
      "$LOCAL_HEALTH_OK" != true ||
      "$NGINX_NETWORK_OK" != true ||
      "$HOST_HEALTH_OK" != true ||
      "$EDGE_OK" != true ]]; then

  warn "deployment completed with one or more health warnings"

else

  ok "all health checks passed"
fi

exit 0
