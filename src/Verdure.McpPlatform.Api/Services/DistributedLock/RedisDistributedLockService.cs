using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace Verdure.McpPlatform.Api.Services.DistributedLock;

/// <summary>
/// Redis-based distributed lock service implementation using RedLock algorithm
/// </summary>
public class RedisDistributedLockService : IDistributedLockService, IAsyncDisposable
{
    private readonly IDistributedLockFactory _lockFactory;
    private readonly ILogger<RedisDistributedLockService> _logger;

    public RedisDistributedLockService(
        IConnectionMultiplexer redisConnection,
        ILogger<RedisDistributedLockService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Create RedLock factory with Redis connection
        var multiplexers = new List<RedLockMultiplexer>
        {
            new RedLockMultiplexer(redisConnection)
        };
        
        _lockFactory = RedLockFactory.Create(multiplexers);
        
        _logger.LogInformation("Redis distributed lock service initialized");
    }

    /// <inheritdoc/>
    public async Task<IDistributedLockHandle?> AcquireLockAsync(
        string resourceKey,
        TimeSpan expiryTime,
        TimeSpan waitTime,
        TimeSpan retryTime,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Attempting to acquire lock for resource: {ResourceKey}", resourceKey);
            
            var redLock = await _lockFactory.CreateLockAsync(
                resourceKey,
                expiryTime,
                waitTime,
                retryTime,
                cancellationToken);

            if (redLock.IsAcquired)
            {
                _logger.LogInformation("Successfully acquired lock for resource: {ResourceKey}", resourceKey);
                return new RedisDistributedLockHandle(redLock, resourceKey, _logger);
            }

            _logger.LogWarning("Failed to acquire lock for resource: {ResourceKey} within {WaitTime}", 
                resourceKey, waitTime);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring lock for resource: {ResourceKey}", resourceKey);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IDistributedLockHandle?> TryAcquireLockAsync(
        string resourceKey,
        TimeSpan expiryTime,
        CancellationToken cancellationToken = default)
    {
        // Try once without waiting
        return await AcquireLockAsync(
            resourceKey,
            expiryTime,
            TimeSpan.Zero,
            TimeSpan.Zero,
            cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_lockFactory != null)
        {
            await Task.Run(() =>
            {
                if (_lockFactory is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            });
            _logger.LogInformation("Redis distributed lock service disposed");
        }
    }
}

/// <summary>
/// Redis distributed lock handle implementation
/// </summary>
internal class RedisDistributedLockHandle : IDistributedLockHandle
{
    private readonly IRedLock _redLock;
    private readonly ILogger _logger;
    private bool _disposed;

    public string ResourceKey { get; }
    public bool IsAcquired => _redLock?.IsAcquired ?? false;

    public RedisDistributedLockHandle(
        IRedLock redLock,
        string resourceKey,
        ILogger logger)
    {
        _redLock = redLock ?? throw new ArgumentNullException(nameof(redLock));
        ResourceKey = resourceKey ?? throw new ArgumentNullException(nameof(resourceKey));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ReleaseAsync()
    {
        if (_disposed || !IsAcquired)
        {
            return;
        }

        try
        {
            await Task.Run(() => _redLock.Dispose());
            _logger.LogInformation("Released lock for resource: {ResourceKey}", ResourceKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock for resource: {ResourceKey}", ResourceKey);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await ReleaseAsync();
        _disposed = true;
    }
}
