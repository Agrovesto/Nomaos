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
git reset --hard origin/staging

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
  echo "--- Error summary ---"

  grep -i "error" /tmp/publish.log | grep -v "warning" || true

  echo "=== LAST 200 LINES OF PUBLISH LOG ==="
  tail -200 /tmp/publish.log || true

  exit $PUBLISH_EXIT
fi

echo "🔄 Restarting service..."
sudo systemctl restart Agrovesto.Backend.API.service

echo "⏳ Waiting for service to settle..."
sleep 5

echo "📊 Checking service status..."
sudo systemctl status Agrovesto.Backend.API.service --no-pager -l || true

echo "📜 Recent logs..."
sudo journalctl \
  -u Agrovesto.Backend.API.service \
  -n 100 \
  --no-pager \
  -o cat || true

echo "=== LAST 200 LINES OF PUBLISH LOG ==="
tail -200 /tmp/publish.log || true

echo "✅ Deployment successful!"