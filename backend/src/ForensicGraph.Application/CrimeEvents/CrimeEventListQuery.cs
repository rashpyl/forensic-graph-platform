namespace ForensicGraph.Application.CrimeEvents;

/// <summary>
/// Query parameters accepted by <c>GET /api/crime-events</c>.
/// All fields are optional; use <see cref="Normalize"/> to produce a query with sane defaults
/// and paging values clamped to the allowed range.
/// </summary>
public sealed record CrimeEventListQuery
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    public int Page { get; init; } = DefaultPage;
    public int PageSize { get; init; } = DefaultPageSize;
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int? MinSeverity { get; init; }
    public int? MaxSeverity { get; init; }

    /// <summary>
    /// Returns a copy of this query with <see cref="Page"/> and <see cref="PageSize"/>
    /// clamped to sensible bounds: page &lt; 1 → 1, pageSize outside [1, 200] → 50 or 200.
    /// </summary>
    public CrimeEventListQuery Normalize()
    {
        var page = Page < 1 ? DefaultPage : Page;
        var pageSize = PageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => PageSize,
        };
        return this with { Page = page, PageSize = pageSize };
    }
}
