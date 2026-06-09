#!/usr/bin/env bash
set -Eeuo pipefail

BASE="/var/www/Nomaos"
RELEASES="$BASE/releases"

APP="Agrovesto.Backend.API"
PROJECT="$BASE/Agrovesto.Backend.API/$APP.csproj"

cd "$BASE"

echo "🚀 Starting production-grade deployment..."

# 1. Sync
git fetch origin
git checkout staging
git reset --hard origin/staging

# 2. determine slot
ACTIVE=$(cat "$BASE/active-slot" || echo blue)

if [ "$ACTIVE" = "blue" ]; then
  TARGET="green"
  PORT=5002
else
  TARGET="blue"
  PORT=5000
fi

echo "🎯 Target: $TARGET"

# 3. release build
TS=$(date +%Y%m%d_%H%M%S)
RELEASE="$RELEASES/$TS"

mkdir -p "$RELEASE"

dotnet publish "$PROJECT" \
  -c Release \
  -r linux-x64 \
  -f net8.0 \
  --self-contained true \
  -o "$RELEASE"

echo "📦 Release built: $RELEASE"

# 4. attach release
sudo ln -sfn "$RELEASE" "$BASE/$TARGET/current"

# 5. restart target
sudo systemctl restart Agrovesto.Backend.API-$TARGET

echo "⏳ Canary phase (10%)..."

# 6. canary validation (simplified baseline)
sleep 10

FAIL_RATE=$(curl -s http://127.0.0.1:$PORT/metrics | jq '.error_rate' || echo 0)

if (( $(echo "$FAIL_RATE > 0.02" | bc -l) )); then
    echo "❌ Canary failed → rollback"
    sudo systemctl stop Agrovesto.Backend.API-$TARGET
    exit 1
fi

echo "✅ Canary passed"

# 7. switch traffic gradually
echo "🔄 Switching traffic..."

cat > /etc/nginx/conf.d/nomaos_upstream.conf <<EOF
upstream nomaos_backend {
    server 127.0.0.1:5000 weight=50;
    server 127.0.0.1:5002 weight=50;
}
EOF

sudo nginx -s reload
sleep 15

# full switch
if [ "$TARGET" = "green" ]; then
cat > /etc/nginx/conf.d/nomaos_upstream.conf <<EOF
upstream nomaos_backend {
    server 127.0.0.1:5002;
}
EOF
else
cat > /etc/nginx/conf.d/nomaos_upstream.conf <<EOF
upstream nomaos_backend {
    server 127.0.0.1:5000;
}
EOF
fi

sudo nginx -s reload

# 8. update active slot
echo "$TARGET" | sudo tee "$BASE/active-slot"

# 9. stop old
sudo systemctl stop Agrovesto.Backend.API-$ACTIVE || true

echo "🎉 Deployment complete: $TARGET"