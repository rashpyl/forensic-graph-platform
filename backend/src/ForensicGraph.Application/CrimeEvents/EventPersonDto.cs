using ForensicGraph.Domain.CrimeEvents;

namespace ForensicGraph.Application.CrimeEvents;

/// <summary>
/// Nested projection: one person's participation in an event.
/// Only carries the fields needed by the event detail panel — the full
/// <c>PersonDto</c> is available via <c>GET /api/persons/{id}</c>.
/// </summary>
public sealed record EventPersonDto
{
    public Guid PersonId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public EventRole Role { get; init; }
}
