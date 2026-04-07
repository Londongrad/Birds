using Birds.Domain.Enums;
using Birds.Domain.Extensions;
using Birds.Shared.Localization;
using Birds.UI.Services.Localization;
using FluentAssertions;
using System.Globalization;

namespace Birds.Tests.UI.Services
{
    public sealed class LocalizationDisplayTests
    {
        [Fact]
        public void LanguageOption_ToString_Should_Return_DisplayName()
        {
            var sut = new LanguageOption(AppLanguages.Russian, "Русский");

            sut.ToString().Should().Be("Русский");
        }

        [Fact]
        public void ThemeOption_ToString_Should_Return_DisplayName()
        {
            var sut = new ThemeOption("graphite", "Graphite");

            sut.ToString().Should().Be("Graphite");
        }

        [Fact]
        public void Chickadee_Display_Name_Should_Use_Updated_English_Translation()
        {
            var culture = CultureInfo.GetCultureInfo(AppLanguages.English);

            ((BirdsName)6).ToDisplayName(culture).Should().Be("Black-capped chickadee");
        }
    }
}
