namespace ForensicGraph.Domain.CrimeEvents;

/// <summary>
/// Aggregate root representing a single crime event on the forensic timeline.
/// Instances are constructed via <see cref="Create"/> and mutated via <see cref="Update"/>,
/// both of which enforce the domain invariants required by the crime-event-api spec.
/// </summary>
public sealed class CrimeEvent
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int AddressMaxLength = 500;
    public const int MinSeverity = 1;
    public const int MaxSeverity = 5;

    private readonly List<EventPerson> _persons = new();
    private readonly List<EventLink> _outgoingLinks = new();

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Address { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public int Severity { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public IReadOnlyList<EventPerson> Persons => _persons.AsReadOnly();
    public IReadOnlyList<EventLink> OutgoingLinks => _outgoingLinks.AsReadOnly();

    private CrimeEvent()
    {
    }

    public static CrimeEvent Create(
        Guid id,
        string title,
        string? description,
        string? address,
        DateTime occurredAt,
        int severity,
        double? latitude,
        double? longitude,
        DateTime nowUtc)
    {
        GuardTitle(title);
        GuardDescription(description);
        GuardAddress(address);
        GuardSeverity(severity);
        GuardCoordinates(latitude, longitude);
        var occurredUtc = EnsureUtc(occurredAt, nameof(occurredAt));
        var createdUtc = EnsureUtc(nowUtc, nameof(nowUtc));

        return new CrimeEvent
        {
            Id = id,
            Title = title,
            Description = description,
            Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
            OccurredAt = occurredUtc,
            Severity = severity,
            Latitude = latitude,
            Longitude = longitude,
            CreatedAt = createdUtc,
            UpdatedAt = createdUtc,
        };
    }

    public void Update(
        string title,
        string? description,
        string? address,
        DateTime occurredAt,
        int severity,
        double? latitude,
        double? longitude,
        DateTime nowUtc)
    {
        GuardTitle(title);
        GuardDescription(description);
        GuardAddress(address);
        GuardSeverity(severity);
        GuardCoordinates(latitude, longitude);

        Title = title;
        Description = description;
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        OccurredAt = EnsureUtc(occurredAt, nameof(occurredAt));
        Severity = severity;
        Latitude = latitude;
        Longitude = longitude;
        UpdatedAt = EnsureUtc(nowUtc, nameof(nowUtc));
    }

    private static void GuardTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title must not be empty.", nameof(title));
        }

        if (title.Length > TitleMaxLength)
        {
            throw new ArgumentException(
                $"Title must be at most {TitleMaxLength} characters.", nameof(title));
        }
    }

    private static void GuardDescription(string? description)
    {
        if (description is { Length: > DescriptionMaxLength })
        {
            throw new ArgumentException(
                $"Description must be at most {DescriptionMaxLength} characters.", nameof(description));
        }
    }

    private static void GuardAddress(string? address)
    {
        if (address is { Length: > AddressMaxLength })
        {
            throw new ArgumentException(
                $"Address must be at most {AddressMaxLength} characters.", nameof(address));
        }
    }

    private static void GuardSeverity(int severity)
    {
        if (severity is < MinSeverity or > MaxSeverity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(severity),
                severity,
                $"Severity must be between {MinSeverity} and {MaxSeverity}.");
        }
    }

    private static void GuardCoordinates(double? latitude, double? longitude)
    {
        if (latitude.HasValue != longitude.HasValue)
        {
            throw new ArgumentException(
                "Latitude and longitude must be provided together or both left empty.",
                latitude.HasValue ? nameof(longitude) : nameof(latitude));
        }

        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be in [-90, 90].");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be in [-180, 180].");
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
