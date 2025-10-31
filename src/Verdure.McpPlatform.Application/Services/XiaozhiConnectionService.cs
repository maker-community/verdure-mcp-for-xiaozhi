using Microsoft.Extensions.Logging;
using Verdure.McpPlatform.Contracts.DTOs;
using Verdure.McpPlatform.Contracts.Requests;
using Verdure.McpPlatform.Domain.AggregatesModel.XiaozhiConnectionAggregate;

namespace Verdure.McpPlatform.Application.Services;

/// <summary>
/// Service implementation for MCP Server operations
/// </summary>
public class XiaozhiConnectionService : IXiaozhiConnectionService
{
    private readonly IXiaozhiConnectionRepository _repository;
    private readonly ILogger<XiaozhiConnectionService> _logger;

    public XiaozhiConnectionService(
        IXiaozhiConnectionRepository repository,
        ILogger<XiaozhiConnectionService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<XiaozhiConnectionDto> CreateAsync(CreateXiaozhiConnectionRequest request, string userId)
    {
        var server = new XiaozhiConnection(request.Name, request.Address, userId, request.Description);
        
        _repository.Add(server);
        await _repository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation(
            "Created MCP server {ServerId} for user {UserId}",
            server.Id,
            userId);

        return MapToDto(server);
    }

    public async Task<XiaozhiConnectionDto?> GetByIdAsync(string id, string userId)
    {
        var server = await _repository.GetAsync(id);
        
        if (server == null || server.UserId != userId)
        {
            _logger.LogWarning("MCP server {ServerId} not found or access denied for user {UserId}", id, userId);
            return null;
        }

        return MapToDto(server);
    }

    public async Task<IEnumerable<XiaozhiConnectionDto>> GetByUserAsync(string userId)
    {
        var servers = await _repository.GetByUserIdAsync(userId);
        return servers.Select(MapToDto);
    }

    public async Task UpdateAsync(string id, UpdateXiaozhiConnectionRequest request, string userId)
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

    private static XiaozhiConnectionDto MapToDto(XiaozhiConnection server)
    {
        return new XiaozhiConnectionDto
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
            ServiceBindings = server.ServiceBindings.Select(b => new McpServiceBindingDto
            {
                Id = b.Id,
                ServiceName = b.ServiceName,
                NodeAddress = b.NodeAddress,
                XiaozhiConnectionId = b.XiaozhiConnectionId,
                Description = b.Description,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            }).ToList()
        };
    }
}
