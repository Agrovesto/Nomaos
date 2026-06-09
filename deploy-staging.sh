#!/usr/bin/env bash
set -Eeuo pipefail

echo "🚀 Blue/Green Deployment Starting..."

BASE_DIR="/var/www/Nomaos"
RELEASES_DIR="$BASE_DIR/releases"

APP_NAME="Agrovesto.Backend.API"
PROJECT_FILE="$BASE_DIR/Agrovesto.Backend.API/$APP_NAME.csproj"

cd "$BASE_DIR"

# -----------------------------
# 1. Sync repo
# -----------------------------
git fetch origin
git checkout staging
git reset --hard origin/staging

# -----------------------------
# 2. Determine slot
# -----------------------------
if [ ! -f "$BASE_DIR/active-slot" ]; then
  echo "blue" | sudo tee "$BASE_DIR/active-slot" > /dev/null
fi

ACTIVE=$(cat "$BASE_DIR/active-slot")

if [ "$ACTIVE" = "blue" ]; then
  TARGET="green"
  TARGET_PORT=5002
else
  TARGET="blue"
  TARGET_PORT=5000
fi

echo "📌 Active: $ACTIVE"
echo "🎯 Target: $TARGET"

# -----------------------------
# 3. Create immutable release
# -----------------------------
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
RELEASE_DIR="$RELEASES_DIR/$TIMESTAMP"

mkdir -p "$RELEASE_DIR"

echo "🏗️ Publishing..."
dotnet publish "$PROJECT_FILE" \
  -c Release \
  -r linux-x64 \
  -f net8.0 \
  --self-contained true \
  -o "$RELEASE_DIR"

if [ ! -f "$RELEASE_DIR/$APP_NAME.dll" ]; then
  echo "❌ Publish failed"
  exit 1
fi

echo "📦 Release ready: $RELEASE_DIR"

# -----------------------------
# 4. Switch slot pointer FIRST (safe because no traffic yet)
# -----------------------------
echo "$TARGET" | sudo tee "$BASE_DIR/active-slot" > /dev/null

# IMPORTANT: update SLOT symlink (NOT global current)
SLOT_DIR="$BASE_DIR/$TARGET"
ln -sfn "$RELEASE_DIR" "$SLOT_DIR/current"

echo "🔗 Slot updated: $TARGET → $RELEASE_DIR"

# -----------------------------
# 5. Restart only target service
# -----------------------------
sudo systemctl restart "Agrovesto.Backend.API-$TARGET.service"

# -----------------------------
# 6. Health check
# -----------------------------
HEALTH_OK=false

for i in {1..30}; do
  if curl -sf "http://127.0.0.1:$TARGET_PORT/health"; then
    HEALTH_OK=true
    break
  fi
  sleep 2
done

if [ "$HEALTH_OK" = false ]; then
  echo "❌ Health failed — rolling back"

  OLD_RELEASE=$(ls -dt "$RELEASES_DIR"/* | sed -n '2p' || true)

  if [ -n "$OLD_RELEASE" ]; then
    ln -sfn "$OLD_RELEASE" "$BASE_DIR/$TARGET/current"
  fi

  sudo systemctl restart "Agrovesto.Backend.API-$TARGET.service"
  exit 1
fi

echo "✅ Healthy"

# -----------------------------
# 7. Update nginx
# -----------------------------
sudo tee /etc/nginx/conf.d/nomaos_upstream.conf > /dev/null <<EOF
upstream nomaos_backend {
    server 127.0.0.1:$TARGET_PORT;
}
EOF

sudo nginx -s reload

# -----------------------------
# 8. Stop old slot
# -----------------------------
sudo systemctl stop "Agrovesto.Backend.API-$ACTIVE.service" || true

echo "🎉 Deployment successful: $TARGET"