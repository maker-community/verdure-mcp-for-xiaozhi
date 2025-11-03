# Verdure MCP Platform - 快速启动脚本
# 用于开发和测试环境

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("aspire", "web", "api", "all", "clean")]
    [string]$Mode = "aspire"
)

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Verdure MCP Platform 启动脚本" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

$projectRoot = $PSScriptRoot
$apiPath = Join-Path $projectRoot "src\Verdure.McpPlatform.Api"
$webPath = Join-Path $projectRoot "src\Verdure.McpPlatform.Web"
$appHostPath = Join-Path $projectRoot "src\Verdure.McpPlatform.AppHost"

switch ($Mode) {
    "clean" {
        Write-Host "🧹 清理编译输出..." -ForegroundColor Yellow
        dotnet clean
        
        Write-Host "🗑️  删除 bin 和 obj 目录..." -ForegroundColor Yellow
        Get-ChildItem -Path $projectRoot -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force
        
        Write-Host "✅ 清理完成！" -ForegroundColor Green
    }
    
    "aspire" {
        Write-Host "🚀 启动 Aspire 应用宿主..." -ForegroundColor Green
        Write-Host "   这将启动所有服务（API + Web + 依赖）" -ForegroundColor Gray
        Write-Host ""
        
        Set-Location $appHostPath
        dotnet run
    }
    
    "api" {
        Write-Host "🔧 仅启动 API 服务..." -ForegroundColor Green
        Write-Host ""
        
        Set-Location $apiPath
        dotnet run
    }
    
    "web" {
        Write-Host "🌐 仅启动 Web 前端..." -ForegroundColor Green
        Write-Host "   注意：需要 API 服务同时运行" -ForegroundColor Yellow
        Write-Host ""
        
        Set-Location $webPath
        dotnet run
    }
    
    "all" {
        Write-Host "🚀 启动所有服务（分离模式）..." -ForegroundColor Green
        Write-Host ""
        
        # 启动 API
        Write-Host "启动 API 服务..." -ForegroundColor Cyan
        $apiJob = Start-Job -ScriptBlock {
            param($path)
            Set-Location $path
            dotnet run
        } -ArgumentList $apiPath
        
        Start-Sleep -Seconds 5
        
        # 启动 Web
        Write-Host "启动 Web 前端..." -ForegroundColor Cyan
        $webJob = Start-Job -ScriptBlock {
            param($path)
            Set-Location $path
            dotnet run
        } -ArgumentList $webPath
        
        Write-Host ""
        Write-Host "✅ 所有服务已启动！" -ForegroundColor Green
        Write-Host "   API Job ID: $($apiJob.Id)" -ForegroundColor Gray
        Write-Host "   Web Job ID: $($webJob.Id)" -ForegroundColor Gray
        Write-Host ""
        Write-Host "查看日志: Get-Job | Receive-Job" -ForegroundColor Yellow
        Write-Host "停止服务: Get-Job | Stop-Job | Remove-Job" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  访问地址" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Web UI:   https://localhost:5001" -ForegroundColor White
Write-Host "  API:      https://localhost:5000" -ForegroundColor White
if ($Mode -eq "aspire") {
    Write-Host "  Aspire:   https://localhost:17181" -ForegroundColor White
}
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "使用说明:" -ForegroundColor Yellow
Write-Host "  .\start-dev.ps1                    # 使用 Aspire 启动（推荐）" -ForegroundColor Gray
Write-Host "  .\start-dev.ps1 -Mode web          # 仅启动 Web 前端" -ForegroundColor Gray
Write-Host "  .\start-dev.ps1 -Mode api          # 仅启动 API 服务" -ForegroundColor Gray
Write-Host "  .\start-dev.ps1 -Mode all          # 分离启动所有服务" -ForegroundColor Gray
Write-Host "  .\start-dev.ps1 -Mode clean        # 清理编译输出" -ForegroundColor Gray
Write-Host ""
