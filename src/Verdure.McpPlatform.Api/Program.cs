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
}

// Apply database migrations
await app.ApplyDatabaseMigrations();

// Use CORS
app.UseCors();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Map API endpoints
app.MapXiaozhiMcpEndpointApi();
app.MapMcpServiceBindingApi();
app.MapMcpServiceConfigApi();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("Health");

app.Run();
