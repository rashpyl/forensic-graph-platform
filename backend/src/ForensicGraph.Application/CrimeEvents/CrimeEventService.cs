using ForensicGraph.Application.Common;
using ForensicGraph.Domain.CrimeEvents;

namespace ForensicGraph.Application.CrimeEvents;

/// <summary>
/// Orchestrates crime-event use cases: list, get, create, update, delete.
/// Depends only on <see cref="ICrimeEventRepository"/> for persistence and
/// <see cref="TimeProvider"/> for server-managed timestamps — keeping the layer database-agnostic.
/// </summary>
public sealed class CrimeEventService
{
    private readonly ICrimeEventRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CrimeEventService(ICrimeEventRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
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
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(CrimeEvent), id);
        return entity.ToDto();
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
}
