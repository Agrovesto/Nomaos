#!/usr/bin/env bash
set -Eeuo pipefail

echo "🚀 Blue/Green deployment starting..."

BASE_DIR="/var/www/Nomaos"
APP_NAME="Agrovesto.Backend.API"
PROJECT_FILE="/var/www/Nomaos/${APP_NAME}.csproj"

cd "$BASE_DIR"

# Ensure git is up to date
echo "📡 Syncing repository..."
git fetch origin
git checkout staging
git reset --hard origin/staging

# Ensure active-slot exists (CRITICAL FIX)
if [ ! -f "$BASE_DIR/active-slot" ]; then
  echo "blue" | sudo tee "$BASE_DIR/active-slot" > /dev/null
fi

ACTIVE=$(cat "$BASE_DIR/active-slot")
echo "🔵 Current active slot: $ACTIVE"

# Flip target safely
if [ "$ACTIVE" = "blue" ]; then
  TARGET="green"
  TARGET_PORT=5002
  ACTIVE_PORT=5000
else
  TARGET="blue"
  TARGET_PORT=5000
  ACTIVE_PORT=5002
fi

echo "📦 Deploying to slot: $TARGET"

TARGET_DIR="$BASE_DIR/$TARGET"

# Ensure directory exists cleanly
sudo rm -rf "$TARGET_DIR"
sudo mkdir -p "$TARGET_DIR"
sudo chown -R www-data:www-data "$TARGET_DIR"

echo "🏗️ Publishing .NET application..."

dotnet publish "$PROJECT_FILE" \
  -c Release \
  -r linux-x64 \
  -f net8.0 \
  --self-contained true \
  -o "$TARGET_DIR" \
  --verbosity minimal

# Verify publish output (CRITICAL FIX)
if [ ! -f "$TARGET_DIR/$APP_NAME.dll" ]; then
  echo "❌ Publish failed: DLL not found in $TARGET_DIR"
  exit 1
fi

echo "📂 Publish verified successfully"

echo "▶️ Starting target service..."
sudo systemctl start "Agrovesto.Backend.API-$TARGET.service"

echo "⏳ Running health checks on http://127.0.0.1:$TARGET_PORT/health"

HEALTH_OK=false

for i in {1..30}; do
  if curl -sf "http://127.0.0.1:$TARGET_PORT/health" > /dev/null; then
    HEALTH_OK=true
    break
  fi
  sleep 2
done

if [ "$HEALTH_OK" = false ]; then
  echo "❌ Health check failed — rolling back"

  sudo systemctl stop "Agrovesto.Backend.API-$TARGET.service" || true
  exit 1
fi

echo "✅ Target is healthy"

echo "🔁 Switching active slot to $TARGET"
echo "$TARGET" | sudo tee "$BASE_DIR/active-slot" > /dev/null

echo "🔄 Updating nginx upstream..."
sudo tee /etc/nginx/conf.d/nomaos_upstream.conf > /dev/null <<EOF
upstream nomaos_backend {
    server 127.0.0.1:$TARGET_PORT;
}
EOF

sudo nginx -s reload

echo "🧹 Stopping old service..."
sudo systemctl stop "Agrovesto.Backend.API-$ACTIVE.service" || true

echo "🎉 Deployment completed successfully (active: $TARGET)"