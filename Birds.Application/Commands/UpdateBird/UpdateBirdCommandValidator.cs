using Birds.Application.Common.Validation;
using FluentValidation;

namespace Birds.Application.Commands.UpdateBird;

public class UpdateBirdCommandValidator : AbstractValidator<UpdateBirdCommand>
{
    public UpdateBirdCommandValidator()
    {
        RuleFor(x => x.Name).ApplySpeciesRules();

        RuleFor(x => x.Arrival).ApplyArrivalRules();

        RuleFor(x => x.Description).ApplyDescriptionRules();

        RuleFor(x => x.Version)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Version must be greater than zero");

        RuleFor(x => x.Departure)
            .Must(departure => departure is null || departure <= DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Departure date cannot be in the future");

        RuleFor(x => x)
            .Must(x => x.Departure is null || x.Departure >= x.Arrival)
            .WithMessage("Departure date cannot be earlier than arrival date");

        RuleFor(x => x)
            .Must(x => x.IsAlive || x.Departure is not null)
            .WithMessage("Departure date is required when the bird is marked as dead");
    }
}
