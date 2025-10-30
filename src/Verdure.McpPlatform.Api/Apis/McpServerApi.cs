using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Verdure.McpPlatform.Api.Services;
using Verdure.McpPlatform.Application.Services;
using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Api.Apis;

/// <summary>
/// API endpoints for MCP Server management
/// </summary>
public static class McpServerApi
{
    public static RouteGroupBuilder MapMcpServerApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/mcp-servers")
            .RequireAuthorization()
            .WithTags("MCP Servers")
            .WithOpenApi();

        api.MapGet("/", GetMcpServersAsync)
            .WithName("GetMcpServers")
            .Produces<IEnumerable<McpServerDto>>();

        api.MapGet("/{id:int}", GetMcpServerAsync)
            .WithName("GetMcpServer")
            .Produces<McpServerDto>()
            .Produces(StatusCodes.Status404NotFound);

        api.MapPost("/", CreateMcpServerAsync)
            .WithName("CreateMcpServer")
            .Produces<McpServerDto>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        api.MapPut("/{id:int}", UpdateMcpServerAsync)
            .WithName("UpdateMcpServer")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        api.MapDelete("/{id:int}", DeleteMcpServerAsync)
            .WithName("DeleteMcpServer")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<Ok<IEnumerable<McpServerDto>>> GetMcpServersAsync(
        IMcpServerService mcpServerService,
        IIdentityService identityService)
    {
        var userId = identityService.GetUserIdentity();
        var servers = await mcpServerService.GetByUserAsync(userId);
        return TypedResults.Ok(servers);
    }

    private static async Task<Results<Ok<McpServerDto>, NotFound>> GetMcpServerAsync(
        int id,
        IMcpServerService mcpServerService,
        IIdentityService identityService)
    {
        var userId = identityService.GetUserIdentity();
        var server = await mcpServerService.GetByIdAsync(id, userId);
        
        return server is not null
            ? TypedResults.Ok(server)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Created<McpServerDto>, ValidationProblem>> CreateMcpServerAsync(
        CreateMcpServerRequest request,
        IMcpServerService mcpServerService,
        IIdentityService identityService)
    {
        var userId = identityService.GetUserIdentity();
        var server = await mcpServerService.CreateAsync(request, userId);
        return TypedResults.Created($"/api/mcp-servers/{server.Id}", server);
    }

    private static async Task<Results<NoContent, NotFound>> UpdateMcpServerAsync(
        int id,
        UpdateMcpServerRequest request,
        IMcpServerService mcpServerService,
        IIdentityService identityService)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            await mcpServerService.UpdateAsync(id, request, userId);
            return TypedResults.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<NoContent, NotFound>> DeleteMcpServerAsync(
        int id,
        IMcpServerService mcpServerService,
        IIdentityService identityService)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            await mcpServerService.DeleteAsync(id, userId);
            return TypedResults.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }
}
