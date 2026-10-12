#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# Shared helpers for the Nomaos Docker deployment scripts.
# Sourced, never executed directly.
# ─────────────────────────────────────────────────────────────────────────────

export NOMAOS_STATE_DIR="${NOMAOS_STATE_DIR:-/opt/nomaos}"

COMPOSE_DIR="${COMPOSE_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../compose" && pwd)}"
export COMPOSE_DIR

# ─────────────────────────────────────────────────────────────────────────────
# Output
# ─────────────────────────────────────────────────────────────────────────────

log() {
  printf '[%s] %s\n' "$(date -u +%H:%M:%S)" "$*"
}

info() {
  printf '==> %s\n' "$*"
}

ok() {
  printf ' OK  %s\n' "$*"
}

warn() {
  printf 'WARN  %s\n' "$*" >&2
}

die() {
  printf 'FAIL  %s\n' "$*" >&2
  exit 1
}

# ─────────────────────────────────────────────────────────────────────────────
# Slot helpers
# ─────────────────────────────────────────────────────────────────────────────

active_slot_file() {
  printf '%s/state/%s.active-slot' "$NOMAOS_STATE_DIR" "$1"
}

get_active_slot() {
  local f

  f="$(active_slot_file "$1")"

  if [[ -r "$f" ]]; then
    tr -d '[:space:]' < "$f"
  else
    printf 'green\n'
  fi
}

other_slot() {
  case "$1" in
    blue)
      printf 'green\n'
      ;;
    green)
      printf 'blue\n'
      ;;
    *)
      die "invalid slot '$1'; expected blue or green"
      ;;
  esac
}

set_active_slot() {
  local app="$1"
  local slot="$2"
  local f

  case "$slot" in
    blue|green)
      ;;
    *)
      die "invalid slot '$slot'; expected blue or green"
      ;;
  esac

  f="$(active_slot_file "$app")"

  mkdir -p "$(dirname "$f")"
  printf '%s\n' "$slot" > "$f"
}

# ─────────────────────────────────────────────────────────────────────────────
# Docker helpers
# ─────────────────────────────────────────────────────────────────────────────

require_docker() {
  command -v docker >/dev/null 2>&1 ||
    die "docker is not installed."

  docker compose version >/dev/null 2>&1 ||
    die "docker compose v2 plugin is missing."

  docker info >/dev/null 2>&1 ||
    die "cannot communicate with Docker daemon."
}

container_state() {
  docker inspect \
    -f '{{.State.Status}}' \
    "$1" 2>/dev/null || printf 'absent\n'
}

container_health() {
  docker inspect \
    -f '{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' \
    "$1" 2>/dev/null || printf 'absent\n'
}

wait_for_health() {
  local name="$1"
  local timeout="${2:-180}"
  local waited=0
  local state
  local health

  while (( waited < timeout )); do
    state="$(container_state "$name")"
    health="$(container_health "$name")"

    case "$state" in
      running)
        case "$health" in
          healthy)
            ok "$name is healthy (${waited}s)"
            return 0
            ;;
          none)
            ok "$name is running with no healthcheck"
            return 0
            ;;
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
    waited=$((waited + 3))

    if (( waited % 15 == 0 )); then
      log "waiting on $name ... ${waited}s (state=$state health=$health)"
    fi
  done

  warn "$name did not become healthy within ${timeout}s"
  return 1
}

# ─────────────────────────────────────────────────────────────────────────────
# JSON environment loader
# ─────────────────────────────────────────────────────────────────────────────

