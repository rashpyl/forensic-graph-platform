namespace ForensicGraph.Application.Persons;

/// <summary>
/// Request payload for creating or fully replacing a person.
/// Server-managed fields (<c>Id</c>, <c>CreatedAt</c>, <c>UpdatedAt</c>) are intentionally
/// omitted so clients cannot forge them; they are assigned by <see cref="PersonService"/>.
/// </summary>
public sealed record PersonWriteDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public IReadOnlyList<string> Citizenships { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PassportNumbers { get; init; } = Array.Empty<string>();
    public string? Phone { get; init; }
    public string? PhysicalDescription { get; init; }
}
