# Batch refactoring script for renaming MCP entities
# Run this from the repository root

$rootPath = "c:\Users\gil\Music\github\verdure-mcp-for-xiaozhi\src"

# Define replacement patterns
$replacements = @(
    # Namespace replacements
    @{ Pattern = 'Verdure\.McpPlatform\.Domain\.AggregatesModel\.McpServerAggregate'; Replacement = 'Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiConnectionAggregate' }
    
    # Class/Interface name replacements
    @{ Pattern = '\bIMcpServerRepository\b'; Replacement = 'IXiaozhiConnectionRepository' }
    @{ Pattern = '\bMcpServerRepository\b'; Replacement = 'XiaozhiConnectionRepository' }
    @{ Pattern = '\bIMcpServerService\b'; Replacement = 'IXiaozhiConnectionService' }
    @{ Pattern = '\bMcpServerService\b'; Replacement = 'XiaozhiConnectionService' }
    @{ Pattern = '\bIMcpBindingService\b'; Replacement = 'IMcpServiceBindingService' }
    @{ Pattern = '\bMcpBindingService\b'; Replacement = 'McpServiceBindingService' }
    @{ Pattern = '\bIMcpServerClientService\b'; Replacement = 'IXiaozhiConnectionClientService' }
    @{ Pattern = '\bMcpServerClientService\b'; Replacement = 'XiaozhiConnectionClientService' }
    @{ Pattern = '\bIMcpBindingClientService\b'; Replacement = 'IMcpServiceBindingClientService' }
    @{ Pattern = '\bMcpBindingClientService\b'; Replacement = 'McpServiceBindingClientService' }
    
    # Type name replacements
    @{ Pattern = '\bMcpServer\b'; Replacement = 'XiaozhiConnection' }
    @{ Pattern = '\bMcpBinding\b'; Replacement = 'McpServiceBinding' }
    
    # DTO and Request replacements
    @{ Pattern = '\bMcpServerDto\b'; Replacement = 'XiaozhiConnectionDto' }
    @{ Pattern = '\bMcpBindingDto\b'; Replacement = 'McpServiceBindingDto' }
    @{ Pattern = '\bCreateMcpServerRequest\b'; Replacement = 'CreateXiaozhiConnectionRequest' }
    @{ Pattern = '\bUpdateMcpServerRequest\b'; Replacement = 'UpdateXiaozhiConnectionRequest' }
    @{ Pattern = '\bCreateMcpBindingRequest\b'; Replacement = 'CreateMcpServiceBindingRequest' }
    @{ Pattern = '\bUpdateMcpBindingRequest\b'; Replacement = 'UpdateMcpServiceBindingRequest' }
    
    # Property name replacements
    @{ Pattern = '\bMcpServerId\b'; Replacement = 'XiaozhiConnectionId' }
    @{ Pattern = '\b\.Bindings\b'; Replacement = '.ServiceBindings' }
    @{ Pattern = '\b_bindings\b'; Replacement = '_serviceBindings' }
    
    # API route replacements - commented out as we may want different routes
    # @{ Pattern = '/api/mcp-servers'; Replacement = '/api/xiaozhi-connections' }
    # @{ Pattern = '/api/mcp-bindings'; Replacement = '/api/mcp-service-bindings' }
)

# Get all C# and Razor files
$files = Get-ChildItem -Path $rootPath -Include *.cs,*.razor -Recurse

Write-Host "Found $($files.Count) files to process..."

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    
    foreach ($replacement in $replacements) {
        $content = $content -replace $replacement.Pattern, $replacement.Replacement
    }
    
    if ($content -ne $originalContent) {
        Set-Content $file.FullName -Value $content -Encoding UTF8 -NoNewline
        Write-Host "Updated: $($file.FullName)"
    }
}

Write-Host "Refactoring complete!"
