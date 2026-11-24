using ForensicGraph.Domain.Persons;

namespace ForensicGraph.Application.Persons;

/// <summary>
/// Hand-written mapping between the <see cref="Person"/> aggregate and its DTOs.
/// Same style as <c>CrimeEventMapping</c>: explicit, auditable, no runtime reflection.
/// </summary>
public static class PersonMapping
{
    public static PersonDto ToDto(this Person entity)
    {
        return new PersonDto
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Citizenships = entity.Citizenships.ToArray(),
            PassportNumbers = entity.PassportNumbers.ToArray(),
            Phones = entity.Phones.ToArray(),
            PhysicalDescription = entity.PhysicalDescription,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Version = entity.Version,
        };
    }

    public static void ApplyTo(this PersonWriteDto dto, Person entity, DateTime nowUtc)
    {
        entity.Update(
            firstName: dto.FirstName,
            lastName: dto.LastName,
            citizenships: dto.Citizenships,
            passportNumbers: dto.PassportNumbers,
            phones: dto.Phones,
            physicalDescription: dto.PhysicalDescription,
            nowUtc: nowUtc);
    }
}
