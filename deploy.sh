#!/bin/bash
# Robust Deployment Script for Agrovesto.Backend.API
# Author: Dolapo Fashina

set -e
set -x   # Command tracing

# ------------------------------------------------------------
# Detect project root directory (directory where deploy.sh lives)
# ------------------------------------------------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# ------------------------------------------------------------
# Update RDS SSL Certificate
# ------------------------------------------------------------
sudo wget https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem \
  -O /usr/local/share/ca-certificates/rds-global-bundle.crt

sudo update-ca-certificates


# ------------------------------------------------------------
# Configuration
# ------------------------------------------------------------
APP_NAME="Agrovesto.Backend.API"
APP_DIR="/var/www/$APP_NAME"
SERVICE_FILE="/etc/systemd/system/$APP_NAME.service"
ENV_FILE="/etc/default/$APP_NAME"
USER_NAME="www-data"
LOAD_BALANCER_DNS="18.202.191.229"
CI_USER="ubuntu"

# Fix HOME for CI/CD
export HOME=/tmp
export DOTNET_CLI_HOME=/tmp

echo "Starting deployment for $APP_NAME from $SCRIPT_DIR"


# ------------------------------------------------------------
# 1. Install prerequisites
# ------------------------------------------------------------
sudo apt-get update
sudo apt-get install -y wget apt-transport-https software-properties-common ufw nginx


# ------------------------------------------------------------
# 2. Add Microsoft Repo if Missing
# ------------------------------------------------------------
if [ ! -f /etc/apt/sources.list.d/microsoft-prod.list ]; then
    echo "Adding Microsoft .NET repository..."
    sudo wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb \
      -O packages-microsoft-prod.deb

    sudo dpkg -i packages-microsoft-prod.deb
    sudo rm packages-microsoft-prod.deb
    sudo apt-get update
fi


# ------------------------------------------------------------
# 3. Install .NET SDK/Runtime if Missing
# ------------------------------------------------------------
if ! command -v dotnet &>/dev/null || ! dotnet --list-sdks | grep -q "8\."; then
    echo "Installing .NET 8 SDK & Runtime..."
    sudo apt-get install -y dotnet-sdk-8.0 dotnet-runtime-8.0 aspnetcore-runtime-8.0
fi


# ------------------------------------------------------------
# 4. Configure UFW Firewall
# ------------------------------------------------------------
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 5000/tcp
sudo ufw --force enable


# ------------------------------------------------------------
# 5. Ensure NuGet Cache Is Writable
# ------------------------------------------------------------
DOTNET_NUGET_CACHE="/tmp/.nuget/packages"
sudo mkdir -p "$DOTNET_NUGET_CACHE"
sudo chown -R "$CI_USER:$CI_USER" "$DOTNET_NUGET_CACHE"
export NUGET_PACKAGES="$DOTNET_NUGET_CACHE"


# ------------------------------------------------------------
# 6. Fix permissions
# ------------------------------------------------------------
sudo chown -R "$CI_USER:$CI_USER" "$SCRIPT_DIR"
sudo mkdir -p "$APP_DIR"
sudo chown -R "$CI_USER:$CI_USER" "$APP_DIR"


# ------------------------------------------------------------
# 7. Clean previous build artifacts
# ------------------------------------------------------------
sudo -u "$CI_USER" dotnet clean "$SCRIPT_DIR"

# ------------------------------------------------------------
# 8. Restore, Build & Publish
# ------------------------------------------------------------
echo "Restoring dependencies..."
sudo -u "$CI_USER" dotnet workload update --skip-manifest-update || true
sudo -u "$CI_USER" dotnet restore "$SCRIPT_DIR" --no-cache

echo "Building application..."
sudo -u "$CI_USER" dotnet build "$SCRIPT_DIR" -c Release --no-restore

echo "Publishing application..."
BUILD_PATH="$SCRIPT_DIR/bin/Release/net8.0/linux-x64/publish"
sudo -u "$CI_USER" dotnet publish "$SCRIPT_DIR" -c Release -r linux-x64 --self-contained true \
  /p:PublishTrimmed=false -o "$BUILD_PATH"


# ------------------------------------------------------------
# 9. Deploy to target directory
# ------------------------------------------------------------
sudo mkdir -p "$APP_DIR"
# Remove any conflicting Agrovesto.Backend.API (dir or file) to avoid cp errors
if [ -e "$APP_DIR/$APP_NAME" ]; then
  sudo rm -rf "$APP_DIR/$APP_NAME"
fi
sudo cp -r "$BUILD_PATH/"* "$APP_DIR/"
sudo chown -R "$USER_NAME:$USER_NAME" "$APP_DIR"


# ------------------------------------------------------------
# 10. Environment Variables
# ------------------------------------------------------------
sudo tee "$ENV_FILE" > /dev/null <<EOF
ASPNETCORE_ENVIRONMENT=Production
DOTNET_PRINT_TELEMETRY_MESSAGE=false
DOTNET_CLI_HOME=/tmp
ASPNETCORE_URLS=http://0.0.0.0:5000
EOF


# ------------------------------------------------------------
# 11. Systemd Service
# ------------------------------------------------------------
sudo tee "$SERVICE_FILE" > /dev/null <<EOF
[Unit]
Description=$APP_NAME .NET Application
After=network.target

[Service]
WorkingDirectory=$APP_DIR
ExecStart=/usr/bin/dotnet $APP_DIR/$APP_NAME.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=$APP_NAME
User=$USER_NAME
EnvironmentFile=$ENV_FILE

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable "$APP_NAME.service"
sudo systemctl restart "$APP_NAME.service"


# ------------------------------------------------------------
# 12. NGINX Reverse Proxy
# ------------------------------------------------------------
sudo rm -f /etc/nginx/sites-enabled/default

NGINX_SITE="/etc/nginx/sites-available/$APP_NAME"

sudo tee "$NGINX_SITE" > /dev/null <<EOF
server {
    listen 443;
    server_name $LOAD_BALANCER_DNS _;

    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;

    location /health {
        proxy_pass http://127.0.0.1:5000/health;
        proxy_http_version 1.1;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }

    location / {
        proxy_pass http://127.0.0.1:5000/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_cache_bypass \$http_upgrade;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }
}
EOF

sudo ln -sf "$NGINX_SITE" /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx


# ------------------------------------------------------------
# Done
# ------------------------------------------------------------
echo "Deployment completed successfully!"
echo "Check service: sudo systemctl status $APP_NAME"
echo "Check logs: journalctl -u $APP_NAME -f"
