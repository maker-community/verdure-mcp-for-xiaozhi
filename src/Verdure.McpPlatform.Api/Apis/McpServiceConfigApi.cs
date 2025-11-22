using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Verdure.McpPlatform.Api.Services;
using Verdure.McpPlatform.Application.Services;
using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Models;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Api.Apis;

/// <summary>
/// API endpoints for MCP Service Configuration management
/// </summary>
public static class McpServiceConfigApi
{
    public static RouteGroupBuilder MapMcpServiceConfigApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/mcp-services")
            .RequireAuthorization()
            .WithTags("MCP Service Configurations")
            .WithOpenApi();

        api.MapGet("/", GetMcpServicesAsync)
            .WithName("GetMcpServices")
            .Produces<IEnumerable<McpServiceConfigDto>>();

        api.MapGet("/paged", GetMcpServicesPagedAsync)
            .WithName("GetMcpServicesPaged")
            .Produces<PagedResult<McpServiceConfigDto>>();

        api.MapGet("/public", GetPublicMcpServicesAsync)
            .WithName("GetPublicMcpServices")
            .Produces<IEnumerable<McpServiceConfigDto>>();

        api.MapGet("/public/paged", GetPublicMcpServicesPagedAsync)
            .WithName("GetPublicMcpServicesPaged")
            .Produces<PagedResult<McpServiceConfigDto>>();

        api.MapGet("/{id}", GetMcpServiceAsync)
            .WithName("GetMcpService")
            .Produces<McpServiceConfigDto>()
            .Produces(StatusCodes.Status404NotFound);

        api.MapGet("/{id}/tools", GetMcpServiceToolsAsync)
            .WithName("GetMcpServiceTools")
            .Produces<IEnumerable<McpToolDto>>()
            .Produces(StatusCodes.Status404NotFound);

        api.MapPost("/", CreateMcpServiceAsync)
            .WithName("CreateMcpService")
            .Produces<McpServiceConfigDto>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        api.MapPut("/{id}", UpdateMcpServiceAsync)
            .WithName("UpdateMcpService")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        api.MapDelete("/{id}", DeleteMcpServiceAsync)
            .WithName("DeleteMcpService")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        api.MapPost("/{id}/sync-tools", SyncToolsAsync)
            .WithName("SyncMcpServiceTools")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        api.MapPut("/{id}/set-public", SetPublicAsync)
            .WithName("SetMcpServicePublic")
            .RequireAuthorization("AdminOnly")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        api.MapPut("/{id}/set-private", SetPrivateAsync)
            .WithName("SetMcpServicePrivate")
            .RequireAuthorization("AdminOnly")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        api.MapPost("/sync-all-tools", SyncAllToolsAsync)
            .WithName("SyncAllMcpServiceTools")
            .RequireAuthorization("AdminOnly")
            .Produces<SyncAllToolsResultDto>()
            .Produces(StatusCodes.Status403Forbidden);

        return api;
    }

    private static async Task<Ok<IEnumerable<McpServiceConfigDto>>> GetMcpServicesAsync(
        IMcpServiceConfigService mcpServiceConfigService,
        IIdentityService identityService)
    {
        var userId = identityService.GetUserIdentity();
        var services = await mcpServiceConfigService.GetByUserAsync(userId);
        return TypedResults.Ok(services);
    }

    private static async Task<Ok<PagedResult<McpServiceConfigDto>>> GetMcpServicesPagedAsync(
        [AsParameters] PagedRequest request,
        IMcpServiceConfigService mcpServiceConfigService,
        IIdentityService identityService)
    {
        var userId = identityService.GetUserIdentity();
        var result = await mcpServiceConfigService.GetByUserPagedAsync(userId, request);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<IEnumerable<McpServiceConfigDto>>> GetPublicMcpServicesAsync(
        IMcpServiceConfigService mcpServiceConfigService)
    {
        var services = await mcpServiceConfigService.GetPublicServicesAsync();
        return TypedResults.Ok(services);
    }

    private static async Task<Ok<PagedResult<McpServiceConfigDto>>> GetPublicMcpServicesPagedAsync(
        [AsParameters] PagedRequest request,
        IMcpServiceConfigService mcpServiceConfigService)
    {
        var result = await mcpServiceConfigService.GetPublicServicesPagedAsync(request);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<McpServiceConfigDto>, NotFound>> GetMcpServiceAsync(
        string id,
        IMcpServiceConfigService mcpServiceConfigService,
        IIdentityService identityService)
    {
        var userId = identityService.GetUserIdentity();
        var service = await mcpServiceConfigService.GetByIdAsync(id, userId);
        
        return service is not null
            ? TypedResults.Ok(service)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<IEnumerable<McpToolDto>>, NotFound>> GetMcpServiceToolsAsync(
        string id,
        IMcpServiceConfigService mcpServiceConfigService,
        IIdentityService identityService)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            var tools = await mcpServiceConfigService.GetToolsAsync(id, userId);
            return TypedResults.Ok(tools);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Created<McpServiceConfigDto>, ValidationProblem>> CreateMcpServiceAsync(
        CreateMcpServiceConfigRequest request,
        IMcpServiceConfigService mcpServiceConfigService,
        IIdentityService identityService)
    {
        var userId = identityService.GetUserIdentity();
        var service = await mcpServiceConfigService.CreateAsync(request, userId);
        return TypedResults.Created($"/api/mcp-services/{service.Id}", service);
    }

    private static async Task<Results<NoContent, NotFound>> UpdateMcpServiceAsync(
        string id,
        UpdateMcpServiceConfigRequest request,
        IMcpServiceConfigService mcpServiceConfigService,
        IIdentityService identityService)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            await mcpServiceConfigService.UpdateAsync(id, request, userId);
            return TypedResults.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<NoContent, NotFound>> DeleteMcpServiceAsync(
        string id,
        IMcpServiceConfigService mcpServiceConfigService,
        IIdentityService identityService)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            await mcpServiceConfigService.DeleteAsync(id, userId);
            return TypedResults.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> SyncToolsAsync(
        string id,
        IMcpServiceConfigService mcpServiceConfigService,
        IIdentityService identityService)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            await mcpServiceConfigService.SyncToolsAsync(id, userId);
            return TypedResults.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
        catch (NotImplementedException ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status501NotImplemented);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<Results<NoContent, NotFound>> SetPublicAsync(
        string id,
        IMcpServiceConfigService mcpServiceConfigService,
        IIdentityService identityService)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            await mcpServiceConfigService.SetPublicAsync(id, userId);
            return TypedResults.NoContent();
        }
        catch (InvalidOperationException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<NoContent, NotFound>> SetPrivateAsync(
        string id,
        IMcpServiceConfigService mcpServiceConfigService,
        IIdentityService identityService)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            await mcpServiceConfigService.SetPrivateAsync(id, userId);
            return TypedResults.NoContent();
        }
        catch (InvalidOperationException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Ok<SyncAllToolsResultDto>> SyncAllToolsAsync(
        IMcpServiceConfigService mcpServiceConfigService)
    {
        var result = await mcpServiceConfigService.SyncAllToolsAsync();
        return TypedResults.Ok(result);
    }
}
