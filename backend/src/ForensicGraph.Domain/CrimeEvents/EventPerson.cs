using ForensicGraph.Domain.Persons;

namespace ForensicGraph.Domain.CrimeEvents;

/// <summary>
/// Join entity linking a <see cref="Person"/> to a <see cref="CrimeEvent"/> with a
/// specific <see cref="EventRole"/>. The composite key <c>(CrimeEventId, PersonId, Role)</c>
/// means the same person may participate in the same event under more than one role
/// (e.g. reporter and witness), and in different events under different roles.
/// </summary>
public sealed class EventPerson
{
    public Guid CrimeEventId { get; private set; }
    public Guid PersonId { get; private set; }
    public EventRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private EventPerson()
    {
    }

    public static EventPerson Create(Guid crimeEventId, Guid personId, EventRole role, DateTime nowUtc)
    {
        if (crimeEventId == Guid.Empty)
        {
            throw new ArgumentException("Crime event id must not be empty.", nameof(crimeEventId));
        }

        if (personId == Guid.Empty)
        {
            throw new ArgumentException("Person id must not be empty.", nameof(personId));
        }

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown event role.");
        }

        var createdUtc = nowUtc.Kind switch
        {
            DateTimeKind.Utc => nowUtc,
            DateTimeKind.Local => nowUtc.ToUniversalTime(),
            _ => throw new ArgumentException("nowUtc must be a UTC DateTime.", nameof(nowUtc)),
        };

        return new EventPerson
        {
            CrimeEventId = crimeEventId,
            PersonId = personId,
            Role = role,
            CreatedAt = createdUtc,
        };
    }
}
