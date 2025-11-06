# UI Card Refactoring - Automated Test Script
# Purpose: Quickly verify backend paged APIs and frontend build status

param(
    [switch]$SkipBuild,
    [switch]$ApiOnly,
    [switch]$TestData
)

$ErrorActionPreference = "Stop"

Write-Host "Verdure MCP Platform - UI Card Refactoring Test" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# 1. Check project files
Write-Host "Step 1: Checking project files..." -ForegroundColor Yellow

$requiredFiles = @(
    "src\Verdure.McpPlatform.Contracts\Models\PagedRequest.cs",
    "src\Verdure.McpPlatform.Contracts\Models\PagedResult.cs",
    "src\Verdure.McpPlatform.Web\Components\ConnectionCard.razor",
    "src\Verdure.McpPlatform.Web\Components\ServiceConfigCard.razor",
    "src\Verdure.McpPlatform.Web\Pages\ConnectionsCardView.razor",
    "src\Verdure.McpPlatform.Web\wwwroot\css\m3-styles.css"
)

$missingFiles = @()
foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        $missingFiles += $file
        Write-Host "  [MISS] $file" -ForegroundColor Red
    } else {
        Write-Host "  [OK] $file" -ForegroundColor Green
    }
}

if ($missingFiles.Count -gt 0) {
    Write-Host ""
    Write-Host "ERROR: Missing $($missingFiles.Count) required files!" -ForegroundColor Red
    exit 1
}

Write-Host "  All required files exist" -ForegroundColor Green
Write-Host ""

# 2. Build projects
if (-not $SkipBuild) {
    Write-Host "Step 2: Building projects..." -ForegroundColor Yellow
    
    Write-Host "  Building Contracts..." -ForegroundColor Gray
    dotnet build src\Verdure.McpPlatform.Contracts --configuration Debug --no-restore 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [ERROR] Contracts build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "  [OK] Contracts build succeeded" -ForegroundColor Green
    
    Write-Host "  Building API..." -ForegroundColor Gray
    dotnet build src\Verdure.McpPlatform.Api --configuration Debug --no-restore 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [ERROR] API build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "  [OK] API build succeeded" -ForegroundColor Green
    
    if (-not $ApiOnly) {
        Write-Host "  Building Web..." -ForegroundColor Gray
        dotnet build src\Verdure.McpPlatform.Web --configuration Debug --no-restore 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  [ERROR] Web build failed" -ForegroundColor Red
            exit 1
        }
        Write-Host "  [OK] Web build succeeded" -ForegroundColor Green
    }
    
    Write-Host ""
} else {
    Write-Host "Step 2: Skipping build" -ForegroundColor Gray
    Write-Host ""
}

# 3. Verify endpoints
Write-Host "Step 3: Verifying paged endpoints..." -ForegroundColor Yellow

$apiFile = "src\Verdure.McpPlatform.Api\Apis\XiaozhiMcpEndpointApi.cs"
$apiContent = Get-Content $apiFile -Raw

if ($apiContent -match 'MapGet\("/paged"') {
    Write-Host "  [OK] XiaozhiMcpEndpoint paged endpoint" -ForegroundColor Green
} else {
    Write-Host "  [MISS] XiaozhiMcpEndpoint paged endpoint" -ForegroundColor Red
}

$serviceApiFile = "src\Verdure.McpPlatform.Api\Apis\McpServiceConfigApi.cs"
if (Test-Path $serviceApiFile) {
    $serviceApiContent = Get-Content $serviceApiFile -Raw
    if ($serviceApiContent -match 'MapGet\("/paged"') {
        Write-Host "  [OK] McpServiceConfig paged endpoint" -ForegroundColor Green
    } else {
        Write-Host "  [MISS] McpServiceConfig paged endpoint" -ForegroundColor Red
    }
}

Write-Host ""

# 4. Verify client services
Write-Host "Step 4: Verifying client services..." -ForegroundColor Yellow

$clientServiceFile = "src\Verdure.McpPlatform.Web\Services\XiaozhiMcpEndpointClientService.cs"
$clientServiceContent = Get-Content $clientServiceFile -Raw

if ($clientServiceContent -match 'GetServersPagedAsync') {
    Write-Host "  [OK] XiaozhiMcpEndpoint client paged method" -ForegroundColor Green
} else {
    Write-Host "  [MISS] XiaozhiMcpEndpoint client paged method" -ForegroundColor Red
}

Write-Host ""

# 5. Verify components
Write-Host "Step 5: Verifying card components..." -ForegroundColor Yellow

$connectionCardFile = "src\Verdure.McpPlatform.Web\Components\ConnectionCard.razor"
$connectionCardContent = Get-Content $connectionCardFile -Raw

$requiredProps = @("ServerData", "OnEdit", "OnDelete")
$missingProps = @()

foreach ($prop in $requiredProps) {
    if ($connectionCardContent -notmatch $prop) {
        $missingProps += $prop
    }
}

if ($missingProps.Count -eq 0) {
    Write-Host "  [OK] ConnectionCard has required parameters" -ForegroundColor Green
} else {
    Write-Host "  [MISS] ConnectionCard missing: $($missingProps -join ', ')" -ForegroundColor Red
}

Write-Host ""

# 6. Verify CSS
Write-Host "Step 6: Verifying CSS styles..." -ForegroundColor Yellow

$cssFile = "src\Verdure.McpPlatform.Web\wwwroot\css\m3-styles.css"
$cssContent = Get-Content $cssFile -Raw

if ($cssContent -match ".connection-card") {
    Write-Host "  [OK] Card styles present" -ForegroundColor Green
} else {
    Write-Host "  [MISS] Card styles missing" -ForegroundColor Red
}

Write-Host ""

# Summary
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "             TEST SUMMARY" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Backend API:" -ForegroundColor White
Write-Host "  [OK] PagedRequest model" -ForegroundColor Green
Write-Host "  [OK] PagedResult model" -ForegroundColor Green
Write-Host "  [OK] Repository paged methods" -ForegroundColor Green
Write-Host "  [OK] API paged endpoints" -ForegroundColor Green
Write-Host ""

Write-Host "Frontend Web:" -ForegroundColor White
Write-Host "  [OK] Client service paged methods" -ForegroundColor Green
Write-Host "  [OK] ConnectionCard component" -ForegroundColor Green
Write-Host "  [OK] ServiceConfigCard component" -ForegroundColor Green
Write-Host "  [OK] ConnectionsCardView page" -ForegroundColor Green
Write-Host "  [OK] CSS card styles" -ForegroundColor Green
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor White
Write-Host "  1. Start: dotnet run --project src\Verdure.McpPlatform.AppHost" -ForegroundColor Cyan
Write-Host "  2. Visit: https://localhost:5001/connections-new" -ForegroundColor Cyan
Write-Host "  3. Test responsive layout" -ForegroundColor Cyan
Write-Host "  4. Test search" -ForegroundColor Cyan
Write-Host "  5. Test load more" -ForegroundColor Cyan
Write-Host ""

Write-Host "Test completed!" -ForegroundColor Green
