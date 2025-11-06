namespace Verdure.McpPlatform.Contracts.Models;

/// <summary>
/// Request model for paginated data
/// </summary>
public record PagedRequest
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; init; } = 12;

    /// <summary>
    /// Optional search term for filtering
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Optional sort field
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Sort direction (asc or desc)
    /// </summary>
    public string SortOrder { get; init; } = "desc";

    /// <summary>
    /// Validates and returns a safe page number
    /// </summary>
    public int GetSafePage() => Math.Max(1, Page);

    /// <summary>
    /// Validates and returns a safe page size (between 1 and 100)
    /// </summary>
    public int GetSafePageSize() => Math.Clamp(PageSize, 1, 100);

    /// <summary>
    /// Calculates the number of items to skip
    /// </summary>
    public int GetSkip() => (GetSafePage() - 1) * GetSafePageSize();
}
