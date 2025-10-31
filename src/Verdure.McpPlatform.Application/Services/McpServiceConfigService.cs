using Microsoft.Extensions.Logging;
using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;
using Verdure.McpPlatform.Domain.AggregatesModel.McpServiceConfigAggregate;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Service implementation for MCP Service Configuration operations
/// </summary>
public class McpServiceConfigService : IMcpServiceConfigService
{
    private readonly IMcpServiceConfigRepository _repository;
    private readonly ILogger<McpServiceConfigService> _logger;

    public McpServiceConfigService(
        IMcpServiceConfigRepository repository,
        ILogger<McpServiceConfigService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<McpServiceConfigDto> CreateAsync(CreateMcpServiceConfigRequest request, string userId)
    {
        var config = new McpServiceConfig(
            request.Name,
            request.Endpoint,
            userId,
            request.Description,
            request.IsPublic,
            request.AuthenticationType,
            request.AuthenticationConfig,
            request.Protocol ?? "stdio");
        
        _repository.Add(config);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation(
            "Created MCP service config {ConfigId} for user {UserId}",
            config.Id,
            userId);

        return MapToDto(config);
    }

    public async Task<McpServiceConfigDto?> GetByIdAsync(string id, string userId)
    {
        var config = await _repository.GetByIdAsync(id);
        
        // Allow access if user owns the config OR if it's public
        if (config == null || (config.UserId != userId && !config.IsPublic))
        {
            _logger.LogWarning("MCP service config {ConfigId} not found or access denied for user {UserId}", id, userId);
            return null;
        }

        return MapToDto(config);
    }

    public async Task<IEnumerable<McpServiceConfigDto>> GetByUserAsync(string userId)
    {
        var configs = await _repository.GetByUserAsync(userId);
        return configs.Select(MapToDto);
    }

    public async Task<IEnumerable<McpServiceConfigDto>> GetPublicServicesAsync()
    {
        var configs = await _repository.GetPublicServicesAsync();
        return configs.Select(MapToDto);
    }

    public async Task UpdateAsync(string id, UpdateMcpServiceConfigRequest request, string userId)
    {
        var config = await _repository.GetByIdAsync(id);
        
        if (config == null || config.UserId != userId)
        {
            throw new UnauthorizedAccessException($"Service config {id} not found or access denied");
        }

        config.UpdateInfo(
            request.Name,
            request.Endpoint,
            request.Description,
            request.IsPublic,
            request.AuthenticationType,
            request.AuthenticationConfig,
            request.Protocol);
        
        _repository.Update(config);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation(
            "Updated MCP service config {ConfigId} for user {UserId}",
            config.Id,
            userId);
    }

    public async Task DeleteAsync(string id, string userId)
    {
        var config = await _repository.GetByIdAsync(id);
        
        if (config == null || config.UserId != userId)
        {
            throw new UnauthorizedAccessException($"Service config {id} not found or access denied");
        }

        await _repository.DeleteAsync(id);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation(
            "Deleted MCP service config {ConfigId} for user {UserId}",
            config.Id,
            userId);
    }

    public async Task SyncToolsAsync(string id, string userId)
    {
        var config = await _repository.GetByIdAsync(id);
        
        if (config == null || config.UserId != userId)
        {
            throw new UnauthorizedAccessException($"Service config {id} not found or access denied");
        }

        // Tool syncing will be implemented in the API layer where MCP client is available
        // This method is a placeholder for now
        _logger.LogInformation(
            "Tool sync requested for MCP service config {ConfigId}. Implementation pending.",
            config.Id);
        
        throw new NotImplementedException("Tool synchronization will be implemented in the API layer");
    }

    public async Task<IEnumerable<McpToolDto>> GetToolsAsync(string serviceId, string userId)
    {
        var config = await _repository.GetByIdAsync(serviceId);
        
        // Allow access if user owns the config OR if it's public
        if (config == null || (config.UserId != userId && !config.IsPublic))
        {
            throw new UnauthorizedAccessException($"Service config {serviceId} not found or access denied");
        }

        return config.Tools.Select(MapToolToDto);
    }

    private static McpServiceConfigDto MapToDto(McpServiceConfig config)
    {
        return new McpServiceConfigDto
        {
            Id = config.Id,
            Name = config.Name,
            Endpoint = config.Endpoint,
            UserId = config.UserId,
            Description = config.Description,
            IsPublic = config.IsPublic,
            AuthenticationType = config.AuthenticationType,
            AuthenticationConfig = config.AuthenticationConfig,
            Protocol = config.Protocol,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt,
            LastSyncedAt = config.LastSyncedAt,
            Tools = config.Tools.Select(MapToolToDto).ToList()
        };
    }

    private static McpToolDto MapToolToDto(McpTool tool)
    {
        return new McpToolDto
        {
            Id = tool.Id,
            Name = tool.Name,
            McpServiceConfigId = tool.McpServiceConfigId,
            Description = tool.Description,
            InputSchema = tool.InputSchema,
            CreatedAt = tool.CreatedAt,
            UpdatedAt = tool.UpdatedAt
        };
    }
}
