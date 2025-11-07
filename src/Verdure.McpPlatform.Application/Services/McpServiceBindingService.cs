using Microsoft.Extensions.Logging;
using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;
using Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiMcpEndpointAggregate;
using Verdure.McpPlatform.Domain.AggregatesModel.McpServiceConfigAggregate;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Service implementation for MCP Binding operations
/// </summary>
public class McpServiceBindingService : IMcpServiceBindingService
{
    private readonly IXiaozhiMcpEndpointRepository _repository;
    private readonly IMcpServiceConfigRepository _configRepository;
    private readonly ILogger<McpServiceBindingService> _logger;

    public McpServiceBindingService(
        IXiaozhiMcpEndpointRepository repository,
        IMcpServiceConfigRepository configRepository,
        ILogger<McpServiceBindingService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _configRepository = configRepository ?? throw new ArgumentNullException(nameof(configRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<McpServiceBindingDto> CreateAsync(CreateMcpServiceBindingRequest request, string userId)
    {
        var server = await _repository.GetAsync(request.ServerId);
        
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException("Server not found or access denied");
        }

        var binding = server.AddServiceBinding(
            request.McpServiceConfigId,
            request.Description,
            request.SelectedToolNames);
        
        _repository.Update(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation(
            "Created MCP binding {BindingId} for server {ServerId} with config {ConfigId}",
            binding.Id,
            request.ServerId,
            request.McpServiceConfigId);

        return await MapToDtoAsync(binding);
    }

    public async Task<McpServiceBindingDto?> GetByIdAsync(string id, string userId)
    {
        var binding = await _repository.GetServiceBindingAsync(id);
        
        if (binding == null)
        {
            return null;
        }

        var server = await _repository.GetAsync(binding.XiaozhiMcpEndpointId);
        if (server == null || server.UserId != userId)
        {
            _logger.LogWarning("Access denied to binding {BindingId} for user {UserId}", id, userId);
            return null;
        }

        return await MapToDtoAsync(binding);
    }

    public async Task<IEnumerable<McpServiceBindingDto>> GetByServerAsync(string serverId, string userId)
    {
        var server = await _repository.GetAsync(serverId);
        
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException("Server not found or access denied");
        }

        var bindings = await _repository.GetServiceBindingsByConnectionIdAsync(serverId);
        return await MapToDtosAsync(bindings);
    }

    public async Task<IEnumerable<McpServiceBindingDto>> GetActiveServiceBindingsAsync()
    {
        var bindings = await _repository.GetActiveServiceBindingsAsync();
        return await MapToDtosAsync(bindings);
    }

    public async Task UpdateAsync(string id, UpdateMcpServiceBindingRequest request, string userId)
    {
        var binding = await _repository.GetServiceBindingAsync(id);
        
        if (binding == null)
        {
            throw new KeyNotFoundException($"Binding {id} not found");
        }

        var server = await _repository.GetAsync(binding.XiaozhiMcpEndpointId);
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException("Access denied");
        }

        binding.UpdateInfo(
            request.McpServiceConfigId,
            userId,
            request.Description,
            request.SelectedToolNames);
        _repository.Update(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation("Updated MCP binding {BindingId} with config {ConfigId}", id, request.McpServiceConfigId);
    }

    public async Task ActivateAsync(string id, string userId)
    {
        var binding = await _repository.GetServiceBindingAsync(id);
        
        if (binding == null)
        {
            throw new KeyNotFoundException($"Binding {id} not found");
        }

        var server = await _repository.GetAsync(binding.XiaozhiMcpEndpointId);
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException("Access denied");
        }

        binding.Activate();
        _repository.Update(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation("Activated MCP binding {BindingId}", id);
    }

    public async Task DeactivateAsync(string id, string userId)
    {
        var binding = await _repository.GetServiceBindingAsync(id);
        
        if (binding == null)
        {
            throw new KeyNotFoundException($"Binding {id} not found");
        }

        var server = await _repository.GetAsync(binding.XiaozhiMcpEndpointId);
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException("Access denied");
        }

        binding.Deactivate();
        _repository.Update(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation("Deactivated MCP binding {BindingId}", id);
    }

    public async Task DeleteAsync(string id, string userId)
    {
        var binding = await _repository.GetServiceBindingAsync(id);
        
        if (binding == null)
        {
            throw new KeyNotFoundException($"Binding {id} not found");
        }

        var server = await _repository.GetAsync(binding.XiaozhiMcpEndpointId);
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException("Access denied");
        }

        server.RemoveServiceBinding(binding);
        _repository.Update(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation("Deleted MCP binding {BindingId}", id);
    }

    /// <summary>
    /// Map a single binding to DTO (used when we already have endpoint and config loaded)
    /// </summary>
    private async Task<McpServiceBindingDto> MapToDtoAsync(McpServiceBinding binding)
    {
        // Fetch endpoint and config sequentially to avoid EF Core concurrency issues
        // Note: Parallel execution with Task.WhenAll causes "A second operation was started" 
        // error with PostgreSQL as the same DbContext instance is used concurrently
        var endpoint = await _repository.GetAsync(binding.XiaozhiMcpEndpointId);
        var config = await _configRepository.GetByIdAsync(binding.McpServiceConfigId);
        
        return new McpServiceBindingDto
        {
            Id = binding.Id,
            XiaozhiMcpEndpointId = binding.XiaozhiMcpEndpointId,
            ConnectionName = endpoint?.Name ?? string.Empty,
            McpServiceConfigId = binding.McpServiceConfigId,
            ServiceName = config?.Name ?? string.Empty,
            Description = binding.Description,
            IsActive = binding.IsActive,
            SelectedToolNames = binding.SelectedToolNames.ToList(),
            CreatedAt = binding.CreatedAt,
            UpdatedAt = binding.UpdatedAt
        };
    }

    /// <summary>
    /// Map multiple bindings to DTOs efficiently using batch loading
    /// This prevents N+1 query problem by loading all endpoints and configs in two queries
    /// </summary>
    private async Task<IEnumerable<McpServiceBindingDto>> MapToDtosAsync(IEnumerable<McpServiceBinding> bindings)
    {
        var bindingList = bindings.ToList();
        if (!bindingList.Any())
        {
            return Enumerable.Empty<McpServiceBindingDto>();
        }

        // Collect all unique IDs
        var endpointIds = bindingList.Select(b => b.XiaozhiMcpEndpointId).Distinct().ToList();
        var configIds = bindingList.Select(b => b.McpServiceConfigId).Distinct().ToList();

        // Batch load all endpoints and configs in just 2 queries instead of N queries
        var endpoints = (await _repository.GetByIdsAsync(endpointIds))
            .ToDictionary(e => e.Id, e => e);
        var configs = (await _configRepository.GetByIdsAsync(configIds))
            .ToDictionary(c => c.Id, c => c);

        // Map bindings to DTOs using the pre-loaded data
        return bindingList.Select(binding => new McpServiceBindingDto
        {
            Id = binding.Id,
            XiaozhiMcpEndpointId = binding.XiaozhiMcpEndpointId,
            ConnectionName = endpoints.TryGetValue(binding.XiaozhiMcpEndpointId, out var endpoint) 
                ? endpoint.Name 
                : string.Empty,
            McpServiceConfigId = binding.McpServiceConfigId,
            ServiceName = configs.TryGetValue(binding.McpServiceConfigId, out var config) 
                ? config.Name 
                : string.Empty,
            Description = binding.Description,
            IsActive = binding.IsActive,
            SelectedToolNames = binding.SelectedToolNames.ToList(),
            CreatedAt = binding.CreatedAt,
            UpdatedAt = binding.UpdatedAt
        }).ToList();
    }
}
