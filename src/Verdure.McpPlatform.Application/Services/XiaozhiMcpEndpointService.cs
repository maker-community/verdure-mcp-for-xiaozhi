using Microsoft.Extensions.Logging;
using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Models;
using Verdure.McpPlatform.Contracts.Requests;
using Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiMcpEndpointAggregate;
using Verdure.McpPlatform.Domain.AggregatesModel.McpServiceConfigAggregate;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Service implementation for MCP Server operations
/// </summary>
public class XiaozhiMcpEndpointService : IXiaozhiMcpEndpointService
{
    private readonly IXiaozhiMcpEndpointRepository _repository;
    private readonly IMcpServiceConfigRepository _configRepository;
    private readonly ILogger<XiaozhiMcpEndpointService> _logger;

    public XiaozhiMcpEndpointService(
        IXiaozhiMcpEndpointRepository repository,
        IMcpServiceConfigRepository configRepository,
        ILogger<XiaozhiMcpEndpointService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<XiaozhiMcpEndpointDto> CreateAsync(CreateXiaozhiMcpEndpointRequest request, string userId)
    {
        var server = new XiaozhiMcpEndpoint(request.Name, request.Address, userId, request.Description);
        
        _repository.Add(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation(
            "Created MCP server {ServerId} for user {UserId}",
            server.Id,
            userId);

        return await MapToDtoAsync(server);
    }

    public async Task<XiaozhiMcpEndpointDto?> GetByIdAsync(string id, string userId)
    {
        var server = await _repository.GetAsync(id);
        
        if (server == null || server.UserId != userId)
        {
            _logger.LogWarning("MCP server {ServerId} not found or access denied for user {UserId}", id, userId);
            return null;
        }

        return await MapToDtoAsync(server);
    }

    public async Task<IEnumerable<XiaozhiMcpEndpointDto>> GetByUserAsync(string userId)
    {
        var servers = await _repository.GetByUserIdAsync(userId);
        var dtos = new List<XiaozhiMcpEndpointDto>();
        foreach (var server in servers)
        {
            dtos.Add(await MapToDtoAsync(server));
        }
        return dtos;
    }

    public async Task<PagedResult<XiaozhiMcpEndpointDto>> GetByUserPagedAsync(string userId, PagedRequest request)
    {
        var (items, totalCount) = await _repository.GetByUserIdPagedAsync(
            userId,
            request.GetSkip(),
            request.GetSafePageSize(),
            request.SearchTerm,
            request.SortBy,
            request.SortOrder?.ToLower() == "desc");

        var dtos = new List<XiaozhiMcpEndpointDto>();
        foreach (var server in items)
        {
            dtos.Add(await MapToDtoAsync(server));
        }

        return PagedResult<XiaozhiMcpEndpointDto>.Create(
            dtos,
            totalCount,
            request.GetSafePage(),
            request.GetSafePageSize());
    }

    public async Task UpdateAsync(string id, UpdateXiaozhiMcpEndpointRequest request, string userId)
    {
        var server = await _repository.GetAsync(id);
        
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException($"Server {id} not found or access denied");
        }

        server.UpdateInfo(request.Name, request.Address, request.Description);
        _repository.Update(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation(
            "Updated MCP server {ServerId} for user {UserId}",
            server.Id,
            userId);
    }

    public async Task DeleteAsync(string id, string userId)
    {
        var server = await _repository.GetAsync(id);
        
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException($"Server {id} not found or access denied");
        }

        _repository.Delete(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation(
            "Deleted MCP server {ServerId} for user {UserId}",
            id,
            userId);
    }

    public async Task EnableAsync(string id, string userId)
    {
        var server = await _repository.GetAsync(id);
        
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException($"Server {id} not found or access denied");
        }

        server.Enable();
        _repository.Update(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation(
            "Enabled MCP server {ServerId} for user {UserId}",
            id,
            userId);
    }

    public async Task DisableAsync(string id, string userId)
    {
        var server = await _repository.GetAsync(id);
        
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException($"Server {id} not found or access denied");
        }

        server.Disable();
        _repository.Update(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation(
            "Disabled MCP server {ServerId} for user {UserId}",
            id,
            userId);
    }

    private async Task<XiaozhiMcpEndpointDto> MapToDtoAsync(XiaozhiMcpEndpoint server)
    {
        return new XiaozhiMcpEndpointDto
        {
            Id = server.Id,
            Name = server.Name,
            Address = server.Address,
            Description = server.Description,
            IsEnabled = server.IsEnabled,
            IsConnected = server.IsConnected,
            CreatedAt = server.CreatedAt,
            UpdatedAt = server.UpdatedAt,
            LastConnectedAt = server.LastConnectedAt,
            LastDisconnectedAt = server.LastDisconnectedAt,
            ServiceBindings = await MapBindingsToDtoAsync(server.ServiceBindings, server.Name)
        };
    }

    private async Task<List<McpServiceBindingDto>> MapBindingsToDtoAsync(
        IReadOnlyCollection<McpServiceBinding> bindings, 
        string connectionName)
    {
        var dtos = new List<McpServiceBindingDto>();
        foreach (var binding in bindings)
        {
            var config = await _configRepository.GetByIdAsync(binding.McpServiceConfigId);
            dtos.Add(new McpServiceBindingDto
            {
                Id = binding.Id,
                XiaozhiMcpEndpointId = binding.XiaozhiMcpEndpointId,
                ConnectionName = connectionName,
                McpServiceConfigId = binding.McpServiceConfigId,
                ServiceName = config?.Name ?? string.Empty,
                Description = binding.Description,
                IsActive = binding.IsActive,
                SelectedToolNames = binding.SelectedToolNames.ToList(),
                CreatedAt = binding.CreatedAt,
                UpdatedAt = binding.UpdatedAt
            });
        }
        return dtos;
    }
}
