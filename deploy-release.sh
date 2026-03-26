#!/usr/bin/env bash
# ============================================================
# Agrovesto Backend API — Production Release Deployment Script
# Zero-downtime oriented, rollback-capable, idempotent
# ============================================================

set -Eeuo pipefail

# ------------------------------------------------------------
# CONFIGURATION
# ------------------------------------------------------------
APP_NAME="Agrovesto.Backend.API"
DOMAIN="release-noma-api.com"

APP_DIR="/var/www/Agrovesto.Backend.Release"
RELEASES_DIR="$APP_DIR/releases"
CURRENT_DIR="$APP_DIR/current"
PREVIOUS_DIR="$APP_DIR/previous"

SERVICE_FILE="/etc/systemd/system/Agrovesto.Backend.Release.service"
ENV_FILE="/etc/Agrovesto.Backend.Release.env"
NGINX_SITE="/etc/nginx/sites-available/Agrovesto.Backend.Release"

PROJECT_PATH="$APP_DIR/Agrovesto.Backend.API/Agrovesto.Backend.API.csproj"

USER_NAME="www-data"
CI_USER="ubuntu"

KEEP_RELEASES=3
APP_PORT=5003
HEALTH_ENDPOINT="http://127.0.0.1:$APP_PORT/health"

export HOME=/tmp
export DOTNET_CLI_HOME=/tmp
export NUGET_PACKAGES="/tmp/.nuget/packages"

RELEASE_ID="$(date +%Y%m%d%H%M%S)"
RELEASE_DIR="$RELEASES_DIR/$RELEASE_ID"

# ------------------------------------------------------------
# LOGGING & ERROR HANDLING
# ------------------------------------------------------------
log()  { echo -e "\033[1;32m[INFO]\033[0m $*"; }
warn() { echo -e "\033[1;33m[WARN]\033[0m $*"; }
err()  { echo -e "\033[1;31m[ERROR]\033[0m $*"; }

rollback() {
    err "Deployment failed. Rolling back..."
    if [ -L "$PREVIOUS_DIR" ]; then
        sudo ln -sfn "$(readlink -f "$PREVIOUS_DIR")" "$CURRENT_DIR"
        sudo systemctl restart "Agrovesto.Backend.Release"
        log "Rollback complete."
    else
        err "No previous release found."
    fi
}

trap rollback ERR

# ------------------------------------------------------------
# PRECHECKS
# ------------------------------------------------------------
log "Starting deployment: Agrovesto.Backend.Release ($RELEASE_ID)"

command -v dotnet >/dev/null || { err "dotnet not installed"; exit 1; }
command -v nginx  >/dev/null || { err "nginx not installed"; exit 1; }
command -v certbot >/dev/null || warn "certbot not installed (SSL step may fail)"

mkdir -p "$RELEASE_DIR"
sudo mkdir -p "$RELEASES_DIR"

# ------------------------------------------------------------
# BUILD & PUBLISH
# ------------------------------------------------------------
log "Building application..."

dotnet publish "$PROJECT_PATH" \
  -c Release \
  --self-contained false \
  -o "$RELEASE_DIR"

# ------------------------------------------------------------
# SAVE PREVIOUS RELEASE
# ------------------------------------------------------------
if [ -L "$CURRENT_DIR" ]; then
    sudo ln -sfn "$(readlink -f "$CURRENT_DIR")" "$PREVIOUS_DIR"
fi

# ------------------------------------------------------------
# PERMISSIONS
# ------------------------------------------------------------
sudo chown -R "$USER_NAME:$USER_NAME" "$APP_DIR"

# ------------------------------------------------------------
# SYSTEMD SERVICE
# ------------------------------------------------------------
log "Configuring systemd service..."

sudo tee "$ENV_FILE" > /dev/null <<EOF
ASPNETCORE_ENVIRONMENT=Staging
ASPNETCORE_URLS=http://0.0.0.0:$APP_PORT
DOTNET_PRINT_TELEMETRY_MESSAGE=false
EOF

sudo tee "$SERVICE_FILE" > /dev/null <<EOF
[Unit]
Description=Agrovesto.Backend.Release
After=network.target

[Service]
WorkingDirectory=$CURRENT_DIR
ExecStart=/usr/bin/dotnet $CURRENT_DIR/Agrovesto.Backend.Release.dll
Restart=always
RestartSec=5
KillSignal=SIGINT
User=$USER_NAME
EnvironmentFile=$ENV_FILE
SyslogIdentifier=Agrovesto.Backend.Release
Port=5003

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable "Agrovesto.Backend.Release"

# ------------------------------------------------------------
# ATOMIC RELEASE SWITCH
# ------------------------------------------------------------
log "Switching to new release..."

sudo ln -sfn "$RELEASE_DIR" "$CURRENT_DIR"

# ------------------------------------------------------------
# START SERVICE
# ------------------------------------------------------------
log "Restarting application..."
sudo systemctl restart "Agrovesto.Backend.Release"

sleep 5

# ------------------------------------------------------------
# HEALTH CHECK
# ------------------------------------------------------------
# log "Running health check..."

# if ! curl -fsS "$HEALTH_ENDPOINT" >/dev/null; then
#     err "Health check failed!"
#     exit 1
# fi

# log "Health check passed."

# ------------------------------------------------------------
# CLEANUP OLD RELEASES
# ------------------------------------------------------------
log "Cleaning old releases..."

ls -1dt "$RELEASES_DIR"/* | tail -n +$((KEEP_RELEASES+1)) | xargs -r sudo rm -rf

# ------------------------------------------------------------
# NGINX CONFIG
# ------------------------------------------------------------
log "Configuring Nginx..."

sudo tee "$NGINX_SITE" > /dev/null <<EOF
server {
    listen 80;
    server_name $DOMAIN;

    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }

    location / {
        return 301 https://\$host\$request_uri;
    }
}

server {
    listen 443 ssl http2;
    server_name $DOMAIN;

    ssl_certificate     /etc/letsencrypt/live/$DOMAIN/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/$DOMAIN/privkey.pem;

    add_header X-Frame-Options SAMEORIGIN;
    add_header X-Content-Type-Options nosniff;
    add_header X-XSS-Protection "1; mode=block";

    gzip on;
    gzip_types text/plain application/json application/javascript text/css;

    location / {
        proxy_pass http://127.0.0.1:$APP_PORT;
        proxy_http_version 1.1;

        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host \$host;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }
}
EOF

sudo ln -sf "$NGINX_SITE" /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default

sudo nginx -t
sudo systemctl reload nginx

# ------------------------------------------------------------
# SSL (CERTBOT)
# ------------------------------------------------------------
log "Ensuring SSL certificate..."

if ! sudo certbot certificates | grep -q "$DOMAIN"; then
    sudo certbot --nginx \
        -d "$DOMAIN" \
        --non-interactive \
        --agree-tos \
        --email dolapofashina@gmail.com \
        --redirect
else
    sudo certbot renew --quiet || warn "Cert renewal skipped"
fi

sudo systemctl reload nginx

# ------------------------------------------------------------
# DONE
# ------------------------------------------------------------
log "Deployment successful!"
echo ""
echo "🌍 https://$DOMAIN"
echo "📦 Release: $RELEASE_ID"
echo "🔁 Rollback: ln -sfn \$(readlink -f $PREVIOUS_DIR) $CURRENT_DIR && systemctl restart Agrovesto.Backend.Release"
echo "📜 Logs: journalctl -u Agrovesto.Backend.Release -f"
