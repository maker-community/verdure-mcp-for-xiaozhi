using ModelContextProtocol.Client;
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

    /// <summary>
    /// Creates an MCP client based on service configuration
    /// This is a shared method used by both tool synchronization and WebSocket connections
    /// </summary>
    /// <param name="config">MCP service configuration containing endpoint, protocol, and authentication settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created and initialized MCP client</returns>
    Task<McpClient> CreateMcpClientAsync(
        McpServiceConfig config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an MCP client from parameters (for WebSocket scenarios with McpServiceEndpoint)
    /// </summary>
    Task<McpClient> CreateMcpClientAsync(
        string name,
        string endpoint,
        string? protocol = null,
        string? authenticationType = null,
        string? authenticationConfig = null,
        CancellationToken cancellationToken = default);
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
