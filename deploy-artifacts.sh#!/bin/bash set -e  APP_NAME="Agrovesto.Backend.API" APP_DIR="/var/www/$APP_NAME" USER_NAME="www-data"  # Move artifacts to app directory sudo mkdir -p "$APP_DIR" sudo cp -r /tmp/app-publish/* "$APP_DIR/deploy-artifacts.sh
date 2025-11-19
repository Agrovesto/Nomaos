#!/bin/bash
set -e

APP_NAME="Agrovesto.Backend.API"
APP_DIR="/var/www/$APP_NAME"
USER_NAME="www-data"

# Move artifacts to app directory
sudo mkdir -p "$APP_DIR"
sudo cp -r /tmp/app-publish/* "$APP_DIR/"
sudo chown -R $USER_NAME:$USER_NAME "$APP_DIR"

# Restart service
sudo systemctl restart "$APP_NAME.service"
