using FluentValidation;

namespace Birds.Application.Commands.UpdateBirdDeparture
{
    public class UpdateBirdDepartureCommandValidator : AbstractValidator<UpdateBirdDepartureCommand>
    {
        public UpdateBirdDepartureCommandValidator()
        {
            When(x => x.Departure.HasValue, () =>
            {
                RuleFor(x => x.Departure!.Value)
                    .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1))
                    .WithMessage("Departure date cannot be in the future.")
                    .GreaterThanOrEqualTo(new DateOnly(2020, 01, 01))
                    .WithMessage("Departure date cannot be earlier than 2020.");
            });
        }
    }
}