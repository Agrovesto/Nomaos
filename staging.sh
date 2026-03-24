#!/bin/bash
set -euo pipefail
set -x

APP_NAME="Agrovesto.Backend.API"
APP_DIR="/var/www/$APP_NAME"
INCOMING_DIR="$APP_DIR/incoming"
RELEASES_DIR="$APP_DIR/releases"
RELEASE_ID="$(date +%Y%m%d%H%M%S)"
RELEASE_DIR="$RELEASES_DIR/$RELEASE_ID"
CURRENT_DIR="$APP_DIR/current"
PREVIOUS_DIR="$APP_DIR/previous"
USER_NAME="www-data"

echo "🚀 Deploying $APP_NAME — release $RELEASE_ID"

# ------------------------------------------------------------
# Prepare release
# ------------------------------------------------------------
mkdir -p "$RELEASE_DIR"
cp -r "$INCOMING_DIR"/* "$RELEASE_DIR"/

chmod +x "$RELEASE_DIR/$APP_NAME" || true

# ------------------------------------------------------------
# Save previous release
# ------------------------------------------------------------
if [ -L "$CURRENT_DIR" ]; then
  ln -sfn "$(readlink -f "$CURRENT_DIR")" "$PREVIOUS_DIR"
fi

# ------------------------------------------------------------
# Atomic switch
# ------------------------------------------------------------
ln -sfn "$RELEASE_DIR" "$CURRENT_DIR"
chown -R "$USER_NAME:$USER_NAME" "$RELEASE_DIR"

systemctl restart "$APP_NAME"

# ------------------------------------------------------------
# Cleanup old releases (keep last 3)
# ------------------------------------------------------------
ls -1dt "$RELEASES_DIR"/* | tail -n +4 | xargs rm -rf || true

echo "✅ Deployment successful"
