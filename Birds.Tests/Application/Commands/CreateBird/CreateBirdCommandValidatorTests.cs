using Birds.Application.Commands.CreateBird;
using Birds.Domain.Enums;
using FluentValidation.TestHelper;

namespace Birds.Tests.Application.Commands.CreateBird
{
    public class CreateBirdCommandValidatorTests
    {
        private readonly CreateBirdCommandValidator _validator = new();

        [Fact]
        public void Should_Have_Error_When_Arrival_In_Future()
        {
            var command = new CreateBirdCommand(
                BirdsName.Воробей,
                "Test bird",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
            );

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Arrival)
                  .WithErrorMessage("Arrival date cannot be in the future");
        }

        [Fact]
        public void Should_Have_Error_When_Arrival_Too_Early()
        {
            var command = new CreateBirdCommand(
                BirdsName.Воробей,
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
                BirdsName.Воробей,
                new string('A', 101),
                DateOnly.FromDateTime(DateTime.UtcNow)
            );

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Description)
                  .WithErrorMessage("Description must not exceed 100 characters");
        }

        [Fact]
        public void Should_Not_Have_Error_For_Valid_Command()
        {
            var command = new CreateBirdCommand(
                BirdsName.Воробей,
                "Healthy sparrow",
                DateOnly.FromDateTime(DateTime.UtcNow)
            );

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}