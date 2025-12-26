#!/usr/bin/env pwsh
# Verdure MCP Platform - Stop Local Services

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host "  Stopping Verdure MCP Platform Services" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""

$dockerDir = Join-Path (Join-Path $PSScriptRoot "..") "docker"
Set-Location $dockerDir

Write-Host "[*] Stopping services..." -ForegroundColor Yellow

docker-compose -f docker-compose.single-image.yml down

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] All services stopped successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "Note: Data volumes are preserved. To remove all data:" -ForegroundColor Yellow
    Write-Host "  docker-compose -f docker-compose.single-image.yml down -v" -ForegroundColor Gray
} else {
    Write-Host "[ERROR] Failed to stop services" -ForegroundColor Red
    exit 1
}

Write-Host ""
