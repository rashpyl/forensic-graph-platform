namespace ForensicGraph.Application.Persons;

/// <summary>
/// Full response payload for a single person.
/// </summary>
public sealed record PersonDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public IReadOnlyList<string> Citizenships { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PassportNumbers { get; init; } = Array.Empty<string>();
    public string? Phone { get; init; }
    public string? PhysicalDescription { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
