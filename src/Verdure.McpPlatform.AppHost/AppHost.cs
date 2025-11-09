var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("verdure_mcp_data")
    .WithPgAdmin(
       c => c.WithImage("dpage/pgadmin4")
             .WithImageTag("9.9.0")
             .WithHostPort(5052)
    );

var mcpDb = postgres.AddDatabase("mcpdb");
var identityDb = postgres.AddDatabase("identitydb");

// Add API service (now includes Blazor WebAssembly static files)
// The API project references the Web project, so Blazor WASM will be served from the API
builder.AddProject<Projects.Verdure_McpPlatform_Api>("api")
    .WithReference(mcpDb)
    .WithReference(identityDb)
    .WaitFor(postgres)
    .WithExternalHttpEndpoints();

// Note: Web project is no longer needed as a separate service
// It's now served as static files from the API project

builder.Build().Run();
