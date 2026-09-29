#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# migrate-data.sh — move host SQL Server + Redis into their containers
#
#   sudo deploy/scripts/migrate-data.sh --db nomaosdb [--skip-redis]
#
# This is the one genuinely destructive-adjacent step of the whole migration,
# so it is deliberately conservative:
#
#   * BACKUP only  -> take a .bak from the host instance and stop.
#   * RESTORE only -> load an existing .bak into the container.
#   * Default      -> backup, then restore, verifying row counts on both sides.
#
# It never drops anything on the host. Rolling back = point the connection
# string at the host again; the original data is still there, untouched.
#
# Requires: sqlcmd on the host (the existing SQL Server install provides it).
# ─────────────────────────────────────────────────────────────────────────────
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib.sh
source "$SCRIPT_DIR/lib.sh"

DB_NAME="${DB_NAME:-nomaosdb}"
HOST_SQL_SERVER="${HOST_SQL_SERVER:-localhost,1433}"
HOST_SA_USER="${HOST_SA_USER:-sa}"
HOST_SA_PASSWORD="${HOST_SA_PASSWORD:-}"
MSSQL_CONTAINER="${MSSQL_CONTAINER:-nomaos-mssql}"
REDIS_CONTAINER="${REDIS_CONTAINER:-nomaos-redis}"
BACKUP_DIR="${BACKUP_DIR:-$NOMAOS_STATE_DIR/backups}"
MODE="full"
SKIP_REDIS=false

usage() {
  cat <<'EOF'
Usage: sudo migrate-data.sh [options]

  --db <name>          Database to migrate (default: nomaosdb)
  --host-sql <addr>    Source SQL Server (default: localhost,1433)
  --sa-password <pw>   Host sa password. Prefer the HOST_SA_PASSWORD env var
                       so it stays out of your shell history.
  --backup-only        Take the .bak and stop
  --restore-only <f>   Restore an existing .bak into the container
  --skip-redis         Do not touch Redis
  -h, --help

Redis note: the cache is rebuildable, so the default is a clean start. Pass
nothing special and this script will only warn if the host Redis holds keys.
EOF
}

RESTORE_FILE=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --db)           DB_NAME="${2:?}"; shift 2 ;;
    --host-sql)     HOST_SQL_SERVER="${2:?}"; shift 2 ;;
    --sa-password)  HOST_SA_PASSWORD="${2:?}"; shift 2 ;;
    --backup-only)  MODE="backup"; shift ;;
    --restore-only) MODE="restore"; RESTORE_FILE="${2:?}"; shift 2 ;;
    --skip-redis)   SKIP_REDIS=true; shift ;;
    -h|--help)      usage; exit 0 ;;
    *) usage >&2; die "unknown argument: $1" ;;
  esac
done

require_docker
mkdir -p "$BACKUP_DIR"

# Locate a sqlcmd on the host (path differs across mssql-tools versions).
find_sqlcmd() {
  for c in sqlcmd /opt/mssql-tools18/bin/sqlcmd /opt/mssql-tools/bin/sqlcmd; do
    command -v "$c" >/dev/null 2>&1 && { printf '%s' "$c"; return 0; }
    [[ -x "$c" ]] && { printf '%s' "$c"; return 0; }
  done
  return 1
}

# sqlcmd 18+ requires -C (trust cert) which older builds reject.
sqlcmd_host() {
  local bin; bin="$(find_sqlcmd)" || die "sqlcmd not found on the host"
  local extra=()
  [[ "$bin" == *tools18* ]] && extra=(-C)
  "$bin" -S "$HOST_SQL_SERVER" -U "$HOST_SA_USER" -P "$HOST_SA_PASSWORD" \
    "${extra[@]}" -b "$@"
}

sqlcmd_container() {
  docker exec -e SAPW="$MSSQL_SA_PASSWORD" "$MSSQL_CONTAINER" \
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b "$@"
}

TS="$(date -u +%Y%m%d-%H%M%S)"
BAK_NAME="${DB_NAME}-${TS}.bak"

