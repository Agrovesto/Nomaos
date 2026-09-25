#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# cutover-from-systemd.sh — retire the old blue/green systemd deployment
#
#   sudo deploy/scripts/cutover-from-systemd.sh          # dry run, default
#   sudo deploy/scripts/cutover-from-systemd.sh --commit # actually do it
#
# Run this ONLY after the Docker stack has served production traffic happily
# for a while. It is the last step, not the first.
#
# What it does:
#   1. Refuses to run unless a containerised slot is live and healthy
#   2. Frees :80/:443 from host nginx so the nginx container can bind them
#   3. Disables the Agrovesto.Backend.API-{blue,green} units
#   4. Leaves everything on disk, disabled not deleted, so you can reverse it
#
# Reversing: deploy/scripts/cutover-from-systemd.sh --undo
# ─────────────────────────────────────────────────────────────────────────────
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib.sh
source "$SCRIPT_DIR/lib.sh"

[[ $EUID -eq 0 ]] || die "run me with sudo"

APPLY=false
UNDO=false
APP_NAME="${APP_NAME:-nomaos-api}"
LEGACY_UNITS=(
  "Agrovesto.Backend.API-blue"
  "Agrovesto.Backend.API-green"
  "Agrovesto.Backend.API"
)

while [[ $# -gt 0 ]]; do
  case "$1" in
    --commit) APPLY=true; shift ;;
    --undo)   UNDO=true; shift ;;
    --app)    APP_NAME="${2:?}"; shift 2 ;;
    -h|--help)
      sed -n '2,20p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
      exit 0 ;;
    *) die "unknown argument: $1" ;;
  esac
done

run() {
  if [[ "$APPLY" == true ]]; then
    "$@"
  else
    printf '    %s[dry-run]%s %s\n' "$C_DIM" "$C_RESET" "$*"
  fi
}

# ── Undo path ────────────────────────────────────────────────────────────────
if [[ "$UNDO" == true ]]; then
  APPLY=true
  warn "reversing the cutover — going back to host nginx + systemd"

  info "stopping the nginx container so it releases :80/:443"
  docker stop nomaos-nginx 2>/dev/null || true

  for unit in "${LEGACY_UNITS[@]}"; do
    if systemctl list-unit-files 2>/dev/null | grep -q "^${unit}.service"; then
      info "re-enabling $unit"
      systemctl enable "$unit" 2>/dev/null || true
    fi
  done

  if systemctl list-unit-files 2>/dev/null | grep -q '^nginx.service'; then
    info "restarting host nginx"
    systemctl enable --now nginx || warn "could not start host nginx"
  fi

  ok "reverted. Start the legacy slot you want: systemctl start Agrovesto.Backend.API-blue"
  exit 0
fi

# ── Safety gate ──────────────────────────────────────────────────────────────
info "verifying the containerised stack is actually serving"

active="$(get_active_slot "$APP_NAME")"
container="${APP_NAME}-${active}"
state="$(container_state "$container")"
health="$(container_health "$container")"

printf '    active slot : %s\n    container   : %s (%s / %s)\n' \
  "$active" "$container" "$state" "$health"

[[ "$state" == "running" ]] \
  || die "$container is '$state'. Deploy and verify the Docker stack before cutting over."

[[ "$health" == "healthy" || "$health" == "none" ]] \
  || die "$container health is '$health'. Not cutting over."

ok "container stack looks healthy"

if [[ "$APPLY" != true ]]; then
  cat <<EOF

${C_YELLOW}DRY RUN${C_RESET} — nothing will change. Re-run with --commit to apply.

EOF
fi

# ── 1. Free the edge ports ───────────────────────────────────────────────────
info "freeing :80/:443 from host nginx"
if systemctl is-active --quiet nginx 2>/dev/null; then
  # Snapshot the old config first — it documents the routing that used to work.
  snap="$NOMAOS_STATE_DIR/backups/host-nginx-$(date -u +%Y%m%d-%H%M%S).tar.gz"
  run mkdir -p "$NOMAOS_STATE_DIR/backups"
  run tar czf "$snap" -C /etc nginx 2>/dev/null || true
  [[ "$APPLY" == true ]] && ok "host nginx config archived to $snap"

  run systemctl disable --now nginx
  ok "host nginx stopped and disabled"
else
  ok "host nginx not running"
fi

# ── 2. Stop the legacy app units ─────────────────────────────────────────────
for unit in "${LEGACY_UNITS[@]}"; do
  if systemctl list-unit-files 2>/dev/null | grep -q "^${unit}.service"; then
    info "disabling $unit"
    run systemctl disable --now "$unit"
  fi
done

# ── 3. Optionally stop host SQL Server / Redis ───────────────────────────────
for svc in mssql-server redis-server redis; do
  if systemctl is-active --quiet "$svc" 2>/dev/null; then
    warn "$svc is still running on the host."
    warn "Leave it until you are certain the containerised data is correct."
    warn "Then: sudo systemctl disable --now $svc"
  fi
done

# ── 4. Restart the nginx container to claim the ports ────────────────────────
info "starting the nginx container on :80/:443"
run docker start nomaos-nginx

if [[ "$APPLY" == true ]]; then
  sleep 3
  if [[ "$(container_state nomaos-nginx)" == "running" ]]; then
    ok "nginx container holds the edge"
  else
    warn "nginx container is not running — check: docker logs nomaos-nginx"
    warn "the most likely cause is another process still holding :80"
    warn "  sudo ss -tlnp '( sport = :80 )'"
  fi
fi

cat <<EOF

${C_GREEN}Cutover $( [[ "$APPLY" == true ]] && echo complete || echo 'dry run complete' )${C_RESET}

Left in place on purpose (delete manually once you are confident):
  /var/www/Nomaos/releases/*        old rsync artifacts
  /etc/systemd/system/Agrovesto.Backend.API-*.service
  /etc/nginx/                       host nginx config (archived)

Reverse this:  sudo $0 --undo

EOF
