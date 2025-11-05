# Refactor XiaozhiConnection to XiaozhiMcpEndpoint
# This script performs a comprehensive rename across the entire solution

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Refactoring XiaozhiConnection -> XiaozhiMcpEndpoint" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Define replacements
$replacements = @(
    @{ Old = 'XiaozhiConnectionAggregate'; New = 'XiaozhiMcpEndpointAggregate' }
    @{ Old = 'XiaozhiConnection'; New = 'XiaozhiMcpEndpoint' }
    @{ Old = 'xiaozhi_connections'; New = 'xiaozhi_mcp_endpoints' }
)

# File patterns to search
$patterns = @(
    "*.cs",
    "*.razor",
    "*.json",
    "*.md",
    "*.resx"
)

# Directories to exclude
$excludeDirs = @(
    "bin",
    "obj",
    "node_modules",
    ".vs",
    ".git"
)

Write-Host "`n[1/4] Finding files to process..." -ForegroundColor Yellow

# Get all files
$allFiles = @()
foreach ($pattern in $patterns) {
    $files = Get-ChildItem -Path "." -Filter $pattern -Recurse -File | Where-Object {
        $path = $_.FullName
        $exclude = $false
        foreach ($dir in $excludeDirs) {
            if ($path -like "*\$dir\*") {
                $exclude = $true
                break
            }
        }
        -not $exclude
    }
    $allFiles += $files
}

Write-Host "Found $($allFiles.Count) files to process" -ForegroundColor Green

Write-Host "`n[2/4] Processing file contents..." -ForegroundColor Yellow

$processedFiles = 0
$modifiedFiles = 0

foreach ($file in $allFiles) {
    $processedFiles++
    Write-Progress -Activity "Processing files" -Status "$processedFiles of $($allFiles.Count)" -PercentComplete (($processedFiles / $allFiles.Count) * 100)
    
    try {
        $content = Get-Content $file.FullName -Raw -Encoding UTF8
        $originalContent = $content
        
        # Apply replacements
        foreach ($replacement in $replacements) {
            $content = $content -replace [regex]::Escape($replacement.Old), $replacement.New
        }
        
        # Save if modified
        if ($content -ne $originalContent) {
            Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
            $modifiedFiles++
            Write-Host "  Modified: $($file.FullName.Replace($PWD.Path, '.'))" -ForegroundColor Gray
        }
    }
    catch {
        Write-Host "  Error processing $($file.FullName): $_" -ForegroundColor Red
    }
}

Write-Progress -Activity "Processing files" -Completed

Write-Host "`nModified $modifiedFiles files" -ForegroundColor Green

Write-Host "`n[3/4] Renaming directories..." -ForegroundColor Yellow

# Rename aggregate directory
$oldDir = "src\Verdure.McpPlatform.Domain\AggregatesModel\XiaozhiConnectionAggregate"
$newDir = "src\Verdure.McpPlatform.Domain\AggregatesModel\XiaozhiMcpEndpointAggregate"

if (Test-Path $oldDir) {
    Rename-Item -Path $oldDir -NewName "XiaozhiMcpEndpointAggregate"
    Write-Host "  Renamed: $oldDir -> $newDir" -ForegroundColor Green
}

Write-Host "`n[4/4] Renaming files..." -ForegroundColor Yellow

# Rename files
$filesToRename = @(
    @{ Old = "src\Verdure.McpPlatform.Domain\AggregatesModel\XiaozhiMcpEndpointAggregate\XiaozhiConnection.cs"; New = "XiaozhiMcpEndpoint.cs" }
    @{ Old = "src\Verdure.McpPlatform.Domain\AggregatesModel\XiaozhiMcpEndpointAggregate\IXiaozhiConnectionRepository.cs"; New = "IXiaozhiMcpEndpointRepository.cs" }
    @{ Old = "src\Verdure.McpPlatform.Infrastructure\Repositories\XiaozhiConnectionRepository.cs"; New = "XiaozhiMcpEndpointRepository.cs" }
    @{ Old = "src\Verdure.McpPlatform.Infrastructure\Data\EntityConfigurations\XiaozhiConnectionEntityTypeConfiguration.cs"; New = "XiaozhiMcpEndpointEntityTypeConfiguration.cs" }
    @{ Old = "src\Verdure.McpPlatform.Application\Services\XiaozhiConnectionService.cs"; New = "XiaozhiMcpEndpointService.cs" }
    @{ Old = "src\Verdure.McpPlatform.Application\Services\IXiaozhiConnectionService.cs"; New = "IXiaozhiMcpEndpointService.cs" }
    @{ Old = "src\Verdure.McpPlatform.Contracts\DTOs\XiaozhiConnectionDto.cs"; New = "XiaozhiMcpEndpointDto.cs" }
    @{ Old = "src\Verdure.McpPlatform.Contracts\Requests\CreateXiaozhiConnectionRequest.cs"; New = "CreateXiaozhiMcpEndpointRequest.cs" }
    @{ Old = "src\Verdure.McpPlatform.Contracts\Requests\UpdateXiaozhiConnectionRequest.cs"; New = "UpdateXiaozhiMcpEndpointRequest.cs" }
    @{ Old = "src\Verdure.McpPlatform.Api\Apis\XiaozhiConnectionApi.cs"; New = "XiaozhiMcpEndpointApi.cs" }
    @{ Old = "src\Verdure.McpPlatform.Web\Services\XiaozhiConnectionClientService.cs"; New = "XiaozhiMcpEndpointClientService.cs" }
    @{ Old = "src\Verdure.McpPlatform.Web\Services\IXiaozhiConnectionClientService.cs"; New = "IXiaozhiMcpEndpointClientService.cs" }
)

foreach ($fileRename in $filesToRename) {
    if (Test-Path $fileRename.Old) {
        Rename-Item -Path $fileRename.Old -NewName $fileRename.New
        Write-Host "  Renamed: $($fileRename.Old) -> $($fileRename.New)" -ForegroundColor Green
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Refactoring completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Run: dotnet build" -ForegroundColor White
Write-Host "2. Create migration: dotnet ef migrations add RenameXiaozhiConnectionToEndpoint --project src\Verdure.McpPlatform.Infrastructure --startup-project src\Verdure.McpPlatform.Api" -ForegroundColor White
Write-Host "3. Review and apply migration" -ForegroundColor White
