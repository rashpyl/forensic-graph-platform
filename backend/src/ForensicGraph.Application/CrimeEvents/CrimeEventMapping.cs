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
    /// Projects the aggregate to a DTO with persons and every related event
    /// (both outgoing and incoming links) populated. Callers must pass
    /// already-loaded lookups; the mapping itself is pure.
    /// </summary>
    public static CrimeEventDto ToDetailDto(
        this CrimeEvent entity,
        IReadOnlyDictionary<Guid, Person> personLookup,
        IReadOnlyList<EventLink> incomingLinks,
        IReadOnlyDictionary<Guid, string> relatedTitleLookup)
    {
        var persons = entity.Persons
            .Select(ep =>
            {
                var person = personLookup.GetValueOrDefault(ep.PersonId);
                return new EventPersonDto
                {
                    PersonId = ep.PersonId,
                    FirstName = person?.FirstName ?? string.Empty,
                    LastName = person?.LastName ?? string.Empty,
                    Role = ep.Role,
                };
            })
            .ToArray();

        var outgoing = entity.OutgoingLinks.Select(l => new EventLinkDto
        {
            FromEventId = l.FromEventId,
            ToEventId = l.ToEventId,
            OtherEventId = l.ToEventId,
            OtherEventTitle = relatedTitleLookup.GetValueOrDefault(l.ToEventId, string.Empty),
            Note = l.Note,
            CreatedAt = l.CreatedAt,
        });

        var incoming = incomingLinks.Select(l => new EventLinkDto
        {
            FromEventId = l.FromEventId,
            ToEventId = l.ToEventId,
            OtherEventId = l.FromEventId,
            OtherEventTitle = relatedTitleLookup.GetValueOrDefault(l.FromEventId, string.Empty),
            Note = l.Note,
            CreatedAt = l.CreatedAt,
        });

        var links = outgoing
            .Concat(incoming)
            .OrderByDescending(l => l.CreatedAt)
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
