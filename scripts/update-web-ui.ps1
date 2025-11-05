# Update Web UI text and routes to be more user-friendly
$rootPath = "c:\Users\gil\Music\github\verdure-mcp-for-xiaozhi\src\Verdure.McpPlatform.Web"

# Route replacements for Razor pages
$routeReplacements = @(
    @{ Pattern = '@page "/servers"'; Replacement = '@page "/connections"' }
    @{ Pattern = '@page "/servers/create"'; Replacement = '@page "/connections/create"' }
    @{ Pattern = '@page "/servers/edit/\{Id:int\}"'; Replacement = '@page "/connections/edit/{Id:int}"' }
    @{ Pattern = '@page "/servers/\{ServerId:int\}/bindings"'; Replacement = '@page "/connections/{ConnectionId:int}/service-bindings"' }
    @{ Pattern = '@page "/bindings"'; Replacement = '@page "/service-bindings"' }
    @{ Pattern = '@page "/bindings/create"'; Replacement = '@page "/service-bindings/create"' }
    @{ Pattern = '@page "/bindings/edit/\{Id:int\}"'; Replacement = '@page "/service-bindings/edit/{Id:int}"' }
    
    # Navigation URL replacements
    @{ Pattern = 'Href="/servers"'; Replacement = 'Href="/connections"' }
    @{ Pattern = 'Href="/servers/create"'; Replacement = 'Href="/connections/create"' }
    @{ Pattern = 'NavigateTo\("/servers"\)'; Replacement = 'NavigateTo("/connections")' }
    @{ Pattern = 'NavigateTo\(\$"/servers/edit/\{'; Replacement = 'NavigateTo($"/connections/edit/{' }
    @{ Pattern = 'NavigateTo\(\$"/servers/\{.*?\}/bindings"\)'; Replacement = 'NavigateTo($"/connections/{id}/service-bindings")' }
    @{ Pattern = 'Href="/bindings"'; Replacement = 'Href="/service-bindings"' }
    @{ Pattern = 'Href="/bindings/create'; Replacement = 'Href="/service-bindings/create' }
    @{ Pattern = 'NavigateTo\("/bindings"\)'; Replacement = 'NavigateTo("/service-bindings")' }
    @{ Pattern = 'NavigateTo\(\$"/bindings/edit/'; Replacement = 'NavigateTo($"/service-bindings/edit/' }
    
    # UI Text replacements - make it clear these are Xiaozhi connections
    @{ Pattern = '<PageTitle>MCP Servers -'; Replacement = '<PageTitle>Xiaozhi Connections -' }
    @{ Pattern = '<PageTitle>@\(_isEdit \? "Edit" : "Create"\) Server -'; Replacement = '<PageTitle>@(_isEdit ? "Edit" : "Create") Xiaozhi Connection -' }
    @{ Pattern = '<PageTitle>MCP Bindings -'; Replacement = '<PageTitle>MCP Service Bindings -' }
    @{ Pattern = '<PageTitle>@\(_isEdit \? "Edit" : "Create"\) Binding -'; Replacement = '<PageTitle>@(_isEdit ? "Edit" : "Create") Service Binding -' }
    
    @{ Pattern = '<MudText Typo="Typo\.h3">.*?MCP Servers.*?</MudText>'; Replacement = '<MudText Typo="Typo.h3"><MudIcon Icon="@Icons.Material.Filled.Link" Class="mr-2" />Xiaozhi AI Connections</MudText>' }
    @{ Pattern = '<MudText Typo="Typo\.h6">Your MCP Servers</MudText>'; Replacement = '<MudText Typo="Typo.h6">Your Xiaozhi Connections</MudText>' }
    @{ Pattern = 'Add Server'; Replacement = 'Add Connection' }
    @{ Pattern = '"Search servers\.\.\."'; Replacement = '"Search connections..."' }
    @{ Pattern = 'No servers found\..*?first MCP server!'; Replacement = 'No connections found. Click "Add Connection" to create your first Xiaozhi connection!' }
    @{ Pattern = 'Error loading servers'; Replacement = 'Error loading connections' }
    @{ Pattern = 'Server created successfully'; Replacement = 'Connection created successfully' }
    @{ Pattern = 'Server updated successfully'; Replacement = 'Connection updated successfully' }
    @{ Pattern = 'Server deleted successfully'; Replacement = 'Connection deleted successfully' }
    @{ Pattern = '''Server'''; Replacement = '''Connection''' }
    @{ Pattern = 'Delete server'; Replacement = 'Delete connection' }
    @{ Pattern = 'server '''; Replacement = 'connection ''' }
    
    @{ Pattern = 'Edit.*?MCP Server'; Replacement = 'Edit Xiaozhi Connection' }
    @{ Pattern = 'Create.*?MCP Server'; Replacement = 'Create Xiaozhi Connection' }
    @{ Pattern = 'Server Name'; Replacement = 'Connection Name' }
    @{ Pattern = 'Server Address'; Replacement = 'Xiaozhi WebSocket Endpoint' }
    @{ Pattern = 'Server address is required'; Replacement = 'Xiaozhi endpoint is required' }
    @{ Pattern = 'e\.g\., http://localhost:5000 or https://api\.example\.com'; Replacement = 'e.g., wss://api.xiaozhi.me/mcp/?token=YOUR_TOKEN' }
    @{ Pattern = 'Optional description for this server'; Replacement = 'Optional description for this Xiaozhi connection' }
    @{ Pattern = 'Server Information'; Replacement = 'Connection Information' }
    
    @{ Pattern = '<MudText Typo="Typo\.h3">.*?MCP Bindings.*?</MudText>'; Replacement = '<MudText Typo="Typo.h3"><MudIcon Icon="@Icons.Material.Filled.Extension" Class="mr-2" />MCP Service Bindings</MudText>' }
    @{ Pattern = 'MCP Binding'; Replacement = 'MCP Service Binding' }
    @{ Pattern = 'Add Binding'; Replacement = 'Add Service' }
    @{ Pattern = 'Service Bindings'; Replacement = 'External MCP Services' }
    @{ Pattern = '"Search bindings\.\.\."'; Replacement = '"Search services..."' }
    @{ Pattern = 'No bindings found\..*?first service binding!'; Replacement = 'No services found. Click "Add Service" to bind your first external MCP service!' }
    @{ Pattern = 'Error loading bindings'; Replacement = 'Error loading services' }
    @{ Pattern = 'Binding created successfully'; Replacement = 'Service binding created successfully' }
    @{ Pattern = 'Binding updated successfully'; Replacement = 'Service binding updated successfully' }
    @{ Pattern = 'Binding deleted successfully'; Replacement = 'Service binding deleted successfully' }
    @{ Pattern = 'Delete the binding'; Replacement = 'Delete the service binding' }
    @{ Pattern = 'binding '''; Replacement = 'service ''' }
    
    @{ Pattern = 'MCP Server'; Replacement = 'Xiaozhi Connection' }
    @{ Pattern = 'Please select a server'; Replacement = 'Please select a Xiaozhi connection' }
    @{ Pattern = 'Name of the MCP service'; Replacement = 'Name of the external MCP service' }
    @{ Pattern = 'Node Address'; Replacement = 'MCP Service Endpoint' }
    @{ Pattern = 'Node address is required'; Replacement = 'Service endpoint is required' }
    @{ Pattern = 'e\.g\., ws://localhost:3000 or wss://node\.example\.com'; Replacement = 'e.g., http://localhost:5000/calculator or https://mcp.example.com/weather' }
    @{ Pattern = 'Optional description for this binding'; Replacement = 'Optional description for this service binding' }
    @{ Pattern = 'Binding Information'; Replacement = 'Service Binding Information' }
    
    # Parameter name updates
    @{ Pattern = '\[Parameter\]\s+public int\? ServerId'; Replacement = '[Parameter]\n    public int? ConnectionId' }
    @{ Pattern = 'private int\? _serverId => ServerId'; Replacement = 'private int? _connectionId => ConnectionId' }
    @{ Pattern = '_serverId\.HasValue'; Replacement = '_connectionId.HasValue' }
    @{ Pattern = '_serverId\.Value'; Replacement = '_connectionId.Value' }
    @{ Pattern = '\$"/servers/\{_serverId\}"'; Replacement = '$"/connections/{_connectionId}"' }
    @{ Pattern = 'serverId=\{_serverId\}'; Replacement = 'connectionId={_connectionId}' }
    @{ Pattern = 'serverId='; Replacement = 'connectionId=' }
)

# Get all Razor files
$razorFiles = Get-ChildItem -Path $rootPath -Include *.razor -Recurse

foreach ($file in $razorFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    
    foreach ($replacement in $routeReplacements) {
        $content = $content -replace $replacement.Pattern, $replacement.Replacement
    }
    
    if ($content -ne $originalContent) {
        Set-Content $file.FullName -Value $content -Encoding UTF8 -NoNewline
        Write-Host "Updated: $($file.Name)"
    }
}

# Update NavMenu
$navMenuFile = "$rootPath\Layout\NavMenu.razor"
if (Test-Path $navMenuFile) {
    $content = Get-Content $navMenuFile -Raw -Encoding UTF8
    $content = $content -replace 'Href="/servers"', 'Href="/connections"'
    $content = $content -replace 'Href="/bindings"', 'Href="/service-bindings"'
    $content = $content -replace '>Servers<', '>Connections<'
    $content = $content -replace '>Bindings<', '>Services<'
    Set-Content $navMenuFile -Value $content -Encoding UTF8 -NoNewline
    Write-Host "Updated NavMenu.razor"
}

Write-Host "Web UI updates complete!"
