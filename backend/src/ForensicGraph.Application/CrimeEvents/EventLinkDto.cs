namespace ForensicGraph.Application.CrimeEvents;

/// <summary>
/// Nested projection: one manual investigator-created link out of an event.
/// The target event's title is denormalised for cheap rendering in the
/// detail panel.
/// </summary>
public sealed record EventLinkDto
{
    public Guid ToEventId { get; init; }
    public string ToEventTitle { get; init; } = string.Empty;
    public string? Note { get; init; }
    public DateTime CreatedAt { get; init; }
}
