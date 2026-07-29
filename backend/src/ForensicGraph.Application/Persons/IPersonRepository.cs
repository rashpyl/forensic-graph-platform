using ForensicGraph.Domain.Persons;

namespace ForensicGraph.Application.Persons;

public interface IPersonRepository
{
    Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Person>> GetManyAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<Person> Items, int TotalCount)> ListAsync(
        PersonListQuery query,
        CancellationToken cancellationToken);

    Task AddAsync(Person entity, CancellationToken cancellationToken);

    Task UpdateAsync(Person entity, CancellationToken cancellationToken);

    Task RemoveAsync(Person entity, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
