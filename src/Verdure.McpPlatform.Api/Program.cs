using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Scalar.AspNetCore;
using Verdure.McpPlatform.Api.Apis;
using Verdure.McpPlatform.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add application services (includes authentication)
builder.AddApplicationServices();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    // In development, enable detailed errors for Blazor
    app.UseWebAssemblyDebugging();
}

// Apply database migrations
await app.ApplyDatabaseMigrations();

// Use CORS
app.UseCors();

app.UseHttpsRedirection();

// Serve Blazor WebAssembly static files
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Map API endpoints with /api prefix
app.MapUserApi();
app.MapXiaozhiMcpEndpointApi();
app.MapMcpServiceBindingApi();
app.MapMcpServiceConfigApi();

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("Health");

// Fallback to index.html for Blazor SPA routing
// This must be the last mapping
app.MapFallbackToFile("index.html");

app.Run();
