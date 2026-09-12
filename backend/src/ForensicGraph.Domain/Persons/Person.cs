namespace ForensicGraph.Domain.Persons;

/// <summary>
/// Aggregate root representing an individual known to the investigation:
/// victim, suspect, witness, perpetrator, reporter, or officer. The role a
/// person plays is per-event and is stored on the <c>EventPerson</c> join,
/// not on the <see cref="Person"/> itself, because the same individual may
/// appear across multiple events in different roles.
/// </summary>
public sealed class Person
{
    public const int NameMaxLength = 100;
    public const int PhoneMaxLength = 40;
    public const int PhysicalDescriptionMaxLength = 2000;
    public const int CitizenshipMaxLength = 60;
    public const int PassportMaxLength = 40;

    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? PhysicalDescription { get; private set; }
    public List<string> Citizenships { get; private set; } = new();
    public List<string> PassportNumbers { get; private set; } = new();
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Person()
    {
    }

    public static Person Create(
        Guid id,
        string firstName,
        string lastName,
        IEnumerable<string>? citizenships,
        IEnumerable<string>? passportNumbers,
        string? phone,
        string? physicalDescription,
        DateTime nowUtc)
    {
        GuardName(firstName, nameof(firstName));
        GuardName(lastName, nameof(lastName));
        GuardPhone(phone);
        GuardPhysicalDescription(physicalDescription);
        var createdUtc = EnsureUtc(nowUtc, nameof(nowUtc));

        return new Person
        {
            Id = id,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            PhysicalDescription = string.IsNullOrWhiteSpace(physicalDescription)
                ? null
                : physicalDescription.Trim(),
            Citizenships = NormalizeStringList(citizenships, CitizenshipMaxLength, nameof(citizenships)),
            PassportNumbers = NormalizeStringList(passportNumbers, PassportMaxLength, nameof(passportNumbers)),
            CreatedAt = createdUtc,
            UpdatedAt = createdUtc,
        };
    }

    public void Update(
        string firstName,
        string lastName,
        IEnumerable<string>? citizenships,
        IEnumerable<string>? passportNumbers,
        string? phone,
        string? physicalDescription,
        DateTime nowUtc)
    {
        GuardName(firstName, nameof(firstName));
        GuardName(lastName, nameof(lastName));
        GuardPhone(phone);
        GuardPhysicalDescription(physicalDescription);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        PhysicalDescription = string.IsNullOrWhiteSpace(physicalDescription)
            ? null
            : physicalDescription.Trim();
        Citizenships = NormalizeStringList(citizenships, CitizenshipMaxLength, nameof(citizenships));
        PassportNumbers = NormalizeStringList(passportNumbers, PassportMaxLength, nameof(passportNumbers));
        UpdatedAt = EnsureUtc(nowUtc, nameof(nowUtc));
    }

    private static List<string> NormalizeStringList(
        IEnumerable<string>? values,
        int maxLength,
        string paramName)
    {
        var result = new List<string>();
        if (values is null)
        {
            return result;
        }

        foreach (var raw in values)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var trimmed = raw.Trim();
            if (trimmed.Length > maxLength)
            {
                throw new ArgumentException(
                    $"Value '{trimmed}' exceeds {maxLength} characters.",
                    paramName);
            }

            if (!result.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(trimmed);
            }
        }

        return result;
    }

    private static void GuardName(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} must not be empty.", paramName);
        }

        if (value.Length > NameMaxLength)
        {
            throw new ArgumentException(
                $"{paramName} must be at most {NameMaxLength} characters.", paramName);
        }
    }

    private static void GuardPhone(string? phone)
    {
        if (phone is { Length: > PhoneMaxLength })
        {
            throw new ArgumentException(
                $"Phone must be at most {PhoneMaxLength} characters.", nameof(phone));
        }
    }

    private static void GuardPhysicalDescription(string? description)
    {
        if (description is { Length: > PhysicalDescriptionMaxLength })
        {
            throw new ArgumentException(
                $"Physical description must be at most {PhysicalDescriptionMaxLength} characters.",
                nameof(description));
        }
    }

    private static DateTime EnsureUtc(DateTime value, string paramName)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => throw new ArgumentException(
                $"{paramName} must be a UTC DateTime (Kind was Unspecified).", paramName),
        };
    }
}
