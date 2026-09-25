#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# bootstrap.sh — prepare a fresh (or existing) EC2 host for the Docker stack
#
#   sudo deploy/scripts/bootstrap.sh
#
# Idempotent. Safe to re-run. Does NOT touch the running systemd deployment —
# migrating off that is a deliberate step (see migrate-from-systemd.sh).
# ─────────────────────────────────────────────────────────────────────────────
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"
# shellcheck source=lib.sh
source "$SCRIPT_DIR/lib.sh"

[[ $EUID -eq 0 ]] || die "run me with sudo"

DEPLOY_USER="${DEPLOY_USER:-ubuntu}"

# ── 1. Docker engine + compose v2 ────────────────────────────────────────────
if ! command -v docker >/dev/null 2>&1; then
  info "installing Docker Engine"
  apt-get update -qq
  apt-get install -y -qq ca-certificates curl gnupg

  install -m 0755 -d /etc/apt/keyrings
  if [[ ! -f /etc/apt/keyrings/docker.asc ]]; then
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
      -o /etc/apt/keyrings/docker.asc
    chmod a+r /etc/apt/keyrings/docker.asc
  fi

  cat > /etc/apt/sources.list.d/docker.list <<EOF
deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] \
https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable
EOF

  apt-get update -qq
  apt-get install -y -qq \
    docker-ce docker-ce-cli containerd.io \
    docker-buildx-plugin docker-compose-plugin
  ok "Docker installed"
else
  ok "Docker already present ($(docker --version))"
fi

systemctl enable --now docker >/dev/null 2>&1 || true

if ! id -nG "$DEPLOY_USER" 2>/dev/null | grep -qw docker; then
  usermod -aG docker "$DEPLOY_USER"
  warn "$DEPLOY_USER added to the docker group — it must re-login for this to apply"
fi

# ── 2. Daemon config: log rotation + sane defaults ───────────────────────────
# Without this, json-file logs grow unbounded and eventually fill the root
# volume — the classic way a dockerised EC2 box falls over at 3am.
if [[ ! -f /etc/docker/daemon.json ]]; then
  info "writing /etc/docker/daemon.json"
  mkdir -p /etc/docker
  cat > /etc/docker/daemon.json <<'EOF'
{
  "log-driver": "json-file",
  "log-opts": { "max-size": "20m", "max-file": "5" },
  "live-restore": true,
  "default-address-pools": [
    { "base": "172.30.0.0/16", "size": 24 }
  ]
}
EOF
  systemctl restart docker
  ok "daemon configured (log rotation on, live-restore on)"
else
  ok "/etc/docker/daemon.json already exists — left untouched"
  grep -q 'max-size' /etc/docker/daemon.json \
    || warn "no log rotation in daemon.json — container logs can fill the disk"
fi

# ── 3. Host directory layout ─────────────────────────────────────────────────
info "creating $NOMAOS_STATE_DIR"
mkdir -p \
  "$NOMAOS_STATE_DIR"/env \
  "$NOMAOS_STATE_DIR"/nginx/upstreams \
  "$NOMAOS_STATE_DIR"/nginx/certs \
  "$NOMAOS_STATE_DIR"/state \
  "$NOMAOS_STATE_DIR"/backups \
  "$NOMAOS_STATE_DIR"/logs/nginx \
  "$NOMAOS_STATE_DIR"/logs/nomaos-api/blue \
  "$NOMAOS_STATE_DIR"/logs/nomaos-api/green \
  "$NOMAOS_STATE_DIR"/logs/nomaos-app2/blue \
  "$NOMAOS_STATE_DIR"/logs/nomaos-app2/green

# The app container runs as UID 1654 ("app" in the Microsoft base image) and
# must be able to write its Serilog file sink into the bind-mounted log dir.
chown -R 1654:1654 "$NOMAOS_STATE_DIR"/logs/nomaos-api "$NOMAOS_STATE_DIR"/logs/nomaos-app2
chmod 700 "$NOMAOS_STATE_DIR/env"
ok "layout ready"

# ── 4. Seed placeholder upstreams so nginx can start before any app exists ───
# nginx refuses to load a config referencing an unresolvable host, so each
# upstream starts life pointing at a loopback dead-end that returns 502.
for up in nomaos_api nomaos_app2; do
  f="$NOMAOS_STATE_DIR/nginx/upstreams/${up}.conf"
  if [[ ! -f "$f" ]]; then
    cat > "$f" <<EOF
# Placeholder written by bootstrap.sh — replaced on first deploy.
upstream ${up} {
    server 127.0.0.1:1 max_fails=0;
}
EOF
    chmod 644 "$f"
    ok "seeded $f"
  fi
done

# ── 5. Env file templates ────────────────────────────────────────────────────
for app in nomaos-api nomaos-app2; do
  f="$NOMAOS_STATE_DIR/env/${app}.env"
  if [[ ! -f "$f" ]]; then
    sed "s/__APP_NAME__/$app/g" "$REPO_DIR/deploy/env/app.env.example" > "$f"
    chmod 600 "$f"
    chown root:root "$f"
    warn "created $f from template — FILL IN the Vault + DB values before deploying"
  else
    chmod 600 "$f"
  fi
done

f="$NOMAOS_STATE_DIR/env/infra.env"
if [[ ! -f "$f" ]]; then
  cp "$REPO_DIR/deploy/env/infra.env.example" "$f"
  chmod 600 "$f"
  warn "created $f — set MSSQL_SA_PASSWORD before starting infra"
fi

# ── 6. Log rotation for the bind-mounted app + nginx logs ────────────────────
cat > /etc/logrotate.d/nomaos <<EOF
$NOMAOS_STATE_DIR/logs/*/*/*.log $NOMAOS_STATE_DIR/logs/nginx/*.log {
    daily
    rotate 14
    compress
    delaycompress
    missingok
    notifempty
    copytruncate
    su root root
}
EOF
ok "logrotate configured"

# ── 7. Firewall ──────────────────────────────────────────────────────────────
# Docker publishes ports by writing iptables DOCKER rules that bypass ufw, so
# ufw alone will NOT protect a published port. The compose file deliberately
# binds SQL Server to 127.0.0.1 for that reason; real edge filtering belongs in
# the EC2 security group.
if command -v ufw >/dev/null 2>&1 && ufw status | grep -q 'Status: active'; then
  warn "ufw is active. Note: Docker's iptables rules bypass ufw for published"
  warn "ports — rely on the EC2 security group for 80/443 exposure."
fi

cat <<EOF

${C_GREEN}Bootstrap complete${C_RESET}

Next:
  1. Fill in secrets:
       sudo vi $NOMAOS_STATE_DIR/env/infra.env       # MSSQL_SA_PASSWORD
       sudo vi $NOMAOS_STATE_DIR/env/nomaos-api.env  # VAULT_*, DB_CONNECTION_STRING

  2. Start infrastructure (nginx + SQL Server + Redis):
       cd $REPO_DIR/deploy/compose
       sudo docker compose -p nomaos-infra --env-file $NOMAOS_STATE_DIR/env/infra.env \\
            -f docker-compose.infra.yml up -d

  3. Move your data in (see deploy/scripts/migrate-data.sh)

  4. Deploy:
       sudo $REPO_DIR/deploy/scripts/deploy.sh --app nomaos-api --image <ecr-ref>:<tag>

EOF
