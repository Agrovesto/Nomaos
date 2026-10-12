#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# status.sh — what is actually running right now
# ─────────────────────────────────────────────────────────────────────────────
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib.sh
source "$SCRIPT_DIR/lib.sh"

require_docker

if (( $# > 0 )); then
  APPS=("$@")
else
  APPS=(nomaos-api nomaos-app2)
fi

printf '\n%sInfrastructure%s\n' "$C_BLUE" "$C_RESET"
for c in nomaos-nginx nomaos-mssql nomaos-redis; do
  printf '  %-16s %-10s %s\n' "$c" "$(container_state "$c")" "$(container_health "$c")"
done

for app in "${APPS[@]}"; do
  active="$(get_active_slot "$app")"
  printf '\n%s%s%s  (active slot: %s%s%s)\n' \
    "$C_BLUE" "$app" "$C_RESET" "$C_GREEN" "$active" "$C_RESET"

  for slot in blue green; do
    name="${app}-${slot}"
    state="$(container_state "$name")"
    [[ "$state" == "absent" ]] && continue

    marker='   '
    [[ "$slot" == "$active" ]] && marker="${C_GREEN}==>${C_RESET}"
    image="$(docker inspect -f '{{.Config.Image}}' "$name" 2>/dev/null || echo '-')"
    commit="$(docker inspect -f '{{index .Config.Labels "com.nomaos.commit"}}' "$name" 2>/dev/null || echo '-')"
    started="$(docker inspect -f '{{.State.StartedAt}}' "$name" 2>/dev/null | cut -c1-19 || echo '-')"

    printf '  %b %-8s %-9s %-10s %s\n' \
      "$marker" "$slot" "$state" "$(container_health "$name")" "$started"
    printf '       image  %s\n' "$image"
    printf '       commit %s\n' "$commit"
  done

  upstream_name="${app//-/_}"
  up_file="$NOMAOS_STATE_DIR/nginx/upstreams/${upstream_name}.conf"
  if [[ -r "$up_file" ]]; then
    printf '     %supstream%s %s\n' "$C_DIM" "$C_RESET" \
      "$(grep -E '^\s+server' "$up_file" | tr -s ' ' | sed 's/^ //' | tr '\n' ' ')"
  fi
done

printf '\n%sDisk%s\n' "$C_BLUE" "$C_RESET"
docker system df 2>/dev/null | sed 's/^/  /'
printf '\n'
