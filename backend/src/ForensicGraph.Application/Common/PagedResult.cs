namespace ForensicGraph.Application.Common;

/// <summary>
/// Envelope returned by paginated list endpoints.
/// <see cref="Page"/> and <see cref="PageSize"/> reflect the effective values applied by the service
/// (i.e. after normalisation and any caps), not necessarily the raw request values.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
