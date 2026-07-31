using ForensicGraph.Application.Persons;
using ForensicGraph.Domain.Persons;
using ForensicGraph.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForensicGraph.Infrastructure.Persons;

/// <summary>
/// EF Core-backed implementation of <see cref="IPersonRepository"/>.
/// The <c>q</c> search parameter is a case-insensitive substring match against
/// <c>first_name || ' ' || last_name</c> using PostgreSQL's <c>ILIKE</c>.
/// </summary>
public sealed class PersonRepository : IPersonRepository
{
    private readonly ForensicGraphDbContext _db;

    public PersonRepository(ForensicGraphDbContext db)
    {
        _db = db;
    }

    public Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.Persons.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetManyAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken)
    {
        var idList = ids.Distinct().ToArray();
        if (idList.Length == 0)
        {
            return Array.Empty<Person>();
        }

        return await _db.Persons
            .AsNoTracking()
            .Where(p => idList.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Person> Items, int TotalCount)> ListAsync(
        PersonListQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<Person> q = _db.Persons.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = $"%{query.Q.Trim()}%";
            q = q.Where(p =>
                EF.Functions.ILike(p.FirstName + " " + p.LastName, pattern));
        }

        var total = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ThenBy(p => p.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(Person entity, CancellationToken cancellationToken)
    {
        await _db.Persons.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(Person entity, CancellationToken cancellationToken)
    {
        _db.Persons.Update(entity);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Person entity, CancellationToken cancellationToken)
    {
        _db.Persons.Remove(entity);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
