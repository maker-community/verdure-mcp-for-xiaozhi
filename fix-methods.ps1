# Fix remaining method name issues
$rootPath = "c:\Users\gil\Music\github\verdure-mcp-for-xiaozhi\src"

$methodReplacements = @(
    @{ Pattern = '\.GetBindingAsync\('; Replacement = '.GetServiceBindingAsync(' }
    @{ Pattern = '\.GetBindingsByServerIdAsync\('; Replacement = '.GetServiceBindingsByConnectionIdAsync(' }
    @{ Pattern = '\.GetActiveBindingsAsync\(\)'; Replacement = '.GetActiveServiceBindingsAsync()' }
)

$files = Get-ChildItem -Path $rootPath -Include *.cs -Recurse

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    
    foreach ($replacement in $methodReplacements) {
        $content = $content -replace $replacement.Pattern, $replacement.Replacement
    }
    
    if ($content -ne $originalContent) {
        Set-Content $file.FullName -Value $content -Encoding UTF8 -NoNewline
        Write-Host "Updated: $($file.FullName)"
    }
}

Write-Host "Method name updates complete!"
