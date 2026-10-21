using ForensicGraph.Domain.Persons;

namespace ForensicGraph.Tests.Domain;

public class PersonTests
{
    private static readonly DateTime NowUtc = new(2026, 4, 12, 14, 51, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("+420111222333", "+420111222333")]
    [InlineData(" +420111222333 ,+421999888777,, +420111222333 ", "+420111222333, +421999888777")]
    [InlineData(" , ", null)]
    [InlineData(null, null)]
    public void Phone_holds_a_normalized_comma_separated_list(string? input, string? expected)
    {
        var person = Person.Create(
            Guid.NewGuid(), "Anna", "Novak", null, null, input, null, NowUtc);

        Assert.Equal(expected, person.Phone);
    }

    [Fact]
    public void Update_normalizes_phone_list()
    {
        var person = Person.Create(Guid.NewGuid(), "Anna", "Novak", null, null, null, null, NowUtc);

        person.Update("Anna", "Novak", null, null, "111,  222", null, NowUtc);

        Assert.Equal("111, 222", person.Phone);
    }
}
