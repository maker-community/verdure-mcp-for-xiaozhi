using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Verdure.McpPlatform.Api.Services;
using Verdure.McpPlatform.Api.Services.WebSocket;
using Verdure.McpPlatform.Application.Services;
using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Models;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Api.Apis;

/// <summary>
/// API endpoints for MCP Server management
/// </summary>
public static class XiaozhiMcpEndpointApi
{
    public static RouteGroupBuilder MapXiaozhiMcpEndpointApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/xiaozhi-mcp-endpoints")
            .RequireAuthorization()
            .WithTags("Xiaozhi MCP Endpoints")
            .WithOpenApi();

        api.MapGet("/", GetMcpServersAsync)
            .WithName("GetMcpServers")
            .Produces<IEnumerable<XiaozhiMcpEndpointDto>>();

        api.MapGet("/paged", GetMcpServersPagedAsync)
            .WithName("GetMcpServersPaged")
            .Produces<PagedResult<XiaozhiMcpEndpointDto>>();

        api.MapGet("/{id}", GetMcpServerAsync)
            .WithName("GetMcpServer")
            .Produces<XiaozhiMcpEndpointDto>()
            .Produces(StatusCodes.Status404NotFound);

        api.MapPost("/", CreateMcpServerAsync)
            .WithName("CreateMcpServer")
            .Produces<XiaozhiMcpEndpointDto>(StatusCodes.Status201Created)
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

    private static async Task<Ok<IEnumerable<XiaozhiMcpEndpointDto>>> GetMcpServersAsync(
        IXiaozhiMcpEndpointService XiaozhiMcpEndpointService,
        IIdentityService identityService)
    {
        var userId = identityService.GetUserIdentity();
        var servers = await XiaozhiMcpEndpointService.GetByUserAsync(userId);
        return TypedResults.Ok(servers);
    }

    private static async Task<Ok<PagedResult<XiaozhiMcpEndpointDto>>> GetMcpServersPagedAsync(
        [AsParameters] PagedRequest request,
        IXiaozhiMcpEndpointService XiaozhiMcpEndpointService,
        IIdentityService identityService)
    {
        var userId = identityService.GetUserIdentity();
        var result = await XiaozhiMcpEndpointService.GetByUserPagedAsync(userId, request);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<XiaozhiMcpEndpointDto>, NotFound>> GetMcpServerAsync(
        string id,
        IXiaozhiMcpEndpointService XiaozhiMcpEndpointService,
        IIdentityService identityService)
    {
        var userId = identityService.GetUserIdentity();
        var server = await XiaozhiMcpEndpointService.GetByIdAsync(id, userId);
        
        return server is not null
            ? TypedResults.Ok(server)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Created<XiaozhiMcpEndpointDto>, ValidationProblem>> CreateMcpServerAsync(
        CreateXiaozhiMcpEndpointRequest request,
        IXiaozhiMcpEndpointService XiaozhiMcpEndpointService,
        IIdentityService identityService)
    {
        var userId = identityService.GetUserIdentity();
        var server = await XiaozhiMcpEndpointService.CreateAsync(request, userId);
        return TypedResults.Created($"/api/mcp-servers/{server.Id}", server);
    }

    private static async Task<Results<NoContent, NotFound>> UpdateMcpServerAsync(
        string id,
        UpdateXiaozhiMcpEndpointRequest request,
        IXiaozhiMcpEndpointService XiaozhiMcpEndpointService,
        IIdentityService identityService)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            await XiaozhiMcpEndpointService.UpdateAsync(id, request, userId);
            return TypedResults.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<NoContent, NotFound>> DeleteMcpServerAsync(
        string id,
        IXiaozhiMcpEndpointService XiaozhiMcpEndpointService,
        IIdentityService identityService)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            await XiaozhiMcpEndpointService.DeleteAsync(id, userId);
            return TypedResults.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<NoContent, NotFound>> EnableMcpServerAsync(
        string id,
        IXiaozhiMcpEndpointService XiaozhiMcpEndpointService,
        IIdentityService identityService,
        McpSessionManager sessionManager)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            await XiaozhiMcpEndpointService.EnableAsync(id, userId);
            
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
        IXiaozhiMcpEndpointService XiaozhiMcpEndpointService,
        IIdentityService identityService,
        McpSessionManager sessionManager)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            await XiaozhiMcpEndpointService.DisableAsync(id, userId);
            
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
