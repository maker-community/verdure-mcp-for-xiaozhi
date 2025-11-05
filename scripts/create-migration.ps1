# PowerShell script to create EF Core migration for adding UserId to child entities

Write-Host "Creating EF Core migration: AddUserIdToChildEntities" -ForegroundColor Cyan

# Navigate to repository root
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

# Create migration
Write-Host "`nRunning: dotnet ef migrations add AddUserIdToChildEntities..." -ForegroundColor Yellow
dotnet ef migrations add AddUserIdToChildEntities `
    --project src/Verdure.McpPlatform.Infrastructure `
    --startup-project src/Verdure.McpPlatform.Api `
    --context McpPlatformContext

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✓ Migration created successfully!" -ForegroundColor Green
    Write-Host "`nTo apply the migration, run:" -ForegroundColor Cyan
    Write-Host "  .\scripts\apply-migration.ps1" -ForegroundColor White
} else {
    Write-Host "`n✗ Migration creation failed!" -ForegroundColor Red
    Write-Host "Please check the error messages above." -ForegroundColor Yellow
    exit 1
}
