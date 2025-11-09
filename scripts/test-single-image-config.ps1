#!/usr/bin/env pwsh
# Test Single Image Deployment
# This script verifies that the single image deployment is working correctly

$ErrorActionPreference = "Stop"

Write-Host "=== Testing Single Image Deployment ===" -ForegroundColor Cyan
Write-Host ""

$testsPassed = 0
$testsFailed = 0

function Test-Condition {
    param(
        [string]$TestName,
        [scriptblock]$Condition,
        [string]$FailureMessage
    )
    
    Write-Host "Testing: $TestName..." -ForegroundColor Yellow -NoNewline
    
    try {
        $result = & $Condition
        if ($result) {
            Write-Host " ✓ PASS" -ForegroundColor Green
            $script:testsPassed++
            return $true
        } else {
            Write-Host " ✗ FAIL" -ForegroundColor Red
            Write-Host "  $FailureMessage" -ForegroundColor Red
            $script:testsFailed++
            return $false
        }
    } catch {
        Write-Host " ✗ ERROR" -ForegroundColor Red
        Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
        $script:testsFailed++
        return $false
    }
}

# Test 1: API project references Web project
Test-Condition `
    -TestName "API project references Web project" `
    -Condition {
        $apiCsproj = Get-Content "src/Verdure.McpPlatform.Api/Verdure.McpPlatform.Api.csproj" -Raw
        $apiCsproj -match "Verdure.McpPlatform.Web.csproj"
    } `
    -FailureMessage "API project does not reference Web project"

# Test 1.5: API project has WebAssembly.Server package
Test-Condition `
    -TestName "API project has WebAssembly.Server package" `
    -Condition {
        $apiCsproj = Get-Content "src/Verdure.McpPlatform.Api/Verdure.McpPlatform.Api.csproj" -Raw
        $apiCsproj -match "Microsoft.AspNetCore.Components.WebAssembly.Server"
    } `
    -FailureMessage "API project missing Microsoft.AspNetCore.Components.WebAssembly.Server package"

# Test 2: Program.cs has UseBlazorFrameworkFiles
Test-Condition `
    -TestName "Program.cs configures Blazor static files" `
    -Condition {
        $programCs = Get-Content "src/Verdure.McpPlatform.Api/Program.cs" -Raw
        ($programCs -match "UseBlazorFrameworkFiles") -and ($programCs -match "UseStaticFiles")
    } `
    -FailureMessage "Program.cs missing UseBlazorFrameworkFiles or UseStaticFiles"

# Test 3: Program.cs has MapFallbackToFile
Test-Condition `
    -TestName "Program.cs has SPA fallback routing" `
    -Condition {
        $programCs = Get-Content "src/Verdure.McpPlatform.Api/Program.cs" -Raw
        $programCs -match "MapFallbackToFile"
    } `
    -FailureMessage "Program.cs missing MapFallbackToFile"

# Test 4: Web appsettings.json uses relative path
Test-Condition `
    -TestName "Web appsettings uses relative API path" `
    -Condition {
        $appsettings = Get-Content "src/Verdure.McpPlatform.Web/wwwroot/appsettings.json" -Raw | ConvertFrom-Json
        $appsettings.ApiBaseAddress -eq ""
    } `
    -FailureMessage "ApiBaseAddress should be empty string for relative path"

# Test 5: AppHost.cs updated
Test-Condition `
    -TestName "AppHost configuration updated" `
    -Condition {
        $appHost = Get-Content "src/Verdure.McpPlatform.AppHost/AppHost.cs" -Raw
        $appHost -notmatch 'AddProject<Projects.Verdure_McpPlatform_Web>\("web"\)'
    } `
    -FailureMessage "AppHost should not have separate Web project configuration"

# Test 6: Dockerfile exists
Test-Condition `
    -TestName "Single-image Dockerfile exists" `
    -Condition {
        Test-Path "docker/Dockerfile.single-image"
    } `
    -FailureMessage "docker/Dockerfile.single-image not found"

# Test 7: Docker Compose file exists
Test-Condition `
    -TestName "Docker Compose configuration exists" `
    -Condition {
        Test-Path "docker/docker-compose.single-image.yml"
    } `
    -FailureMessage "docker-compose.single-image.yml not found"

# Test 8: Deployment documentation exists
Test-Condition `
    -TestName "Deployment documentation exists" `
    -Condition {
        Test-Path "docs/guides/SINGLE_IMAGE_DEPLOYMENT.md"
    } `
    -FailureMessage "Deployment documentation not found"

# Test 9: Health check endpoint in Program.cs
Test-Condition `
    -TestName "Health check endpoint configured" `
    -Condition {
        $programCs = Get-Content "src/Verdure.McpPlatform.Api/Program.cs" -Raw
        $programCs -match '/api/health'
    } `
    -FailureMessage "Health check endpoint missing or not prefixed with /api"

Write-Host ""
Write-Host "=== Test Results ===" -ForegroundColor Cyan
Write-Host "Tests Passed: $testsPassed" -ForegroundColor Green
Write-Host "Tests Failed: $testsFailed" -ForegroundColor $(if ($testsFailed -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($testsFailed -eq 0) {
    Write-Host "✓ All tests passed! Single image deployment is ready." -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Build and test locally:" -ForegroundColor Gray
    Write-Host "     dotnet build src/Verdure.McpPlatform.Api" -ForegroundColor Cyan
    Write-Host "     dotnet run --project src/Verdure.McpPlatform.AppHost" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  2. Build Docker image:" -ForegroundColor Gray
    Write-Host "     docker build -f docker/Dockerfile.single-image -t verdure-mcp-platform:latest ." -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  3. Start with Docker Compose:" -ForegroundColor Gray
    Write-Host "     cd docker && docker-compose -f docker-compose.single-image.yml up -d" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Or use the quick start script:" -ForegroundColor Gray
    Write-Host "     .\scripts\start-single-image.ps1" -ForegroundColor Cyan
    Write-Host ""
    exit 0
} else {
    Write-Host "✗ Some tests failed. Please fix the issues above." -ForegroundColor Red
    exit 1
}
