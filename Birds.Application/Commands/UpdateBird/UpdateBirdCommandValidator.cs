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

        RuleFor(x => x.Version).ApplyVersionRules();

        RuleFor(x => x.Departure).ApplyDepartureDateRules();

        this.ApplyBirdStateRules(
            command => command.Arrival,
            command => command.Departure,
            command => command.IsAlive);
    }
}
