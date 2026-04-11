using System.Text.Json;
using Birds.UI.Services.Import;
using FluentAssertions;

namespace Birds.Tests.UI.Services;

public class JsonImportServiceTests
{
    [Fact]
    public async Task ImportAsync_Should_Read_Versioned_Envelope()
    {
        var sut = new JsonImportService();
        var path = Path.Combine(Path.GetTempPath(), $"birds-import-{Guid.NewGuid():N}.json");

        try
        {
            var payload = new
            {
                version = 1,
                exportedAt = DateTime.UtcNow,
                items = new[]
                {
                    new
                    {
                        id = Guid.NewGuid(),
                        name = "Sparrow",
                        description = "note",
                        arrival = new DateOnly(2026, 4, 1),
                        departure = (DateOnly?)null,
                        isAlive = true,
                        createdAt = (DateTime?)null,
                        updatedAt = (DateTime?)null
                    }
                }
            };

            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload));

            var result = await sut.ImportAsync(path, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().ContainSingle();
            result.Value!.Single().Name.Should().Be("Sparrow");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task ImportAsync_Should_Fail_For_Unsupported_Version()
    {
        var sut = new JsonImportService();
        var path = Path.Combine(Path.GetTempPath(), $"birds-import-{Guid.NewGuid():N}.json");

        try
        {
            await File.WriteAllTextAsync(path, """{"version":2,"items":[]}""");

            var result = await sut.ImportAsync(path, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNullOrWhiteSpace();
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}