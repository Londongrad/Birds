using System.Text.Json;
using Birds.Application.DTOs;
using Birds.Domain.Enums;
using Birds.UI.Services.Export;
using FluentAssertions;

namespace Birds.Tests.UI.Services;

public sealed class JsonExportServiceTests
{
    [Fact]
    public async Task ExportAsync_Should_Write_Metadata_Stable_Species_And_Display_Name()
    {
        var sut = new JsonExportService();
        using var directory = TempDirectory.Create();
        var path = Path.Combine(directory.Path, "birds-export.json");
        var bird = CreateBirdDto();

        await sut.ExportAsync(new[] { bird }, path, CancellationToken.None);

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        document.RootElement.GetProperty("formatVersion").GetInt32().Should().Be(1);
        document.RootElement.GetProperty("version").GetInt32().Should().Be(1);
        document.RootElement.GetProperty("exportedAtUtc").GetDateTime().Kind.Should().Be(DateTimeKind.Utc);
        document.RootElement.GetProperty("appVersion").GetString().Should().NotBeNullOrWhiteSpace();
        document.RootElement.GetProperty("itemCount").GetInt32().Should().Be(1);

        var exportedBird = document.RootElement.GetProperty("items")[0];
        exportedBird.GetProperty("Species").GetString().Should().Be("Sparrow");
        exportedBird.GetProperty("Name").GetString().Should().Be("Sparrow");
    }

    [Fact]
    public async Task ExportAsync_Should_Create_Backup_When_Overwriting_Existing_File()
    {
        var sut = new JsonExportService();
        using var directory = TempDirectory.Create();
        var path = Path.Combine(directory.Path, "birds-export.json");
        var backupPath = $"{path}.bak";
        const string originalContent = """{"version":1,"items":[]}""";
        await File.WriteAllTextAsync(path, originalContent);

        await sut.ExportAsync(new[] { CreateBirdDto() }, path, CancellationToken.None);

        File.Exists(path).Should().BeTrue();
        File.Exists(backupPath).Should().BeTrue();
        (await File.ReadAllTextAsync(backupPath)).Should().Be(originalContent);
        (await File.ReadAllTextAsync(path)).Should().NotBe(originalContent);
    }

    [Fact]
    public async Task ExportAsync_Should_Not_Corrupt_Existing_File_When_Export_Source_Fails()
    {
        var sut = new JsonExportService();
        using var directory = TempDirectory.Create();
        var path = Path.Combine(directory.Path, "birds-export.json");
        const string originalContent = """{"version":1,"items":[]}""";
        await File.WriteAllTextAsync(path, originalContent);

        var act = () => sut.ExportAsync(ThrowingBirds(), path, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await File.ReadAllTextAsync(path)).Should().Be(originalContent);
        Directory.GetFiles(directory.Path, "*.tmp").Should().BeEmpty();
    }

    private static BirdDTO CreateBirdDto()
    {
        return new BirdDTO(
            Guid.NewGuid(),
            "Sparrow",
            null,
            new DateOnly(2026, 4, 1),
            null,
            true,
            null,
            null)
        {
            Species = BirdSpecies.Sparrow
        };
    }

    private static IEnumerable<BirdDTO> ThrowingBirds()
    {
        yield return CreateBirdDto();
        throw new InvalidOperationException("export source failed");
    }

    private sealed class TempDirectory : IDisposable
    {
        private TempDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempDirectory Create()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"birds-export-{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return new TempDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
    }
}
