#!/usr/bin/env pwsh
# Verdure MCP Platform - Quick Start Script for Single Image Deployment

$ErrorActionPreference = "Stop"

Write-Host "=== Verdure MCP Platform - Single Image Deployment ===" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is running
Write-Host "Checking Docker..." -ForegroundColor Yellow
try {
    docker info | Out-Null
    Write-Host "✓ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "✗ Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Check if .env file exists
$envFile = Join-Path (Join-Path (Join-Path $PSScriptRoot "..") "docker") ".env"
if (-not (Test-Path $envFile)) {
    Write-Host "⚠ .env file not found. Creating from .env.example..." -ForegroundColor Yellow
    $envExample = Join-Path (Join-Path (Join-Path $PSScriptRoot "..") "docker") ".env.example"
    Copy-Item $envExample $envFile
    Write-Host "✓ Created .env file. Please edit it with your configuration." -ForegroundColor Green
    Write-Host ""
    Write-Host "Important: Update the following in .env:" -ForegroundColor Yellow
    Write-Host "  - POSTGRES_PASSWORD" -ForegroundColor Yellow
    Write-Host "  - IDENTITY_CLIENT_SECRET" -ForegroundColor Yellow
    Write-Host ""
    
    $continue = Read-Host "Continue with default values? (y/n)"
    if ($continue -ne "y") {
        Write-Host "Exiting. Please update .env and run again." -ForegroundColor Yellow
        exit 0
    }
}

# Navigate to docker directory
Set-Location (Join-Path (Join-Path $PSScriptRoot "..") "docker")

Write-Host ""
Write-Host "Step 1: Building Docker image..." -ForegroundColor Yellow
docker-compose -f docker-compose.single-image.yml build

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Build completed" -ForegroundColor Green
Write-Host ""

Write-Host "Step 2: Starting services..." -ForegroundColor Yellow
docker-compose -f docker-compose.single-image.yml up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Failed to start services" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Services started" -ForegroundColor Green
Write-Host ""

Write-Host "Step 3: Waiting for services to be healthy..." -ForegroundColor Yellow
$maxWait = 60
$waited = 0

while ($waited -lt $maxWait) {
    Start-Sleep -Seconds 2
    $waited += 2
    
    $status = docker-compose -f docker-compose.single-image.yml ps --format json | ConvertFrom-Json
    $healthy = $true
    
    foreach ($service in $status) {
        if ($service.Health -and $service.Health -ne "healthy") {
            $healthy = $false
            break
        }
    }
    
    if ($healthy) {
        Write-Host "✓ All services are healthy" -ForegroundColor Green
        break
    }
    
    Write-Host "  Waiting... ($waited/$maxWait seconds)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== Deployment Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Application URL: http://localhost:8080" -ForegroundColor Green
Write-Host "Health Check:   http://localhost:8080/api/health" -ForegroundColor Green
Write-Host "API Docs:       http://localhost:8080/scalar/v1 (if in Development)" -ForegroundColor Green
Write-Host ""
Write-Host "Useful commands:" -ForegroundColor Yellow
Write-Host "  View logs:     docker-compose -f docker-compose.single-image.yml logs -f app" -ForegroundColor Gray
Write-Host "  Stop services: docker-compose -f docker-compose.single-image.yml down" -ForegroundColor Gray
Write-Host "  Restart:       docker-compose -f docker-compose.single-image.yml restart app" -ForegroundColor Gray
Write-Host ""

# Optional: Open browser
$openBrowser = Read-Host "Open application in browser? (y/n)"
if ($openBrowser -eq "y") {
    Start-Process "http://localhost:8080"
}

Write-Host ""
Write-Host "To view logs, run:" -ForegroundColor Yellow
Write-Host "  docker-compose -f docker-compose.single-image.yml logs -f app" -ForegroundColor Cyan
