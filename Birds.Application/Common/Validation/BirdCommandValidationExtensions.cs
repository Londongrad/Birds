using Birds.Domain.Common;
using Birds.Domain.Enums;
using FluentValidation;

namespace Birds.Application.Common.Validation;

internal static class BirdCommandValidationExtensions
{
    public static IRuleBuilderOptions<T, BirdsName> ApplySpeciesRules<T>(
        this IRuleBuilderInitial<T, BirdsName> ruleBuilder)
    {
        return ruleBuilder
            .IsInEnum()
            .WithMessage("Bird species is invalid");
    }

    public static IRuleBuilderOptions<T, DateOnly> ApplyArrivalRules<T>(
        this IRuleBuilderInitial<T, DateOnly> ruleBuilder)
    {
        return ruleBuilder
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Arrival date cannot be in the future")
            .GreaterThanOrEqualTo(BirdValidationRules.MinimumArrivalDate)
            .WithMessage($"Arrival date cannot be earlier than {BirdValidationRules.MinimumArrivalDate.Year}.");
    }

    public static IRuleBuilderOptions<T, string?> ApplyDescriptionRules<T>(
        this IRuleBuilderInitial<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .MaximumLength(BirdValidationRules.DescriptionMaxLength)
            .WithMessage($"Description must not exceed {BirdValidationRules.DescriptionMaxLength} characters");
    }
}
