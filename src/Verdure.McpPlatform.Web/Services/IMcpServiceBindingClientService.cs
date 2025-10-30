using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// Service for managing MCP bindings through the API
/// </summary>
public interface IMcpServiceBindingClientService
{
    Task<IEnumerable<McpServiceBindingDto>> GetBindingsByServerAsync(int serverId);
    Task<IEnumerable<McpServiceBindingDto>> GetActiveBindingsAsync();
    Task<McpServiceBindingDto?> GetBindingAsync(int id);
    Task<McpServiceBindingDto> CreateBindingAsync(CreateMcpServiceBindingRequest request);
    Task UpdateBindingAsync(int id, UpdateMcpServiceBindingRequest request);
    Task ActivateBindingAsync(int id);
    Task DeactivateBindingAsync(int id);
    Task DeleteBindingAsync(int id);
}
