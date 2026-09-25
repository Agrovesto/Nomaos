#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# deploy.sh — containerised blue/green deploy for a NomaOS app on EC2
#
#   deploy.sh --app nomaos-api --image <ecr-ref>:<sha> [--commit <sha>]
#
# Replaces deploy-prod.sh / deploy-staging.sh. Same idea as before — bring the
# idle slot up, prove it healthy, flip the edge, retire the old slot — but the
# unit of deployment is now an immutable image instead of an rsync'd folder.
#
#   1. Resolve active -> target slot from ${NOMAOS_STATE_DIR}/state
#   2. Pull the image (fail before touching anything if the tag is bad)
#   3. Start the target slot alongside the live one
#   4. Gate on the container healthcheck, then on a direct HTTP probe
#   5. Rewrite the nginx upstream + reload  <- the actual traffic flip
#   6. Verify through the edge, then stop the old slot
#
# Any failure before step 5 leaves production untouched and tears the
# candidate down. A failure after step 5 rolls the upstream back.
#
# Exit codes: 0 deployed · 1 failed (prod untouched or rolled back) · 2 usage
# ─────────────────────────────────────────────────────────────────────────────
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib.sh
source "$SCRIPT_DIR/lib.sh"

APP_NAME=""
IMAGE=""
COMMIT_SHA=""
HEALTH_TIMEOUT="${HEALTH_TIMEOUT:-180}"
KEEP_OLD="${KEEP_OLD:-false}"
DRY_RUN=false

usage() {
  cat <<'EOF'
Usage: deploy.sh --app <name> --image <ref> [options]

Required:
  --app    <name>    App key: nomaos-api | nomaos-app2
                     (must match ${NOMAOS_STATE_DIR}/env/<name>.env)
  --image  <ref>     Full image reference including tag, e.g.
                     1234.dkr.ecr.eu-west-1.amazonaws.com/nomaos:abc123

Options:
  --commit <sha>     Commit SHA recorded on the container + in state
  --timeout <sec>    Health gate timeout per stage (default 180)
  --keep-old         Leave the previous slot running (manual rollback standby)
  --dry-run          Print the plan and exit
  -h, --help         This message
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --app)     APP_NAME="${2:?}"; shift 2 ;;
    --image)   IMAGE="${2:?}"; shift 2 ;;
    --commit)  COMMIT_SHA="${2:?}"; shift 2 ;;
    --timeout) HEALTH_TIMEOUT="${2:?}"; shift 2 ;;
    --keep-old) KEEP_OLD=true; shift ;;
    --dry-run) DRY_RUN=true; shift ;;
    -h|--help) usage; exit 0 ;;
    *) usage >&2; die "unknown argument: $1" ;;
  esac
done

[[ -n "$APP_NAME" ]] || { usage >&2; exit 2; }
[[ -n "$IMAGE"    ]] || { usage >&2; exit 2; }
[[ "$IMAGE" == *:* ]] || die "--image must include an explicit tag (never deploy :latest)"

require_docker
load_app_env "$APP_NAME"

# APP_PORT / UPSTREAM_NAME come from the app env file; fall back for the API.
APP_PORT="${APP_PORT:-5000}"
UPSTREAM_NAME="${UPSTREAM_NAME:-${APP_NAME//-/_}}"
UPSTREAM_DIR="$NOMAOS_STATE_DIR/nginx/upstreams"
UPSTREAM_FILE="$UPSTREAM_DIR/${UPSTREAM_NAME}.conf"
NGINX_CONTAINER="${NGINX_CONTAINER:-nomaos-nginx}"

ACTIVE="$(get_active_slot "$APP_NAME")"
TARGET="$(other_slot "$ACTIVE")"
ACTIVE_CONTAINER="${APP_NAME}-${ACTIVE}"
TARGET_CONTAINER="${APP_NAME}-${TARGET}"
COMMIT_SHA="${COMMIT_SHA:-unknown}"

