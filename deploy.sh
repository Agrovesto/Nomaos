#!/bin/bash
# Robust Deployment Script for Nomaos.API
# Author: Dolapo Fashina

set -e
set -x   # Command tracing

# --- Dry-run mode ---
DRY_RUN=false
if [[ "$1" == "--dry-run" ]]; then
    DRY_RUN=true
    echo "Dry-run mode enabled: deployment steps will be skipped"
fi

# --- Configuration ---
APP_NAME="Agrovesto.Backend.API"
APP_DIR="/var/www/$APP_NAME"
REPO_DIR="/home/ubuntu/agrovesto-backend"  # ← ADD THIS
SERVICE_FILE="/etc/systemd/system/$APP_NAME.service"
ENV_FILE="/etc/default/$APP_NAME"
USER_NAME="www-data"
LOAD_BALANCER_DNS="agrovesto-alb-710830026.eu-west-1.elb.amazonaws.com"
CI_USER="ubuntu"

# --- Fix HOME for CI/CD ---
export HOME=/tmp
export DOTNET_CLI_HOME=/tmp

echo "Starting deployment for $APP_NAME"

# --- 0. Clone or update repository ---
if [ "$DRY_RUN" = false ]; then
    if [ ! -d "$REPO_DIR" ]; then
        echo "Cloning repository..."
        git clone <YOUR_REPO_URL> "$REPO_DIR"
    else
        echo "Updating repository..."
        cd "$REPO_DIR"
        git fetch origin
        git reset --hard origin/master
    fi
    cd "$REPO_DIR"
else
    echo "Dry-run: skipping repository update"
    # For dry-run, still need to be in repo directory if it exists
    if [ -d "$REPO_DIR" ]; then
        cd "$REPO_DIR"
    fi
fi

# ----- update certificate__________#
if [ "$DRY_RUN" = false ]; then
    sudo wget https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem -O /usr/local/share/ca-certificates/rds-global-bundle.crt
    sudo update-ca-certificates
else
    echo "Dry-run: skipping certificate update"
fi

# --- 1. Install prerequisites ---
if [ "$DRY_RUN" = false ]; then
    sudo apt-get update
    sudo apt-get install -y wget apt-transport-https software-properties-common ufw nginx git
else
    echo "Dry-run: skipping package installation"
fi

# --- 2. Add Microsoft .NET repo if missing ---
if [ ! -f /etc/apt/sources.list.d/microsoft-prod.list ] && [ "$DRY_RUN" = false ]; then
    echo "Adding Microsoft .NET repository..."
    sudo wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    sudo rm packages-microsoft-prod.deb
    sudo apt-get update
else
    echo "Dry-run: skipping adding Microsoft repo"
fi

# --- 3. Install .NET SDK & Runtime if missing ---
if [ "$DRY_RUN" = false ]; then
    if ! command -v dotnet &> /dev/null || ! dotnet --list-sdks | grep -q "8\."; then
        echo "Installing .NET 8 SDK and Runtime..."
        sudo apt-get install -y dotnet-sdk-8.0 dotnet-runtime-8.0 aspnetcore-runtime-8.0
    fi
else
    echo "Dry-run: skipping .NET SDK installation"
fi

# --- 4. Configure UFW ---
if [ "$DRY_RUN" = false ]; then
    sudo ufw default deny incoming
    sudo ufw default allow outgoing
    sudo ufw allow 22/tcp
    sudo ufw allow 80/tcp
    sudo ufw allow 443/tcp
    sudo ufw allow 5000/tcp
    sudo ufw --force enable
else
    echo "Dry-run: skipping UFW configuration"
fi

# --- 5. Ensure NuGet cache is writable ---
DOTNET_NUGET_CACHE="/tmp/.nuget/packages"
if [ "$DRY_RUN" = false ]; then
    sudo mkdir -p "$DOTNET_NUGET_CACHE"
    sudo chown -R $CI_USER:$CI_USER "$DOTNET_NUGET_CACHE"
fi
export NUGET_PACKAGES="$DOTNET_NUGET_CACHE"

# --- 6. Fix project permissions ---
if [ "$DRY_RUN" = false ]; then
    sudo chown -R $CI_USER:$CI_USER "$REPO_DIR"
    sudo mkdir -p "$APP_DIR"
    sudo chown -R $CI_USER:$CI_USER "$APP_DIR"
else
    echo "Dry-run: skipping permissions changes"
fi

# --- 7. Clean previous build artifacts ---
sudo -u $CI_USER dotnet clean

# --- 8. Restore, Build & Publish as CI_USER ---
echo "Restoring dependencies..."
sudo -u $CI_USER dotnet workload update --skip-manifest-update || true
sudo -u $CI_USER dotnet restore --no-cache

echo "Building application..."
sudo -u $CI_USER dotnet build -c Release --no-restore

echo "Publishing application (self-contained)..."
BUILD_PATH="$REPO_DIR/bin/Release/net8.0/linux-x64/publish"
sudo -u $CI_USER dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishTrimmed=false -o "$BUILD_PATH"

# --- 9. Deploy to target directory ---
if [ "$DRY_RUN" = false ]; then
    sudo mkdir -p "$APP_DIR"
    sudo cp -r "$BUILD_PATH/"* "$APP_DIR/"
    sudo chown -R $USER_NAME:$USER_NAME "$APP_DIR"
else
    echo "Dry-run: skipping file copy and permissions"
fi

# [Rest of the script remains the same...]
