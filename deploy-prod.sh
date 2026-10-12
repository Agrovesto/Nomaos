#!/usr/bin/env bash
# deploy-prod.sh  <commit-sha>
# ─────────────────────────────────────────────────────────────────────────────
# Runs on the EC2. Never compiles anything. The artifact was already rsync'd
# by GitHub Actions into /var/www/Nomaos/incoming/<sha>/
#
# What it does (all fast):
#   1. Symlink the new artifact into the idle slot
#   2. Restart only the idle slot's service (live slot keeps running)
#   3. Health-check the idle slot
#   4. Flip Nginx to the new slot atomically
#   5. Stop the old slot's service
#   6. Prune old releases (keep last 5 by sha)
# ─────────────────────────────────────────────────────────────────────────────
set -Eeuo pipefail

COMMIT_SHA="${1:?Usage: deploy-prod.sh <commit-sha>}"

BASE="/var/www/Nomaos"
INCOMING="$BASE/releases/$COMMIT_SHA"
NGINX_CONF="/etc/nginx/conf.d/nomaos_upstream.conf"
ACTIVE_FILE="$BASE/active-slot"
MAX_RELEASES=5

# Verify SHA (this should be the FIRST thing after variable declarations)
DEPLOYED_SHA=$(cat "$INCOMING/.commit-sha" 2>/dev/null || echo "")
if [[ "$DEPLOYED_SHA" != "$COMMIT_SHA" ]]; then
  echo "❌ SHA mismatch in artifact (expected $COMMIT_SHA, got $DEPLOYED_SHA)"
  exit 1
fi

# ── Determine slots ───────────────────────────────────────────────────────────
ACTIVE=$(cat "$ACTIVE_FILE" 2>/dev/null || echo "blue")

if [[ "$ACTIVE" == "blue" ]]; then
  TARGET="green"
  TARGET_PORT=5002
  ACTIVE_PORT=5000
else
  TARGET="blue"
  TARGET_PORT=5000
  ACTIVE_PORT=5002
fi

echo "🎯 Active: $ACTIVE ($ACTIVE_PORT)  →  Target: $TARGET ($TARGET_PORT)"
echo "📦 Commit: $COMMIT_SHA"

# ── 1. Symlink artifact into the idle slot ────────────────────────────────────
echo "🔗 Linking artifact → $TARGET slot..."
sudo ln -sfn "$INCOMING" "$BASE/$TARGET/current"

# ── 2. Restart the IDLE slot only (live slot untouched) ──────────────────────
echo "🔄 Restarting $TARGET slot..."
sudo systemctl restart "Agrovesto.Backend.API-$TARGET"

# ── 3. Health-check the idle slot (up to 30s) ────────────────────────────────
# HARD GATE: never flip Nginx to a slot that has not proven healthy.
# Aborting here leaves the live slot ($ACTIVE) serving and the candidate
# ($TARGET) on disk for inspection — zero impact on traffic.
echo "⏳ Health-checking $TARGET on port $TARGET_PORT..."

HEALTH_URL="http://127.0.0.1:$TARGET_PORT/health"
ATTEMPTS=0
MAX_ATTEMPTS=15   # 15 × 2s = 30s timeout

until curl -sf --max-time 3 "$HEALTH_URL" > /dev/null 2>&1; do
  ATTEMPTS=$(( ATTEMPTS + 1 ))
  if (( ATTEMPTS >= MAX_ATTEMPTS )); then
    echo "❌ Health check failed after ${MAX_ATTEMPTS} attempts — aborting before the traffic flip."
    echo "   Live slot $ACTIVE ($ACTIVE_PORT) is untouched."
    echo "   Inspect the candidate: journalctl -u Agrovesto.Backend.API-$TARGET"
    exit 1
  fi
  echo "   ... waiting ($ATTEMPTS/$MAX_ATTEMPTS)"
  sleep 2
done

echo "✅ $TARGET slot healthy"

# ── 4. Flip Nginx atomically ──────────────────────────────────────────────────
# Write to a temp file then move — avoids nginx reading a partial file
echo "🔀 Switching Nginx → $TARGET ($TARGET_PORT)..."

TMPCONF=$(mktemp)
cat > "$TMPCONF" <<EOF
upstream nomaos_backend {
    server 127.0.0.1:$TARGET_PORT;
    keepalive 32;
}
EOF

sudo mv "$TMPCONF" "$NGINX_CONF"
sudo chown root:root "$NGINX_CONF"
sudo nginx -t && sudo nginx -s reload

echo "🌐 Nginx now routing to $TARGET"

# ── 5. Update active-slot record ─────────────────────────────────────────────
echo "$TARGET" | sudo tee "$ACTIVE_FILE" > /dev/null

# ── 6. Stop the old slot (graceful — no downtime since Nginx already switched) ─
echo "🛑 Stopping old slot ($ACTIVE)..."
sudo systemctl stop "Agrovesto.Backend.API-$ACTIVE" || true

# ── 7. Prune old incoming artifacts (keep last MAX_RELEASES) ──────────────────
echo "🧹 Pruning old releases (keeping last $MAX_RELEASES)..."

# List by modification time (oldest first), skip current
mapfile -t OLD_RELEASES < <(
  ls -1t "$BASE/releases/" 2>/dev/null \
  | tail -n "+$(( MAX_RELEASES + 1 ))"
)

for OLD in "${OLD_RELEASES[@]}"; do
  # Safety: never delete the currently active symlink target
  CURRENT_TARGET=$(readlink -f "$BASE/$TARGET/current" 2>/dev/null || echo "")
  CANDIDATE="$BASE/releases/$OLD"
  if [[ "$CANDIDATE" == "$CURRENT_TARGET" ]]; then
    echo "   ⚠️  Skipping $OLD (currently active)"
    continue
  fi
  echo "   🗑  Removing $BASE/releases/$OLD"
  sudo rm -rf "$BASE/releases/$OLD"
done

echo "✅ Deployment successful!"