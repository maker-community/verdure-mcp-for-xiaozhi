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
public static class McpServiceBindingApi
{
    public static RouteGroupBuilder MapMcpServiceBindingApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/mcp-bindings")
            .RequireAuthorization()
            .WithTags("MCP Bindings")
            .WithOpenApi();

        api.MapGet("/server/{serverId}", GetBindingsByServerAsync)
            .WithName("GetBindingsByServer")
            .Produces<IEnumerable<McpServiceBindingDto>>();

        api.MapGet("/active", GetActiveBindingsAsync)
            .WithName("GetActiveBindings")
            .Produces<IEnumerable<McpServiceBindingDto>>();

        api.MapGet("/{id}", GetBindingAsync)
            .WithName("GetBinding")
            .Produces<McpServiceBindingDto>()
            .Produces(StatusCodes.Status404NotFound);

        api.MapPost("/", CreateBindingAsync)
            .WithName("CreateBinding")
            .Produces<McpServiceBindingDto>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        api.MapPut("/{id}", UpdateBindingAsync)
            .WithName("UpdateBinding")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        api.MapPut("/{id}/activate", ActivateBindingAsync)
            .WithName("ActivateBinding")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        api.MapPut("/{id}/deactivate", DeactivateBindingAsync)
            .WithName("DeactivateBinding")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        api.MapDelete("/{id}", DeleteBindingAsync)
            .WithName("DeleteBinding")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<Ok<IEnumerable<McpServiceBindingDto>>> GetBindingsByServerAsync(
        string serverId,
        IMcpServiceBindingService McpServiceBindingService,
        IIdentityService identityService)
    {
        var userId = identityService.GetUserIdentity();
        var bindings = await McpServiceBindingService.GetByServerAsync(serverId, userId);
        return TypedResults.Ok(bindings);
    }

    private static async Task<Ok<IEnumerable<McpServiceBindingDto>>> GetActiveBindingsAsync(
        IMcpServiceBindingService McpServiceBindingService)
    {
        var bindings = await McpServiceBindingService.GetActiveServiceBindingsAsync();
        return TypedResults.Ok(bindings);
    }

    private static async Task<Results<Ok<McpServiceBindingDto>, NotFound>> GetBindingAsync(
        string id,
        IMcpServiceBindingService McpServiceBindingService,
        IIdentityService identityService)
    {
        var userId = identityService.GetUserIdentity();
        var binding = await McpServiceBindingService.GetByIdAsync(id, userId);
        
        return binding is not null
            ? TypedResults.Ok(binding)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Created<McpServiceBindingDto>, ValidationProblem>> CreateBindingAsync(
        CreateMcpServiceBindingRequest request,
        IMcpServiceBindingService McpServiceBindingService,
        IIdentityService identityService,
        McpSessionManager sessionManager)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            var binding = await McpServiceBindingService.CreateAsync(request, userId);
            
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
        string id,
        UpdateMcpServiceBindingRequest request,
        IMcpServiceBindingService McpServiceBindingService,
        IIdentityService identityService,
        McpSessionManager sessionManager)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            
            // Get binding before updating to get server ID
            var binding = await McpServiceBindingService.GetByIdAsync(id, userId);
            if (binding == null)
            {
                return TypedResults.NotFound();
            }
            
            await McpServiceBindingService.UpdateAsync(id, request, userId);
            
            // Restart session to pick up updated binding and tool selections
            _ = Task.Run(async () => await sessionManager.RestartSessionAsync(binding.XiaozhiConnectionId));
            
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
        string id,
        IMcpServiceBindingService McpServiceBindingService,
        IIdentityService identityService,
        McpSessionManager sessionManager)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            
            // Get binding to get server ID
            var binding = await McpServiceBindingService.GetByIdAsync(id, userId);
            if (binding == null)
            {
                return TypedResults.NotFound();
            }
            
            await McpServiceBindingService.ActivateAsync(id, userId);
            
            // Restart session to activate binding
            _ = Task.Run(async () => await sessionManager.RestartSessionAsync(binding.XiaozhiConnectionId));
            
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
        string id,
        IMcpServiceBindingService McpServiceBindingService,
        IIdentityService identityService,
        McpSessionManager sessionManager)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            
            // Get binding to get server ID
            var binding = await McpServiceBindingService.GetByIdAsync(id, userId);
            if (binding == null)
            {
                return TypedResults.NotFound();
            }
            
            await McpServiceBindingService.DeactivateAsync(id, userId);
            
            // Restart session to deactivate binding
            _ = Task.Run(async () => await sessionManager.RestartSessionAsync(binding.XiaozhiConnectionId));
            
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
        string id,
        IMcpServiceBindingService McpServiceBindingService,
        IIdentityService identityService,
        McpSessionManager sessionManager)
    {
        try
        {
            var userId = identityService.GetUserIdentity();
            
            // Get binding before deleting to get server ID
            var binding = await McpServiceBindingService.GetByIdAsync(id, userId);
            if (binding == null)
            {
                return TypedResults.NotFound();
            }
            
            await McpServiceBindingService.DeleteAsync(id, userId);
            
            // Restart session to remove deleted binding
            _ = Task.Run(async () => await sessionManager.RestartSessionAsync(binding.XiaozhiConnectionId));
            
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
