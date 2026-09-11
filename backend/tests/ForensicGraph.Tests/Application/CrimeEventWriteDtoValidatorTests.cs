using FluentValidation.TestHelper;
using ForensicGraph.Application.CrimeEvents;
using ForensicGraph.Domain.CrimeEvents;

namespace ForensicGraph.Tests.Application;

public class CrimeEventWriteDtoValidatorTests
{
    private static readonly DateTime Now = new(2026, 6, 27, 10, 30, 0, DateTimeKind.Utc);

    private static CrimeEventWriteDtoValidator BuildValidator()
        => new(new FixedTimeProvider(Now));

    private static CrimeEventWriteDto ValidDto() => new()
    {
        Title = "Break-in",
        Description = "Rear door forced open.",
        OccurredAt = Now.AddDays(-1),
        Severity = 3,
        Latitude = 50.0619,
        Longitude = 19.9368,
    };

    [Fact]
    public void Valid_dto_passes_all_rules()
    {
        var result = BuildValidator().TestValidate(ValidDto());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_title_fails()
    {
        var dto = ValidDto() with { Title = "" };
        BuildValidator().TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_over_max_length_fails()
    {
        var dto = ValidDto() with { Title = new string('a', CrimeEvent.TitleMaxLength + 1) };
        BuildValidator().TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Description_over_max_length_fails()
    {
        var dto = ValidDto() with { Description = new string('a', CrimeEvent.DescriptionMaxLength + 1) };
        BuildValidator().TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Description_null_is_valid()
    {
        var dto = ValidDto() with { Description = null };
        BuildValidator().TestValidate(dto)
            .ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void OccurredAt_default_fails()
    {
        var dto = ValidDto() with { OccurredAt = default };
        BuildValidator().TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.OccurredAt);
    }

    [Fact]
    public void OccurredAt_non_utc_fails()
    {
        var dto = ValidDto() with { OccurredAt = DateTime.SpecifyKind(Now.AddDays(-1), DateTimeKind.Unspecified) };
        BuildValidator().TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.OccurredAt);
    }

    [Fact]
    public void OccurredAt_more_than_24h_in_future_fails()
    {
        var dto = ValidDto() with { OccurredAt = Now.AddHours(25) };
        BuildValidator().TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.OccurredAt);
    }

    [Fact]
    public void OccurredAt_within_24h_future_passes()
    {
        var dto = ValidDto() with { OccurredAt = Now.AddHours(23) };
        BuildValidator().TestValidate(dto)
            .ShouldNotHaveValidationErrorFor(x => x.OccurredAt);
    }

    [Fact]
    public void Severity_below_range_fails()
    {
        var dto = ValidDto() with { Severity = 0 };
        BuildValidator().TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.Severity);
    }

    [Fact]
    public void Severity_above_range_fails()
    {
        var dto = ValidDto() with { Severity = 6 };
        BuildValidator().TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.Severity);
    }

    [Fact]
    public void Latitude_out_of_range_fails()
    {
        var dto = ValidDto() with { Latitude = 91 };
        BuildValidator().TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.Latitude);
    }

    [Fact]
    public void Longitude_out_of_range_fails()
    {
        var dto = ValidDto() with { Longitude = 181 };
        BuildValidator().TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.Longitude);
    }

    [Fact]
    public void Latitude_without_longitude_fails()
    {
        var dto = ValidDto() with { Longitude = null };
        BuildValidator().TestValidate(dto)
            .ShouldHaveValidationErrorFor("Coordinates");
    }

    [Fact]
    public void Longitude_without_latitude_fails()
    {
        var dto = ValidDto() with { Latitude = null };
        BuildValidator().TestValidate(dto)
            .ShouldHaveValidationErrorFor("Coordinates");
    }

    [Fact]
    public void Both_coordinates_null_is_valid()
    {
        var dto = ValidDto() with { Latitude = null, Longitude = null };
        BuildValidator().TestValidate(dto)
            .ShouldNotHaveValidationErrorFor("Coordinates");
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTime utcNow)
        {
            _utcNow = new DateTimeOffset(utcNow, TimeSpan.Zero);
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
