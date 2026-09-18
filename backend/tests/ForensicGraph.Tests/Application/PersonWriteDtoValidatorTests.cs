using FluentValidation.TestHelper;
using ForensicGraph.Application.Persons;
using ForensicGraph.Domain.Persons;

namespace ForensicGraph.Tests.Application;

public class PersonWriteDtoValidatorTests
{
    private static PersonWriteDto Valid() => new()
    {
        FirstName = "Anna",
        LastName = "Nováková",
        Citizenships = new[] { "CZ" },
        PassportNumbers = new[] { "P123" },
        Phone = "+420111222333",
        PhysicalDescription = "Tall.",
    };

    [Fact]
    public void Valid_dto_passes()
    {
        var result = new PersonWriteDtoValidator().TestValidate(Valid());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_first_name_fails()
    {
        var result = new PersonWriteDtoValidator().TestValidate(Valid() with { FirstName = "" });
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Empty_last_name_fails()
    {
        var result = new PersonWriteDtoValidator().TestValidate(Valid() with { LastName = "" });
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void First_name_over_limit_fails()
    {
        var result = new PersonWriteDtoValidator().TestValidate(
            Valid() with { FirstName = new string('a', Person.NameMaxLength + 1) });
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Phone_over_limit_fails()
    {
        var result = new PersonWriteDtoValidator().TestValidate(
            Valid() with { Phone = new string('9', Person.PhoneMaxLength + 1) });
        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void Physical_description_over_limit_fails()
    {
        var result = new PersonWriteDtoValidator().TestValidate(
            Valid() with { PhysicalDescription = new string('a', Person.PhysicalDescriptionMaxLength + 1) });
        result.ShouldHaveValidationErrorFor(x => x.PhysicalDescription);
    }

    [Fact]
    public void Citizenship_over_limit_fails()
    {
        var result = new PersonWriteDtoValidator().TestValidate(
            Valid() with { Citizenships = new[] { new string('a', Person.CitizenshipMaxLength + 1) } });
        result.ShouldHaveValidationErrorFor("Citizenships[0]");
    }

    [Fact]
    public void Passport_over_limit_fails()
    {
        var result = new PersonWriteDtoValidator().TestValidate(
            Valid() with { PassportNumbers = new[] { new string('a', Person.PassportMaxLength + 1) } });
        result.ShouldHaveValidationErrorFor("PassportNumbers[0]");
    }

    [Fact]
    public void Blank_citizenship_fails()
    {
        var result = new PersonWriteDtoValidator().TestValidate(
            Valid() with { Citizenships = new[] { "  " } });
        result.ShouldHaveValidationErrorFor("Citizenships[0]");
    }

    [Fact]
    public void Blank_passport_fails()
    {
        var result = new PersonWriteDtoValidator().TestValidate(
            Valid() with { PassportNumbers = new[] { "" } });
        result.ShouldHaveValidationErrorFor("PassportNumbers[0]");
    }
}
