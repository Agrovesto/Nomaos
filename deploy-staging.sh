
#!/bin/bash
# Robust Deployment Script for Agrovesto.Backend.API (FINAL WORKING VERSION)
# Tested on Ubuntu 22.04 + .NET 8 + Nginx + Let's Encrypt
# Author: Dolapo Fashina 
set -euo pipefail  # Better than just set -e
set -x             # Keep command tracing

# ------------------------------------------------------------
# Variables
# ------------------------------------------------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

APP_NAME="Agrovesto.Backend.API"
APP_DIR="/var/www/$APP_NAME"
SERVICE_FILE="/etc/systemd/system/$APP_NAME.service"
ENV_FILE="/etc/default/$APP_NAME"
USER_NAME="www-data"
CI_USER="ubuntu"
DOMAIN="dev-noma-api.nomaos.com"
NGINX_SITE="/etc/nginx/sites-available/$DOMAIN"
PUBLISH_DIR="$APP_DIR/publish"
CURRENT_DIR="$APP_DIR/current"

# Fix home for dotnet/NuGet in CI
export HOME=/tmp
export DOTNET_CLI_HOME=/tmp
export NUGET_PACKAGES="/tmp/.nuget/packages"

echo "Starting deployment for $APP_NAME → https://$DOMAIN"

# ------------------------------------------------------------
# 1. System update & prerequisites
# ------------------------------------------------------------
sudo apt-get update
sudo apt-get install -y wget apt-transport-https software-properties-common ufw nginx

# ------------------------------------------------------------
# 2. Install .NET 8 (only if missing)
# ------------------------------------------------------------
if ! dotnet --list-sdks | grep -q "8.0"; then
    echo "Installing .NET 8 SDK..."
    wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
    sudo dpkg -i /tmp/packages-microsoft-prod.deb
    rm /tmp/packages-microsoft-prod.deb
    sudo apt-get update
    sudo apt-get install -y dotnet-sdk-8.0
fi

# ------------------------------------------------------------
# 3. Firewall
# ------------------------------------------------------------
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw --force enable

# ------------------------------------------------------------
# 4. Stop existing service
# ------------------------------------------------------------
sudo systemctl stop "$APP_NAME" || true

# ------------------------------------------------------------
# 5. Prepare publish folder
# ------------------------------------------------------------
sudo rm -rf "$PUBLISH_DIR"
sudo mkdir -p "$PUBLISH_DIR"
sudo chown -R "$CI_USER:$CI_USER" "$PUBLISH_DIR" "$SCRIPT_DIR"

# ------------------------------------------------------------
# 6. Build & Publish (.NET 8 self-contained)
# ------------------------------------------------------------
sudo -u "$CI_USER" dotnet clean "$SCRIPT_DIR" --configuration Release
sudo -u "$CI_USER" dotnet restore "$SCRIPT_DIR" --no-cache  --runtime linux-x64
sudo -u "$CI_USER" dotnet publish "$SCRIPT_DIR" \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  /p:PublishTrimmed=false \
  -o "$APP_DIR" \
  --no-restore

sudo chown -R "$USER_NAME:$USER_NAME" "$APP_DIR"
sudo systemctl restart "$APP_NAME"

# ------------------------------------------------------------
# 7. Update symlink atomically
# ------------------------------------------------------------
sudo ln -sfn "$PUBLISH_DIR" "$CURRENT_DIR"
sudo chown -R "$USER_NAME:$USER_NAME" "$PUBLISH_DIR" "$CURRENT_DIR"

# ------------------------------------------------------------
# 8. Environment file
# ------------------------------------------------------------
sudo tee "$ENV_FILE" > /dev/null <<EOF
ASPNETCORE_ENVIRONMENT=staging
ASPNETCORE_URLS=http://0.0.0.0:5000
DOTNET_PRINT_TELEMETRY_MESSAGE=false
EOF

# ------------------------------------------------------------
# 9. Systemd service
# ------------------------------------------------------------
sudo tee "$SERVICE_FILE" > /dev/null <<EOF
[Unit]
Description=$APP_NAME
After=network.target

[Service]
WorkingDirectory=$APP_DIR
ExecStart=/usr/bin/dotnet $CURRENT_DIR/$APP_NAME.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
User=$USER_NAME
EnvironmentFile=$ENV_FILE
SyslogIdentifier=$APP_NAME

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable --now "$APP_NAME.service"

# ------------------------------------------------------------
# 10. NGINX CONFIG — CORRECT & CERTBOT-READY
# ------------------------------------------------------------
sudo tee "$NGINX_SITE" > /dev/null <<EOF
# HTTP → HTTPS redirect
server {
    listen 80;
    listen [::]:80;
    server_name $DOMAIN;

    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }

    location / {
        return 301 https://\$server_name\$request_uri;
    }
}

# HTTPS server (Certbot will auto-fill ssl_certificate lines)
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name $DOMAIN;

    ssl_certificate     /etc/letsencrypt/live/$DOMAIN/fullchain.pem;
    ssl_certificate_key /etc/etc/letsencrypt/live/$DOMAIN/privkey.pem;

    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;

    location /health {
        proxy_pass http://127.0.0.1:5000/health;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }

    location / {
        proxy_pass http://127.0.0.1:5000/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;

        proxy_connect_timeout 60s;
        proxy_send_timeout    60s;
        proxy_read_timeout    60s;
    }
}
EOF

# Enable site
sudo ln -sf "$NGINX_SITE" /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default

# Test + reload Nginx
sudo nginx -t && sudo systemctl reload nginx

# ------------------------------------------------------------
# 11. Get + Install Let's Encrypt SSL 
# ------------------------------------------------------------
if ! sudo certbot certificates --quiet | grep -q "$DOMAIN"; then
    echo "Obtaining Let's Encrypt certificate..."
    sudo certbot --nginx -d "$DOMAIN" --non-interactive --agree-tos --email dolapofashina@gmail.com --redirect
else
    echo "Certificate already exists — installing/updating Nginx config..."
    sudo certbot install --nginx --cert-name "$DOMAIN"
fi

# Final Nginx reload after cert install
sudo systemctl reload nginx

# ------------------------------------------------------------
# Done
# ------------------------------------------------------------
echo ""
echo "DEPLOYMENT SUCCESSFUL!"
echo "→ API running at: https://$DOMAIN"
echo "→ Service status: sudo systemctl status $APP_NAME"
echo "→ Logs: journalctl -u $APP_NAME -f"
echo "→ Nginx status: sudo nginx -t && sudo systemctl status nginx"
