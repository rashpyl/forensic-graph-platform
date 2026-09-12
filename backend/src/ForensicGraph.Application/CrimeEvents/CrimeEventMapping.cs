using ForensicGraph.Domain.CrimeEvents;
using ForensicGraph.Domain.Persons;

namespace ForensicGraph.Application.CrimeEvents;

/// <summary>
/// Hand-written mapping between the <see cref="CrimeEvent"/> aggregate and its DTOs.
/// Deliberately avoids AutoMapper — the surface is small and explicit mapping keeps
/// the projection auditable for the diploma work.
/// </summary>
public static class CrimeEventMapping
{
    /// <summary>
    /// Projects the aggregate to a DTO without persons or links populated.
    /// Suitable for list responses where nested navigations would be too costly.
    /// </summary>
    public static CrimeEventDto ToDto(this CrimeEvent entity)
    {
        return new CrimeEventDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            Address = entity.Address,
            OccurredAt = entity.OccurredAt,
            Severity = entity.Severity,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
    }

    /// <summary>
    /// Projects the aggregate to a DTO with persons and outgoing links populated.
    /// Callers must pass already-loaded lookups; the mapping itself is pure.
    /// </summary>
    public static CrimeEventDto ToDetailDto(
        this CrimeEvent entity,
        IReadOnlyDictionary<Guid, Person> personLookup,
        IReadOnlyDictionary<Guid, string> targetTitleLookup)
    {
        var persons = entity.Persons
            .Select(ep => new EventPersonDto
            {
                PersonId = ep.PersonId,
                FirstName = personLookup.TryGetValue(ep.PersonId, out var person)
                    ? person.FirstName
                    : string.Empty,
                LastName = personLookup.TryGetValue(ep.PersonId, out var person2)
                    ? person2.LastName
                    : string.Empty,
                Role = ep.Role,
            })
            .ToArray();

        var links = entity.OutgoingLinks
            .Select(l => new EventLinkDto
            {
                ToEventId = l.ToEventId,
                ToEventTitle = targetTitleLookup.TryGetValue(l.ToEventId, out var title)
                    ? title
                    : string.Empty,
                Note = l.Note,
                CreatedAt = l.CreatedAt,
            })
            .ToArray();

        return entity.ToDto() with { Persons = persons, Links = links };
    }

    public static void ApplyTo(this CrimeEventWriteDto dto, CrimeEvent entity, DateTime nowUtc)
    {
        entity.Update(
            title: dto.Title,
            description: dto.Description,
            address: dto.Address,
            occurredAt: dto.OccurredAt,
            severity: dto.Severity,
            latitude: dto.Latitude,
            longitude: dto.Longitude,
            nowUtc: nowUtc);
    }
}
