#!/bin/bash
# Robust Deployment Script for Agrovesto.Backend.API
# Author: Dolapo Fashina 
set -euo pipefail
set -x

APP_NAME="Agrovesto.Backend.API"
APP_DIR="/var/www/$APP_NAME"
RELEASES_DIR="$APP_DIR/releases"
RELEASE_ID="$(date +%Y%m%d%H%M%S)"
RELEASE_DIR="$RELEASES_DIR/$RELEASE_ID"
CURRENT_DIR="$APP_DIR/current"
PREVIOUS_DIR="$APP_DIR/previous"
USER_NAME="www-data"
CI_USER="ubuntu"

export HOME=/tmp
export DOTNET_CLI_HOME=/tmp
export NUGET_PACKAGES="/tmp/.nuget/packages"

echo "🚀 Deploying $APP_NAME — release $RELEASE_ID"

mkdir -p "$RELEASE_DIR"
sudo chown -R "$CI_USER:$CI_USER" "$APP_DIR"

# ------------------------------------------------------------
# Build & publish
# ------------------------------------------------------------
dotnet publish \
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
# Health gate
# ------------------------------------------------------------
sleep 5
if ! curl -fs http://127.0.0.1:5000/health; then
  echo "❌ Health check failed — rolling back"
  sudo ln -sfn "$(readlink -f "$PREVIOUS_DIR")" "$CURRENT_DIR"
  sudo systemctl restart "$APP_NAME"
  exit 1
fi

# ------------------------------------------------------------
# Cleanup old releases (keep last 3)
# ------------------------------------------------------------
ls -1dt "$RELEASES_DIR"/* | tail -n +4 | sudo xargs rm -rf || true

echo "✅ Deployment successful"



