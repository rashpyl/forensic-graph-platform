using ForensicGraph.Domain.CrimeEvents;

namespace ForensicGraph.Tests.Domain;

public class CrimeEventTests
{
    private static readonly DateTime NowUtc = new(2026, 4, 12, 14, 51, 0, DateTimeKind.Utc);
    private static readonly DateTime OccurredUtc = new(2026, 4, 10, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidArguments_SetsAllProperties()
    {
        var id = Guid.NewGuid();

        var evt = CrimeEvent.Create(
            id,
            title: "Robbery at Main St.",
            description: "Armed robbery reported at 09:00.",
            occurredAt: OccurredUtc,
            severity: 3,
            latitude: 50.0755,
            longitude: 14.4378,
            nowUtc: NowUtc);

        Assert.Equal(id, evt.Id);
        Assert.Equal("Robbery at Main St.", evt.Title);
        Assert.Equal("Armed robbery reported at 09:00.", evt.Description);
        Assert.Equal(OccurredUtc, evt.OccurredAt);
        Assert.Equal(DateTimeKind.Utc, evt.OccurredAt.Kind);
        Assert.Equal(3, evt.Severity);
        Assert.Equal(50.0755, evt.Latitude);
        Assert.Equal(14.4378, evt.Longitude);
        Assert.Equal(NowUtc, evt.CreatedAt);
        Assert.Equal(NowUtc, evt.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyTitle_Throws(string title)
    {
        Assert.Throws<ArgumentException>(() => CrimeEvent.Create(
            Guid.NewGuid(), title, null, OccurredUtc, 1, null, null, NowUtc));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Create_WithSeverityOutOfRange_Throws(int severity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CrimeEvent.Create(
            Guid.NewGuid(), "Title", null, OccurredUtc, severity, null, null, NowUtc));
    }

    [Fact]
    public void Create_WithLatitudeButNoLongitude_Throws()
    {
        Assert.Throws<ArgumentException>(() => CrimeEvent.Create(
            Guid.NewGuid(), "Title", null, OccurredUtc, 3, latitude: 50.0, longitude: null, NowUtc));
    }

    [Fact]
    public void Create_WithLongitudeButNoLatitude_Throws()
    {
        Assert.Throws<ArgumentException>(() => CrimeEvent.Create(
            Guid.NewGuid(), "Title", null, OccurredUtc, 3, latitude: null, longitude: 14.0, NowUtc));
    }

    [Fact]
    public void Create_WithNonUtcOccurredAt_Throws()
    {
        var unspecified = DateTime.SpecifyKind(OccurredUtc, DateTimeKind.Unspecified);

        Assert.Throws<ArgumentException>(() => CrimeEvent.Create(
            Guid.NewGuid(), "Title", null, unspecified, 3, null, null, NowUtc));
    }

    [Fact]
    public void Update_WithValidArguments_MutatesAndBumpsUpdatedAt()
    {
        var evt = CrimeEvent.Create(
            Guid.NewGuid(), "Original", null, OccurredUtc, 1, null, null, NowUtc);

        var later = NowUtc.AddHours(1);
        evt.Update(
            title: "Updated",
            description: "New details",
            occurredAt: OccurredUtc,
            severity: 5,
            latitude: 40.0,
            longitude: -70.0,
            nowUtc: later);

        Assert.Equal("Updated", evt.Title);
        Assert.Equal("New details", evt.Description);
        Assert.Equal(5, evt.Severity);
        Assert.Equal(40.0, evt.Latitude);
        Assert.Equal(-70.0, evt.Longitude);
        Assert.Equal(NowUtc, evt.CreatedAt);
        Assert.Equal(later, evt.UpdatedAt);
    }

    [Fact]
    public void Update_WithEmptyTitle_Throws()
    {
        var evt = CrimeEvent.Create(
            Guid.NewGuid(), "Original", null, OccurredUtc, 1, null, null, NowUtc);

        Assert.Throws<ArgumentException>(() => evt.Update(
            title: "", description: null, occurredAt: OccurredUtc, severity: 1,
            latitude: null, longitude: null, nowUtc: NowUtc));
    }
}
