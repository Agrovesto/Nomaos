#!/bin/bash

set -euo pipefail

IMAGE="$1"

APP_NAME="agrovesto"
STATE_FILE="/opt/agrovesto/current"
NGINX_UPSTREAM="/etc/nginx/conf.d/upstream.conf"

ACTIVE=$(cat "$STATE_FILE" 2>/dev/null || echo "blue")

if [ "$ACTIVE" = "blue" ]; then
    NEW="green"
    OLD="blue"
    NEW_PORT=5006
else
    NEW="blue"
    OLD="green"
    NEW_PORT=5005
fi

echo "====================================="
echo "Current: $ACTIVE"
echo "Deploying: $NEW"
echo "Image: $IMAGE"
echo "====================================="

# Pull latest image
docker pull "$IMAGE"

# Remove failed/stale deployment container
docker rm -f "${APP_NAME}-${NEW}" >/dev/null 2>&1 || true

echo "Starting new container..."

docker run -d \
  --name "${APP_NAME}-${NEW}" \
  --restart unless-stopped \
  -p ${NEW_PORT}:5005 \
  -e ASPNETCORE_ENVIRONMENT=Staging \
  "$IMAGE"

echo "Waiting for application startup..."

HEALTHY=false

for i in {1..30}; do

    if curl -fs "http://localhost:${NEW_PORT}/health" >/dev/null 2>&1; then
        HEALTHY=true
        echo "Health check passed"
        break
    fi

    echo "Attempt $i/30 - waiting..."
    sleep 5
done

if [ "$HEALTHY" != "true" ]; then

    echo "Deployment failed"

    docker logs "${APP_NAME}-${NEW}" || true

    docker rm -f "${APP_NAME}-${NEW}" || true

    exit 1
fi

echo "Switching nginx..."

cat > "$NGINX_UPSTREAM" <<EOF
upstream agrovesto_api {
    server 127.0.0.1:${NEW_PORT};
}
EOF

nginx -t

systemctl reload nginx

echo "Traffic switched"

sleep 10

echo "Stopping old container..."

docker rm -f "${APP_NAME}-${OLD}" >/dev/null 2>&1 || true

echo "$NEW" > "$STATE_FILE"

echo "Deployment successful"