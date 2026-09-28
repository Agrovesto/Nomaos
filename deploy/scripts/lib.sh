#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# Shared helpers for the NomaOS Docker deployment scripts.
# Sourced, never executed directly.
# ─────────────────────────────────────────────────────────────────────────────

# Layout on the EC2 host. Everything mutable lives under NOMAOS_STATE_DIR so the
# checkout stays read-only and disposable.
#
#   /opt/nomaos/
#     ├── env/                  root:root 0600 — Vault AppRole + DB creds per app
#     │     ├── nomaos-api.env
#     │     └── nomaos-api.extra.env      (optional, non-secret overrides)
#     ├── nginx/
#     │     ├── upstreams/      rewritten on every flip
#     │     └── certs/
#     ├── state/                <app>.active-slot  (replaces /var/www/Nomaos/active-slot)
#     └── logs/
export NOMAOS_STATE_DIR="${NOMAOS_STATE_DIR:-/opt/nomaos}"

COMPOSE_DIR="${COMPOSE_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../compose" && pwd)}"
export COMPOSE_DIR

# ── Output ───────────────────────────────────────────────────────────────────
if [[ -t 1 ]]; then
  C_RESET=$'\033[0m'; C_RED=$'\033[31m'; C_GREEN=$'\033[32m'
  C_YELLOW=$'\033[33m'; C_BLUE=$'\033[34m'; C_DIM=$'\033[2m'
else
  C_RESET=''; C_RED=''; C_GREEN=''; C_YELLOW=''; C_BLUE=''; C_DIM=''
fi

log()  { printf '%s[%s]%s %s\n' "$C_DIM" "$(date -u +%H:%M:%S)" "$C_RESET" "$*"; }
info() { printf '%s==>%s %s\n' "$C_BLUE" "$C_RESET" "$*"; }
ok()   { printf '%s OK %s %s\n' "$C_GREEN" "$C_RESET" "$*"; }
warn() { printf '%s WARN%s %s\n' "$C_YELLOW" "$C_RESET" "$*" >&2; }
die()  { printf '%sFAIL%s %s\n' "$C_RED" "$C_RESET" "$*" >&2; exit 1; }

# ── Slot helpers ─────────────────────────────────────────────────────────────
# The active slot is a file, not a guess. If it is missing we assume "green" is
# live so that the first containerised deploy targets "blue" and we never
# collide with a name that might already exist.
active_slot_file() { printf '%s/state/%s.active-slot' "$NOMAOS_STATE_DIR" "$1"; }

get_active_slot() {
  local f; f="$(active_slot_file "$1")"
  if [[ -r "$f" ]]; then
    tr -d '[:space:]' < "$f"
  else
    printf 'green'
  fi
}

other_slot() { [[ "$1" == "blue" ]] && printf 'green' || printf 'blue'; }

set_active_slot() {
  local app="$1" slot="$2" f
  f="$(active_slot_file "$app")"
  mkdir -p "$(dirname "$f")"
  printf '%s\n' "$slot" > "$f"
}

# ── Docker helpers ───────────────────────────────────────────────────────────
require_docker() {
  command -v docker >/dev/null 2>&1 || die "docker is not installed. Run deploy/scripts/bootstrap.sh first."
  docker compose version >/dev/null 2>&1 || die "docker compose v2 plugin missing. Run deploy/scripts/bootstrap.sh first."
  docker info >/dev/null 2>&1 || die "cannot talk to the Docker daemon (permission or daemon down)."
}

container_state() {
  docker inspect -f '{{.State.Status}}' "$1" 2>/dev/null || printf 'absent'
}

container_health() {
  # "none" when the image declares no HEALTHCHECK.
  docker inspect -f '{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' "$1" 2>/dev/null \
    || printf 'absent'
}

# Wait until a container reports healthy. Fails fast if it dies or restarts.
# wait_for_health <container> <timeout_seconds>
wait_for_health() {
  local name="$1" timeout="${2:-180}" waited=0 state health
  while (( waited < timeout )); do
    state="$(container_state "$name")"
    health="$(container_health "$name")"

    case "$state" in
      running)
        case "$health" in
          healthy) ok "$name is healthy (${waited}s)"; return 0 ;;
          none)    ok "$name is running (no healthcheck declared)"; return 0 ;;
        esac
        ;;
      exited|dead)
        warn "$name exited while starting up"
        return 1
        ;;
      absent)
        warn "$name does not exist"
        return 1
        ;;
    esac

    sleep 3
    waited=$(( waited + 3 ))
    (( waited % 15 == 0 )) && log "   waiting on $name ... ${waited}s (state=$state health=$health)"
  done

  warn "$name did not become healthy within ${timeout}s"
  return 1
}

# ── Env file loading ─────────────────────────────────────────────────────────
# Secrets live in root-owned 0600 files on the host, never in the repo and never
# in `docker run` argv (which is world-readable via /proc and `docker inspect`).
load_app_env() {
  local app="$1" f="$NOMAOS_STATE_DIR/env/$1.env"
  [[ -r "$f" ]] || die "missing env file $f — copy deploy/env/app.env.example and fill it in (chmod 600)."

  local perms
  perms="$(stat -c '%a' "$f" 2>/dev/null || stat -f '%A' "$f" 2>/dev/null || echo '')"
  if [[ -n "$perms" && "$perms" != "600" && "$perms" != "400" ]]; then
    warn "$f is mode $perms — it holds Vault credentials, tighten it: sudo chmod 600 $f"
  fi

  # Deliberately NOT `source`. These files hold SQL Server connection strings
  # full of spaces and semicolons; sourcing an unquoted one makes the shell try
  # to execute "Initial Catalog=..." as a command. Parse line by line instead
  # and treat everything after the first '=' as a literal value.
  local line key val lineno=0
  while IFS= read -r line || [[ -n "$line" ]]; do
    lineno=$(( lineno + 1 ))

    # Skip blanks and comments.
    [[ -z "${line//[[:space:]]/}" ]] && continue
    [[ "${line#"${line%%[![:space:]]*}"}" == \#* ]] && continue

    # Tolerate `export FOO=bar`.
    line="${line#export }"

    if [[ "$line" != *=* ]]; then
      warn "$f:$lineno — ignoring malformed line (no '=')"
      continue
    fi

    key="${line%%=*}"
    val="${line#*=}"

    # Trim surrounding whitespace from the key only; values are taken verbatim.
    key="${key//[[:space:]]/}"
    [[ "$key" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]] || {
      warn "$f:$lineno — ignoring invalid variable name '$key'"
      continue
    }

    # Strip one matching pair of surrounding quotes, if present.
    if [[ ${#val} -ge 2 && "$val" == \"*\" ]]; then
      val="${val:1:${#val}-2}"
    elif [[ ${#val} -ge 2 && "$val" == \'*\' ]]; then
      val="${val:1:${#val}-2}"
    fi

    printf -v "$key" '%s' "$val"
    export "${key?}"
  done < "$f"
}