# ─────────────────────────────────────────────────────────────────────────────
# Backup from the host instance
# ─────────────────────────────────────────────────────────────────────────────
do_backup() {
  [[ -n "$HOST_SA_PASSWORD" ]] || die "host sa password required (--sa-password or HOST_SA_PASSWORD)"

  info "checking host SQL Server at $HOST_SQL_SERVER"
  sqlcmd_host -Q "SELECT 1" >/dev/null || die "cannot connect to the host SQL Server"

  info "row-count snapshot (source)"
  sqlcmd_host -d "$DB_NAME" -h -1 -W -Q "
    SET NOCOUNT ON;
    SELECT SUM(p.rows)
    FROM sys.tables t
    JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0,1);
  " | tr -d '[:space:]' > "$BACKUP_DIR/.rowcount-source"
  ok "source rows: $(cat "$BACKUP_DIR/.rowcount-source")"

  info "backing up $DB_NAME (COPY_ONLY, CHECKSUM)"
  # /var/opt/mssql/backup is the host instance's own directory. We write there
  # first because the SQL Server service account owns it, then move the file.
  local host_bak="/var/opt/mssql/backup/$BAK_NAME"
  mkdir -p /var/opt/mssql/backup

  sqlcmd_host -Q "
    BACKUP DATABASE [$DB_NAME]
    TO DISK = N'$host_bak'
    WITH COPY_ONLY, INIT, CHECKSUM, COMPRESSION, STATS = 10;
  " || die "backup failed"

  info "verifying backup integrity"
  sqlcmd_host -Q "RESTORE VERIFYONLY FROM DISK = N'$host_bak' WITH CHECKSUM;" \
    || die "backup failed VERIFYONLY — do not proceed"

  cp "$host_bak" "$BACKUP_DIR/$BAK_NAME"
  chmod 600 "$BACKUP_DIR/$BAK_NAME"
  ok "backup at $BACKUP_DIR/$BAK_NAME ($(du -h "$BACKUP_DIR/$BAK_NAME" | cut -f1))"
  printf '%s' "$BACKUP_DIR/$BAK_NAME" > "$BACKUP_DIR/.latest"
}

# ─────────────────────────────────────────────────────────────────────────────
# Restore into the container
# ─────────────────────────────────────────────────────────────────────────────
do_restore() {
  local bak="$1"
  [[ -r "$bak" ]] || die "backup file not readable: $bak"

  # MSSQL_SA_PASSWORD for the container comes from infra.env.
  if [[ -z "${MSSQL_SA_PASSWORD:-}" ]]; then
    local infra_env="$NOMAOS_STATE_DIR/env/infra.env"
    [[ -r "$infra_env" ]] || die "MSSQL_SA_PASSWORD unset and $infra_env unreadable"
    # shellcheck disable=SC1090
    set -a; source "$infra_env"; set +a
  fi
  [[ -n "${MSSQL_SA_PASSWORD:-}" ]] || die "MSSQL_SA_PASSWORD not set"

  [[ "$(container_state "$MSSQL_CONTAINER")" == "running" ]] \
    || die "$MSSQL_CONTAINER is not running — start the infra stack first"

  info "waiting for the container instance to accept connections"
  local waited=0
  until sqlcmd_container -Q "SELECT 1" >/dev/null 2>&1; do
    (( waited >= 180 )) && die "container SQL Server never came up"
    sleep 5; waited=$(( waited + 5 ))
  done
  ok "container SQL Server ready"

  info "copying backup into the container"
  docker cp "$bak" "$MSSQL_CONTAINER:/var/opt/mssql/backup/restore.bak"

  info "reading logical file names from the backup"
  # RESTORE needs an explicit MOVE for each logical file, and the logical names
  # are whatever the source instance used — never assume they match the db name.
  local filelist
  filelist="$(sqlcmd_container -h -1 -W -s '|' -Q "
    SET NOCOUNT ON;
    RESTORE FILELISTONLY FROM DISK = N'/var/opt/mssql/backup/restore.bak';
  ")" || die "could not read the backup file list"

  local move_clauses=""
  while IFS='|' read -r logical physical type rest; do
    [[ -z "$logical" || "$logical" == -* ]] && continue
    logical="$(printf '%s' "$logical" | sed 's/[[:space:]]*$//')"
    type="$(printf '%s' "$type" | tr -d '[:space:]')"
    local ext target
    if [[ "$type" == "L" ]]; then ext="_log.ldf"; else ext=".mdf"; fi
    # Multiple data files would collide on one name; suffix with the logical name.
    target="/var/opt/mssql/data/${logical}${ext}"
    move_clauses+=" MOVE N'${logical}' TO N'${target}',"
  done <<< "$filelist"

  [[ -n "$move_clauses" ]] || die "backup file list came back empty"

  info "restoring $DB_NAME into the container"
  sqlcmd_container -Q "
    RESTORE DATABASE [$DB_NAME]
    FROM DISK = N'/var/opt/mssql/backup/restore.bak'
    WITH ${move_clauses} REPLACE, RECOVERY, STATS = 10;
  " || die "restore failed"

  # Backups carry orphaned SIDs; without this the app login authenticates but
  # has no rights inside the database.
  info "checking for orphaned users"
  sqlcmd_container -d "$DB_NAME" -h -1 -W -Q "
    SET NOCOUNT ON;
    SELECT dp.name
    FROM sys.database_principals dp
    LEFT JOIN sys.server_principals sp ON dp.sid = sp.sid
    WHERE sp.sid IS NULL
      AND dp.type IN ('S','U','G')
      AND dp.principal_id > 4;
  " | sed '/^$/d' | sed 's/^/    orphaned: /' || true

  info "row-count verification (target)"
  local target_rows
  target_rows="$(sqlcmd_container -d "$DB_NAME" -h -1 -W -Q "
    SET NOCOUNT ON;
    SELECT SUM(p.rows)
    FROM sys.tables t
    JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0,1);
  " | tr -d '[:space:]')"

  if [[ -r "$BACKUP_DIR/.rowcount-source" ]]; then
    local source_rows; source_rows="$(cat "$BACKUP_DIR/.rowcount-source")"
    if [[ "$source_rows" == "$target_rows" ]]; then
      ok "row counts match: $target_rows"
    else
      warn "ROW COUNT MISMATCH — source=$source_rows target=$target_rows"
      warn "investigate before cutting traffic over"
    fi
  else
    ok "target rows: $target_rows"
  fi

  docker exec "$MSSQL_CONTAINER" rm -f /var/opt/mssql/backup/restore.bak || true

  cat <<EOF

