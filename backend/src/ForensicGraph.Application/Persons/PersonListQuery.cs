namespace ForensicGraph.Application.Persons;

/// <summary>
/// Query parameters accepted by <c>GET /api/persons</c>.
/// The <see cref="Q"/> parameter powers autocomplete: a case-insensitive substring
/// match against the concatenation of first and last name.
/// </summary>
public sealed record PersonListQuery
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    public int Page { get; init; } = DefaultPage;
    public int PageSize { get; init; } = DefaultPageSize;
    public string? Q { get; init; }

    public PersonListQuery Normalize()
    {
        var page = Page < 1 ? DefaultPage : Page;
        var pageSize = PageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => PageSize,
        };
        var q = string.IsNullOrWhiteSpace(Q) ? null : Q.Trim();
        return this with { Page = page, PageSize = pageSize, Q = q };
    }
}
