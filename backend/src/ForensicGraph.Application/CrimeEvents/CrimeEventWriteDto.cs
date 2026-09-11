namespace ForensicGraph.Application.CrimeEvents;

/// <summary>
/// Request payload for creating or fully replacing a crime event.
/// Server-managed fields (<c>Id</c>, <c>CreatedAt</c>, <c>UpdatedAt</c>) are intentionally omitted
/// so clients cannot forge them; they are assigned by <see cref="CrimeEventService"/>.
/// </summary>
public sealed record CrimeEventWriteDto
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime OccurredAt { get; init; }
    public int Severity { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}
