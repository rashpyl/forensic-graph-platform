using FluentValidation;
using ForensicGraph.Domain.CrimeEvents;

namespace ForensicGraph.Application.CrimeEvents;

/// <summary>
/// Enforces the write-side invariants from the <c>crime-event-api</c> spec (Requirement §4).
/// Runs before the request reaches <see cref="CrimeEventService"/>, so the service can trust
/// its input; the domain entity still keeps its own guards as a last line of defence.
/// </summary>
public sealed class CrimeEventWriteDtoValidator : AbstractValidator<CrimeEventWriteDto>
{
    private readonly TimeProvider _timeProvider;

    public CrimeEventWriteDtoValidator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title must not be empty.")
            .MaximumLength(CrimeEvent.TitleMaxLength)
            .WithMessage($"Title must be at most {CrimeEvent.TitleMaxLength} characters.");

        RuleFor(x => x.Description)
            .MaximumLength(CrimeEvent.DescriptionMaxLength)
            .WithMessage($"Description must be at most {CrimeEvent.DescriptionMaxLength} characters.");

        RuleFor(x => x.OccurredAt)
            .NotEqual(default(DateTime))
            .WithMessage("OccurredAt must be provided.")
            .Must(BeUtc)
            .WithMessage("OccurredAt must be a UTC DateTime.")
            .Must(NotBeMoreThan24HoursInTheFuture)
            .WithMessage("OccurredAt must not be more than 24 hours in the future.");

        RuleFor(x => x.Severity)
            .InclusiveBetween(CrimeEvent.MinSeverity, CrimeEvent.MaxSeverity)
            .WithMessage($"Severity must be between {CrimeEvent.MinSeverity} and {CrimeEvent.MaxSeverity}.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .When(x => x.Latitude.HasValue)
            .WithMessage("Latitude must be in [-90, 90].");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .When(x => x.Longitude.HasValue)
            .WithMessage("Longitude must be in [-180, 180].");

        RuleFor(x => x)
            .Must(x => x.Latitude.HasValue == x.Longitude.HasValue)
            .WithName("Coordinates")
            .WithMessage("Latitude and longitude must be provided together or both omitted.");
    }

    private static bool BeUtc(DateTime value) => value.Kind == DateTimeKind.Utc;

    private bool NotBeMoreThan24HoursInTheFuture(DateTime value)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        return value <= nowUtc.AddHours(24);
    }
}
