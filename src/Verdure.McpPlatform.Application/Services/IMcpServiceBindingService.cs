using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Service interface for MCP Binding operations
/// </summary>
public interface IMcpServiceBindingService
{
    Task<McpServiceBindingDto> CreateAsync(CreateMcpServiceBindingRequest request, string userId);
    Task<McpServiceBindingDto?> GetByIdAsync(int id, string userId);
    Task<IEnumerable<McpServiceBindingDto>> GetByServerAsync(int serverId, string userId);
    Task<IEnumerable<McpServiceBindingDto>> GetActiveServiceBindingsAsync();
    Task UpdateAsync(int id, UpdateMcpServiceBindingRequest request, string userId);
    Task ActivateAsync(int id, string userId);
    Task DeactivateAsync(int id, string userId);
    Task DeleteAsync(int id, string userId);
}
