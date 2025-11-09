#!/usr/bin/env pwsh
# Test configuration compression in Docker container

$ErrorActionPreference = "Stop"

Write-Host "=== Testing Configuration Compression ===" -ForegroundColor Cyan
Write-Host ""

# Check if docker is running
try {
    docker info | Out-Null
    Write-Host "✓ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "✗ Docker is not running" -ForegroundColor Red
    exit 1
}

# Check if image exists
$imageExists = docker images verdure-mcp-platform:latest -q
if (-not $imageExists) {
    Write-Host "✗ Image verdure-mcp-platform:latest not found" -ForegroundColor Red
    Write-Host "Please build the image first:" -ForegroundColor Yellow
    Write-Host "  docker build -f docker/Dockerfile.single-image -t verdure-mcp-platform:latest ." -ForegroundColor Cyan
    exit 1
}

Write-Host "✓ Found image: verdure-mcp-platform:latest" -ForegroundColor Green
Write-Host ""

# Create test config directory
$testConfigDir = "docker/test-config"
New-Item -ItemType Directory -Force -Path $testConfigDir | Out-Null

# Create a test appsettings.json with unique content
$testContent = @"
{
  "ApiBaseAddress": "",
  "Oidc": {
    "Authority": "https://test-auth-$(Get-Date -Format 'HHmmss').example.com/realms/test",
    "ClientId": "test-client-$(Get-Date -Format 'HHmmss')",
    "ResponseType": "code",
    "PostLogoutRedirectUri": "",
    "Scope": "openid profile email",
    "DefaultScopes": [
      "openid",
      "profile",
      "email"
    ]
  }
}
"@

$testConfigFile = Join-Path $testConfigDir "appsettings.json"
$testContent | Out-File -FilePath $testConfigFile -Encoding utf8 -NoNewline

Write-Host "Created test configuration file" -ForegroundColor Yellow
Write-Host "  $testConfigFile" -ForegroundColor Gray
Write-Host ""

# Run container with mounted config
Write-Host "Starting test container..." -ForegroundColor Yellow
$containerName = "verdure-test-compression-$(Get-Date -Format 'HHmmss')"

try {
    docker run -d `
        --name $containerName `
        -p 18080:8080 `
        -v "${PWD}/${testConfigFile}:/app/wwwroot/appsettings.json:ro" `
        -e ConnectionStrings__mcpdb="Host=localhost;Database=test" `
        -e ConnectionStrings__identitydb="Host=localhost;Database=test" `
        -e ConnectionStrings__redis="localhost:6379" `
        verdure-mcp-platform:latest | Out-Null
    
    Write-Host "✓ Container started: $containerName" -ForegroundColor Green
    Write-Host ""
    
    # Wait for container to start
    Write-Host "Waiting for container to initialize..." -ForegroundColor Yellow
    Start-Sleep -Seconds 3
    
    # Check logs
    Write-Host ""
    Write-Host "=== Container Logs ===" -ForegroundColor Cyan
    docker logs $containerName 2>&1 | Select-Object -First 20
    Write-Host ""
    
    # Verify files in container
    Write-Host "=== Verifying Compressed Files ===" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "Checking appsettings files:" -ForegroundColor Yellow
    docker exec $containerName sh -c "ls -lh /app/wwwroot/appsettings.json*"
    Write-Host ""
    
    # Check if .br file exists
    $brExists = docker exec $containerName sh -c "test -f /app/wwwroot/appsettings.json.br && echo 'yes' || echo 'no'" 2>$null
    if ($brExists -match "yes") {
        Write-Host "✓ appsettings.json.br exists" -ForegroundColor Green
        
        # Verify .br content
        Write-Host "  Verifying Brotli compressed content..." -ForegroundColor Gray
        $brContent = docker exec $containerName sh -c "brotli -d -c /app/wwwroot/appsettings.json.br" 2>$null
        if ($brContent -match "test-auth") {
            Write-Host "  ✓ Brotli content matches test configuration" -ForegroundColor Green
        } else {
            Write-Host "  ✗ Brotli content doesn't match" -ForegroundColor Red
        }
    } else {
        Write-Host "✗ appsettings.json.br NOT found" -ForegroundColor Red
    }
    
    # Check if .gz file exists
    $gzExists = docker exec $containerName sh -c "test -f /app/wwwroot/appsettings.json.gz && echo 'yes' || echo 'no'" 2>$null
    if ($gzExists -match "yes") {
        Write-Host "✓ appsettings.json.gz exists" -ForegroundColor Green
        
        # Verify .gz content
        Write-Host "  Verifying Gzip compressed content..." -ForegroundColor Gray
        $gzContent = docker exec $containerName sh -c "gzip -d -c /app/wwwroot/appsettings.json.gz" 2>$null
        if ($gzContent -match "test-auth") {
            Write-Host "  ✓ Gzip content matches test configuration" -ForegroundColor Green
        } else {
            Write-Host "  ✗ Gzip content doesn't match" -ForegroundColor Red
        }
    } else {
        Write-Host "✗ appsettings.json.gz NOT found" -ForegroundColor Red
    }
    
    # Check hash file
    $hashExists = docker exec $containerName sh -c "test -f /app/wwwroot/.appsettings.json.hash && echo 'yes' || echo 'no'" 2>$null
    if ($hashExists -match "yes") {
        Write-Host "✓ Hash file exists (.appsettings.json.hash)" -ForegroundColor Green
        $hash = docker exec $containerName sh -c "cat /app/wwwroot/.appsettings.json.hash" 2>$null
        Write-Host "  MD5 Hash: $hash" -ForegroundColor Gray
    } else {
        Write-Host "✗ Hash file NOT found" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "=== Test Summary ===" -ForegroundColor Cyan
    if ($brExists -match "yes" -and $gzExists -match "yes" -and $hashExists -match "yes") {
        Write-Host "✓ All compression tests PASSED" -ForegroundColor Green
        Write-Host ""
        Write-Host "The entrypoint script successfully:" -ForegroundColor Green
        Write-Host "  • Detected the mounted configuration" -ForegroundColor Green
        Write-Host "  • Generated Brotli compressed version (.br)" -ForegroundColor Green
        Write-Host "  • Generated Gzip compressed version (.gz)" -ForegroundColor Green
        Write-Host "  • Saved configuration hash for future comparison" -ForegroundColor Green
    } else {
        Write-Host "✗ Some tests FAILED" -ForegroundColor Red
    }
    
} finally {
    Write-Host ""
    Write-Host "Cleaning up..." -ForegroundColor Yellow
    docker stop $containerName 2>&1 | Out-Null
    docker rm $containerName 2>&1 | Out-Null
    Remove-Item -Recurse -Force $testConfigDir -ErrorAction SilentlyContinue
    Write-Host "✓ Cleanup complete" -ForegroundColor Green
}

Write-Host ""
Write-Host "Test completed!" -ForegroundColor Cyan
