using ForensicGraph.Domain.CrimeEvents;

namespace ForensicGraph.Application.CrimeEvents;

/// <summary>
/// Persistence contract for <see cref="CrimeEvent"/>.
/// The application layer depends only on this interface; the EF Core-backed implementation
/// lives in <c>ForensicGraph.Infrastructure</c>.
/// </summary>
public interface ICrimeEventRepository
{
    Task<CrimeEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<CrimeEvent> Items, int TotalCount)> ListAsync(
        CrimeEventListQuery query,
        CancellationToken cancellationToken);

    Task AddAsync(CrimeEvent entity, CancellationToken cancellationToken);

    Task UpdateAsync(CrimeEvent entity, CancellationToken cancellationToken);

    Task RemoveAsync(CrimeEvent entity, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
