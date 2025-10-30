using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// Service for managing MCP bindings through the API
/// </summary>
public interface IMcpBindingClientService
{
    Task<IEnumerable<McpBindingDto>> GetBindingsByServerAsync(int serverId);
    Task<IEnumerable<McpBindingDto>> GetActiveBindingsAsync();
    Task<McpBindingDto?> GetBindingAsync(int id);
    Task<McpBindingDto> CreateBindingAsync(CreateMcpBindingRequest request);
    Task UpdateBindingAsync(int id, UpdateMcpBindingRequest request);
    Task ActivateBindingAsync(int id);
    Task DeactivateBindingAsync(int id);
    Task DeleteBindingAsync(int id);
}
