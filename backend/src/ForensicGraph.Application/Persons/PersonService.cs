using ForensicGraph.Application.Common;
using ForensicGraph.Domain.Persons;

namespace ForensicGraph.Application.Persons;

/// <summary>
/// Use cases for the <see cref="Person"/> aggregate: list (with autocomplete),
/// get by id, create, update, delete. Follows the same service-per-aggregate
/// pattern as <c>CrimeEventService</c>.
/// </summary>
public sealed class PersonService
{
    private readonly IPersonRepository _repository;
    private readonly TimeProvider _timeProvider;

    public PersonService(IPersonRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<PersonDto>> ListAsync(
        PersonListQuery query,
        CancellationToken cancellationToken)
    {
        var normalized = query.Normalize();
        var (items, total) = await _repository.ListAsync(normalized, cancellationToken);
        var dtos = items.Select(p => p.ToDto()).ToList();
        return new PagedResult<PersonDto>(dtos, total, normalized.Page, normalized.PageSize);
    }

    public async Task<PersonDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Person), id);
        return entity.ToDto();
    }

    public async Task<PersonDto> CreateAsync(
        PersonWriteDto dto,
        CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var entity = Person.Create(
            id: Guid.NewGuid(),
            firstName: dto.FirstName,
            lastName: dto.LastName,
            citizenships: dto.Citizenships,
            passportNumbers: dto.PassportNumbers,
            phone: dto.Phone,
            physicalDescription: dto.PhysicalDescription,
            nowUtc: nowUtc);

        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<PersonDto> UpdateAsync(
        Guid id,
        PersonWriteDto dto,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Person), id);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        dto.ApplyTo(entity, nowUtc);

        await _repository.UpdateAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Person), id);

        await _repository.RemoveAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
