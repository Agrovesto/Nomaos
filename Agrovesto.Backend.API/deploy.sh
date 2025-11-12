#!/bin/bash

# Configuration
APP_NAME="agrovesto"
APP_PATH="/var/www/$APP_NAME"
SERVICE_FILE="/etc/systemd/system/$APP_NAME.service"
ENV_FILE="/etc/default/$APP_NAME"
BUILD_PATH="./bin/Release/net8.0/linux-x64/publish"
USER_NAME="www-data"

# Exit on error
set -e

echo "Starting deployment process for $APP_NAME"

## 1. Restore dependencies
echo "Restoring NuGet packages..."
dotnet restore

## 2. Build the application
echo "Building application..."
dotnet build -c Release --no-restore

## 3. Publish the application
echo "Publishing application..."
dotnet publish -c Release -r linux-x64 --self-contained false -o $BUILD_PATH

## 4. Prepare deployment directory
echo "Preparing deployment directory..."
sudo mkdir -p $APP_PATH
sudo chown -R $USER_NAME:$USER_NAME $APP_PATH

## 5. Copy published files
echo "Copying published files..."
sudo cp -r $BUILD_PATH/* $APP_PATH/

## 6. Create environment file (if needed)
echo "Creating environment file..."
sudo tee $ENV_FILE > /dev/null <<EOF
# Application environment variables
ASPNETCORE_ENVIRONMENT=Production
DOTNET_PRINT_TELEMETRY_MESSAGE=false
# Add other variables here or use your existing .env file
EOF

## 7. Create systemd service file
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

## 8. Reload and enable service
echo "Configuring systemd service..."
sudo systemctl daemon-reload
sudo systemctl enable $APP_NAME.service

## 9. Restart the service
echo "Restarting application service..."
sudo systemctl restart $APP_NAME.service

echo "Deployment completed successfully!"
echo "Application is running and will start automatically on system boot."
echo "To check status: sudo systemctl status $APP_NAME"
echo "To view logs: journalctl -u $APP_NAME -f"
