namespace ForensicGraph.Application.CrimeEvents;

/// <summary>
/// One connection between the current event and another event, as seen from the
/// current event's perspective. Because <see cref="Domain.CrimeEvents.EventLink"/>
/// is directional, a single logical "connection" is projected once — either the
/// current event is the <c>from</c> side (outgoing) or the <c>to</c> side
/// (incoming). The <c>Other*</c> pair identifies the peer for display; the
/// <c>From</c>/<c>To</c> ids are the underlying edge, needed to construct the
/// correct <c>DELETE /api/events/{fromId}/links/{toId}</c> call regardless of
/// direction.
/// </summary>
public sealed record EventLinkDto
{
    public Guid FromEventId { get; init; }
    public Guid ToEventId { get; init; }
    public Guid OtherEventId { get; init; }
    public string OtherEventTitle { get; init; } = string.Empty;
    public string? Note { get; init; }
    public DateTime CreatedAt { get; init; }
}
