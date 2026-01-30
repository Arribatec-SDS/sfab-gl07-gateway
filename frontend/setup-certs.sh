#!/bin/bash
# Setup SSL Certificates for Frontend Development
# This script generates trusted local SSL certificates using mkcert

CERTS_DIR=".certs"
CERT_BASE_NAME="_wildcard.localtest.me"

echo -e "\033[0;32mSetting up SSL certificates for frontend development...\033[0m"
echo ""

# Check if mkcert is installed
if ! command -v mkcert &> /dev/null; then
    echo -e "\033[0;31m✗ mkcert is not installed\033[0m"
    echo ""
    echo -e "\033[0;33mPlease install mkcert first:\033[0m"
    echo -e "\033[0;37m  Mac: brew install mkcert\033[0m"
    echo -e "\033[0;37m  Linux: See https://github.com/FiloSottile/mkcert#installation\033[0m"
    echo ""
    echo -e "\033[0;33mAfter installing, run: mkcert -install\033[0m"
    exit 1
fi

MKCERT_VERSION=$(mkcert -version 2>&1)
echo -e "\033[0;32m✓ mkcert found: $MKCERT_VERSION\033[0m"

# Create .certs directory if it doesn't exist
if [ ! -d "$CERTS_DIR" ]; then
    echo -e "\033[0;33mCreating $CERTS_DIR directory...\033[0m"
    mkdir -p "$CERTS_DIR"
fi

# Check if certificates already exist
CERT_FILE="$CERTS_DIR/${CERT_BASE_NAME}.pem"
KEY_FILE="$CERTS_DIR/${CERT_BASE_NAME}-key.pem"

if [ -f "$CERT_FILE" ] && [ -f "$KEY_FILE" ]; then
    echo ""
    echo -e "\033[0;33mCertificates already exist:\033[0m"
    echo -e "\033[0;90m  Cert: $CERT_FILE\033[0m"
    echo -e "\033[0;90m  Key:  $KEY_FILE\033[0m"
    echo ""
    read -p "Regenerate certificates? (y/n) " -n 1 -r
    echo ""
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo -e "\033[0;32mKeeping existing certificates.\033[0m"
        exit 0
    fi
    echo ""
fi

# Generate certificates
echo -e "\033[0;33mGenerating SSL certificates...\033[0m"
cd "$CERTS_DIR" || exit 1

# Generate wildcard certificate for *.localtest.me and localtest.me
mkcert "*.localtest.me" "localtest.me"

if [ $? -eq 0 ]; then
    echo ""
    echo -e "\033[0;32m✓ Certificates generated successfully!\033[0m"
    echo ""
    echo -e "\033[0;36mCertificate files:\033[0m"
    ls -1 *.pem | while read file; do
        echo -e "\033[0;37m  - $file\033[0m"
    done
    echo ""
    echo -e "\033[0;32m✓ Your development server will now use HTTPS with trusted certificates\033[0m"
    echo ""
else
    echo ""
    echo -e "\033[0;31m✗ Certificate generation failed\033[0m"
    echo ""
    echo -e "\033[0;33mTroubleshooting:\033[0m"
    echo -e "\033[0;37m  1. Make sure you ran 'mkcert -install' first\033[0m"
    echo -e "\033[0;37m  2. Check if mkcert is in your PATH\033[0m"
    echo -e "\033[0;37m  3. Try running with sudo if needed\033[0m"
    exit 1
fi

cd ..
