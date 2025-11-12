#!/bin/bash
# Deployment script for Agrovesto.Backend.API
# Author: Dolapo Fashina

APP_NAME="Agrovesto.Backend.API"
APP_PATH="/var/www/$APP_NAME"
SERVICE_FILE="/etc/systemd/system/$APP_NAME.service"
ENV_FILE="/etc/default/$APP_NAME"
BUILD_PATH="./bin/Release/net8.0/linux-x64/publish"
USER_NAME="www-data"
LOAD_BALANCER_DNS="agrovesto-alb-83128556.eu-west-1.elb.amazonaws.com"
# Install .NET 8.0.17 SDK and runtimes
DOTNET_VERSION="8.0.411"   # SDK version
RUNTIME_VERSION="8.0.17"   # Microsoft.NETCore.App runtime

set -e
export HOME=/tmp
export DOTNET_CLI_HOME=/tmp

echo "Starting deployment for $APP_NAME"

## 1. Install .NET SDK and runtime if missing
echo "Checking .NET SDK..."
if ! command -v dotnet &> /dev/null || ! dotnet --list-sdks | grep -q "8\."; then
    echo "Installing .NET 8 SDK and runtimes..."
    . /etc/os-release
    wget https://packages.microsoft.com/config/ubuntu/$VERSION_ID/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    sudo apt-get update
    sudo apt-get install -y dotnet-sdk-$DOTNET_VERSION dotnet-runtime-$RUNTIME_VERSION aspnetcore-runtime-$RUNTIME_VERSION
fi

## 2. Configure UFW
echo "Configuring firewall..."
sudo apt-get install -y ufw
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow 22/tcp 80/tcp 443/tcp 5000/tcp
sudo ufw --force enable
sudo ufw status verbose

## 3. Restore, build, and publish
echo "Restoring dependencies..."
dotnet restore --no-cache
echo "Building application..."
dotnet build -c Release --no-restore
echo "Publishing application (self-contained)..."
dotnet publish -c Release -r linux-x64 --self-contained true -o $BUILD_PATH

## 4. Deploy to target directory
echo "Preparing deployment directory..."
sudo mkdir -p $APP_PATH
sudo chown -R $USER_NAME:$USER_NAME $APP_PATH
echo "Copying published files..."
sudo cp -r $BUILD_PATH/* $APP_PATH/

## 5. Environment variables
echo "Creating environment file..."
sudo tee $ENV_FILE > /dev/null <<EOF
ASPNETCORE_ENVIRONMENT=Production
DOTNET_PRINT_TELEMETRY_MESSAGE=false
DOTNET_CLI_HOME=/tmp
ASPNETCORE_URLS=http://0.0.0.0:5000
EOF

## 6. Systemd service (self-contained binary)
echo "Creating systemd service..."
sudo tee $SERVICE_FILE > /dev/null <<EOF
[Unit]
Description=$APP_NAME self-contained .NET application
After=network.target

[Service]
WorkingDirectory=$APP_PATH
ExecStart=$APP_PATH/$APP_NAME
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=$APP_NAME
User=$USER_NAME
EnvironmentFile=$ENV_FILE

[Install]
WantedBy=multi-user.target
EOF

echo "Reloading and enabling service..."
sudo systemctl daemon-reload
sudo systemctl enable $APP_NAME.service

## 7. Nginx setup
echo "Configuring Nginx..."
sudo apt-get install -y nginx
sudo rm -f /etc/nginx/sites-enabled/default
NGINX_SITE="/etc/nginx/sites-available/$APP_NAME"
sudo tee $NGINX_SITE > /dev/null <<EOF
server {
    listen 80;
    server_name $LOAD_BALANCER_DNS _;

    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;

    location /health {
        proxy_pass http://127.0.0.1:5000/health;
        proxy_http_version 1.1;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }

    location / {
        proxy_pass http://127.0.0.1:5000/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_cache_bypass \$http_upgrade;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }
}
EOF

sudo ln -sf $NGINX_SITE /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart $APP_NAME.service
sudo systemctl restart nginx

echo "Deployment completed successfully!"
echo "Check service: sudo systemctl status $APP_NAME"
echo "Check logs: journalctl -u $APP_NAME -f"
