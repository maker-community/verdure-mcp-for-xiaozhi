var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent);

var mcpDb = postgres.AddDatabase("mcpdb");
var identityDb = postgres.AddDatabase("identitydb");

// Add API service
var api = builder.AddProject<Projects.Verdure_McpPlatform_Api>("api")
    .WithReference(mcpDb)
    .WithReference(identityDb)
    .WithExternalHttpEndpoints();

// Add Blazor WebAssembly frontend
builder.AddProject<Projects.Verdure_McpPlatform_Web>("web")
    .WithReference(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
