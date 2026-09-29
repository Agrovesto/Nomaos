#!/bin/bash
set -euo pipefail

IMAGE=$1

APP_NAME="nomaos-api"
NEW_CONTAINER="${APP_NAME}_new"
OLD_CONTAINER="${APP_NAME}_old"
PORT=5005

echo "=============================="
echo "Deploying: $IMAGE"
echo "=============================="

echo "Logging into ECR..."
REGISTRY=$(echo "$IMAGE" | cut -d/ -f1)

aws ecr get-login-password --region eu-west-1 \
  | docker login --username AWS --password-stdin $REGISTRY

echo "Pulling image..."
docker pull "$IMAGE"

echo "Starting new container..."
docker run -d \
  --name "$NEW_CONTAINER" \
  --restart unless-stopped \
  -p $PORT:$PORT \
  "$IMAGE"

echo "Waiting for health check..."
for i in {1..30}; do
  if curl -fs http://localhost:$PORT/health; then
    echo "Health check passed"
    break
  fi
  sleep 2
done

echo "Switching traffic..."

# Rename current running container to old
if docker ps --format '{{.Names}}' | grep -q "^$APP_NAME$"; then
  docker rename "$APP_NAME" "$OLD_CONTAINER" || true
fi

# Promote new container
docker rename "$NEW_CONTAINER" "$APP_NAME"

echo "Stopping old container..."
docker stop "$OLD_CONTAINER" || true
docker rm "$OLD_CONTAINER" || true

echo "Cleaning unused images..."
docker image prune -f || true

echo "Deployment successful"