cat <<EOF

  app        : $APP_NAME
  image      : $IMAGE
  commit     : $COMMIT_SHA
  live slot  : $ACTIVE  ($ACTIVE_CONTAINER, $(container_state "$ACTIVE_CONTAINER"))
  target slot: $TARGET  ($TARGET_CONTAINER)
  upstream   : $UPSTREAM_FILE

EOF

if [[ "$DRY_RUN" == true ]]; then
  info "dry run — nothing changed"
  exit 0
fi

mkdir -p "$UPSTREAM_DIR" "$NOMAOS_STATE_DIR/logs/$APP_NAME/$TARGET"

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

# ── Rollback / cleanup on any unexpected failure ─────────────────────────────
FLIPPED=false
UPSTREAM_BACKUP=""

cleanup_on_failure() {
  local rc=$?
  (( rc == 0 )) && return 0

  warn "deployment failed (exit $rc) — cleaning up"

  if [[ "$FLIPPED" == true && -n "$UPSTREAM_BACKUP" && -r "$UPSTREAM_BACKUP" ]]; then
    warn "restoring previous upstream (traffic back to $ACTIVE)"
    cp "$UPSTREAM_BACKUP" "$UPSTREAM_FILE"
    reload_nginx || warn "nginx reload failed during rollback — CHECK THE EDGE NOW"
    set_active_slot "$APP_NAME" "$ACTIVE"
  fi

  warn "tearing down candidate slot $TARGET"
  compose_target down --remove-orphans >/dev/null 2>&1 || true

  warn "live slot remains: $ACTIVE"
  exit "$rc"
}
trap cleanup_on_failure EXIT

reload_nginx() {
  # Validate before reloading — a bad config must never reach a live edge.
  if ! docker exec "$NGINX_CONTAINER" nginx -t >/dev/null 2>&1; then
    warn "nginx -t failed:"
    docker exec "$NGINX_CONTAINER" nginx -t 2>&1 | sed 's/^/    /' >&2 || true
    return 1
  fi
  docker exec "$NGINX_CONTAINER" nginx -s reload
}

write_upstream() {
  # Only the serving slot is listed. nginx resolves upstream names at
  # config-load time, so naming a container that does not exist yet would make
  # the reload fail — which is exactly why the idle slot is never in this file.
  local slot="$1" tmp
  tmp="$(mktemp)"
  cat > "$tmp" <<EOF
# Generated by deploy.sh — $(date -u +%Y-%m-%dT%H:%M:%SZ)
# app=$APP_NAME slot=$slot commit=$COMMIT_SHA
upstream ${UPSTREAM_NAME} {
    server ${APP_NAME}-${slot}:${APP_PORT} max_fails=2 fail_timeout=5s;
    keepalive 32;
}
EOF
  # Same-filesystem mv => atomic replace; nginx never reads a partial file.
  mv "$tmp" "$UPSTREAM_FILE"
  chmod 644 "$UPSTREAM_FILE"
}

# ── 1. Pull first — fail before touching the live slot ───────────────────────
info "pulling $IMAGE"
docker pull --quiet "$IMAGE" >/dev/null || die "could not pull $IMAGE (bad tag, or ECR login expired)"
ok "image present locally"

# ── 2. Start the candidate slot next to the live one ─────────────────────────
info "starting $TARGET slot"
# Remove any stale container from a previously failed run.
compose_target down --remove-orphans >/dev/null 2>&1 || true
compose_target up -d --no-build

# ── 3. Health gate: container healthcheck, then a direct HTTP probe ──────────
info "waiting for $TARGET_CONTAINER to report healthy"
if ! wait_for_health "$TARGET_CONTAINER" "$HEALTH_TIMEOUT"; then
  warn "last 60 log lines from $TARGET_CONTAINER:"
  docker logs --tail 60 "$TARGET_CONTAINER" 2>&1 | sed 's/^/    /' >&2 || true
  die "candidate slot never became healthy — production still on $ACTIVE"
