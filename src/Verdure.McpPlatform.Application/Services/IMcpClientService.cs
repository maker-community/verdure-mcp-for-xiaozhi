using Verdure.McpPlatform.Domain.AggregatesModel.McpServiceConfigAggregate;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Service interface for connecting to and interacting with MCP services
/// </summary>
public interface IMcpClientService
{
    /// <summary>
    /// Connects to an MCP service and retrieves its available tools
    /// </summary>
    /// <param name="config">The MCP service configuration</param>
    /// <returns>List of tools available in the MCP service</returns>
    Task<IEnumerable<McpToolInfo>> GetToolsAsync(McpServiceConfig config);
}

/// <summary>
/// Information about a tool retrieved from an MCP service
/// </summary>
public record McpToolInfo
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? InputSchema { get; init; }
}
