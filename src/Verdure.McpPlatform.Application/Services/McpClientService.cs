using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using Verdure.McpPlatform.Domain.AggregatesModel.McpServiceConfigAggregate;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Service implementation for connecting to and interacting with MCP services
/// Uses ModelContextProtocol SDK 0.4.0-preview.3
/// </summary>
public class McpClientService : IMcpClientService
{
    private readonly ILogger<McpClientService> _logger;

    public McpClientService(ILogger<McpClientService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<McpToolInfo>> GetToolsAsync(McpServiceConfig config)
    {
        _logger.LogInformation(
            "Connecting to MCP service {ServiceName} at {Endpoint} using protocol {Protocol}",
            config.Name,
            config.Endpoint,
            config.Protocol);

        return config.Protocol?.ToLowerInvariant() switch
        {
            "stdio" => await GetToolsViaStdioAsync(config),
            "http" => await GetToolsViaHttpAsync(config),
            "sse" => throw new NotSupportedException("SSE protocol is not yet supported. Please use stdio or http protocol."),
            "websocket" => throw new NotSupportedException("WebSocket protocol is not yet supported. Please use stdio or http protocol."),
            _ => throw new InvalidOperationException($"Unknown protocol: {config.Protocol}")
        };
    }

    private async Task<IEnumerable<McpToolInfo>> GetToolsViaStdioAsync(McpServiceConfig config)
    {
        McpClient? client = null;
        
        try
        {
            // Parse the endpoint as a command
            // Expected format: "command arg1 arg2" or just "command"
            var parts = config.Endpoint.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length == 0)
            {
                throw new InvalidOperationException("Endpoint cannot be empty for stdio protocol");
            }

            var command = parts[0];
            var args = parts.Length > 1 ? parts.Skip(1).ToArray() : Array.Empty<string>();

            _logger.LogDebug(
                "Creating stdio transport with command: {Command}, args: {Args}",
                command,
                string.Join(" ", args));

            // TODO: StdioMcpTransport not available in ModelContextProtocol 0.4.0-preview.3
            throw new NotSupportedException(
                "Stdio protocol is not yet supported in ModelContextProtocol SDK 0.4.0-preview.3. " +
                "Please use HTTP protocol instead or wait for SDK update.");
            
            // Create stdio transport using ModelContextProtocol SDK
            // var transport = new StdioMcpTransport(command, args); // Not available in 0.4.0
            
            // Create MCP client
            // client = await McpClient.CreateAsync(transport);

            _logger.LogInformation(
                "Successfully connected to MCP service {ServiceName} via stdio",
                config.Name);

            // List available tools
            var toolsResult = await client.ListToolsAsync();
            
            if (toolsResult == null || toolsResult.Count == 0)
            {
                _logger.LogWarning("No tools returned from MCP service {ServiceName}", config.Name);
                return Enumerable.Empty<McpToolInfo>();
            }

            var tools = toolsResult.Select(tool => new McpToolInfo
            {
                Name = tool.Name ?? "Unknown",
                Description = tool.Description,
                // TODO: Extract schema when we determine the correct property name in v0.4.0
                InputSchema = null
            }).ToList();

            _logger.LogInformation(
                "Successfully retrieved {Count} tools from MCP service {ServiceName}",
                tools.Count,
                config.Name);

            return tools;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error connecting to MCP service {ServiceName} via stdio",
                config.Name);
            throw new InvalidOperationException(
                $"Failed to retrieve tools from MCP service '{config.Name}': {ex.Message}",
                ex);
        }
        finally
        {
            if (client != null)
            {
                await client.DisposeAsync();
            }
        }
    }

    private async Task<IEnumerable<McpToolInfo>> GetToolsViaHttpAsync(McpServiceConfig config)
    {
        McpClient? client = null;
        
        try
        {
            if (string.IsNullOrEmpty(config.Endpoint))
            {
                throw new InvalidOperationException("Endpoint cannot be empty for http protocol");
            }

            if (!Uri.TryCreate(config.Endpoint, UriKind.Absolute, out var endpointUri))
            {
                throw new InvalidOperationException($"Invalid endpoint URL: {config.Endpoint}");
            }

            _logger.LogDebug(
                "Creating HTTP transport for endpoint: {Endpoint}",
                config.Endpoint);

            // Create HTTP client transport using ModelContextProtocol SDK
            var transport = new HttpClientTransport(new HttpClientTransportOptions
            {
                Endpoint = endpointUri,
                Name = config.Name
            });
            
            // Create MCP client
            client = await McpClient.CreateAsync(transport);

            _logger.LogInformation(
                "Successfully connected to MCP service {ServiceName} via HTTP",
                config.Name);

            // List available tools
            var toolsResult = await client.ListToolsAsync();
            
            if (toolsResult == null || toolsResult.Count == 0)
            {
                _logger.LogWarning("No tools returned from MCP service {ServiceName}", config.Name);
                return Enumerable.Empty<McpToolInfo>();
            }

            var tools = toolsResult.Select(tool => new McpToolInfo
            {
                Name = tool.Name ?? "Unknown",
                Description = tool.Description,
                // TODO: Extract schema when we determine the correct property name in v0.4.0
                InputSchema = null
            }).ToList();

            _logger.LogInformation(
                "Successfully retrieved {Count} tools from MCP service {ServiceName}",
                tools.Count,
                config.Name);

            return tools;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error connecting to MCP service {ServiceName} via HTTP",
                config.Name);
            throw new InvalidOperationException(
                $"Failed to retrieve tools from MCP service '{config.Name}': {ex.Message}",
                ex);
        }
        finally
        {
            if (client != null)
            {
                await client.DisposeAsync();
            }
        }
    }
}
