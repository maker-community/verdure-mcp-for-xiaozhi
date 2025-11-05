namespace Verdure.McpPlatform.Api.Services.DistributedLock;

/// <summary>
/// Distributed lock service interface for managing WebSocket connection locks across multiple instances
/// </summary>
public interface IDistributedLockService
{
    /// <summary>
    /// Acquire a distributed lock for a specific resource
    /// </summary>
    /// <param name="resourceKey">Unique key for the resource to lock</param>
    /// <param name="expiryTime">Lock expiration time</param>
    /// <param name="waitTime">Maximum time to wait for acquiring the lock</param>
    /// <param name="retryTime">Time between retry attempts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lock handle if successful, null otherwise</returns>
    Task<IDistributedLockHandle?> AcquireLockAsync(
        string resourceKey,
        TimeSpan expiryTime,
        TimeSpan waitTime,
        TimeSpan retryTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Try to acquire a lock without waiting
    /// </summary>
    /// <param name="resourceKey">Unique key for the resource to lock</param>
    /// <param name="expiryTime">Lock expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lock handle if successful, null otherwise</returns>
    Task<IDistributedLockHandle?> TryAcquireLockAsync(
        string resourceKey,
        TimeSpan expiryTime,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Handle for a distributed lock that can be released
/// </summary>
public interface IDistributedLockHandle : IAsyncDisposable
{
    /// <summary>
    /// The resource key this lock is for
    /// </summary>
    string ResourceKey { get; }

    /// <summary>
    /// Whether the lock is still valid
    /// </summary>
    bool IsAcquired { get; }

    /// <summary>
    /// Release the lock
    /// </summary>
    Task ReleaseAsync();
}
