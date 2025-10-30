# Update API routes and Web UI text
$rootPath = "c:\Users\gil\Music\github\verdure-mcp-for-xiaozhi\src"

# API Class name replacements
$apiReplacements = @(
    @{ Pattern = 'public static class McpServerApi'; Replacement = 'public static class XiaozhiConnectionApi' }
    @{ Pattern = 'public static class McpBindingApi'; Replacement = 'public static class McpServiceBindingApi' }
    @{ Pattern = 'MapMcpServerApi'; Replacement = 'MapXiaozhiConnectionApi' }
    @{ Pattern = 'MapMcpBindingApi'; Replacement = 'MapMcpServiceBindingApi' }
)

# Get API files
$apiFiles = Get-ChildItem -Path "$rootPath\Verdure.McpPlatform.Api\Apis" -Include *.cs -Recurse

foreach ($file in $apiFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    
    foreach ($replacement in $apiReplacements) {
        $content = $content -replace $replacement.Pattern, $replacement.Replacement
    }
    
    if ($content -ne $originalContent) {
        Set-Content $file.FullName -Value $content -Encoding UTF8 -NoNewline
        Write-Host "Updated API: $($file.FullName)"
    }
}

# Update Extensions.cs service registration
$extensionsFile = "$rootPath\Verdure.McpPlatform.Api\Extensions\Extensions.cs"
if (Test-Path $extensionsFile) {
    $content = Get-Content $extensionsFile -Raw -Encoding UTF8
    $content = $content -replace '\.MapMcpServerApi\(\)', '.MapXiaozhiConnectionApi()'
    $content = $content -replace '\.MapMcpBindingApi\(\)', '.MapMcpServiceBindingApi()'
    Set-Content $extensionsFile -Value $content -Encoding UTF8 -NoNewline
    Write-Host "Updated Extensions.cs"
}

Write-Host "API updates complete!"
