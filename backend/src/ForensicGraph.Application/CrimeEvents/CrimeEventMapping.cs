using ForensicGraph.Domain.CrimeEvents;

namespace ForensicGraph.Application.CrimeEvents;

/// <summary>
/// Hand-written mapping between the <see cref="CrimeEvent"/> aggregate and its DTOs.
/// Deliberately avoids AutoMapper — the surface is small and explicit mapping keeps
/// the projection auditable for the diploma work.
/// </summary>
public static class CrimeEventMapping
{
    public static CrimeEventDto ToDto(this CrimeEvent entity)
    {
        return new CrimeEventDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            OccurredAt = entity.OccurredAt,
            Severity = entity.Severity,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
    }

    public static void ApplyTo(this CrimeEventWriteDto dto, CrimeEvent entity, DateTime nowUtc)
    {
        entity.Update(
            title: dto.Title,
            description: dto.Description,
            occurredAt: dto.OccurredAt,
            severity: dto.Severity,
            latitude: dto.Latitude,
            longitude: dto.Longitude,
            nowUtc: nowUtc);
    }
}