load_app_json_env() {
  local file="$1"
  local pairs
  local key
  local value

  command -v python3 >/dev/null 2>&1 ||
    die "python3 is required to read JSON environment files."

  pairs="$(mktemp)" ||
    die "could not create temporary environment file."

  chmod 600 "$pairs"

  if ! python3 - "$file" > "$pairs" <<'PYJSON'
import json
import re
import sys

path = sys.argv[1]

try:
    with open(path, encoding="utf-8") as stream:
        values = json.load(stream)
except (OSError, json.JSONDecodeError) as exc:
    print(
        f"could not parse JSON environment file {path}: {exc}",
        file=sys.stderr
    )
    sys.exit(1)

if not isinstance(values, dict) or not values:
    print(
        "JSON environment must be a non-empty top-level object.",
        file=sys.stderr
    )
    sys.exit(1)

for key, value in values.items():
    if not isinstance(key, str) or not re.fullmatch(
        r"[A-Za-z_][A-Za-z0-9_]*",
        key
    ):
        print(
            f"invalid environment variable name: {key}",
            file=sys.stderr
        )
        sys.exit(1)

    if value is None:
        text = ""
    elif isinstance(value, bool):
        text = "true" if value else "false"
    elif isinstance(value, (str, int, float)):
        text = str(value)
    else:
        print(
            f"JSON environment value for {key} must be scalar.",
            file=sys.stderr
        )
        sys.exit(1)

    if "\x00" in text:
        print(
            f"JSON environment value for {key} contains a NUL character.",
            file=sys.stderr
        )
        sys.exit(1)

    sys.stdout.buffer.write(
        key.encode("utf-8") +
        b"\0" +
        text.encode("utf-8") +
        b"\0"
    )
PYJSON
  then
    rm -f "$pairs"
    die "invalid JSON environment file: $file"
  fi

  while IFS= read -r -d '' key &&
        IFS= read -r -d '' value; do
    printf -v "$key" '%s' "$value"
    export "$key"
  done < "$pairs"

  rm -f "$pairs"
}

# ─────────────────────────────────────────────────────────────────────────────
# .env loader
# ─────────────────────────────────────────────────────────────────────────────

