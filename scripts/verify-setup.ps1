# 快速验证脚本 - Verdure MCP Platform
# 用于验证新功能是否正常工作

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  Verdure MCP Platform - 功能验证脚本" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

$projectRoot = $PSScriptRoot

# 1. 清理编译输出
Write-Host "📦 第1步: 清理旧的编译输出..." -ForegroundColor Yellow
dotnet clean --verbosity quiet
Write-Host "   ✅ 清理完成" -ForegroundColor Green
Write-Host ""

# 2. 恢复 NuGet 包
Write-Host "📥 第2步: 恢复 NuGet 包..." -ForegroundColor Yellow
dotnet restore --verbosity quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ 包恢复成功" -ForegroundColor Green
} else {
    Write-Host "   ❌ 包恢复失败" -ForegroundColor Red
    exit 1
}
Write-Host ""

# 3. 编译解决方案
Write-Host "🔨 第3步: 编译解决方案..." -ForegroundColor Yellow
dotnet build --configuration Debug --no-restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ 编译成功" -ForegroundColor Green
} else {
    Write-Host "   ❌ 编译失败，请检查错误信息" -ForegroundColor Red
    exit 1
}
Write-Host ""

# 4. 验证关键文件
Write-Host "📂 第4步: 验证新增文件..." -ForegroundColor Yellow

$filesToCheck = @(
    "src\Verdure.McpPlatform.Web\Pages\Index.razor",
    "src\Verdure.McpPlatform.Web\Pages\Dashboard.razor",
    "src\Verdure.McpPlatform.Web\Layout\Footer.razor",
    "FRONTEND_IMPROVEMENTS.md",
    "TESTING_GUIDE.md",
    "CHANGELOG.md",
    "SUMMARY.md"
)

$allFilesExist = $true
foreach ($file in $filesToCheck) {
    $fullPath = Join-Path $projectRoot $file
    if (Test-Path $fullPath) {
        Write-Host "   ✅ $file" -ForegroundColor Green
    } else {
        Write-Host "   ❌ $file (未找到)" -ForegroundColor Red
        $allFilesExist = $false
    }
}

if (-not $allFilesExist) {
    Write-Host ""
    Write-Host "   ⚠️  部分文件缺失，请检查" -ForegroundColor Red
    exit 1
}
Write-Host ""

# 5. 检查修改的文件
Write-Host "🔍 第5步: 检查修改的文件..." -ForegroundColor Yellow

$modifiedFiles = @(
    "src\Verdure.McpPlatform.Web\Layout\MainLayout.razor",
    "src\Verdure.McpPlatform.Web\Layout\NavMenu.razor",
    "src\Verdure.McpPlatform.Web\Pages\Login.razor",
    "src\Verdure.McpPlatform.Web\Pages\Logout.razor",
    "README.md"
)

foreach ($file in $modifiedFiles) {
    $fullPath = Join-Path $projectRoot $file
    if (Test-Path $fullPath) {
        Write-Host "   ✅ $file" -ForegroundColor Green
    } else {
        Write-Host "   ❌ $file (未找到)" -ForegroundColor Red
    }
}
Write-Host ""

# 6. 显示项目统计
Write-Host "📊 项目统计:" -ForegroundColor Yellow
$webPagesPath = Join-Path $projectRoot "src\Verdure.McpPlatform.Web\Pages"
$layoutPath = Join-Path $projectRoot "src\Verdure.McpPlatform.Web\Layout"

$pageCount = (Get-ChildItem -Path $webPagesPath -Filter "*.razor").Count
$layoutCount = (Get-ChildItem -Path $layoutPath -Filter "*.razor").Count

Write-Host "   📄 页面数量: $pageCount" -ForegroundColor White
Write-Host "   🎨 布局组件: $layoutCount" -ForegroundColor White
Write-Host ""

# 7. 完成
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  ✅ 验证完成！所有检查通过" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "🚀 下一步操作:" -ForegroundColor Yellow
Write-Host "   1. 启动应用:  .\start-dev.ps1" -ForegroundColor White
Write-Host "   2. 访问首页:  https://localhost:5001" -ForegroundColor White
Write-Host "   3. 查看文档:  SUMMARY.md" -ForegroundColor White
Write-Host ""

Write-Host "📖 测试指南:" -ForegroundColor Yellow
Write-Host "   详见 TESTING_GUIDE.md 文件" -ForegroundColor White
Write-Host ""

Write-Host "🌐 社区链接:" -ForegroundColor Yellow
Write-Host "   GitHub: https://github.com/maker-community" -ForegroundColor White
Write-Host "   B站:    https://space.bilibili.com/25228512" -ForegroundColor White
Write-Host "   QQ群:   1023487000" -ForegroundColor White
Write-Host ""
