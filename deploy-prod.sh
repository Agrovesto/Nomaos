#!/usr/bin/env bash
# ============================================================
# Agrovesto Backend API — Production API Deployment Script
# Zero-downtime oriented, rollback-capable, idempotent
# ============================================================

set -Eeuo pipefail

echo "🚀 Starting deployment..."

cd /var/www/Nomaos

echo "📥 Pulling latest code..."
git fetch origin
git checkout staging
git reset --hard origin/master

echo "🛑 Stopping service..."
sudo systemctl stop Agrovesto.Backend.API.service

echo "📦 Restoring dependencies..."
dotnet restore --force --verbosity minimal

echo "🧹 Cleaning project..."
dotnet clean

echo "🗑️ Wiping stale publish folder..."
rm -rf /var/www/Nomaos/publish/*

echo "🏗️ Publishing application..."

dotnet publish \
  -c Release \
  -f net8.0 \
  -o /var/www/Nomaos/publish \
  --verbosity minimal \
  2>&1 | tee /tmp/publish.log

PUBLISH_EXIT=${PIPESTATUS[0]}

if [ $PUBLISH_EXIT -ne 0 ]; then
  echo "❌ Publish failed! Exit code: $PUBLISH_EXIT"

  echo "=== FULL PUBLISH LOG ==="
  cat /tmp/publish.log || true

  exit $PUBLISH_EXIT
fi

echo "🔄 Restarting service..."
sudo systemctl restart Agrovesto.Backend.API.service
echo ">>> Waiting for service to start..."
SERVICE_ACTIVE=0
for i in {1..30}; do
    if sudo systemctl is-active --quiet Agrovesto.Backend.API.service; then
        echo "✅ Service is active"
        SERVICE_ACTIVE=1
        break
    fi
    sleep 1
done

if [ $SERVICE_ACTIVE -eq 0 ]; then
    echo "❌ Service did not become active after 30s"
    echo "📊 Checking service status for debugging..."
    sudo systemctl status Agrovesto.Backend.API.service --no-pager || true
    echo "❌ Service failed to start! Rolling back..."
    git reset --hard HEAD@{1}
    dotnet publish -c Release -o /var/www/Nomaos/publish --no-restore
    sudo systemctl restart Agrovesto.Backend.API.service
    exit 1
fi

sudo systemctl status Agrovesto.Backend.API.service --no-pager

echo "📜 Recent logs..."
sudo journalctl \
  -u Agrovesto.Backend.API.service \
  -n 100 \
  --no-pager \
  -o cat || true

echo "=== FULL PUBLISH LOG ==="
cat /tmp/publish.log || true

echo "✅ Deployment successful!"
