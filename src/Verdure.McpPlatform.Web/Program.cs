using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using Verdure.McpPlatform.Web;
using Verdure.McpPlatform.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure API base address
// If ApiBaseAddress is empty or whitespace, use the host environment's base address
var configuredApiBase = builder.Configuration["ApiBaseAddress"];
var apiBaseAddress = string.IsNullOrWhiteSpace(configuredApiBase)
    ? builder.HostEnvironment.BaseAddress.TrimEnd('/')
    : configuredApiBase.TrimEnd('/');

// Add localization services
// FIX ATTEMPT 1: Try simple AddLocalization() without ResourcesPath
// Reference: https://github.com/dotnet/aspnetcore GlobalizationWasmApp
// This works when resource namespace matches the default convention
builder.Services.AddLocalization();

// Add MudBlazor services with custom theme
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 4000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
});

// Add OIDC Authentication with custom role mapping
builder.Services.AddOidcAuthentication(options =>
{
    // Load OIDC settings from configuration
    builder.Configuration.Bind("Oidc", options.ProviderOptions);

    if (string.IsNullOrEmpty(options.ProviderOptions.PostLogoutRedirectUri))
    {
        options.ProviderOptions.PostLogoutRedirectUri = apiBaseAddress;
    }
    
    // Configure for Keycloak
    options.ProviderOptions.ResponseType = "code";
    
    // Only add scopes if not already configured
    // This prevents duplicate scopes like "openid profile email openid profile email"
    if (!options.ProviderOptions.DefaultScopes.Contains("openid"))
    {
        options.ProviderOptions.DefaultScopes.Add("openid");
        options.ProviderOptions.DefaultScopes.Add("profile");
        options.ProviderOptions.DefaultScopes.Add("email");
        // 🔑 添加 offline_access scope 以获取 refresh token
        // 这允许应用在用户离线时刷新 access token
        options.ProviderOptions.DefaultScopes.Add("offline_access");
    }
    else
    {
        options.ProviderOptions.DefaultScopes.Add("email");
        // 🔑 添加 offline_access scope 以获取 refresh token
        // 这允许应用在用户离线时刷新 access token
        options.ProviderOptions.DefaultScopes.Add("offline_access");
    }
})
.AddAccountClaimsPrincipalFactory<KeycloakRoleClaimsPrincipalFactory>();

// Register custom authorization message handler
builder.Services.AddScoped<CustomAuthorizationMessageHandler>();

// Configure HTTP client for API with automatic token attachment
builder.Services.AddHttpClient("Verdure.McpPlatform.Api", client =>
{
    client.BaseAddress = new Uri(apiBaseAddress);
})
.AddHttpMessageHandler<CustomAuthorizationMessageHandler>();

// Configure default HTTP client - 使用命名的 HttpClient
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
    .CreateClient("Verdure.McpPlatform.Api"));

// Register utility services
builder.Services.AddScoped<IDateTimeFormatter, DateTimeFormatter>();

// Register API client services
builder.Services.AddScoped<IUserClientService, UserClientService>();
builder.Services.AddScoped<IXiaozhiMcpEndpointClientService, XiaozhiMcpEndpointClientService>();
builder.Services.AddScoped<IMcpServiceBindingClientService, McpServiceBindingClientService>();
builder.Services.AddScoped<IMcpServiceConfigClientService, McpServiceConfigClientService>();

// Build and run the host
// Note: Application culture is set in index.html via Blazor.start({ applicationCulture })
// The Blazor framework will automatically:
// 1. Set CultureInfo.CurrentCulture and CultureInfo.CurrentUICulture
// 2. Load the appropriate satellite assemblies (e.g., zh-CN\Verdure.McpPlatform.Web.resources.dll)
// 3. Make IStringLocalizer work with the loaded resources
// Reference: https://github.com/dotnet/aspnetcore/tree/main/src/Components/test/testassets/GlobalizationWasmApp
await builder.Build().RunAsync();
