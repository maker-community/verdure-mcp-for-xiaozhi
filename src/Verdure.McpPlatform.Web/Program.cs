using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;
using Verdure.McpPlatform.Web;
using Verdure.McpPlatform.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure API base address
var apiBaseAddress = builder.Configuration["ApiBaseAddress"] 
    ?? builder.HostEnvironment.BaseAddress;

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

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Add OIDC Authentication
builder.Services.AddOidcAuthentication(options =>
{
    // Load OIDC settings from configuration
    builder.Configuration.Bind("Oidc", options.ProviderOptions);
    
    // Configure for Keycloak
    options.ProviderOptions.ResponseType = "code";
    options.ProviderOptions.DefaultScopes.Add("openid");
    options.ProviderOptions.DefaultScopes.Add("profile");
    options.ProviderOptions.DefaultScopes.Add("email");
});

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

// Register API client services
builder.Services.AddScoped<IMcpServerClientService, McpServerClientService>();
builder.Services.AddScoped<IMcpBindingClientService, McpBindingClientService>();

await builder.Build().RunAsync();
