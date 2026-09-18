using ForensicGraph.Application.Common;
using ForensicGraph.Application.Persons;
using ForensicGraph.Domain.CrimeEvents;
using ForensicGraph.Domain.Persons;

namespace ForensicGraph.Application.CrimeEvents;

/// <summary>
/// Orchestrates crime-event use cases: list, get, create, update, delete,
/// assign / unassign a <see cref="Person"/>, and link / unlink to another
/// <see cref="CrimeEvent"/>. Depends only on
/// <see cref="ICrimeEventRepository"/> / <see cref="IPersonRepository"/> for
/// persistence and <see cref="TimeProvider"/> for server-managed timestamps.
/// </summary>
public sealed class CrimeEventService
{
    private readonly ICrimeEventRepository _repository;
    private readonly IPersonRepository _personRepository;
    private readonly TimeProvider _timeProvider;

    public CrimeEventService(
        ICrimeEventRepository repository,
        IPersonRepository personRepository,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _personRepository = personRepository;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<CrimeEventDto>> ListAsync(
        CrimeEventListQuery query,
        CancellationToken cancellationToken)
    {
        var normalized = query.Normalize();
        var (items, total) = await _repository.ListAsync(normalized, cancellationToken);
        var dtos = items.Select(e => e.ToDto()).ToList();
        return new PagedResult<CrimeEventDto>(dtos, total, normalized.Page, normalized.PageSize);
    }

    public async Task<CrimeEventDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(CrimeEvent), id);

        var incomingLinks = await _repository.GetIncomingLinksAsync(id, cancellationToken);

        var personIds = entity.Persons.Select(ep => ep.PersonId).Distinct().ToArray();
        var relatedEventIds = entity.OutgoingLinks
            .Select(l => l.ToEventId)
            .Concat(incomingLinks.Select(l => l.FromEventId))
            .Distinct()
            .ToArray();

        var persons = personIds.Length == 0
            ? Array.Empty<Person>()
            : await _personRepository.GetManyAsync(personIds, cancellationToken);
        var personLookup = persons.ToDictionary(p => p.Id);

        IReadOnlyDictionary<Guid, string> titleLookup = relatedEventIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await _repository.GetTitlesAsync(relatedEventIds, cancellationToken);

        return entity.ToDetailDto(personLookup, incomingLinks, titleLookup);
    }

    public async Task<CrimeEventDto> CreateAsync(
        CrimeEventWriteDto dto,
        CancellationToken cancellationToken)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var entity = CrimeEvent.Create(
            id: Guid.NewGuid(),
            title: dto.Title,
            description: dto.Description,
            address: dto.Address,
            occurredAt: dto.OccurredAt,
            severity: dto.Severity,
            latitude: dto.Latitude,
            longitude: dto.Longitude,
            nowUtc: nowUtc);

        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<CrimeEventDto> UpdateAsync(
        Guid id,
        CrimeEventWriteDto dto,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(CrimeEvent), id);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        dto.ApplyTo(entity, nowUtc);

        await _repository.UpdateAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(CrimeEvent), id);

        await _repository.RemoveAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignPersonAsync(
        Guid crimeEventId,
        Guid personId,
        EventRole role,
        CancellationToken cancellationToken)
    {
        var eventEntity = await _repository.GetByIdWithDetailsAsync(crimeEventId, cancellationToken)
            ?? throw new NotFoundException(nameof(CrimeEvent), crimeEventId);

        _ = await _personRepository.GetByIdAsync(personId, cancellationToken)
            ?? throw new NotFoundException(nameof(Person), personId);

        var alreadyAssigned = eventEntity.Persons.Any(ep =>
            ep.PersonId == personId && ep.Role == role);
        if (alreadyAssigned)
        {
            throw new ConflictException(
                $"Person '{personId}' is already assigned to event '{crimeEventId}' with role '{role}'.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var assignment = EventPerson.Create(crimeEventId, personId, role, nowUtc);

        await _repository.AddPersonAssignmentAsync(assignment, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task UnassignPersonAsync(
        Guid crimeEventId,
        Guid personId,
        EventRole role,
        CancellationToken cancellationToken)
    {
        var eventEntity = await _repository.GetByIdWithDetailsAsync(crimeEventId, cancellationToken)
            ?? throw new NotFoundException(nameof(CrimeEvent), crimeEventId);

        var exists = eventEntity.Persons.Any(ep =>
            ep.PersonId == personId && ep.Role == role);
        if (!exists)
        {
            throw new NotFoundException(
                $"Person '{personId}' with role '{role}' is not assigned to event '{crimeEventId}'.");
        }

        await _repository.RemovePersonAssignmentAsync(crimeEventId, personId, role, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task LinkEventAsync(
        Guid fromEventId,
        Guid toEventId,
        string? note,
        CancellationToken cancellationToken)
    {
        if (fromEventId == toEventId)
        {
            throw new ConflictException("An event cannot be linked to itself.");
        }

        var fromEntity = await _repository.GetByIdWithDetailsAsync(fromEventId, cancellationToken)
            ?? throw new NotFoundException(nameof(CrimeEvent), fromEventId);

        var toExists = await _repository.ExistsAsync(toEventId, cancellationToken);
        if (!toExists)
        {
            throw new NotFoundException(nameof(CrimeEvent), toEventId);
        }

        var alreadyLinked = fromEntity.OutgoingLinks.Any(l => l.ToEventId == toEventId);
        if (alreadyLinked)
        {
            throw new ConflictException(
                $"Event '{fromEventId}' is already linked to '{toEventId}'.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var link = EventLink.Create(fromEventId, toEventId, note, nowUtc);

        await _repository.AddLinkAsync(link, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task UnlinkEventAsync(
        Guid fromEventId,
        Guid toEventId,
        CancellationToken cancellationToken)
    {
        var fromEntity = await _repository.GetByIdWithDetailsAsync(fromEventId, cancellationToken)
            ?? throw new NotFoundException(nameof(CrimeEvent), fromEventId);

        var exists = fromEntity.OutgoingLinks.Any(l => l.ToEventId == toEventId);
        if (!exists)
        {
            throw new NotFoundException(
                $"No link from event '{fromEventId}' to '{toEventId}' was found.");
        }

        await _repository.RemoveLinkAsync(fromEventId, toEventId, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
