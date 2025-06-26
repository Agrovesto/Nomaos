#!/bin/bash

# Configuration
APP_NAME="Agrovesto.Backend.API"
APP_PATH="/var/www/$APP_NAME"
SERVICE_FILE="/etc/systemd/system/$APP_NAME.service"
ENV_FILE="/etc/default/$APP_NAME"
BUILD_PATH="./bin/Release/net8.0/"
USER_NAME="www-data"

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

# Reset UFW to default settings (optional - comment out if you have existing rules)
# sudo ufw --force reset

# Set default policies
sudo ufw default deny incoming
sudo ufw default allow outgoing

# Allow HTTP (port 80)
sudo ufw allow 80/tcp

# Allow HTTPS (port 443) - recommended for production
sudo ufw allow 443/tcp

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

## 8. Create environment file (if needed)
echo "Creating environment file..."
sudo tee $ENV_FILE > /dev/null <<EOF
# Application environment variables
ASPNETCORE_ENVIRONMENT=Production
DOTNET_PRINT_TELEMETRY_MESSAGE=false
DOTNET_CLI_HOME=/tmp
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

## 11. Restart the service
echo "Restarting application service..."
sudo systemctl restart $APP_NAME.service

echo "Deployment completed successfully!"
echo "Application is running and will start automatically on system boot."
echo ""
echo "=== Status Information ==="
echo "To check application status: sudo systemctl status $APP_NAME"
echo "To view application logs: journalctl -u $APP_NAME -f"
echo ""
echo "=== Firewall Status ==="
sudo ufw status numbered
echo ""
echo "=== .NET Version ==="
dotnet --version