using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Service interface for MCP Server operations
/// </summary>
public interface IMcpServerService
{
    Task<McpServerDto> CreateAsync(CreateMcpServerRequest request, string userId);
    Task<McpServerDto?> GetByIdAsync(int id, string userId);
    Task<IEnumerable<McpServerDto>> GetByUserAsync(string userId);
    Task UpdateAsync(int id, UpdateMcpServerRequest request, string userId);
    Task DeleteAsync(int id, string userId);
    Task EnableAsync(int id, string userId);
    Task DisableAsync(int id, string userId);
}
