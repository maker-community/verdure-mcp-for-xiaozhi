using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Service interface for MCP Binding operations
/// </summary>
public interface IMcpServiceBindingService
{
    Task<McpServiceBindingDto> CreateAsync(CreateMcpServiceBindingRequest request, string userId);
    Task<McpServiceBindingDto?> GetByIdAsync(string id, string userId);
    Task<IEnumerable<McpServiceBindingDto>> GetByServerAsync(string serverId, string userId);
    Task<IEnumerable<McpServiceBindingDto>> GetActiveServiceBindingsAsync();
    Task UpdateAsync(string id, UpdateMcpServiceBindingRequest request, string userId);
    Task ActivateAsync(string id, string userId);
    Task DeactivateAsync(string id, string userId);
    Task DeleteAsync(string id, string userId);
}
