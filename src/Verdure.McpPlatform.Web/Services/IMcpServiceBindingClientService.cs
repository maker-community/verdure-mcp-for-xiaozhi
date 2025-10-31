using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// Service for managing MCP bindings through the API
/// </summary>
public interface IMcpServiceBindingClientService
{
    Task<IEnumerable<McpServiceBindingDto>> GetBindingsByServerAsync(string serverId);
    Task<IEnumerable<McpServiceBindingDto>> GetActiveBindingsAsync();
    Task<McpServiceBindingDto?> GetBindingAsync(string id);
    Task<McpServiceBindingDto> CreateBindingAsync(CreateMcpServiceBindingRequest request);
    Task UpdateBindingAsync(string id, UpdateMcpServiceBindingRequest request);
    Task ActivateBindingAsync(string id);
    Task DeactivateBindingAsync(string id);
    Task DeleteBindingAsync(string id);
}
