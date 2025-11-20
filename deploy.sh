#!/bin/bash
set -e
set -x

# ----- Update AWS RDS Certificate -----
sudo wget https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem \
  -O /usr/local/share/ca-certificates/rds-global-bundle.crt
sudo update-ca-certificates

APP_NAME="Agrovesto.Backend.API"
APP_DIR="/var/www/$APP_NAME"
SERVICE_FILE="/etc/systemd/system/$APP_NAME.service"
ENV_FILE="/etc/default/$APP_NAME"
USER_NAME="www-data"
LOAD_BALANCER_DNS="agrovesto-alb-710830026.eu-west-1.elb.amazonaws.com"

echo "Starting deployment for $APP_NAME"

# --- Ensure prerequisites ---
sudo apt-get update
sudo apt-get install -y nginx ufw wget software-properties-common

# --- Ensure .NET Runtime Installed ---
if ! command -v dotnet &> /dev/null; then
    echo "Installing Microsoft repo & .NET runtime..."
    sudo wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    sudo apt-get update
    sudo apt-get install -y aspnetcore-runtime-8.0
fi

# --- Configure UFW ---
sudo ufw allow 22
sudo ufw allow 80
sudo ufw allow 443
sudo ufw allow 5000
sudo ufw --force enable

# --- Deploy Published Build from /tmp/app-publish ---
echo "Deploying published files..."
sudo mkdir -p "$APP_DIR"
sudo rm -rf "$APP_DIR"/*
sudo cp -r /tmp/app-publish/* "$APP_DIR"/
sudo chown -R $USER_NAME:$USER_NAME "$APP_DIR"

# --- Create Environment Variables ---
sudo tee "$ENV_FILE" > /dev/null <<EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:5000
DOTNET_PRINT_TELEMETRY_MESSAGE=false
EOF

# --- Create/Update systemd Service ---
sudo tee "$SERVICE_FILE" > /dev/null <<EOF
[Unit]
Description=$APP_NAME Service
After=network.target

[Service]
WorkingDirectory=$APP_DIR
ExecStart=$APP_DIR/$APP_NAME
Restart=always
RestartSec=10
User=$USER_NAME
EnvironmentFile=$ENV_FILE

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable $APP_NAME.service
sudo systemctl restart $APP_NAME.service

# --- Configure Nginx ---
sudo rm -f /etc/nginx/sites-enabled/default
NGINX_SITE="/etc/nginx/sites-available/$APP_NAME"

sudo tee "$NGINX_SITE" > /dev/null <<EOF
server {
    listen 80;
    server_name $LOAD_BALANCER_DNS _;

    location / {
        proxy_pass http://127.0.0.1:5000/;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }
}
EOF

sudo ln -sf "$NGINX_SITE" /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx

echo "Deployment completed successfully!"
