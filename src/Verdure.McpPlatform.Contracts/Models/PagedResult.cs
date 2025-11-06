namespace Verdure.McpPlatform.Contracts.Models;

/// <summary>
/// Generic paginated result wrapper
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public record PagedResult<T>
{
    /// <summary>
    /// Items for the current page
    /// </summary>
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Creates an empty paged result
    /// </summary>
    public static PagedResult<T> Empty(int page = 1, int pageSize = 12) => new()
    {
        Items = Enumerable.Empty<T>(),
        TotalCount = 0,
        Page = page,
        PageSize = pageSize
    };

    /// <summary>
    /// Creates a paged result from a list and total count
    /// </summary>
    public static PagedResult<T> Create(IEnumerable<T> items, int totalCount, int page, int pageSize) => new()
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}
