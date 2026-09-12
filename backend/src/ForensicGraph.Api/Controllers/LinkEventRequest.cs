namespace ForensicGraph.Api.Controllers;

/// <summary>
/// Body payload for <c>POST /api/events/{id}/links</c>.
/// Links are directed (<c>id</c> → <c>ToEventId</c>) and manually created by an
/// investigator; the optional <see cref="Note"/> records why the link was drawn.
/// </summary>
public sealed record LinkEventRequest
{
    public Guid ToEventId { get; init; }
    public string? Note { get; init; }
}
