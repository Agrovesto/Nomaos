#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# rollback.sh — put the previous ECR image back, with the same health gate
#
#   rollback.sh                 # uses /opt/nomaos/PREVIOUS_IMAGE_TAG
#   rollback.sh --tag sha-9f3a1 # explicit target
#   rollback.sh --list          # what can I roll back to?
#
# Compose-level only. No ECS task definitions, no `aws ecs update-service`.
#
# One-liner equivalent, if this script is unavailable:
#   cd /opt/nomaos && sed -i "s/^IMAGE_TAG=.*/IMAGE_TAG=sha-9f3a1/" .env \
#     && docker compose up -d --no-deps --force-recreate api \
#     && curl -fsS http://127.0.0.1:8080/health/ready
# ─────────────────────────────────────────────────────────────────────────────
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=_lib.sh
source "$SCRIPT_DIR/_lib.sh"

SERVICE="${COMPOSE_SERVICE:-api}"
TARGET_TAG=""
HEALTH_TIMEOUT="${HEALTH_TIMEOUT:-180}"
LIST_ONLY=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --tag)     TARGET_TAG="${2:?}"; shift 2 ;;
    --service) SERVICE="${2:?}"; shift 2 ;;
    --timeout) HEALTH_TIMEOUT="${2:?}"; shift 2 ;;
    --list)    LIST_ONLY=true; shift ;;
    -h|--help) sed -n '2,17p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) die "unknown argument: $1" ;;
  esac
done

require_docker
need_cmd curl

ECR_REGISTRY="$(env_get ECR_REGISTRY)"
ECR_REPOSITORY="$(env_get ECR_REPOSITORY)"
CURRENT_TAG="$(env_get IMAGE_TAG)"
API_HOST_PORT="$(env_get API_HOST_PORT)"; API_HOST_PORT="${API_HOST_PORT:-8080}"
HEALTH_BASE="http://127.0.0.1:${API_HOST_PORT}"

if [[ "$LIST_ONLY" == true ]]; then
  echo "current:  ${CURRENT_TAG:-unknown}"
  echo "previous: $(head -1 "$PREVIOUS_TAG_FILE" 2>/dev/null || echo 'none recorded')"
  echo
  echo "locally cached images for ${ECR_REPOSITORY}:"
  docker images --format '  {{.Tag}}\t{{.CreatedSince}}\t{{.Size}}' \
    "${ECR_REGISTRY}/${ECR_REPOSITORY}" 2>/dev/null || true
  exit 0
fi

if [[ -z "$TARGET_TAG" ]]; then
  TARGET_TAG="$(head -1 "$PREVIOUS_TAG_FILE" 2>/dev/null || true)"
fi

[[ -n "$TARGET_TAG" ]] || die "no rollback target: $PREVIOUS_TAG_FILE is empty and --tag was not given."
[[ "$TARGET_TAG" != "$CURRENT_TAG" ]] || die "target tag $TARGET_TAG is already the running tag."

IMAGE_URI="${ECR_REGISTRY}/${ECR_REPOSITORY}:${TARGET_TAG}"
info "rolling back $CURRENT_TAG -> $TARGET_TAG"

# The image is normally still in the local cache, but pull anyway: after an AMI
# refresh or a `docker system prune` it will not be.
if ! docker image inspect "$IMAGE_URI" >/dev/null 2>&1; then
  info "image not cached locally; pulling from ECR"
  need_cmd aws
  AWS_REGION="$(env_get AWS_REGION)"
  aws ecr get-login-password --region "${AWS_REGION:-eu-west-1}" \
    | docker login --username AWS --password-stdin "$ECR_REGISTRY" >/dev/null \
    || die "ECR login failed."
fi

env_set IMAGE_TAG "$TARGET_TAG"

dc pull "$SERVICE" || {
  env_set IMAGE_TAG "$CURRENT_TAG"
  die "could not pull $IMAGE_URI — the tag may have been expired by the ECR lifecycle policy."
}

dc up -d --no-deps --force-recreate "$SERVICE" || {
  dump_failure_context "$SERVICE"
  die "compose up failed during rollback. Service may be down; escalate."
}

if ! poll_health "$HEALTH_BASE/health/live" "$HEALTH_TIMEOUT"; then
  dump_failure_context "$SERVICE"
  die "rollback target $TARGET_TAG is not live either. Escalate."
fi

if ! poll_health "$HEALTH_BASE/health/ready" "$HEALTH_TIMEOUT"; then
  dump_failure_context "$SERVICE"
  warn "rollback target is live but not ready — a shared dependency (SQL/Redis/Vault) is probably the real fault."
  exit 1
fi

# The tag we just left becomes the new rollback pointer (roll forward again).
printf '%s\n' "$CURRENT_TAG" > "$PREVIOUS_TAG_FILE"
printf '%s\n' "$TARGET_TAG"  > "$CURRENT_TAG_FILE"

ok "rolled back to $IMAGE_URI"
