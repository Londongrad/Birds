using System.Globalization;
using Birds.Domain.Enums;
using Birds.Shared.Localization;
using Birds.UI.Converters;
using Birds.UI.Services.BirdNames;
using Birds.UI.Services.Localization;
using FluentAssertions;

namespace Birds.Tests.UI.Services;

[Collection("LocalizationService serial")]
public sealed class LocalizationDisplayTests
{
    [Fact]
    public void BirdNameDisplayConverter_Should_Use_Current_App_Language_Instead_Of_Binding_Culture()
    {
        var localization = LocalizationService.Instance;
        var previousLanguage = localization.CurrentLanguage;
        var previousDateFormat = localization.CurrentDateFormat;

        try
        {
            localization.ApplyLanguage(AppLanguages.Russian);

            var sut = new BirdNameDisplayConverter();
            var result = sut.Convert(
                (BirdSpecies)3,
                typeof(string),
                string.Empty,
                CultureInfo.GetCultureInfo(AppLanguages.English));

            result.Should().Be("Амадин");
        }
        finally
        {
            localization.ApplyLanguage(previousLanguage);
            localization.ApplyDateFormat(previousDateFormat);
        }
    }

    [Fact]
    public void BirdNameDisplayConverter_Should_Follow_Current_App_Language_When_English_Is_Selected()
    {
        var localization = LocalizationService.Instance;
        var previousLanguage = localization.CurrentLanguage;
        var previousDateFormat = localization.CurrentDateFormat;

        try
        {
            localization.ApplyLanguage(AppLanguages.English);

            var sut = new BirdNameDisplayConverter();
            var result = sut.Convert(
                (BirdSpecies)3,
                typeof(string),
                string.Empty,
                CultureInfo.GetCultureInfo(AppLanguages.Russian));

            result.Should().Be("Amadin");
        }
        finally
        {
            localization.ApplyLanguage(previousLanguage);
            localization.ApplyDateFormat(previousDateFormat);
        }
    }

    [Fact]
    public void BirdNameDisplayConverter_MultiBinding_Should_Recompute_When_App_Language_Binding_Changes()
    {
        var localization = LocalizationService.Instance;
        var previousLanguage = localization.CurrentLanguage;
        var previousDateFormat = localization.CurrentDateFormat;

        try
        {
            localization.ApplyLanguage(AppLanguages.English);

            var sut = new BirdNameDisplayConverter();
            var result = sut.Convert(
                [(BirdSpecies)3, localization.CurrentLanguage],
                typeof(string),
                string.Empty,
                CultureInfo.GetCultureInfo(AppLanguages.Russian));

            result.Should().Be("Amadin");
        }
        finally
        {
            localization.ApplyLanguage(previousLanguage);
            localization.ApplyDateFormat(previousDateFormat);
        }
    }

    [Fact]
    public void BirdNameDisplayService_Should_Return_Localized_Name_For_Current_App_Language()
    {
        var localization = LocalizationService.Instance;
        var previousLanguage = localization.CurrentLanguage;
        var previousDateFormat = localization.CurrentDateFormat;

        try
        {
            localization.ApplyLanguage(AppLanguages.English);

            var sut = new BirdNameDisplayService(localization);

            sut.GetDisplayName((BirdSpecies)3).Should().Be("Amadin");
        }
        finally
        {
            localization.ApplyLanguage(previousLanguage);
            localization.ApplyDateFormat(previousDateFormat);
        }
    }
}
