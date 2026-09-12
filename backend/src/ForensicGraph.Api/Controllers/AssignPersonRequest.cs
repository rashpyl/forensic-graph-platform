using ForensicGraph.Domain.CrimeEvents;

namespace ForensicGraph.Api.Controllers;

/// <summary>
/// Body payload for <c>POST /api/events/{id}/persons</c>.
/// A person is assigned to an event under a specific <see cref="EventRole"/>;
/// the same person may hold multiple roles across different events, so <c>Role</c>
/// is part of the identifying key on the join.
/// </summary>
public sealed record AssignPersonRequest
{
    public Guid PersonId { get; init; }
    public EventRole Role { get; init; }
}
