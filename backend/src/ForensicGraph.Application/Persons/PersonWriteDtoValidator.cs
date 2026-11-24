using FluentValidation;
using ForensicGraph.Domain.Persons;

namespace ForensicGraph.Application.Persons;

/// <summary>
/// Validates that a <see cref="PersonWriteDto"/> matches the invariants enforced by
/// <see cref="Person"/>. Runs before the request reaches <see cref="PersonService"/>.
/// </summary>
public sealed class PersonWriteDtoValidator : AbstractValidator<PersonWriteDto>
{
    public PersonWriteDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name must not be empty.")
            .MaximumLength(Person.NameMaxLength)
            .WithMessage($"First name must be at most {Person.NameMaxLength} characters.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name must not be empty.")
            .MaximumLength(Person.NameMaxLength)
            .WithMessage($"Last name must be at most {Person.NameMaxLength} characters.");

        RuleFor(x => x.PhysicalDescription)
            .MaximumLength(Person.PhysicalDescriptionMaxLength)
            .WithMessage(
                $"Physical description must be at most {Person.PhysicalDescriptionMaxLength} characters.");

        RuleForEach(x => x.Citizenships)
            .NotEmpty()
            .WithMessage("Citizenship values must not be empty.")
            .MaximumLength(Person.CitizenshipMaxLength)
            .WithMessage($"Each citizenship must be at most {Person.CitizenshipMaxLength} characters.");

        RuleForEach(x => x.PassportNumbers)
            .NotEmpty()
            .WithMessage("Passport number values must not be empty.")
            .MaximumLength(Person.PassportMaxLength)
            .WithMessage($"Each passport must be at most {Person.PassportMaxLength} characters.");

        RuleFor(x => x.Phones)
            .Must(phones => phones.Count <= Person.MaxPhones)
            .WithMessage($"A person can have at most {Person.MaxPhones} phone numbers.");

        RuleForEach(x => x.Phones)
            .NotEmpty()
            .WithMessage("Phone number values must not be empty.")
            .MaximumLength(Person.PhoneMaxLength)
            .WithMessage($"Each phone number must be at most {Person.PhoneMaxLength} characters.");
    }
}
