using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Verdure.McpPlatform.Api.Services;
using Verdure.McpPlatform.Api.Services.WebSocket;
using Verdure.McpPlatform.Application.Services;
using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Api.Apis;

/// <summary>
/// API endpoints for MCP Binding management
/// </summary>
public static class McpBindingApi
{
    public static RouteGroupBuilder MapMcpBindingApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/mcp-bindings")
            .RequireAuthorization()
            .WithTags("MCP Bindings")
            .WithOpenApi();

        api.MapGet("/server/{serverId:int}", GetBindingsByServerAsync)
            .WithName("GetBindingsByServer")
            .Produces<IEnumerable<McpBindingDto>>();

        api.MapGet("/active", GetActiveBindingsAsync)
            .WithName("GetActiveBindings")
            .Produces<IEnumerable<McpBindingDto>>();

        api.MapGet("/{id:int}", GetBindingAsync)
            .WithName("GetBinding")
            .Produces<McpBindingDto>()
            .Produces(StatusCodes.Status404NotFound);

        api.MapPost("/", CreateBindingAsync)
            .WithName("CreateBinding")
            .Produces<McpBindingDto>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        api.MapPut("/{id:int}", UpdateBindingAsync)
            .WithName("UpdateBinding")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        api.MapPut("/{id:int}/activate", ActivateBindingAsync)
            .WithName("ActivateBinding")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        api.MapPut("/{id:int}/deactivate", DeactivateBindingAsync)
            .WithName("DeactivateBinding")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        api.MapDelete("/{id:int}", DeleteBindingAsync)
            .WithName("DeleteBinding")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<Ok<IEnumerable<McpBindingDto>>> GetBindingsByServerAsync(
        int serverId,
        IMcpBindingService mcpBindingService,
        IIdentityService identityService)
    {
        var userId = identityService.GetUserIdentity();
        var bindings = await mcpBindingService.GetByServerAsync(serverId, userId);
        return TypedResults.Ok(bindings);
    }

    private static async Task<Ok<IEnumerable<McpBindingDto>>> GetActiveBindingsAsync(
        IMcpBindingService mcpBindingService)
    {
        var bindings = await mcpBindingService.GetActiveBindingsAsync();
        return TypedResults.Ok(bindings);
    }

    private static async Task<Results<Ok<McpBindingDto>, NotFound>> GetBindingAsync(
        int id,
        IMcpBindingService mcpBindingService,
        IIdentityService identityService)
    {
        var userId = identityService.GetUserIdentity();
        var binding = await mcpBindingService.GetByIdAsync(id, userId);
        
        return binding is not null
            ? TypedResults.Ok(binding)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Created<McpBindingDto>, ValidationProblem>> CreateBindingAsync(
        CreateMcpBindingRequest request,
        IMcpBindingService mcpBindingService,
        IIdentityService identityService,
        McpSessionManager sessionManager)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            var binding = await mcpBindingService.CreateAsync(request, userId);
            
            // Restart session to pick up new binding
            _ = Task.Run(async () => await sessionManager.RestartSessionAsync(request.ServerId));
            
            return TypedResults.Created($"/api/mcp-bindings/{binding.Id}", binding);
        }
        catch (UnauthorizedAccessException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["error"] = new[] { ex.Message }
            });
        }
    }

    private static async Task<Results<NoContent, NotFound>> UpdateBindingAsync(
        int id,
        UpdateMcpBindingRequest request,
        IMcpBindingService mcpBindingService,
        IIdentityService identityService)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            await mcpBindingService.UpdateAsync(id, request, userId);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<NoContent, NotFound>> ActivateBindingAsync(
        int id,
        IMcpBindingService mcpBindingService,
        IIdentityService identityService)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            await mcpBindingService.ActivateAsync(id, userId);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<NoContent, NotFound>> DeactivateBindingAsync(
        int id,
        IMcpBindingService mcpBindingService,
        IIdentityService identityService)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            await mcpBindingService.DeactivateAsync(id, userId);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<NoContent, NotFound>> DeleteBindingAsync(
        int id,
        IMcpBindingService mcpBindingService,
        IIdentityService identityService,
        McpSessionManager sessionManager)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            
            // Get binding before deleting to get server ID
            var binding = await mcpBindingService.GetByIdAsync(id, userId);
            if (binding == null)
            {
                return TypedResults.NotFound();
            }
            
            await mcpBindingService.DeleteAsync(id, userId);
            
            // Restart session to remove deleted binding
            _ = Task.Run(async () => await sessionManager.RestartSessionAsync(binding.McpServerId));
            
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.NotFound();
        }
    }
}
