# PowerShell script to apply EF Core migrations to the database

Write-Host "Applying EF Core migrations to database" -ForegroundColor Cyan

# Navigate to repository root
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

# Apply migrations
Write-Host "`nRunning: dotnet ef database update..." -ForegroundColor Yellow
dotnet ef database update `
    --project src/Verdure.McpPlatform.Infrastructure `
    --startup-project src/Verdure.McpPlatform.Api `
    --context McpPlatformContext

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✓ Migrations applied successfully!" -ForegroundColor Green
    Write-Host "`nDatabase schema has been updated with the following changes:" -ForegroundColor Cyan
    Write-Host "  • Added UserId column to mcp_service_bindings table" -ForegroundColor White
    Write-Host "  • Added UserId column to mcp_tools table" -ForegroundColor White
    Write-Host "  • Created indexes for improved query performance:" -ForegroundColor White
    Write-Host "    - idx_mcp_service_bindings_user_id" -ForegroundColor Gray
    Write-Host "    - idx_bindings_user_active (UserId, IsActive)" -ForegroundColor Gray
    Write-Host "    - idx_bindings_user_endpoint (UserId, XiaozhiMcpEndpointId)" -ForegroundColor Gray
    Write-Host "    - idx_mcp_tools_user_id" -ForegroundColor Gray
    Write-Host "    - idx_tools_user_service (UserId, McpServiceConfigId)" -ForegroundColor Gray
} else {
    Write-Host "`n✗ Migration failed!" -ForegroundColor Red
    Write-Host "Please check the error messages above." -ForegroundColor Yellow
    exit 1
}
