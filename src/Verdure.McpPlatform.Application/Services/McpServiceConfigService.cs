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
    private readonly IUserInfoService _userInfoService;
    private readonly ILogger<McpServiceConfigService> _logger;

    public McpServiceConfigService(
        IMcpServiceConfigRepository repository,
        IMcpClientService mcpClientService,
        IUserInfoService userInfoService,
        ILogger<McpServiceConfigService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mcpClientService = mcpClientService ?? throw new ArgumentNullException(nameof(mcpClientService));
        _userInfoService = userInfoService ?? throw new ArgumentNullException(nameof(userInfoService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<McpServiceConfigDto> CreateAsync(CreateMcpServiceConfigRequest request, string userId)
    {
        var config = new McpServiceConfig(
            request.Name,
            request.Endpoint,
            userId,
            request.Description,
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
        
        // 批量获取创建者信息
        var userIds = configs.Select(c => c.UserId).Distinct().ToList();
        var userInfoMap = await _userInfoService.GetUsersByIdsAsync(userIds);
        
        return configs.Select(config => MapToPublicDto(config, userInfoMap));
    }

    public async Task<PagedResult<McpServiceConfigDto>> GetPublicServicesPagedAsync(PagedRequest request)
    {
        var (items, totalCount) = await _repository.GetPublicServicesPagedAsync(
            request.GetSkip(),
            request.GetSafePageSize(),
            request.SearchTerm,
            request.SortBy,
            request.SortOrder?.ToLower() == "desc");

        // 批量获取创建者信息
        var userIds = items.Select(c => c.UserId).Distinct().ToList();
        var userInfoMap = await _userInfoService.GetUsersByIdsAsync(userIds);

        var dtos = items.Select(config => MapToPublicDto(config, userInfoMap));

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

    public async Task SetPublicAsync(string id, string adminUserId)
    {
        var config = await _repository.GetByIdAsync(id);
        
        if (config == null)
        {
            throw new InvalidOperationException($"Service config {id} not found");
        }

        config.SetPublic();
        _repository.Update(config);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation(
            "Service config {ConfigId} set to public by admin {AdminUserId}",
            config.Id,
            adminUserId);
    }

    public async Task SetPrivateAsync(string id, string adminUserId)
    {
        var config = await _repository.GetByIdAsync(id);
        
        if (config == null)
        {
            throw new InvalidOperationException($"Service config {id} not found");
        }

        config.SetPrivate();
        _repository.Update(config);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation(
            "Service config {ConfigId} set to private by admin {AdminUserId}",
            config.Id,
            adminUserId);
    }

    public async Task<SyncAllToolsResultDto> SyncAllToolsAsync()
    {
        _logger.LogInformation("Starting sync all tools operation");

        var allConfigs = await _repository.GetAllAsync();
        var allConfigsList = allConfigs.ToList();
        
        var totalProcessed = allConfigsList.Count;
        var successCount = 0;
        var failedServices = new List<FailedServiceSync>();

        foreach (var config in allConfigsList)
        {
            try
            {
                _logger.LogInformation(
                    "Syncing tools for service {ServiceId} ({ServiceName})",
                    config.Id,
                    config.Name);

                // Connect to the MCP service and retrieve tools
                var toolInfos = await _mcpClientService.GetToolsAsync(config);
                
                // Convert tool infos to domain entities
                var tools = toolInfos.Select(info => 
                    new McpTool(info.Name, config.Id, config.UserId, info.Description, info.InputSchema)).ToList();
                
                // Update the service config with the new tools
                config.UpdateTools(tools);
                
                _repository.Update(config);
                await _repository.UnitOfWork.SaveEntitiesAsync();

                successCount++;
                
                _logger.LogInformation(
                    "Successfully synced {ToolCount} tools for service {ServiceId} ({ServiceName})",
                    tools.Count,
                    config.Id,
                    config.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to sync tools for service {ServiceId} ({ServiceName})",
                    config.Id,
                    config.Name);
                
                failedServices.Add(new FailedServiceSync
                {
                    ServiceId = config.Id,
                    ServiceName = config.Name,
                    ErrorMessage = ex.Message
                });
            }
        }

        var result = new SyncAllToolsResultDto
        {
            TotalProcessed = totalProcessed,
            SuccessCount = successCount,
            FailedCount = failedServices.Count,
            FailedServices = failedServices
        };

        _logger.LogInformation(
            "Sync all tools completed: {TotalProcessed} total, {SuccessCount} success, {FailedCount} failed",
            totalProcessed,
            successCount,
            failedServices.Count);

        return result;
    }

    public async Task<PagedResult<McpServiceConfigDto>> GetAllServicesForAdminAsync(PagedRequest request)
    {
        var (items, totalCount) = await _repository.GetAllPagedAsync(
            request.GetSkip(),
            request.GetSafePageSize(),
            request.SearchTerm,
            request.SortBy,
            request.SortOrder?.ToLower() == "desc");

        // 批量获取创建者信息
        var userIds = items.Select(c => c.UserId).Distinct().ToList();
        var userInfoMap = await _userInfoService.GetUsersByIdsAsync(userIds);

        // 使用 MapToPublicDto 隐藏敏感信息
        var dtos = items.Select(config => MapToPublicDto(config, userInfoMap));

        return PagedResult<McpServiceConfigDto>.Create(
            dtos,
            totalCount,
            request.GetSafePage(),
            request.GetSafePageSize());
    }

    public async Task AdminSyncToolsAsync(string id)
    {
        var config = await _repository.GetByIdAsync(id);
        
        if (config == null)
        {
            throw new InvalidOperationException($"Service config {id} not found");
        }

        _logger.LogInformation(
            "Admin starting tool sync for MCP service config {ConfigId} ({ServiceName})",
            config.Id,
            config.Name);

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
                "Admin successfully synced {Count} tools for MCP service config {ConfigId} ({ServiceName})",
                tools.Count,
                config.Id,
                config.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Admin failed to sync tools for MCP service config {ConfigId} ({ServiceName})",
                config.Id,
                config.Name);
            throw;
        }
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
    private static McpServiceConfigDto MapToPublicDto(
        McpServiceConfig config,
        Dictionary<string, UserBasicInfo> userInfoMap)
    {
        // 尝试获取创建者信息
        userInfoMap.TryGetValue(config.UserId, out var creator);

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
            Tools = config.Tools.Select(MapToolToDto).ToList(),
            Creator = creator // 注入创建者信息
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
