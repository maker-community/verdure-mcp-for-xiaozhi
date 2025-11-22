namespace Verdure.McpPlatform.Contracts.DTOs;

/// <summary>
/// Result of syncing all MCP service tools (Admin operation)
/// </summary>
public record SyncAllToolsResultDto
{
    /// <summary>
    /// Total number of services processed
    /// </summary>
    public int TotalProcessed { get; init; }
    
    /// <summary>
    /// Number of services successfully synced
    /// </summary>
    public int SuccessCount { get; init; }
    
    /// <summary>
    /// Number of services that failed to sync
    /// </summary>
    public int FailedCount { get; init; }
    
    /// <summary>
    /// List of failed service names with error messages
    /// </summary>
    public List<FailedServiceSync> FailedServices { get; init; } = new();
}

/// <summary>
/// Details of a failed service sync
/// </summary>
public record FailedServiceSync
{
    /// <summary>
    /// Service ID
    /// </summary>
    public string ServiceId { get; init; } = string.Empty;
    
    /// <summary>
    /// Service name
    /// </summary>
    public string ServiceName { get; init; } = string.Empty;
    
    /// <summary>
    /// Error message
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;
}
