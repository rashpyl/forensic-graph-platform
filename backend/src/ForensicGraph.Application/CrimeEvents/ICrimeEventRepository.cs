using ForensicGraph.Domain.CrimeEvents;

namespace ForensicGraph.Application.CrimeEvents;

/// <summary>
/// Persistence contract for <see cref="CrimeEvent"/>.
/// The application layer depends only on this interface; the EF Core-backed implementation
/// lives in <c>ForensicGraph.Infrastructure</c>.
/// </summary>
public interface ICrimeEventRepository
{
    /// <summary>Loads an event without navigations — cheapest read.</summary>
    Task<CrimeEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Loads an event with its <c>Persons</c> and <c>OutgoingLinks</c> populated.</summary>
    Task<CrimeEvent?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<CrimeEvent> Items, int TotalCount)> ListAsync(
        CrimeEventListQuery query,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns a title lookup for the given event ids — used by the mapping layer to
    /// denormalise link targets without a second round-trip per link.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> GetTitlesAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken);

    Task AddAsync(CrimeEvent entity, CancellationToken cancellationToken);

    Task UpdateAsync(CrimeEvent entity, CancellationToken cancellationToken);

    Task RemoveAsync(CrimeEvent entity, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task AddPersonAssignmentAsync(EventPerson assignment, CancellationToken cancellationToken);

    Task RemovePersonAssignmentAsync(
        Guid crimeEventId,
        Guid personId,
        EventRole role,
        CancellationToken cancellationToken);

    Task AddLinkAsync(EventLink link, CancellationToken cancellationToken);

    Task RemoveLinkAsync(
        Guid fromEventId,
        Guid toEventId,
        CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
