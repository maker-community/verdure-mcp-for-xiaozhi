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
        var dtos = new List<McpServiceBindingDto>();
        foreach (var binding in bindings)
        {
            dtos.Add(await MapToDtoAsync(binding));
        }
        return dtos;
    }

    public async Task<IEnumerable<McpServiceBindingDto>> GetActiveServiceBindingsAsync()
    {
        var bindings = await _repository.GetActiveServiceBindingsAsync();
        var dtos = new List<McpServiceBindingDto>();
        foreach (var binding in bindings)
        {
            dtos.Add(await MapToDtoAsync(binding));
        }
        return dtos;
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

    private async Task<McpServiceBindingDto> MapToDtoAsync(McpServiceBinding binding)
    {
        var config = await _configRepository.GetByIdAsync(binding.McpServiceConfigId);
        
        return new McpServiceBindingDto
        {
            Id = binding.Id,
            XiaozhiMcpEndpointId = binding.XiaozhiMcpEndpointId,
            McpServiceConfigId = binding.McpServiceConfigId,
            McpServiceConfig = config != null ? MapConfigToDto(config) : null,
            ServiceName = config?.Name ?? string.Empty,
            NodeAddress = config?.Endpoint ?? string.Empty,
            Description = binding.Description,
            IsActive = binding.IsActive,
            SelectedToolNames = binding.SelectedToolNames.ToList(),
            CreatedAt = binding.CreatedAt,
            UpdatedAt = binding.UpdatedAt
        };
    }

    private static McpServiceConfigDto MapConfigToDto(McpServiceConfig config)
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
            Protocol = config.Protocol,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt,
            LastSyncedAt = config.LastSyncedAt,
            Tools = config.Tools.Select(t => new McpToolDto
            {
                Name = t.Name,
                Description = t.Description,
                InputSchema = t.InputSchema
            }).ToList()
        };
    }
}
