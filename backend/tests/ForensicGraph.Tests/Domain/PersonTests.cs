using ForensicGraph.Domain.Persons;

namespace ForensicGraph.Tests.Domain;

public class PersonTests
{
    private static readonly DateTime NowUtc = new(2026, 4, 12, 14, 51, 0, DateTimeKind.Utc);

    [Fact]
    public void Phones_are_trimmed_and_deduplicated_in_order()
    {
        var person = Person.Create(
            Guid.NewGuid(), "Anna", "Novak", null, null,
            new[] { " +420111222333 ", "+421999888777", "", "  ", "+420111222333" }, null, NowUtc);

        Assert.Equal(new[] { "+420111222333", "+421999888777" }, person.Phones);
    }

    [Fact]
    public void Missing_phones_become_an_empty_list()
    {
        var person = Person.Create(Guid.NewGuid(), "Anna", "Novak", null, null, null, null, NowUtc);

        Assert.Empty(person.Phones);
    }

    [Fact]
    public void Update_normalizes_phones()
    {
        var person = Person.Create(Guid.NewGuid(), "Anna", "Novak", null, null, null, null, NowUtc);

        person.Update("Anna", "Novak", null, null, new[] { "111", " 222 " }, null, NowUtc);

        Assert.Equal(new[] { "111", "222" }, person.Phones);
    }

    [Fact]
    public void Too_many_phones_are_rejected()
    {
        var phones = Enumerable.Range(0, Person.MaxPhones + 1).Select(i => $"+42060000000{i:D2}");

        Assert.Throws<ArgumentException>(() =>
            Person.Create(Guid.NewGuid(), "Anna", "Novak", null, null, phones, null, NowUtc));
    }
}
