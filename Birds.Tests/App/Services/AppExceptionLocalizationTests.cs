using Birds.App;
using Birds.Shared.Constants;
using Birds.Shared.Localization;
using FluentAssertions;
using System.Globalization;
using System.Reflection;

namespace Birds.Tests.App.Services
{
    public sealed class AppExceptionLocalizationTests
    {
        [Fact]
        public void BuildUserMessage_Should_Use_English_Timeout_Text()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;

            try
            {
                var culture = CultureInfo.GetCultureInfo(AppLanguages.English);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                var message = BuildUserMessage(new TimeoutException(), "UI Dispatcher");

                message.Should().Be(ExceptionMessages.Timeout("UI Dispatcher"));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [Fact]
        public void BuildUserMessage_Should_Use_Russian_Database_Message_For_Npgsql_Exception()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;

            try
            {
                var culture = CultureInfo.GetCultureInfo(AppLanguages.Russian);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                var source = ExceptionMessages.AppDomain;
                var message = BuildUserMessage(new Npgsql.FakeNpgsqlException(), source);

                message.Should().Be(ExceptionMessages.DatabaseConnection(source));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        private static string BuildUserMessage(Exception exception, string source)
        {
            var method = typeof(global::Birds.App.App)
                .GetMethod("BuildUserMessage", BindingFlags.NonPublic | BindingFlags.Static);

            method.Should().NotBeNull();

            return (string)method!.Invoke(null, [exception, source])!;
        }
    }
}

namespace Npgsql
{
    internal sealed class FakeNpgsqlException : Exception;
}