fi

# Independent probe from the nginx container: proves the edge network path
# works, not just the container's view of itself. This is the check that
# catches "healthy but unreachable" wiring mistakes.
info "probing $TARGET_CONTAINER across the edge network"
probe_ok=false
for attempt in $(seq 1 10); do
  if docker exec "$NGINX_CONTAINER" \
       wget -q -O /dev/null --timeout=5 \
       "http://${TARGET_CONTAINER}:${APP_PORT}/health/ready" 2>/dev/null; then
    probe_ok=true
    break
  fi
  log "   probe attempt $attempt/10 failed, retrying"
  sleep 3
done
[[ "$probe_ok" == true ]] || die "nginx cannot reach $TARGET_CONTAINER — production still on $ACTIVE"
ok "candidate reachable from the edge"

# ── 4. Flip traffic ──────────────────────────────────────────────────────────
info "flipping traffic to $TARGET"
if [[ -r "$UPSTREAM_FILE" ]]; then
  UPSTREAM_BACKUP="$(mktemp)"
  cp "$UPSTREAM_FILE" "$UPSTREAM_BACKUP"
fi

write_upstream "$TARGET"
FLIPPED=true
reload_nginx || die "nginx reload failed — upstream rolled back by trap"
set_active_slot "$APP_NAME" "$TARGET"
ok "edge now serving $TARGET"

# ── 5. Post-flip verification through the edge ───────────────────────────────
info "verifying through the edge"
edge_ok=false
for attempt in $(seq 1 5); do
  if docker exec "$NGINX_CONTAINER" \
       wget -q -O /dev/null --timeout=5 \
       "http://127.0.0.1/health" --header="Host: ${EDGE_HEALTH_HOST:-localhost}" 2>/dev/null; then
    edge_ok=true
    break
  fi
  sleep 2
done
if [[ "$edge_ok" == true ]]; then
  ok "edge health check passed"
else
  # Non-fatal: /health includes third-party checks (SendGrid, Termii, Prembly,
  # S3) that can be degraded while our app is perfectly fine. The authoritative
  # gate was /health/ready above.
  warn "edge /health did not return 200 — likely a degraded third-party check."
  warn "inspect: docker exec $NGINX_CONTAINER wget -qO- http://${TARGET_CONTAINER}:${APP_PORT}/health"
fi

# ── 6. Retire the old slot ───────────────────────────────────────────────────
trap - EXIT

if [[ "$KEEP_OLD" == true ]]; then
  info "--keep-old: leaving $ACTIVE running for instant rollback"
elif [[ "$(container_state "$ACTIVE_CONTAINER")" != "absent" ]]; then
  info "draining and stopping old slot $ACTIVE"
  # Give in-flight requests time to finish; compose honours stop_grace_period,
  # and .NET's SIGTERM handler drains Kestrel before exiting.
  sleep "${DRAIN_SECONDS:-5}"
  IMAGE="$IMAGE" APP_NAME="$APP_NAME" SLOT="$ACTIVE" APP_PORT="$APP_PORT" \
  NOMAOS_STATE_DIR="$NOMAOS_STATE_DIR" \
  docker compose -p "${APP_NAME}-${ACTIVE}" \
    -f "$COMPOSE_DIR/docker-compose.app.yml" down --remove-orphans || \
    warn "could not cleanly stop $ACTIVE_CONTAINER"
  ok "old slot stopped"
fi

info "pruning dangling images"
docker image prune -f >/dev/null 2>&1 || true

cat <<EOF

${C_GREEN}Deployment successful${C_RESET}
  app    : $APP_NAME
  slot   : $ACTIVE -> $TARGET
  image  : $IMAGE
  commit : $COMMIT_SHA

  rollback:  deploy/scripts/rollback.sh --app $APP_NAME
  logs    :  docker logs -f $TARGET_CONTAINER

EOF
