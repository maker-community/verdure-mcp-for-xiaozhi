#!/usr/bin/env pwsh
# Verdure MCP Platform - View Service Logs

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("all", "app", "postgres", "redis", "keycloak")]
    [string]$Service = "all",
    
    [Parameter(Mandatory=$false)]
    [switch]$Follow = $true,
    
    [Parameter(Mandatory=$false)]
    [int]$Tail = 100
)

$dockerDir = Join-Path (Join-Path $PSScriptRoot "..") "docker"
Set-Location $dockerDir

Write-Host ""
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host "  Viewing Logs: $Service" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""

$cmd = "docker-compose -f docker-compose.single-image.yml logs --tail=$Tail"

if ($Follow) {
    $cmd += " -f"
}

if ($Service -ne "all") {
    $cmd += " $Service"
}

Write-Host "Command: $cmd" -ForegroundColor Gray
Write-Host ""

Invoke-Expression $cmd