load_app_dotenv() {
  local file="$1"
  local line
  local key
  local val
  local lineno=0

  while IFS= read -r line || [[ -n "$line" ]]; do
    lineno=$((lineno + 1))

    # Skip blank lines.
    [[ -z "${line//[[:space:]]/}" ]] && continue

    # Skip comments after leading whitespace.
    [[ "${line#"${line%%[![:space:]]*}"}" == \#* ]] && continue

    # Tolerate "export KEY=value".
    if [[ "$line" == export\ * ]]; then
      line="${line#export }"
    fi

    if [[ "$line" != *=* ]]; then
      warn "$file:$lineno — ignoring malformed line"
      continue
    fi

    # Preserve everything after the first "=".
    key="${line%%=*}"
    val="${line#*=}"

    # Remove whitespace from key only.
    key="${key//[[:space:]]/}"

    if [[ ! "$key" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]]; then
      die "$file:$lineno has invalid environment variable name: $key"
    fi

    # Strip one matching pair of surrounding double quotes.
    if [[ ${#val} -ge 2 &&
          "${val:0:1}" == '"' &&
          "${val: -1}" == '"' ]]; then
      val="${val:1:${#val}-2}"

    # Strip one matching pair of surrounding single quotes.
    elif [[ ${#val} -ge 2 &&
            "${val:0:1}" == "'" &&
            "${val: -1}" == "'" ]]; then
      val="${val:1:${#val}-2}"
    fi

    printf -v "$key" '%s' "$val"
    export "$key"
  done < "$file"
}

# ─────────────────────────────────────────────────────────────────────────────
# Application environment loader
#
# .env is authoritative.
# .json remains supported as a legacy fallback.
# ─────────────────────────────────────────────────────────────────────────────

load_app_env() {
  local app="$1"
  local dotenv_file="$NOMAOS_STATE_DIR/env/$app.env"
  local json_file="$NOMAOS_STATE_DIR/env/$app.json"
  local file
  local perms

  # Prefer .env.
  if [[ -f "$dotenv_file" ]]; then
    file="$dotenv_file"

    if [[ -f "$json_file" ]]; then
      warn "both $dotenv_file and $json_file exist; using $dotenv_file"
    fi

    [[ -r "$file" ]] ||
      die "environment config is not readable: $file"

    perms="$(
      stat -c '%a' "$file" 2>/dev/null ||
      stat -f '%A' "$file" 2>/dev/null ||
      printf ''
    )"

    if [[ -n "$perms" &&
          "$perms" != "600" &&
          "$perms" != "400" ]]; then
      warn "$file is mode $perms; recommended mode is 600"
    fi

    load_app_dotenv "$file"
    return 0
  fi

  # Fall back to JSON.
  if [[ -f "$json_file" ]]; then
    file="$json_file"

    warn "using legacy JSON environment file: $file"

    [[ -r "$file" ]] ||
      die "environment config is not readable: $file"

    load_app_json_env "$file"
    return 0
  fi

  die "missing environment config; expected $dotenv_file or $json_file"
}

# ─────────────────────────────────────────────────────────────────────────────
# Validate required application runtime variables
# ─────────────────────────────────────────────────────────────────────────────

require_app_runtime_env() {
  local missing=()

  [[ -n "${ASPNETCORE_ENVIRONMENT:-}" ]] ||
    missing+=("ASPNETCORE_ENVIRONMENT")

  [[ -n "${VAULT_ADDR:-}" ]] ||
    missing+=("VAULT_ADDR")

  [[ -n "${VAULT_ROLE_ID:-}" ]] ||
    missing+=("VAULT_ROLE_ID")

  [[ -n "${VAULT_SECRET_ID:-}" ]] ||
    missing+=("VAULT_SECRET_ID")

  [[ -n "${VAULT_SECRET_PATH:-}" ]] ||
    missing+=("VAULT_SECRET_PATH")

  [[ -n "${DB_CONNECTION_STRING:-}" ]] ||
    missing+=("DB_CONNECTION_STRING")

  if (( ${#missing[@]} > 0 )); then
    printf 'Missing required application runtime variables:\n' >&2

    local name

    for name in "${missing[@]}"; do
      printf '  - %s\n' "$name" >&2
    done

    exit 1
  fi
}

# ─────────────────────────────────────────────────────────────────────────────
# Safe runtime summary
# ─────────────────────────────────────────────────────────────────────────────

show_app_runtime_summary() {
  echo ""
  echo "---------------------------------------------------------------"
  echo " Application runtime configuration"
  echo "---------------------------------------------------------------"

  printf ' ASPNETCORE_ENVIRONMENT : %s\n' \
    "${ASPNETCORE_ENVIRONMENT:-<MISSING>}"

  printf ' VAULT_ADDR             : %s\n' \
    "${VAULT_ADDR:-<MISSING>}"

  printf ' VAULT_KV_MOUNT         : %s\n' \
    "${VAULT_KV_MOUNT:-kv}"

  printf ' VAULT_SECRET_PATH      : %s\n' \
    "${VAULT_SECRET_PATH:-<MISSING>}"

  if [[ -n "${VAULT_ROLE_ID:-}" ]]; then
    echo " VAULT_ROLE_ID          : <SET>"
  else
    echo " VAULT_ROLE_ID          : <MISSING>"
  fi

  if [[ -n "${VAULT_SECRET_ID:-}" ]]; then
    echo " VAULT_SECRET_ID        : <SET>"
  else
    echo " VAULT_SECRET_ID        : <MISSING>"
  fi

  if [[ -n "${DB_CONNECTION_STRING:-}" ]]; then
    echo " DB_CONNECTION_STRING   : <SET>"
  else
    echo " DB_CONNECTION_STRING   : <MISSING>"
  fi

  printf ' REDIS_CONNECTION       : %s\n' \
    "${REDIS_CONNECTION_STRING:-redis:6379}"

  echo "---------------------------------------------------------------"
  echo ""
}