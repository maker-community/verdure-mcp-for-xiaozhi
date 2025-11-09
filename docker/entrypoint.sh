#!/bin/sh
set -e

# Entrypoint script for Verdure MCP Platform
# This script handles custom configuration files mounted via Docker volumes

echo "Starting Verdure MCP Platform..."

# Function to compress a file with Brotli
compress_brotli() {
    local file="$1"
    if command -v brotli >/dev/null 2>&1; then
        echo "  Creating Brotli compressed version: ${file}.br"
        brotli -f -q 11 -o "${file}.br" "$file"
    else
        echo "  Warning: brotli not found, skipping .br compression"
    fi
}

# Function to compress a file with Gzip
compress_gzip() {
    local file="$1"
    if command -v gzip >/dev/null 2>&1; then
        echo "  Creating Gzip compressed version: ${file}.gz"
        gzip -f -k -9 "$file"
    else
        echo "  Warning: gzip not found, skipping .gz compression"
    fi
}

# Check if appsettings.json was mounted (different from image version)
if [ -f "/app/wwwroot/appsettings.json" ]; then
    echo "Custom appsettings.json detected"
    
    # Calculate hash of current file to detect if it's been modified
    CURRENT_HASH=$(md5sum /app/wwwroot/appsettings.json 2>/dev/null | cut -d' ' -f1 || echo "")
    STORED_HASH=""
    
    if [ -f "/app/wwwroot/.appsettings.json.hash" ]; then
        STORED_HASH=$(cat /app/wwwroot/.appsettings.json.hash)
    fi
    
    # If file has changed (or no hash exists), regenerate compressed versions
    if [ "$CURRENT_HASH" != "$STORED_HASH" ] || [ ! -f "/app/wwwroot/appsettings.json.br" ] || [ ! -f "/app/wwwroot/appsettings.json.gz" ]; then
        echo "Configuration file changed or compressed versions missing"
        echo "Regenerating compressed files..."
        
        # Remove old compressed versions
        rm -f /app/wwwroot/appsettings.json.br
        rm -f /app/wwwroot/appsettings.json.gz
        
        # Create new compressed versions
        compress_brotli "/app/wwwroot/appsettings.json"
        compress_gzip "/app/wwwroot/appsettings.json"
        
        # Store hash for future comparison
        echo "$CURRENT_HASH" > /app/wwwroot/.appsettings.json.hash
        
        echo "✓ Configuration files updated and compressed successfully"
    else
        echo "✓ Configuration unchanged, using existing compressed files"
    fi
fi

# Start the application
echo "Launching Verdure.McpPlatform.Api..."
exec dotnet Verdure.McpPlatform.Api.dll "$@"
