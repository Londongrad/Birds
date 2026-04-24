using Birds.App.Services;
using FluentAssertions;

namespace Birds.Tests.App.Services;

public sealed class DiagnosticRedactorTests
{
    [Fact]
    public void RedactConnectionString_Should_Remove_Secret_Values()
    {
        const string connectionString =
            "Host=db;Port=5432;Database=birds;Username=user;Password=super-secret;Token=token-secret";

        var redacted = DiagnosticRedactor.RedactConnectionString(connectionString);

        redacted.Should().NotContain("super-secret");
        redacted.Should().NotContain("token-secret");
        redacted.Should().Contain(DiagnosticRedactor.RedactedValue);
        redacted.Should().Contain("host=db", Exactly.Once());
    }

    [Fact]
    public void TryGetSqliteDataSource_Should_Return_DataSource_Without_Other_Connection_Details()
    {
        var path = Path.Combine(Path.GetTempPath(), "birds.db");
        var connectionString = $"Data Source={path};Cache=Shared";

        var dataSource = DiagnosticRedactor.TryGetSqliteDataSource(connectionString);

        dataSource.Should().Be(path);
    }
}
