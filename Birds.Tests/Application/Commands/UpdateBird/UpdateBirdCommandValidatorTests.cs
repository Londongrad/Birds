using Birds.Application.Commands.UpdateBird;
using Birds.Domain.Enums;
using Birds.Shared.Constants;
using FluentValidation.TestHelper;

namespace Birds.Tests.Application.Commands.UpdateBird;

public class UpdateBirdCommandValidatorTests
{
    private readonly UpdateBirdCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Description_Too_Long()
    {
        var command = new UpdateBirdCommand(
            Guid.NewGuid(),
            (BirdsName)1,
            new string('A', BirdValidationRules.DescriptionMaxLength + 1),
            DateOnly.FromDateTime(DateTime.Now),
            null,
            true
        );

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage($"Description must not exceed {BirdValidationRules.DescriptionMaxLength} characters");
    }

    [Fact]
    public void Should_Have_Error_When_Departure_Earlier_Than_Arrival()
    {
        var command = new UpdateBirdCommand(
            Guid.NewGuid(),
            (BirdsName)1,
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
        var command = new UpdateBirdCommand(
            Guid.NewGuid(),
            (BirdsName)1,
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
        var command = new UpdateBirdCommand(
            Guid.NewGuid(),
            (BirdsName)1,
            "Healthy sparrow",
            new DateOnly(2024, 5, 1),
            new DateOnly(2024, 5, 3),
            false
        );

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}