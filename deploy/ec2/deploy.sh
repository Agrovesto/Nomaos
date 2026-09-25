#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# deploy.sh — pull an ECR image and roll it out with Docker Compose on EC2
#
#   login ECR -> render Vault env -> compose pull -> compose up -d
#             -> poll /health/live -> poll /health/ready
#             -> on failure: dump logs, roll back to the previous tag, exit 1
#
# There is no ECS in this path: no task definitions, no update-service, no
# `aws ecs wait`. The unit of deployment is an immutable image tag and the
# unit of rollout is `docker compose up -d --force-recreate`.
#
# Usage:
#   deploy.sh --tag sha-abc1234 [--service api] [--timeout 180] [--no-rollback]
#   IMAGE_TAG=dev deploy.sh
#
# Exit codes: 0 deployed · 1 failed and rolled back (or usage error)
#             2 failed AND the rollback also failed — the service is down, escalate
# ─────────────────────────────────────────────────────────────────────────────
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=_lib.sh
source "$SCRIPT_DIR/_lib.sh"

SERVICE="${COMPOSE_SERVICE:-api}"
IMAGE_TAG="${IMAGE_TAG:-}"
HEALTH_TIMEOUT="${HEALTH_TIMEOUT:-180}"
ROLLBACK_ON_FAILURE=true
SKIP_VAULT_RENDER="${SKIP_VAULT_RENDER:-false}"

usage() {
  cat <<'EOF'
Usage: deploy.sh [--tag <image-tag>] [options]

Options:
  --tag <tag>        Image tag to deploy (default: $IMAGE_TAG, then /opt/nomaos/.env)
                     Prefer the immutable sha-<short_sha> tag CI produces.
  --service <name>   Compose service to recreate (default: api)
  --timeout <sec>    Health gate timeout per probe (default: 180)
  --no-rollback      Leave the failed release running for debugging
  --skip-vault       Do not re-render /opt/nomaos/.env.api from Vault
  -h, --help         This message
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --tag)         IMAGE_TAG="${2:?}"; shift 2 ;;
    --service)     SERVICE="${2:?}"; shift 2 ;;
    --timeout)     HEALTH_TIMEOUT="${2:?}"; shift 2 ;;
    --no-rollback) ROLLBACK_ON_FAILURE=false; shift ;;
    --skip-vault)  SKIP_VAULT_RENDER=true; shift ;;
    -h|--help)     usage; exit 0 ;;
    *) usage >&2; die "unknown argument: $1" ;;
  esac
done

require_docker
need_cmd curl

[[ -r "$ENV_FILE" ]] || die "missing $ENV_FILE — copy deploy/ec2/env.example onto the host."

AWS_REGION="$(env_get AWS_REGION)"
ECR_REGISTRY="$(env_get ECR_REGISTRY)"
ECR_REPOSITORY="$(env_get ECR_REPOSITORY)"
API_HOST_PORT="$(env_get API_HOST_PORT)"; API_HOST_PORT="${API_HOST_PORT:-8080}"

[[ -n "$ECR_REGISTRY"   ]] || die "ECR_REGISTRY is not set in $ENV_FILE"
[[ -n "$ECR_REPOSITORY" ]] || die "ECR_REPOSITORY is not set in $ENV_FILE"

PREVIOUS_TAG="$(env_get IMAGE_TAG)"
IMAGE_TAG="${IMAGE_TAG:-$PREVIOUS_TAG}"
IMAGE_TAG="${IMAGE_TAG:-latest}"
IMAGE_URI="${ECR_REGISTRY}/${ECR_REPOSITORY}:${IMAGE_TAG}"

HEALTH_BASE="http://127.0.0.1:${API_HOST_PORT}"

info "deploying $IMAGE_URI (previous: ${PREVIOUS_TAG:-none})"

# ── 1. ECR login ─────────────────────────────────────────────────────────────
# Credentials come from the instance profile; no long-lived keys on the box.
info "logging in to ECR"
need_cmd aws
aws ecr get-login-password --region "${AWS_REGION:-eu-west-1}" \
  | docker login --username AWS --password-stdin "$ECR_REGISTRY" >/dev/null \
  || die "ECR login failed. Check the instance profile's ecr:GetAuthorizationToken permission."
ok "ECR login"

# ── 2. Secrets from Vault ────────────────────────────────────────────────────
if [[ "$SKIP_VAULT_RENDER" == true ]]; then
  warn "skipping Vault render (--skip-vault); using the existing $ENV_API_FILE"
  [[ -r "$ENV_API_FILE" ]] || die "$ENV_API_FILE does not exist and Vault render was skipped."
