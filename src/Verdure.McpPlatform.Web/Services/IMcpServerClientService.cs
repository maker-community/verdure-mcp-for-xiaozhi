using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;

namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// Service for managing MCP servers through the API
/// </summary>
public interface IMcpServerClientService
{
    Task<IEnumerable<McpServerDto>> GetServersAsync();
    Task<McpServerDto?> GetServerAsync(int id);
    Task<McpServerDto> CreateServerAsync(CreateMcpServerRequest request);
    Task UpdateServerAsync(int id, UpdateMcpServerRequest request);
    Task DeleteServerAsync(int id);
    Task EnableServerAsync(int id);
    Task DisableServerAsync(int id);
}
