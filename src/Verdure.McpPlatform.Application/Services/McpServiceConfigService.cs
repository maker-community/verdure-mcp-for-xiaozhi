using Microsoft.Extensions.Logging;
using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Models;
using Verdure.McpPlatform.Contracts.Requests;
using Verdure.McpPlatform.Domain.AggregatesModel.McpServiceConfigAggregate;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Service implementation for MCP Service Configuration operations
/// </summary>
public class McpServiceConfigService : IMcpServiceConfigService
{
    private readonly IMcpServiceConfigRepository _repository;
    private readonly IMcpClientService _mcpClientService;
    private readonly ILogger<McpServiceConfigService> _logger;

    public McpServiceConfigService(
        IMcpServiceConfigRepository repository,
        IMcpClientService mcpClientService,
        ILogger<McpServiceConfigService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mcpClientService = mcpClientService ?? throw new ArgumentNullException(nameof(mcpClientService));
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

    public async Task<PagedResult<McpServiceConfigDto>> GetByUserPagedAsync(string userId, PagedRequest request)
    {
        var (items, totalCount) = await _repository.GetByUserPagedAsync(
            userId,
            request.GetSkip(),
            request.GetSafePageSize(),
            request.SearchTerm,
            request.SortBy,
            request.SortOrder?.ToLower() == "desc");

        var dtos = items.Select(MapToDto);

        return PagedResult<McpServiceConfigDto>.Create(
            dtos,
            totalCount,
            request.GetSafePage(),
            request.GetSafePageSize());
    }

    public async Task<IEnumerable<McpServiceConfigDto>> GetPublicServicesAsync()
    {
        var configs = await _repository.GetPublicServicesAsync();
        return configs.Select(MapToPublicDto);
    }

    public async Task<PagedResult<McpServiceConfigDto>> GetPublicServicesPagedAsync(PagedRequest request)
    {
        var (items, totalCount) = await _repository.GetPublicServicesPagedAsync(
            request.GetSkip(),
            request.GetSafePageSize(),
            request.SearchTerm,
            request.SortBy,
            request.SortOrder?.ToLower() == "desc");

        var dtos = items.Select(MapToPublicDto);

        return PagedResult<McpServiceConfigDto>.Create(
            dtos,
            totalCount,
            request.GetSafePage(),
            request.GetSafePageSize());
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

        _logger.LogInformation(
            "Starting tool sync for MCP service config {ConfigId}",
            config.Id);

        try
        {
            // Connect to the MCP service and retrieve tools
            var toolInfos = await _mcpClientService.GetToolsAsync(config);
            
            // Convert tool infos to domain entities
            var tools = toolInfos.Select(info => 
                new McpTool(info.Name, config.Id, config.UserId, info.Description, info.InputSchema)).ToList();
            
            // Update the service config with the new tools
            config.UpdateTools(tools);
            
            _repository.Update(config);
            await _repository.UnitOfWork.SaveEntitiesAsync();

            _logger.LogInformation(
                "Successfully synced {Count} tools for MCP service config {ConfigId}",
                tools.Count,
                config.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to sync tools for MCP service config {ConfigId}",
                config.Id);
            throw;
        }
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

    /// <summary>
    /// Map to DTO for public services - excludes sensitive information
    /// </summary>
    private static McpServiceConfigDto MapToPublicDto(McpServiceConfig config)
    {
        return new McpServiceConfigDto
        {
            Id = config.Id,
            Name = config.Name,
            Endpoint = string.Empty, // 隐藏 Endpoint
            UserId = config.UserId,
            Description = config.Description,
            IsPublic = config.IsPublic,
            AuthenticationType = null, // 隐藏认证类型
            AuthenticationConfig = null, // 隐藏认证配置
            Protocol = config.Protocol,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt,
            LastSyncedAt = null, // 隐藏最后同步时间
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
