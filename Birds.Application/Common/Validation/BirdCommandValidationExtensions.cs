using Birds.Domain.Common;
using Birds.Domain.Enums;
using FluentValidation;

namespace Birds.Application.Common.Validation;

internal static class BirdCommandValidationExtensions
{
    public static IRuleBuilderOptions<T, BirdSpecies> ApplySpeciesRules<T>(
        this IRuleBuilderInitial<T, BirdSpecies> ruleBuilder)
    {
        return ruleBuilder
            .Must(BirdValidationRules.IsDefinedSpecies)
            .WithMessage("Bird species is invalid");
    }

    public static IRuleBuilderOptions<T, DateOnly> ApplyArrivalRules<T>(
        this IRuleBuilderInitial<T, DateOnly> ruleBuilder)
    {
        return ruleBuilder
            .Must(arrival => BirdValidationRules.IsDateInAllowedRange(arrival, true))
            .WithMessage($"Arrival date cannot be earlier than {BirdValidationRules.MinimumArrivalDate.Year}.")
            .Must(arrival => BirdValidationRules.IsDateInAllowedRange(arrival))
            .WithMessage("Arrival date cannot be in the future");
    }

    public static IRuleBuilderOptions<T, DateOnly?> ApplyDepartureDateRules<T>(
        this IRuleBuilderInitial<T, DateOnly?> ruleBuilder)
    {
        return ruleBuilder
            .Must(departure => BirdValidationRules.IsOptionalDateInAllowedRange(departure, true))
            .WithMessage($"Departure date cannot be earlier than {BirdValidationRules.MinimumArrivalDate.Year}.")
            .Must(departure => BirdValidationRules.IsOptionalDateInAllowedRange(departure))
            .WithMessage("Departure date cannot be in the future");
    }

    public static void ApplyBirdStateRules<T>(
        this AbstractValidator<T> validator,
        Func<T, DateOnly> arrivalSelector,
        Func<T, DateOnly?> departureSelector,
        Func<T, bool> isAliveSelector)
    {
        validator.RuleFor(x => x)
            .Must(x => BirdValidationRules.IsDateRangeValid(arrivalSelector(x), departureSelector(x)))
            .WithMessage("Departure date cannot be earlier than arrival date");

        validator.RuleFor(x => x)
            .Must(x => BirdValidationRules.HasRequiredDeparture(departureSelector(x), isAliveSelector(x)))
            .WithMessage("Departure date is required when the bird is marked as dead");
    }

    public static IRuleBuilderOptions<T, long> ApplyVersionRules<T>(
        this IRuleBuilderInitial<T, long> ruleBuilder)
    {
        return ruleBuilder
            .Must(BirdValidationRules.IsVersionValid)
            .WithMessage("Version must be greater than zero");
    }

    public static IRuleBuilderOptions<T, string?> ApplyDescriptionRules<T>(
        this IRuleBuilderInitial<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(BirdValidationRules.IsDescriptionLengthValid)
            .WithMessage($"Description must not exceed {BirdValidationRules.DescriptionMaxLength} characters");
    }
}
