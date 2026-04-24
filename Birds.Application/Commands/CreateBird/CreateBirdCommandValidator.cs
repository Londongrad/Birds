using Birds.Application.Common.Validation;
using FluentValidation;

namespace Birds.Application.Commands.CreateBird;

public class CreateBirdCommandValidator : AbstractValidator<CreateBirdCommand>
{
    public CreateBirdCommandValidator()
    {
        RuleFor(x => x.Name).ApplySpeciesRules();

        RuleFor(x => x.Arrival).ApplyArrivalRules();

        RuleFor(x => x.Description).ApplyDescriptionRules();

        RuleFor(x => x.Departure).ApplyDepartureDateRules();

        this.ApplyBirdStateRules(
            command => command.Arrival,
            command => command.Departure,
            command => command.IsAlive);
    }
}
