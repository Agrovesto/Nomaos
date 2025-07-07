#!/bin/bash

# Configuration
APP_NAME="Agrovesto.Backend.API"
APP_PATH="/var/www/$APP_NAME"
SERVICE_FILE="/etc/systemd/system/$APP_NAME.service"
ENV_FILE="/etc/default/$APP_NAME"
BUILD_PATH="./bin/Release/net8.0/"
USER_NAME="www-data"
# Replace with your actual load balancer DNS name
LOAD_BALANCER_DNS="agrovesto-alb-83128556.eu-west-1.elb.amazonaws.com"

# Exit on error
set -e

# Fix for missing HOME env in CI environments like CodePipeline
export HOME=/tmp
export DOTNET_CLI_HOME=/tmp

echo "Starting deployment process for $APP_NAME"

## 1. Install .NET 8 SDK (if not already installed)
echo "Checking and installing .NET 8 SDK..."
if ! command -v dotnet &> /dev/null || ! dotnet --list-sdks | grep -q "8\."; then
    echo "Installing .NET 8 SDK..."
    
    # Get Ubuntu version
    . /etc/os-release
    
    # Download Microsoft signing key and repository
    wget https://packages.microsoft.com/config/ubuntu/$VERSION_ID/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    
    # Update package list and install .NET 8 SDK
    sudo apt-get update
    sudo apt-get install -y dotnet-sdk-8.0
    sudo apt-get install -y aspnetcore-runtime-8.0
    sudo apt-get install -y dotnet-runtime-8.0
    
    echo ".NET 8 SDK installed successfully"
else
    echo ".NET 8 SDK already installed"
fi

## 2. Configure UFW (Uncomplicated Firewall)
echo "Configuring UFW firewall..."
if ! command -v ufw &> /dev/null; then
    echo "Installing UFW..."
    sudo apt-get update
    sudo apt-get install -y ufw
fi

# Set default policies
sudo ufw default deny incoming
sudo ufw default allow outgoing

# Allow SSH (important for remote access)
sudo ufw allow 22/tcp

# Allow HTTP (port 80) - for nginx
sudo ufw allow 82/tcp

# Allow HTTPS (port 443) - for nginx
sudo ufw allow 443/tcp

# DON'T expose port 5000 externally - it should only be accessible internally
# sudo ufw allow 5000/tcp  # REMOVED - we want nginx to handle external traffic

# Enable UFW
echo "Enabling UFW..."
sudo ufw enable

echo "UFW configured and enabled"
sudo ufw status verbose

## 3. Restore dependencies
echo "Restoring NuGet packages..."
dotnet restore

## 4. Build the application
echo "Building application..."
dotnet build -c Release --no-restore

## 5. Publish the application
echo "Publishing application..."
dotnet publish -c Release -r linux-x64 --self-contained false -o $BUILD_PATH

## 6. Prepare deployment directory
echo "Preparing deployment directory..."
sudo mkdir -p $APP_PATH
sudo chown -R $USER_NAME:$USER_NAME $APP_PATH

## 7. Copy published files
echo "Copying published files..."
sudo cp -r $BUILD_PATH/* $APP_PATH/

## 8. Create environment file (IMPROVED)
echo "Creating environment file..."
sudo tee $ENV_FILE > /dev/null <<EOF
# Application environment variables
ASPNETCORE_ENVIRONMENT=Production
DOTNET_PRINT_TELEMETRY_MESSAGE=false
DOTNET_CLI_HOME=/tmp
# IMPORTANT: Bind to all interfaces, not just localhost
ASPNETCORE_URLS=http://0.0.0.0:5000
# Add other variables here or use your existing .env file
EOF

## 9. Create systemd service file
echo "Creating systemd service..."
sudo tee $SERVICE_FILE > /dev/null <<EOF
[Unit]
Description=$APP_NAME .NET Application
After=network.target

[Service]
WorkingDirectory=$APP_PATH
ExecStart=/usr/bin/dotnet $APP_PATH/$APP_NAME.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=$APP_NAME
User=$USER_NAME
EnvironmentFile=$ENV_FILE

[Install]
WantedBy=multi-user.target
EOF

## 10. Reload and enable service
echo "Configuring systemd service..."
sudo systemctl daemon-reload
sudo systemctl enable $APP_NAME.service

## 11. Install and configure Nginx
echo "Installing and configuring Nginx..."

# Install Nginx if not already installed
if ! command -v nginx &> /dev/null; then
    sudo apt-get update
    sudo apt-get install -y nginx
fi

# Remove default site
sudo rm -f /etc/nginx/sites-enabled/default

# Create Nginx config file (IMPROVED)
NGINX_SITE="/etc/nginx/sites-available/$APP_NAME"
sudo tee $NGINX_SITE > /dev/null <<EOF
server {
    listen 82;
    server_name $LOAD_BALANCER_DNS _;
    
    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    
    # Health check endpoint for load balancer
    location /health {
        proxy_pass         http://127.0.0.1:5000/health;
        proxy_http_version 1.1;
        proxy_set_header   Host \$host;
        proxy_set_header   X-Real-IP \$remote_addr;
        proxy_set_header   X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto \$scheme;
        proxy_connect_timeout 5s;
        proxy_send_timeout 5s;
        proxy_read_timeout 5s;
    }
    
    # Main application
    location / {
        proxy_pass         http://127.0.0.1:5000/swagger/;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade \$http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host \$host;
        proxy_cache_bypass \$http_upgrade;
        proxy_set_header   X-Real-IP \$remote_addr;
        proxy_set_header   X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto \$scheme;
        
        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
        
        # Buffer settings
        proxy_buffer_size 4k;
        proxy_buffers 8 4k;
        proxy_busy_buffers_size 8k;
    }
}
EOF

# Enable site
sudo ln -sf $NGINX_SITE /etc/nginx/sites-enabled/

# Test nginx configuration
sudo nginx -t

# Restart services
echo "Restarting services..."
sudo systemctl restart $APP_NAME.service
sudo systemctl restart nginx

# Wait for application to start
echo "Waiting for application to start..."
sleep 5

echo "Deployment completed successfully!"
echo "Application is running and will start automatically on system boot."
echo ""
echo "=== Status Information ==="
echo "To check application status: sudo systemctl status $APP_NAME"
echo "To view application logs: journalctl -u $APP_NAME -f"
echo "To check nginx status: sudo systemctl status nginx"
echo "To view nginx logs: journalctl -u nginx -f"
echo ""
echo "=== Testing Local Connection ==="
echo "Testing local connection to application..."
curl -I http://localhost:5000 || echo "Local connection test failed"
echo ""
echo "=== Firewall Status ==="
sudo ufw status numbered
echo ""
echo "=== .NET Version ==="
dotnet --version

