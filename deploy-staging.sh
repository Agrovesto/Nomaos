#!/usr/bin/env bash
set -Eeuo pipefail

echo "🚀 Blue/Green deployment starting..."

cd /var/www/Nomaos

git fetch origin
git checkout staging
git reset --hard origin/staging

ACTIVE=$(cat active-slot || echo blue)

if [ "$ACTIVE" = "blue" ]; then
  TARGET="green"
  TARGET_PORT=5002
  ACTIVE_PORT=5000
else
  TARGET="blue"
  TARGET_PORT=5000
  ACTIVE_PORT=5002
fi

echo "📦 Deploying to: $TARGET"

TARGET_DIR="/var/www/Nomaos/$TARGET"

rm -rf "$TARGET_DIR"
mkdir -p "$TARGET_DIR"

echo "🏗️ Publishing..."
dotnet publish \
  -c Release \
  -r linux-x64 \
  -f net8.0 \
  --self-contained true \
  -o "$TARGET_DIR" \
  --verbosity minimal

echo "▶️ Starting target service..."
sudo systemctl start Agrovesto-$TARGET.service

echo "⏳ Health check..."
for i in {1..30}; do
  if curl -sf "http://127.0.0.1:$TARGET_PORT/health"; then
    echo "✅ Target healthy"
    break
  fi
  sleep 2
done

if ! curl -sf "http://127.0.0.1:$TARGET_PORT/health"; then
  echo "❌ Health check failed — rolling back"

  sudo systemctl stop Agrovesto-$TARGET.service || true
  exit 1
fi

echo "🔁 Switching traffic to $TARGET"

echo "$TARGET" | sudo tee /var/www/Nomaos/active-slot

sudo tee /etc/nginx/conf.d/agrovesto_upstream.conf > /dev/null <<EOF
upstream agrovesto_backend {
    server 127.0.0.1:$TARGET_PORT;
}
EOF

sudo nginx -s reload

echo "🧹 Stopping old service..."
sudo systemctl stop Agrovesto-$ACTIVE.service || true

echo "🎉 Deployment successful (active: $TARGET)"