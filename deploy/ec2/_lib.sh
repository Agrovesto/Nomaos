#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# Shared helpers for the NomaOS EC2 + Compose scripts. Sourced, never executed.
# ─────────────────────────────────────────────────────────────────────────────

# Host layout. Everything mutable lives here so the checkout stays disposable.
export NOMAOS_DIR="${NOMAOS_DIR:-/opt/nomaos}"
export ENV_FILE="${ENV_FILE:-$NOMAOS_DIR/.env}"
export ENV_API_FILE="${ENV_API_FILE:-$NOMAOS_DIR/.env.api}"
export ENV_OTEL_FILE="${ENV_OTEL_FILE:-$NOMAOS_DIR/.env.otel}"
export PREVIOUS_TAG_FILE="${PREVIOUS_TAG_FILE:-$NOMAOS_DIR/PREVIOUS_IMAGE_TAG}"
export CURRENT_TAG_FILE="${CURRENT_TAG_FILE:-$NOMAOS_DIR/CURRENT_IMAGE_TAG}"

if [[ -t 1 ]]; then
  C_RESET=$'\033[0m'; C_RED=$'\033[31m'; C_GREEN=$'\033[32m'
  C_YELLOW=$'\033[33m'; C_BLUE=$'\033[34m'; C_DIM=$'\033[2m'
else
  C_RESET=''; C_RED=''; C_GREEN=''; C_YELLOW=''; C_BLUE=''; C_DIM=''
fi

log()  { printf '%s[%s]%s %s\n' "$C_DIM" "$(date -u +%H:%M:%S)" "$C_RESET" "$*"; }
info() { printf '%s==>%s %s\n' "$C_BLUE" "$C_RESET" "$*"; }
ok()   { printf '%s OK %s %s\n' "$C_GREEN" "$C_RESET" "$*"; }
warn() { printf '%sWARN%s %s\n' "$C_YELLOW" "$C_RESET" "$*" >&2; }
die()  { printf '%sFAIL%s %s\n' "$C_RED" "$C_RESET" "$*" >&2; exit 1; }

need_cmd() {
  command -v "$1" >/dev/null 2>&1 || die "required command not found: $1"
}

# Remove anything that looks like a token/password before printing an error.
scrub() {
  printf '%s' "$1" | sed -E \
    -e 's/(hvs\.[A-Za-z0-9._-]{4})[A-Za-z0-9._-]+/\1***/g' \
    -e 's/("client_token"[[:space:]]*:[[:space:]]*")[^"]+/\1***/g' \
    -e 's/([Pp]assword=)[^;",[:space:]]+/\1***/g' \
    -e 's/(secret_id"?[[:space:]]*[:=][[:space:]]*"?)[^",[:space:]]+/\1***/g'
}

require_mode_600() {
  local f="$1" mode
  mode="$(stat -c '%a' "$f" 2>/dev/null || stat -f '%OLp' "$f" 2>/dev/null || echo '')"
  case "$mode" in
    600|400) : ;;
    '') warn "could not stat $f to verify permissions" ;;
    *)  die "$f has mode $mode; expected 600 (chmod 600 $f)" ;;
  esac
}

require_docker() {
  need_cmd docker
  docker compose version >/dev/null 2>&1 || die "docker compose v2 plugin is missing."
  docker info >/dev/null 2>&1 || die "cannot talk to the Docker daemon (permission or daemon down)."
}

# ── Compose invocation ───────────────────────────────────────────────────────
# Resolves the compose file list once so every caller (deploy, rollback,
# status) uses an identical command.
compose_files() {
  local base="$NOMAOS_DIR/docker-compose.yml"
  local prod="$NOMAOS_DIR/docker-compose.prod.yml"
  [[ -r "$base" ]] || die "missing $base — copy deploy/ec2/docker-compose.yml onto the host."

  local args=(-f "$base")
  if [[ "${COMPOSE_OVERLAY:-auto}" != "none" && -r "$prod" ]]; then
    args+=(-f "$prod")
  fi
  printf '%s\n' "${args[@]}"
}

dc() {
  local -a files
  mapfile -t files < <(compose_files)
  ( cd "$NOMAOS_DIR" && docker compose --env-file "$ENV_FILE" "${files[@]}" "$@" )
}

# ── Env file helpers ─────────────────────────────────────────────────────────
# Reads KEY from an env file without sourcing it (values may contain spaces,
# semicolons and quotes — sourcing would execute them).
env_get() {
  local key="$1" file="${2:-$ENV_FILE}"
  [[ -r "$file" ]] || return 0
  awk -v k="$key" -F= '
    $0 ~ "^"k"=" { sub("^"k"=", ""); print; exit }
  ' "$file"
}

env_set() {
  local key="$1" value="$2" file="${3:-$ENV_FILE}"
  touch "$file"
  local tmp
  tmp="$(mktemp "${file}.XXXXXX")"
  # Preserve the original permissions of the target file.
  chmod --reference="$file" "$tmp" 2>/dev/null || chmod 644 "$tmp"
  awk -v k="$key" -F= '$0 !~ "^"k"=" { print }' "$file" > "$tmp"
  printf '%s=%s\n' "$key" "$value" >> "$tmp"
  mv -f "$tmp" "$file"
}

# ── Health gating ────────────────────────────────────────────────────────────
# poll_health <url> <timeout_seconds> [<interval>]
# Returns 0 on the first 2xx, 1 on timeout. Never trusts a single probe.
poll_health() {
  local url="$1" timeout="${2:-120}" interval="${3:-3}" waited=0 code

  while (( waited < timeout )); do
    code="$(curl -s -o /dev/null -w '%{http_code}' --max-time 5 "$url" 2>/dev/null || echo 000)"
    if [[ "$code" =~ ^2[0-9][0-9]$ ]]; then
      ok "$url -> $code after ${waited}s"
      return 0
    fi
    sleep "$interval"
    waited=$(( waited + interval ))
    (( waited % 15 == 0 )) && log "   waiting on $url ... ${waited}s (last=$code)"
  done

  warn "$url did not return 2xx within ${timeout}s (last=${code:-none})"
  return 1
}

# Dump everything an on-call engineer needs when the gate fails.
dump_failure_context() {
  local service="$1"
  warn "── container state ───────────────────────────────────────────────"
  dc ps || true
  warn "── last 200 log lines ────────────────────────────────────────────"
  dc logs --tail 200 --no-color "$service" || true
  warn "── docker healthcheck output ─────────────────────────────────────"
  local cid
  cid="$(dc ps -q "$service" 2>/dev/null | head -1)"
  if [[ -n "$cid" ]]; then
    docker inspect -f '{{if .State.Health}}{{range .State.Health.Log}}{{.Output}}{{end}}{{else}}no healthcheck{{end}}' "$cid" || true
  fi
}
