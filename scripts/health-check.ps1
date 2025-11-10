#!/usr/bin/env pwsh
# Verdure MCP Platform - Health Check Script

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Verdure MCP Platform - Health Check" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$dockerDir = Join-Path $PSScriptRoot ".." "docker"
Set-Location $dockerDir

# Service definitions
$services = @(
    @{
        Name = "PostgreSQL"
        Container = "verdure-postgres"
        HealthCheck = { docker exec verdure-postgres pg_isready -U postgres 2>$null }
        Port = "5432"
    },
    @{
        Name = "Redis"
        Container = "verdure-redis"
        HealthCheck = { docker exec verdure-redis redis-cli ping 2>$null }
        Port = "6379"
    },
    @{
        Name = "Keycloak"
        Container = "verdure-keycloak"
        HealthCheck = { 
            $status = docker inspect --format='{{.State.Health.Status}}' verdure-keycloak 2>$null
            if ($status -eq "healthy") { "healthy" } else { $null }
        }
        Port = "8180"
        Url = "http://localhost:8180"
    },
    @{
        Name = "Application"
        Container = "verdure-mcp-app"
        HealthCheck = { 
            try {
                $response = Invoke-WebRequest -Uri "http://localhost:8080/api/health" -TimeoutSec 5 -UseBasicParsing
                if ($response.StatusCode -eq 200) { "healthy" }
            } catch {
                $null
            }
        }
        Port = "8080"
        Url = "http://localhost:8080"
    }
)

$allHealthy = $true

foreach ($service in $services) {
    Write-Host "Checking $($service.Name)..." -ForegroundColor Yellow -NoNewline
    
    # Check if container is running
    $running = docker inspect --format='{{.State.Running}}' $service.Container 2>$null
    
    if ($running -ne "true") {
        Write-Host " ✗ Not Running" -ForegroundColor Red
        $allHealthy = $false
        continue
    }
    
    # Check health
    $health = & $service.HealthCheck
    
    if ($health) {
        Write-Host " ✓ Healthy" -ForegroundColor Green
        if ($service.Url) {
            Write-Host "  → $($service.Url)" -ForegroundColor Gray
        }
    } else {
        Write-Host " ⚠ Unhealthy" -ForegroundColor Yellow
        $allHealthy = $false
    }
}

Write-Host ""

if ($allHealthy) {
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host "  ✓ All services are healthy!" -ForegroundColor Green
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Application:  http://localhost:8080" -ForegroundColor White
    Write-Host "  Keycloak:     http://localhost:8180" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Yellow
    Write-Host "  ⚠ Some services are not healthy" -ForegroundColor Yellow
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Check logs with: .\scripts\view-logs.ps1" -ForegroundColor Gray
    Write-Host ""
}
