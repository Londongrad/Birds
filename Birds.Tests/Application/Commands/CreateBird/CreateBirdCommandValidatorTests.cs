using Birds.Application.Commands.CreateBird;
using Birds.Domain.Common;
using Birds.Domain.Enums;
using FluentValidation.TestHelper;

namespace Birds.Tests.Application.Commands.CreateBird;

public class CreateBirdCommandValidatorTests
{
    private readonly CreateBirdCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Arrival_In_Future()
    {
        var command = new CreateBirdCommand(
            (BirdSpecies)1,
            "Test bird",
            DateOnly.FromDateTime(DateTime.Now.AddDays(1))
        );

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Arrival)
            .WithErrorMessage("Arrival date cannot be in the future");
    }

    [Fact]
    public void Should_Have_Error_When_Arrival_Too_Early()
    {
        var command = new CreateBirdCommand(
            (BirdSpecies)1,
            "Old bird",
            new DateOnly(2019, 12, 31)
        );

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Arrival)
            .WithErrorMessage("Arrival date cannot be earlier than 2020.");
    }

    [Fact]
    public void Should_Have_Error_When_Description_Too_Long()
    {
        var command = new CreateBirdCommand(
            (BirdSpecies)1,
            new string('A', BirdValidationRules.DescriptionMaxLength + 1),
            DateOnly.FromDateTime(DateTime.Now)
        );

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage($"Description must not exceed {BirdValidationRules.DescriptionMaxLength} characters");
    }

    [Fact]
    public void Should_Have_Error_When_Departure_Earlier_Than_Arrival()
    {
        var command = new CreateBirdCommand(
            (BirdSpecies)1,
            "Test bird",
            new DateOnly(2024, 5, 10),
            new DateOnly(2024, 5, 9),
            false
        );

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Departure date cannot be earlier than arrival date");
    }

    [Fact]
    public void Should_Have_Error_When_Dead_Bird_Has_No_Departure()
    {
        var command = new CreateBirdCommand(
            (BirdSpecies)1,
            "Test bird",
            DateOnly.FromDateTime(DateTime.Now),
            null,
            false
        );

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Departure date is required when the bird is marked as dead");
    }

    [Fact]
    public void Should_Not_Have_Error_For_Valid_Command()
    {
        var command = new CreateBirdCommand(
            (BirdSpecies)1,
            "Healthy sparrow",
            DateOnly.FromDateTime(DateTime.Now)
        );

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
