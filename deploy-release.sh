#!/bin/bash
# Robust Deployment Script for Agrovesto.Backend.API
# Author: Dolapo Fashina 
set -euo pipefail
set -x


APP_NAME="Agrovesto.Backend.API"
APP_DIR="/var/www/Agrovesto.Backend.Release"
RELEASES_DIR="$APP_DIR/releases"
RELEASE_ID="$(date +%Y%m%d%H%M%S)"
RELEASE_DIR="$RELEASES_DIR/$RELEASE_ID"
CURRENT_DIR="$APP_DIR/current"
PREVIOUS_DIR="$APP_DIR/previous"
USER_NAME="www-data"
CI_USER="ubuntu"
PROJECT_PATH="$APP_DIR/Agrovesto.Backend.API/Agrovesto.Backend.API.csproj"

export HOME=/tmp
export DOTNET_CLI_HOME=/tmp
export NUGET_PACKAGES="/tmp/.nuget/packages"

echo "🚀 Deploying $APP_NAME — release $RELEASE_ID"

mkdir -p "$RELEASE_DIR"
sudo chown -R "$CI_USER:$CI_USER" "$APP_DIR"

# ------------------------------------------------------------
# Build & publish
# ------------------------------------------------------------
dotnet publish "$PROJECT_PATH"\
  -c Release \
  -r linux-x64 \
  --self-contained true \
  /p:PublishTrimmed=false \
  -o "$RELEASE_DIR"

chmod +x "$RELEASE_DIR/$APP_NAME"

# ------------------------------------------------------------
# Save previous release
# ------------------------------------------------------------
if [ -L "$CURRENT_DIR" ]; then
  sudo ln -sfn "$(readlink -f "$CURRENT_DIR")" "$PREVIOUS_DIR"
fi

# ------------------------------------------------------------
# Switch release atomically
# ------------------------------------------------------------
sudo ln -sfn "$RELEASE_DIR" "$CURRENT_DIR"
sudo chown -R "$USER_NAME:$USER_NAME" "$RELEASE_DIR"
sudo systemctl restart "$APP_NAME"

# ------------------------------------------------------------
# Cleanup old releases (keep last 3)
# ------------------------------------------------------------
ls -1dt "$RELEASES_DIR"/* | tail -n +4 | sudo xargs rm -rf || true
# ------------------------------------------------------------
# 8. Environment file
# ------------------------------------------------------------
sudo tee "$ENV_FILE" > /dev/null <<EOF
ASPNETCORE_ENVIRONMENT=Production
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
WorkingDirectory=$CURRENT_DIR
ExecStart=/usr/bin/dotnet $CURRENT_DIR/$APP_NAME.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
User=$USER_NAME
SyslogIdentifier=$APP_NAME
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable --now "$APP_NAME.service"
sudo systemctl restart "$APP_NAME"
sudo systemctl status $APP_NAME

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
    ssl_certificate_key /etc/letsencrypt/live/$DOMAIN/privkey.pem;

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
echo "→ Logs: sudo journalctl -u $APP_NAME -f"
echo "→ Nginx status: sudo nginx -t && sudo systemctl status nginx"
echo "✅ Deployment successful"



