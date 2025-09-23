using Birds.Domain.Enums;
using FluentValidation;

namespace Birds.Application.Commands.CreateBird
{
    public class CreateBirdCommandValidator : AbstractValidator<CreateBirdCommand>
    {
        public CreateBirdCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .Must(name => Enum.TryParse<BirdsName>(name, true, out _))
                .WithMessage("Invalid bird name");

            RuleFor(x => x.Arrival)
                .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Arrival date cannot be in the future")

                .GreaterThanOrEqualTo(new DateOnly(2020, 01, 01))
                .WithMessage("Arrival date cannot be earlier than 2020.");

            RuleFor(x => x.Description)
                .MaximumLength(100).WithMessage("Description must not exceed 100 characters");
        }
    }
}