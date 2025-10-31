using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Verdure.McpPlatform.Api.Services;
using Verdure.McpPlatform.Api.Services.WebSocket;
using Verdure.McpPlatform.Application.Services;
using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Api.Apis;

/// <summary>
/// API endpoints for MCP Server management
/// </summary>
public static class XiaozhiConnectionApi
{
    public static RouteGroupBuilder MapXiaozhiConnectionApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/mcp-servers")
            .RequireAuthorization()
            .WithTags("MCP Servers")
            .WithOpenApi();

        api.MapGet("/", GetMcpServersAsync)
            .WithName("GetMcpServers")
            .Produces<IEnumerable<XiaozhiConnectionDto>>();

        api.MapGet("/{id}", GetMcpServerAsync)
            .WithName("GetMcpServer")
            .Produces<XiaozhiConnectionDto>()
            .Produces(StatusCodes.Status404NotFound);

        api.MapPost("/", CreateMcpServerAsync)
            .WithName("CreateMcpServer")
            .Produces<XiaozhiConnectionDto>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        api.MapPut("/{id}", UpdateMcpServerAsync)
            .WithName("UpdateMcpServer")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        api.MapDelete("/{id}", DeleteMcpServerAsync)
            .WithName("DeleteMcpServer")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        api.MapPost("/{id}/enable", EnableMcpServerAsync)
            .WithName("EnableMcpServer")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        api.MapPost("/{id}/disable", DisableMcpServerAsync)
            .WithName("DisableMcpServer")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<Ok<IEnumerable<XiaozhiConnectionDto>>> GetMcpServersAsync(
        IXiaozhiConnectionService XiaozhiConnectionService,
        IIdentityService identityService)
    {
        var userId = identityService.GetUserIdentity();
        var servers = await XiaozhiConnectionService.GetByUserAsync(userId);
        return TypedResults.Ok(servers);
    }

    private static async Task<Results<Ok<XiaozhiConnectionDto>, NotFound>> GetMcpServerAsync(
        string id,
        IXiaozhiConnectionService XiaozhiConnectionService,
        IIdentityService identityService)
    {
        var userId = identityService.GetUserIdentity();
        var server = await XiaozhiConnectionService.GetByIdAsync(id, userId);
        
        return server is not null
            ? TypedResults.Ok(server)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Created<XiaozhiConnectionDto>, ValidationProblem>> CreateMcpServerAsync(
        CreateXiaozhiConnectionRequest request,
        IXiaozhiConnectionService XiaozhiConnectionService,
        IIdentityService identityService)
    {
        var userId = identityService.GetUserIdentity();
        var server = await XiaozhiConnectionService.CreateAsync(request, userId);
        return TypedResults.Created($"/api/mcp-servers/{server.Id}", server);
    }

    private static async Task<Results<NoContent, NotFound>> UpdateMcpServerAsync(
        string id,
        UpdateXiaozhiConnectionRequest request,
        IXiaozhiConnectionService XiaozhiConnectionService,
        IIdentityService identityService)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            await XiaozhiConnectionService.UpdateAsync(id, request, userId);
            return TypedResults.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<NoContent, NotFound>> DeleteMcpServerAsync(
        string id,
        IXiaozhiConnectionService XiaozhiConnectionService,
        IIdentityService identityService)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            await XiaozhiConnectionService.DeleteAsync(id, userId);
            return TypedResults.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<NoContent, NotFound>> EnableMcpServerAsync(
        string id,
        IXiaozhiConnectionService XiaozhiConnectionService,
        IIdentityService identityService,
        McpSessionManager sessionManager)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            await XiaozhiConnectionService.EnableAsync(id, userId);
            
            // Start WebSocket session
            _ = Task.Run(async () => await sessionManager.StartSessionAsync(id));
            
            return TypedResults.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<NoContent, NotFound>> DisableMcpServerAsync(
        string id,
        IXiaozhiConnectionService XiaozhiConnectionService,
        IIdentityService identityService,
        McpSessionManager sessionManager)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            await XiaozhiConnectionService.DisableAsync(id, userId);
            
            // Stop WebSocket session
            await sessionManager.StopSessionAsync(id);
            
            return TypedResults.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }
}
