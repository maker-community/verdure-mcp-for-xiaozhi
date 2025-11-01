using Microsoft.Extensions.Logging;
using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;
using Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiConnectionAggregate;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Service implementation for MCP Binding operations
/// </summary>
public class McpServiceBindingService : IMcpServiceBindingService
{
    private readonly IXiaozhiConnectionRepository _repository;
    private readonly ILogger<McpServiceBindingService> _logger;

    public McpServiceBindingService(
        IXiaozhiConnectionRepository repository,
        ILogger<McpServiceBindingService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
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
            request.ServiceName, 
            request.NodeAddress,
            request.McpServiceConfigId,
            request.Description,
            request.SelectedToolNames);
        
        _repository.Update(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation(
            "Created MCP binding {BindingId} for server {ServerId}",
            binding.Id,
            request.ServerId);

        return MapToDto(binding);
    }

    public async Task<McpServiceBindingDto?> GetByIdAsync(string id, string userId)
    {
        var binding = await _repository.GetServiceBindingAsync(id);
        
        if (binding == null)
        {
            return null;
        }

        var server = await _repository.GetAsync(binding.XiaozhiConnectionId);
        if (server == null || server.UserId != userId)
        {
            _logger.LogWarning("Access denied to binding {BindingId} for user {UserId}", id, userId);
            return null;
        }

        return MapToDto(binding);
    }

    public async Task<IEnumerable<McpServiceBindingDto>> GetByServerAsync(string serverId, string userId)
    {
        var server = await _repository.GetAsync(serverId);
        
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException("Server not found or access denied");
        }

        var bindings = await _repository.GetServiceBindingsByConnectionIdAsync(serverId);
        return bindings.Select(MapToDto);
    }

    public async Task<IEnumerable<McpServiceBindingDto>> GetActiveServiceBindingsAsync()
    {
        var bindings = await _repository.GetActiveServiceBindingsAsync();
        return bindings.Select(MapToDto);
    }

    public async Task UpdateAsync(string id, UpdateMcpServiceBindingRequest request, string userId)
    {
        var binding = await _repository.GetServiceBindingAsync(id);
        
        if (binding == null)
        {
            throw new KeyNotFoundException($"Binding {id} not found");
        }

        var server = await _repository.GetAsync(binding.XiaozhiConnectionId);
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException("Access denied");
        }

        binding.UpdateInfo(
            request.ServiceName, 
            request.NodeAddress,
            request.McpServiceConfigId,
            request.Description,
            request.SelectedToolNames);
        _repository.Update(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation("Updated MCP binding {BindingId}", id);
    }

    public async Task ActivateAsync(string id, string userId)
    {
        var binding = await _repository.GetServiceBindingAsync(id);
        
        if (binding == null)
        {
            throw new KeyNotFoundException($"Binding {id} not found");
        }

        var server = await _repository.GetAsync(binding.XiaozhiConnectionId);
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

        var server = await _repository.GetAsync(binding.XiaozhiConnectionId);
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

        var server = await _repository.GetAsync(binding.XiaozhiConnectionId);
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException("Access denied");
        }

        server.RemoveServiceBinding(binding);
        _repository.Update(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation("Deleted MCP binding {BindingId}", id);
    }

    private static McpServiceBindingDto MapToDto(McpServiceBinding binding)
    {
        return new McpServiceBindingDto
        {
            Id = binding.Id,
            ServiceName = binding.ServiceName,
            NodeAddress = binding.NodeAddress,
            XiaozhiConnectionId = binding.XiaozhiConnectionId,
            McpServiceConfigId = binding.McpServiceConfigId,
            Description = binding.Description,
            IsActive = binding.IsActive,
            SelectedToolNames = binding.SelectedToolNames.ToList(),
            CreatedAt = binding.CreatedAt,
            UpdatedAt = binding.UpdatedAt
        };
    }
}
