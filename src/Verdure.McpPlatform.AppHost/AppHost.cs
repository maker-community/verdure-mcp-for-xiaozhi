var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
//var postgres = builder.AddPostgres("postgres")
//    .WithDataVolume("verdure_mcp_data")
//    .WithPgAdmin(
//       c => c.WithImage("dpage/pgadmin4")
//             .WithImageTag("9.9.0")
//             .WithHostPort(5052)
//    );

//var mcpDb = postgres.AddDatabase("mcpdb");
//var identityDb = postgres.AddDatabase("identitydb");

// Add API service
var api = builder.AddProject<Projects.Verdure_McpPlatform_Api>("api")
    //.WithReference(mcpDb)
    //.WithReference(identityDb)
    //.WaitFor(postgres)
    .WithExternalHttpEndpoints();

// Add Blazor WebAssembly frontend
builder.AddProject<Projects.Verdure_McpPlatform_Web>("web")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
