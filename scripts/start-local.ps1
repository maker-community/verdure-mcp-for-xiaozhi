#!/usr/bin/env pwsh
# Verdure MCP Platform - Local Development Quick Start

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host "  Verdure MCP Platform - Local Development Environment" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""

function Write-Section {
    param([string]$Text)
    Write-Host ""
    Write-Host "---------------------------------------------------------------" -ForegroundColor Yellow
    Write-Host " $Text" -ForegroundColor Yellow
    Write-Host "---------------------------------------------------------------" -ForegroundColor Yellow
}

function Test-Docker {
    Write-Section "Checking Docker Environment"
    
    try {
        $dockerVersion = docker version --format '{{.Server.Version}}' 2>$null
        if ($dockerVersion) {
            Write-Host "  [OK] Docker is running (Version: $dockerVersion)" -ForegroundColor Green
            return $true
        }
    } catch {
        Write-Host "  [ERROR] Docker is not running" -ForegroundColor Red
        Write-Host ""
        Write-Host "  Please start Docker Desktop and try again." -ForegroundColor Yellow
        return $false
    }
}

function Initialize-Environment {
    Write-Section "Initializing Environment"
    
    $dockerDir = Join-Path (Join-Path $PSScriptRoot "..") "docker"
    $envFile = Join-Path $dockerDir ".env"
    $envExample = Join-Path $dockerDir ".env.example"
    
    if (-not (Test-Path $envFile)) {
        Write-Host "  [WARN] .env file not found. Creating from template..." -ForegroundColor Yellow
        Copy-Item $envExample $envFile
        Write-Host "  [OK] Created .env file" -ForegroundColor Green
        Write-Host ""
        Write-Host "  Default configuration:" -ForegroundColor Cyan
        Write-Host "    - PostgreSQL Password: postgres" -ForegroundColor Gray
        Write-Host "    - Keycloak Admin: admin / admin" -ForegroundColor Gray
        Write-Host "    - Demo Users: admin/admin123, demo/demo123" -ForegroundColor Gray
        Write-Host ""
        
        $response = Read-Host "  Continue with default configuration? (Y/n)"
        if ($response -and $response.ToLower() -ne 'y') {
            Write-Host ""
            Write-Host "  Please edit $envFile and run this script again." -ForegroundColor Yellow
            return $false
        }
    } else {
        Write-Host "  [OK] Using existing .env file" -ForegroundColor Green
    }
    
    return $true
}

function Stop-ExistingContainers {
    Write-Section "Cleaning Up Existing Containers"
    
    $dockerDir = Join-Path (Join-Path $PSScriptRoot "..") "docker"
    Set-Location $dockerDir
    
    try {
        docker-compose -f docker-compose.single-image.yml down 2>&1 | Out-Null
        Write-Host "  [OK] Containers cleaned up" -ForegroundColor Green
    } catch {
        Write-Host "  [OK] No existing containers to clean up" -ForegroundColor Green
    }
}

function Build-Images {
    Write-Section "Building Application Image"
    
    $dockerDir = Join-Path (Join-Path $PSScriptRoot "..") "docker"
    Set-Location $dockerDir
    
    Write-Host "  Building... (this may take a few minutes)" -ForegroundColor Gray
    
    docker-compose -f docker-compose.single-image.yml build 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [OK] Build completed successfully" -ForegroundColor Green
        return $true
    } else {
        Write-Host "  [ERROR] Build failed" -ForegroundColor Red
        return $false
    }
}

function Start-Services {
    Write-Section "Starting Services"
    
    $dockerDir = Join-Path (Join-Path $PSScriptRoot "..") "docker"
    Set-Location $dockerDir
    
    Write-Host "  Starting PostgreSQL, Redis, Keycloak, and Application..." -ForegroundColor Gray
    
    docker-compose -f docker-compose.single-image.yml up -d
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [OK] All services started" -ForegroundColor Green
        return $true
    } else {
        Write-Host "  [ERROR] Failed to start services" -ForegroundColor Red
        return $false
    }
}

function Wait-ForServices {
    Write-Section "Waiting for Services to be Ready"
    
    $dockerDir = Join-Path (Join-Path $PSScriptRoot "..") "docker"
    Set-Location $dockerDir
    
    $services = @(
        @{Name="PostgreSQL"; Container="verdure-postgres"; MaxWait=30},
        @{Name="Redis"; Container="verdure-redis"; MaxWait=20},
        @{Name="Keycloak"; Container="verdure-keycloak"; MaxWait=90},
        @{Name="Application"; Container="verdure-mcp-app"; MaxWait=60}
    )
    
    foreach ($service in $services) {
        Write-Host "  Waiting for $($service.Name)..." -ForegroundColor Gray -NoNewline
        
        $waited = 0
        $healthy = $false
        
        while ($waited -lt $service.MaxWait) {
            Start-Sleep -Seconds 3
            $waited += 3
            
            $status = docker inspect --format='{{.State.Health.Status}}' $service.Container 2>$null
            
            if ($status -eq "healthy") {
                $healthy = $true
                break
            }
            
            if (-not $status) {
                $running = docker inspect --format='{{.State.Running}}' $service.Container 2>$null
                if ($running -eq "true") {
                    $healthy = $true
                    break
                }
            }
            
            Write-Host "." -ForegroundColor Gray -NoNewline
        }
        
        if ($healthy) {
            Write-Host " [OK] Ready" -ForegroundColor Green
        } else {
            Write-Host " [WARN] Timeout" -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
}

function Show-ConnectionInfo {
    Write-Host ""
    Write-Host "===============================================================" -ForegroundColor Green
    Write-Host "  Services are now running!" -ForegroundColor Green
    Write-Host "===============================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Application" -ForegroundColor Cyan
    Write-Host "     URL:        http://localhost:8080" -ForegroundColor White
    Write-Host "     Health:     http://localhost:8080/api/health" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Keycloak" -ForegroundColor Cyan
    Write-Host "     Admin UI:   http://localhost:8180" -ForegroundColor White
    Write-Host "     Username:   admin" -ForegroundColor Gray
    Write-Host "     Password:   admin" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Demo Users" -ForegroundColor Cyan
    Write-Host "     Admin:      admin / admin123" -ForegroundColor White
    Write-Host "     Demo:       demo / demo123" -ForegroundColor White
    Write-Host ""
    Write-Host "===============================================================" -ForegroundColor Green
    Write-Host ""
}

function Main {
    if (-not (Test-Docker)) {
        exit 1
    }
    
    if (-not (Initialize-Environment)) {
        exit 1
    }
    
    Stop-ExistingContainers
    
    if (-not (Build-Images)) {
        Write-Host ""
        Write-Host "  [ERROR] Build failed" -ForegroundColor Red
        exit 1
    }
    
    if (-not (Start-Services)) {
        Write-Host ""
        Write-Host "  [ERROR] Failed to start services" -ForegroundColor Red
        exit 1
    }
    
    Wait-ForServices
    Show-ConnectionInfo
    
    Write-Host "  Press Ctrl+C to stop viewing logs" -ForegroundColor Yellow
    Write-Host ""
    
    $dockerDir = Join-Path (Join-Path $PSScriptRoot "..") "docker"
    Set-Location $dockerDir
    docker-compose -f docker-compose.single-image.yml logs -f
}

Main
