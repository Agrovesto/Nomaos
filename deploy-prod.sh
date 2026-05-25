#!/bin/bash
# Robust Deployment Script for Agrovesto.Backend.API
# Author: Dolapo Fashina

set -euo pipefail
set -x

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
DOMAIN="noma-api.nomaos.com"

PUBLISH_DIR="$APP_DIR/publish"
CURRENT_DIR="$APP_DIR/current"

# Fix home for dotnet/NuGet in CI
export HOME=/tmp
export DOTNET_CLI_HOME=/tmp
export NUGET_PACKAGES="/tmp/.nuget/packages"

echo "Starting deployment for $APP_NAME → https://$DOMAIN"

# ------------------------------------------------------------
# 1. Install .NET 8 (only if missing)
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
# 2. Firewall
# ------------------------------------------------------------
sudo ufw allow 22/tcp || true
sudo ufw allow 80/tcp || true
sudo ufw allow 443/tcp || true
sudo ufw --force enable || true

# ------------------------------------------------------------
# 3. Stop existing service
# ------------------------------------------------------------
sudo systemctl stop "$APP_NAME" || true

# ------------------------------------------------------------
# 4. Prepare directories
# ------------------------------------------------------------
sudo mkdir -p "$APP_DIR"
sudo rm -rf "$PUBLISH_DIR"
sudo mkdir -p "$PUBLISH_DIR"

sudo chown -R "$CI_USER:$CI_USER" "$SCRIPT_DIR" "$APP_DIR"

# ------------------------------------------------------------
# 5. Build & Publish (.NET 8 self-contained)
# ------------------------------------------------------------
sudo -u "$CI_USER" dotnet clean "$SCRIPT_DIR" -c Release

sudo -u "$CI_USER" dotnet restore "$SCRIPT_DIR" \
  --runtime linux-x64 \
  --no-cache

sudo -u "$CI_USER" dotnet publish "$SCRIPT_DIR" \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  --no-restore \
  -o "$PUBLISH_DIR" \
  /p:PublishTrimmed=false \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true

# Ensure executable permission
sudo chmod +x "$PUBLISH_DIR/$APP_NAME"

# ------------------------------------------------------------
# 6. Atomic symlink update
# ------------------------------------------------------------
sudo ln -sfn "$PUBLISH_DIR" "$CURRENT_DIR"

sudo chown -R "$USER_NAME:$USER_NAME" "$APP_DIR"
sudo chown -h "$USER_NAME:$USER_NAME" "$CURRENT_DIR"

# ------------------------------------------------------------
# 7. Environment file
# ------------------------------------------------------------
sudo tee "$ENV_FILE" > /dev/null <<EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:5000
DOTNET_PRINT_TELEMETRY_MESSAGE=false
EOF

# ------------------------------------------------------------
# 8. Systemd service
# ------------------------------------------------------------
sudo tee "$SERVICE_FILE" > /dev/null <<EOF
[Unit]
Description=$APP_NAME
After=network.target

[Service]
WorkingDirectory=$CURRENT_DIR
ExecStart=$CURRENT_DIR/$APP_NAME
Restart=always
RestartSec=10
KillSignal=SIGINT
User=$USER_NAME
EnvironmentFile=$ENV_FILE
SyslogIdentifier=$APP_NAME

[Install]
WantedBy=multi-user.target
EOF

# ------------------------------------------------------------
# 9. Start service
# ------------------------------------------------------------
sudo systemctl daemon-reload
sudo systemctl enable "$APP_NAME"
sudo systemctl restart "$APP_NAME"

# ------------------------------------------------------------
# 10. Status check
# ------------------------------------------------------------
sudo systemctl status "$APP_NAME" --no-pager
