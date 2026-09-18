namespace ForensicGraph.Application.CrimeEvents;

/// <summary>
/// Full response payload for a single crime event, including server-managed fields.
/// </summary>
public sealed record CrimeEventDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Address { get; init; }
    public DateTime OccurredAt { get; init; }
    public int Severity { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public IReadOnlyList<EventPersonDto> Persons { get; init; } = Array.Empty<EventPersonDto>();
    public IReadOnlyList<EventLinkDto> Links { get; init; } = Array.Empty<EventLinkDto>();
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
