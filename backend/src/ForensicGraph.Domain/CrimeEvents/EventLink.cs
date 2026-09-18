namespace ForensicGraph.Domain.CrimeEvents;

/// <summary>
/// A manual, investigator-created connection between two crime events.
/// Directed: <see cref="FromEventId"/> → <see cref="ToEventId"/>. The pair
/// is unique; a self-link (same source and target) is not permitted.
/// </summary>
public sealed class EventLink
{
    public const int NoteMaxLength = 500;

    public Guid FromEventId { get; private set; }
    public Guid ToEventId { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private EventLink()
    {
    }

    public static EventLink Create(Guid fromEventId, Guid toEventId, string? note, DateTime nowUtc)
    {
        if (fromEventId == Guid.Empty)
        {
            throw new ArgumentException("From event id must not be empty.", nameof(fromEventId));
        }

        if (toEventId == Guid.Empty)
        {
            throw new ArgumentException("To event id must not be empty.", nameof(toEventId));
        }

        if (fromEventId == toEventId)
        {
            throw new ArgumentException("An event cannot be linked to itself.", nameof(toEventId));
        }

        if (note is { Length: > NoteMaxLength })
        {
            throw new ArgumentException(
                $"Note must be at most {NoteMaxLength} characters.", nameof(note));
        }

        var createdUtc = nowUtc.Kind switch
        {
            DateTimeKind.Utc => nowUtc,
            DateTimeKind.Local => nowUtc.ToUniversalTime(),
            _ => throw new ArgumentException("nowUtc must be a UTC DateTime.", nameof(nowUtc)),
        };

        return new EventLink
        {
            FromEventId = fromEventId,
            ToEventId = toEventId,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAt = createdUtc,
        };
    }
}
