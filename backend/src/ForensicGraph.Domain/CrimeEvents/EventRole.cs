namespace ForensicGraph.Domain.CrimeEvents;

/// <summary>
/// The role a <see cref="Persons.Person"/> plays in a specific <see cref="CrimeEvent"/>.
/// The same person may appear across multiple events with different roles; the role
/// therefore lives on the <c>EventPerson</c> join, not on the person itself.
/// </summary>
public enum EventRole
{
    Victim = 1,
    Suspect = 2,
    Witness = 3,
    Perpetrator = 4,
    Reporter = 5,
    Officer = 6,
}
