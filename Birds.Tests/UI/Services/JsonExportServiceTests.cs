using System.Text.Json;
using Birds.Application.DTOs;
using Birds.Domain.Enums;
using Birds.UI.Services.Export;
using FluentAssertions;

namespace Birds.Tests.UI.Services;

public sealed class JsonExportServiceTests
{
    [Fact]
    public async Task ExportAsync_Should_Write_Stable_Species_And_Display_Name()
    {
        var sut = new JsonExportService();
        var path = Path.Combine(Path.GetTempPath(), $"birds-export-{Guid.NewGuid():N}.json");

        try
        {
            var bird = new BirdDTO(
                Guid.NewGuid(),
                "Sparrow",
                null,
                new DateOnly(2026, 4, 1),
                null,
                true,
                null,
                null)
            {
                Species = BirdsName.Воробей
            };

            await sut.ExportAsync(new[] { bird }, path, CancellationToken.None);

            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
            var exportedBird = document.RootElement.GetProperty("items")[0];

            exportedBird.GetProperty("Species").GetInt32().Should().Be((int)BirdsName.Воробей);
            exportedBird.GetProperty("Name").GetString().Should().Be("Sparrow");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
