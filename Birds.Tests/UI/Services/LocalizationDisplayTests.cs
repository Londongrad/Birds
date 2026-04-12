using System.Globalization;
using Birds.Domain.Enums;
using Birds.Domain.Extensions;
using Birds.Shared.Localization;
using Birds.UI.Converters;
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
                (BirdsName)3,
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
                (BirdsName)3,
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
}