${C_YELLOW}Login reminder${C_RESET}
A restored database brings its *users*, but not the server *logins*. If the app
connects as a SQL login other than sa, recreate it on the container instance:

  docker exec -it $MSSQL_CONTAINER /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '<pw>' -C -Q "
    CREATE LOGIN [agrovesto] WITH PASSWORD = '<pw>';
    USE [$DB_NAME];
    ALTER USER [agrovesto] WITH LOGIN = [agrovesto];   -- relink the orphaned SID
  "
EOF
}

# ─────────────────────────────────────────────────────────────────────────────
# Redis
# ─────────────────────────────────────────────────────────────────────────────
do_redis() {
  info "checking host Redis"
  if ! command -v redis-cli >/dev/null 2>&1; then
    warn "redis-cli not on the host — skipping the Redis check"
    return 0
  fi

  local keys
  keys="$(redis-cli -h 127.0.0.1 -p "${HOST_REDIS_PORT:-6379}" DBSIZE 2>/dev/null | awk '{print $NF}')" || keys=""

  if [[ -z "$keys" ]]; then
    ok "host Redis not reachable — nothing to migrate"
    return 0
  fi

  if [[ "$keys" == "0" ]]; then
    ok "host Redis is empty — container starts clean"
    return 0
  fi

  # The app uses Redis purely as a distributed cache (AddStackExchangeRedisCache)
  # plus rate-limit counters. Losing it costs a few cold lookups and resets OTP
  # windows — not worth a risky RDB transplant.
  warn "host Redis holds $keys keys."
  warn "These are cache + rate-limit entries and will be rebuilt on demand."
  warn "To copy them anyway:"
  printf '    redis-cli --rdb %s/redis-%s.rdb\n' "$BACKUP_DIR" "$TS"
  printf '    docker cp %s/redis-%s.rdb %s:/data/dump.rdb\n' "$BACKUP_DIR" "$TS" "$REDIS_CONTAINER"
  printf '    docker restart %s\n' "$REDIS_CONTAINER"
}

# ─────────────────────────────────────────────────────────────────────────────
case "$MODE" in
  backup)
    do_backup
    info "backup-only: stopping here"
    ;;
  restore)
    do_restore "$RESTORE_FILE"
    ;;
  full)
    do_backup
    do_restore "$BACKUP_DIR/$BAK_NAME"
    [[ "$SKIP_REDIS" == true ]] || do_redis
    ;;
esac

cat <<EOF

${C_GREEN}Done${C_RESET}
Point the app at the containers by setting, in $NOMAOS_STATE_DIR/env/nomaos-api.env:

  DB_CONNECTION_STRING=Data Source=mssql,1433;Initial Catalog=$DB_NAME;User Id=sa;Password=<pw>;Encrypt=True;TrustServerCertificate=True
  REDIS_CONNECTION_STRING=redis:6379

The host SQL Server is untouched — rollback is a connection-string change.
Stop it only once you are happy:  sudo systemctl disable --now mssql-server

EOF
