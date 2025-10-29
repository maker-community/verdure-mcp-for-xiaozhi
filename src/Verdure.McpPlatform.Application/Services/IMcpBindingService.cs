using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Service interface for MCP Binding operations
/// </summary>
public interface IMcpBindingService
{
    Task<McpBindingDto> CreateAsync(CreateMcpBindingRequest request, string userId);
    Task<McpBindingDto?> GetByIdAsync(int id, string userId);
    Task<IEnumerable<McpBindingDto>> GetByServerAsync(int serverId, string userId);
    Task<IEnumerable<McpBindingDto>> GetActiveBindingsAsync();
    Task UpdateAsync(int id, UpdateMcpBindingRequest request, string userId);
    Task ActivateAsync(int id, string userId);
    Task DeactivateAsync(int id, string userId);
    Task DeleteAsync(int id, string userId);
}
