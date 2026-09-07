using ForensicGraph.Application.CrimeEvents;
using ForensicGraph.Domain.CrimeEvents;
using ForensicGraph.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForensicGraph.Infrastructure.CrimeEvents;

/// <summary>
/// EF Core-backed implementation of <see cref="ICrimeEventRepository"/>.
/// All queries are scoped to the injected <see cref="ForensicGraphDbContext"/>;
/// no cross-context state is kept. Read paths use <c>AsNoTracking</c> so list
/// responses do not pollute the change tracker.
/// </summary>
public sealed class CrimeEventRepository : ICrimeEventRepository
{
    private readonly ForensicGraphDbContext _db;

    public CrimeEventRepository(ForensicGraphDbContext db)
    {
        _db = db;
    }

    public Task<CrimeEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.CrimeEvents.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public Task<CrimeEvent?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.CrimeEvents
            .Include(e => e.Persons)
            .Include(e => e.OutgoingLinks)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<CrimeEvent> Items, int TotalCount)> ListAsync(
        CrimeEventListQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<CrimeEvent> q = _db.CrimeEvents.AsNoTracking();

        if (query.From.HasValue)
        {
            q = q.Where(e => e.OccurredAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            q = q.Where(e => e.OccurredAt <= query.To.Value);
        }

        if (query.MinSeverity.HasValue)
        {
            q = q.Where(e => e.Severity >= query.MinSeverity.Value);
        }

        if (query.MaxSeverity.HasValue)
        {
            q = q.Where(e => e.Severity <= query.MaxSeverity.Value);
        }

        var total = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(e => e.OccurredAt)
            .ThenBy(e => e.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetTitlesAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken)
    {
        var idList = ids.Distinct().ToArray();
        if (idList.Length == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var pairs = await _db.CrimeEvents
            .AsNoTracking()
            .Where(e => idList.Contains(e.Id))
            .Select(e => new { e.Id, e.Title })
            .ToListAsync(cancellationToken);

        return pairs.ToDictionary(p => p.Id, p => p.Title);
    }

    public async Task AddAsync(CrimeEvent entity, CancellationToken cancellationToken)
    {
        await _db.CrimeEvents.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(CrimeEvent entity, CancellationToken cancellationToken)
    {
        // EF already tracks entities loaded via GetByIdAsync — SaveChangesAsync alone would
        // issue UPDATE. The explicit call keeps the interface honest for non-EF stubs.
        _db.CrimeEvents.Update(entity);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(CrimeEvent entity, CancellationToken cancellationToken)
    {
        _db.CrimeEvents.Remove(entity);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.CrimeEvents.AnyAsync(e => e.Id == id, cancellationToken);
    }

    public async Task AddPersonAssignmentAsync(EventPerson assignment, CancellationToken cancellationToken)
    {
        await _db.EventPersons.AddAsync(assignment, cancellationToken);
    }

    public async Task RemovePersonAssignmentAsync(
        Guid crimeEventId,
        Guid personId,
        EventRole role,
        CancellationToken cancellationToken)
    {
        var assignment = await _db.EventPersons
            .FirstOrDefaultAsync(
                ep => ep.CrimeEventId == crimeEventId
                   && ep.PersonId == personId
                   && ep.Role == role,
                cancellationToken);

        if (assignment is not null)
        {
            _db.EventPersons.Remove(assignment);
        }
    }

    public async Task AddLinkAsync(EventLink link, CancellationToken cancellationToken)
    {
        await _db.EventLinks.AddAsync(link, cancellationToken);
    }

    public async Task<IReadOnlyList<EventLink>> GetIncomingLinksAsync(
        Guid toEventId,
        CancellationToken cancellationToken)
    {
        return await _db.EventLinks
            .AsNoTracking()
            .Where(l => l.ToEventId == toEventId)
            .ToListAsync(cancellationToken);
    }

    public async Task RemoveLinkAsync(
        Guid fromEventId,
        Guid toEventId,
        CancellationToken cancellationToken)
    {
        var link = await _db.EventLinks
            .FirstOrDefaultAsync(
                l => l.FromEventId == fromEventId && l.ToEventId == toEventId,
                cancellationToken);

        if (link is not null)
        {
            _db.EventLinks.Remove(link);
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
