using Microsoft.Extensions.Logging;
using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;
using Verdure.McpPlatform.Domain.AggregatesModel.McpServerAggregate;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Service implementation for MCP Binding operations
/// </summary>
public class McpBindingService : IMcpBindingService
{
    private readonly IMcpServerRepository _repository;
    private readonly ILogger<McpBindingService> _logger;

    public McpBindingService(
        IMcpServerRepository repository,
        ILogger<McpBindingService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<McpBindingDto> CreateAsync(CreateMcpBindingRequest request, string userId)
    {
        var server = await _repository.GetAsync(request.ServerId);
        
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException("Server not found or access denied");
        }

        var binding = server.AddBinding(request.ServiceName, request.NodeAddress, request.Description);
        _repository.Update(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation(
            "Created MCP binding {BindingId} for server {ServerId}",
            binding.Id,
            request.ServerId);

        return MapToDto(binding);
    }

    public async Task<McpBindingDto?> GetByIdAsync(int id, string userId)
    {
        var binding = await _repository.GetBindingAsync(id);
        
        if (binding == null)
        {
            return null;
        }

        var server = await _repository.GetAsync(binding.McpServerId);
        if (server == null || server.UserId != userId)
        {
            _logger.LogWarning("Access denied to binding {BindingId} for user {UserId}", id, userId);
            return null;
        }

        return MapToDto(binding);
    }

    public async Task<IEnumerable<McpBindingDto>> GetByServerAsync(int serverId, string userId)
    {
        var server = await _repository.GetAsync(serverId);
        
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException("Server not found or access denied");
        }

        var bindings = await _repository.GetBindingsByServerIdAsync(serverId);
        return bindings.Select(MapToDto);
    }

    public async Task<IEnumerable<McpBindingDto>> GetActiveBindingsAsync()
    {
        var bindings = await _repository.GetActiveBindingsAsync();
        return bindings.Select(MapToDto);
    }

    public async Task UpdateAsync(int id, UpdateMcpBindingRequest request, string userId)
    {
        var binding = await _repository.GetBindingAsync(id);
        
        if (binding == null)
        {
            throw new KeyNotFoundException($"Binding {id} not found");
        }

        var server = await _repository.GetAsync(binding.McpServerId);
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException("Access denied");
        }

        binding.UpdateInfo(request.ServiceName, request.NodeAddress, request.Description);
        _repository.Update(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation("Updated MCP binding {BindingId}", id);
    }

    public async Task ActivateAsync(int id, string userId)
    {
        var binding = await _repository.GetBindingAsync(id);
        
        if (binding == null)
        {
            throw new KeyNotFoundException($"Binding {id} not found");
        }

        var server = await _repository.GetAsync(binding.McpServerId);
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException("Access denied");
        }

        binding.Activate();
        _repository.Update(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation("Activated MCP binding {BindingId}", id);
    }

    public async Task DeactivateAsync(int id, string userId)
    {
        var binding = await _repository.GetBindingAsync(id);
        
        if (binding == null)
        {
            throw new KeyNotFoundException($"Binding {id} not found");
        }

        var server = await _repository.GetAsync(binding.McpServerId);
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException("Access denied");
        }

        binding.Deactivate();
        _repository.Update(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation("Deactivated MCP binding {BindingId}", id);
    }

    public async Task DeleteAsync(int id, string userId)
    {
        var binding = await _repository.GetBindingAsync(id);
        
        if (binding == null)
        {
            throw new KeyNotFoundException($"Binding {id} not found");
        }

        var server = await _repository.GetAsync(binding.McpServerId);
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException("Access denied");
        }

        server.RemoveBinding(binding);
        _repository.Update(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation("Deleted MCP binding {BindingId}", id);
    }

    private static McpBindingDto MapToDto(McpBinding binding)
    {
        return new McpBindingDto
        {
            Id = binding.Id,
            ServiceName = binding.ServiceName,
            NodeAddress = binding.NodeAddress,
            McpServerId = binding.McpServerId,
            Description = binding.Description,
            IsActive = binding.IsActive,
            CreatedAt = binding.CreatedAt,
            UpdatedAt = binding.UpdatedAt
        };
    }
}