else
  info "rendering secrets from Vault"
  # Pass the non-secret Vault coordinates from .env through to the renderer.
  VAULT_ADDR="${VAULT_ADDR:-$(env_get VAULT_ADDR)}" \
  VAULT_KV_MOUNT="${VAULT_KV_MOUNT:-$(env_get VAULT_KV_MOUNT)}" \
  VAULT_SECRET_PATH="${VAULT_SECRET_PATH:-$(env_get VAULT_SECRET_PATH)}" \
  VAULT_APPROLE_FILE="${VAULT_APPROLE_FILE:-$(env_get VAULT_APPROLE_FILE)}" \
    "$SCRIPT_DIR/render-secrets-from-vault.sh" --output "$ENV_API_FILE" \
    || die "Vault render failed; not touching the running container."
fi

require_mode_600 "$ENV_API_FILE"
# env_file entries must exist even when empty — compose errors out otherwise.
[[ -e "$ENV_OTEL_FILE" ]] || { warn "$ENV_OTEL_FILE missing; creating an empty one"; : > "$ENV_OTEL_FILE"; }

# ── Rollback helper ──────────────────────────────────────────────────────────
# Defined before first use: bash resolves functions at call time, and the
# compose-up step below already needs it.
rollback_or_fail() {
  local reason="$1"
  warn "deploy failed: $reason"

  if [[ "$ROLLBACK_ON_FAILURE" != true ]]; then
    die "rollback disabled (--no-rollback). The failed release is still running."
  fi

  local previous
  previous="$(head -1 "$PREVIOUS_TAG_FILE" 2>/dev/null || true)"
  if [[ -z "$previous" || "$previous" == "$IMAGE_TAG" ]]; then
    die "no previous image tag recorded in $PREVIOUS_TAG_FILE — manual intervention required."
  fi

  warn "rolling back to $previous"
  env_set IMAGE_TAG "$previous"

  if dc up -d --no-deps --force-recreate "$SERVICE" \
     && poll_health "$HEALTH_BASE/health/live" 120; then
    # Swap the pointers so a subsequent rollback.sh rolls *forward* again.
    printf '%s\n' "$IMAGE_TAG" > "$PREVIOUS_TAG_FILE"
    printf '%s\n' "$previous" > "$CURRENT_TAG_FILE"
    warn "rolled back to $previous — the failed tag was $IMAGE_TAG"
    exit 1
  fi

  warn "ROLLBACK ALSO FAILED. Service is down; escalate."
  dump_failure_context "$SERVICE"
  exit 2
}

# ── 3. Pull ──────────────────────────────────────────────────────────────────
# Pull BEFORE touching the running container: a bad tag must fail while the
# current release is still serving traffic.
info "pulling $IMAGE_URI"
env_set IMAGE_TAG "$IMAGE_TAG"
[[ -n "${GIT_SHA:-}" ]] && env_set GIT_SHA "$GIT_SHA"

dc pull "$SERVICE" || {
  env_set IMAGE_TAG "${PREVIOUS_TAG:-latest}"
  die "docker compose pull failed for $IMAGE_URI — tag missing in ECR?"
}
ok "image pulled"

# Record the rollback pointer only once the new image is known-good to pull.
if [[ -n "$PREVIOUS_TAG" && "$PREVIOUS_TAG" != "$IMAGE_TAG" ]]; then
  printf '%s\n' "$PREVIOUS_TAG" > "$PREVIOUS_TAG_FILE"
fi
printf '%s\n' "$IMAGE_TAG" > "$CURRENT_TAG_FILE"

# ── 4. Recreate ──────────────────────────────────────────────────────────────
info "recreating service '$SERVICE'"
dc up -d --no-deps --force-recreate "$SERVICE" || {
  dump_failure_context "$SERVICE"
  rollback_or_fail "compose up failed"
}

# ── 5. Health gate ───────────────────────────────────────────────────────────
info "waiting for liveness"
poll_health "$HEALTH_BASE/health/live" "$HEALTH_TIMEOUT" || {
  dump_failure_context "$SERVICE"
  rollback_or_fail "/health/live never returned 200"
}

info "waiting for readiness (SQL + Redis + Vault)"
poll_health "$HEALTH_BASE/health/ready" "$HEALTH_TIMEOUT" || {
  dump_failure_context "$SERVICE"
  rollback_or_fail "/health/ready never returned 200 — a dependency (SQL/Redis/Vault) is down"
}

# ── 6. Done ──────────────────────────────────────────────────────────────────
info "deployed build:"
curl -s --max-time 5 "$HEALTH_BASE/status" | (jq '{status, ready, build}' 2>/dev/null || cat) || true

ok "deploy complete: $IMAGE_URI"
ok "rollback pointer: $(cat "$PREVIOUS_TAG_FILE" 2>/dev/null || echo 'none')"
