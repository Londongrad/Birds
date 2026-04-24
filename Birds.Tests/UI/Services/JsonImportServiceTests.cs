using System.Text.Json;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Domain.Common;
using Birds.Domain.Enums;
using Birds.UI.Services.Import;
using FluentAssertions;

namespace Birds.Tests.UI.Services;

public class JsonImportServiceTests
{
    private const string LegacyGoldfinchCode = "\u0429\u0435\u0433\u043E\u043B";
    private const string LegacySparrowCode = "\u0412\u043E\u0440\u043E\u0431\u0435\u0439";

    [Fact]
    public async Task ImportAsync_Should_Read_Versioned_Envelope()
    {
        var result = await ImportPayloadAsync(new
        {
            version = 1,
            exportedAt = DateTime.UtcNow,
            items = new[] { CreateBirdPayload(name: "Sparrow") }
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value!.Single().Name.Should().Be("Sparrow");
        result.Value!.Single().Species.Should().Be(BirdSpecies.Sparrow);
    }

    [Fact]
    public async Task ImportAsync_Should_Treat_Missing_Version_As_Legacy_Format()
    {
        var result = await ImportPayloadAsync(new
        {
            exportedAt = DateTime.UtcNow,
            items = new[] { CreateBirdPayload(name: "Sparrow") }
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task ImportAsync_Should_Read_Legacy_Top_Level_Array()
    {
        var result = await ImportPayloadAsync(new[] { CreateBirdPayload(name: "Sparrow") });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value!.Single().Species.Should().Be(BirdSpecies.Sparrow);
    }

    [Fact]
    public async Task ImportAsync_Should_Read_Stable_Species_When_Present()
    {
        var result = await ImportPayloadAsync(new
        {
            version = 1,
            exportedAt = DateTime.UtcNow,
            items = new[]
            {
                CreateBirdPayload(name: "Unknown display text", species: "Goldfinch")
            }
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value!.Single().Species.Should().Be(BirdSpecies.Goldfinch);
        result.Value!.Single().Name.Should().Be("Unknown display text");
    }

    [Fact]
    public async Task ImportAsync_Should_Read_Legacy_Russian_Species_Code_When_Present()
    {
        BirdSpeciesCodes.Parse(LegacyGoldfinchCode).Should().Be(BirdSpecies.Goldfinch);

        var result = await ImportPayloadAsync(new
        {
            version = 1,
            exportedAt = DateTime.UtcNow,
            items = new[]
            {
                CreateBirdPayload(name: "Unknown display text", species: LegacyGoldfinchCode)
            }
        });

        result.IsSuccess.Should().BeTrue($"{result.ErrorCode}: {result.Error}");
        result.Value.Should().ContainSingle();
        result.Value!.Single().Species.Should().Be(BirdSpecies.Goldfinch);
    }

    [Fact]
    public async Task ImportAsync_Should_Fall_Back_To_Legacy_Localized_Name_When_Species_Is_Missing()
    {
        BirdSpeciesCodes.Parse(LegacySparrowCode).Should().Be(BirdSpecies.Sparrow);

        var result = await ImportPayloadAsync(new
        {
            version = 1,
            exportedAt = DateTime.UtcNow,
            items = new[] { CreateBirdPayload(name: LegacySparrowCode) }
        });

        result.IsSuccess.Should().BeTrue($"{result.ErrorCode}: {result.Error}");
        result.Value.Should().ContainSingle();
        result.Value!.Single().Species.Should().Be(BirdSpecies.Sparrow);
        result.Value!.Single().Name.Should().Be(LegacySparrowCode);
    }

    [Fact]
    public async Task ImportAsync_Should_Fail_For_Unsupported_Version()
    {
        var result = await ImportRawJsonAsync("""{"formatVersion":2,"items":[]}""");

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AppErrorCodes.ImportUnsupportedVersion);
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ImportAsync_Should_Fail_For_Malformed_Json()
    {
        var result = await ImportRawJsonAsync("""{"version":1,"items":[""");

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AppErrorCodes.ImportInvalidFile);
    }

    [Fact]
    public async Task ImportAsync_Should_Fail_For_Duplicate_Ids()
    {
        var id = Guid.NewGuid();
        var result = await ImportPayloadAsync(new
        {
            version = 1,
            items = new[]
            {
                CreateBirdPayload(id, "Sparrow"),
                CreateBirdPayload(id, "Sparrow")
            }
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AppErrorCodes.ImportDuplicateIds);
    }

    [Fact]
    public async Task ImportAsync_Should_Fail_Clearly_For_Invalid_Stable_Species()
    {
        var result = await ImportPayloadAsync(new
        {
            version = 1,
            items = new[]
            {
                CreateBirdPayload(name: "Sparrow", species: "UnknownSpecies")
            }
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AppErrorCodes.ImportInvalidSpecies);
    }

    [Fact]
    public async Task ImportAsync_Should_Fail_For_Invalid_Bird_Data_Before_Application_Import()
    {
        var result = await ImportPayloadAsync(new
        {
            version = 1,
            items = new[]
            {
                CreateBirdPayload(
                    name: "Sparrow",
                    arrival: new DateOnly(2026, 4, 2),
                    departure: new DateOnly(2026, 4, 1))
            }
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AppErrorCodes.ImportValidationFailed);
        result.AppError!.ValidationErrors.Should().ContainKey("items[0]");
    }

    [Fact]
    public async Task ImportAsync_Should_Fail_When_Metadata_ItemCount_Does_Not_Match_Items()
    {
        var result = await ImportPayloadAsync(new
        {
            formatVersion = 1,
            itemCount = 2,
            items = new[] { CreateBirdPayload(name: "Sparrow") }
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AppErrorCodes.ImportValidationFailed);
    }

    [Fact]
    public async Task ImportAsync_Should_Fail_For_Description_That_Exceeds_Domain_Limit()
    {
        var result = await ImportPayloadAsync(new
        {
            formatVersion = 1,
            items = new[]
            {
                CreateBirdPayload(
                    name: "Sparrow",
                    description: new string('x', BirdValidationRules.DescriptionMaxLength + 1))
            }
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AppErrorCodes.ImportValidationFailed);
    }

    private static async Task<Result<IReadOnlyList<BirdDTO>>> ImportPayloadAsync(object payload)
    {
        return await ImportRawJsonAsync(JsonSerializer.Serialize(payload));
    }

    private static async Task<Result<IReadOnlyList<BirdDTO>>> ImportRawJsonAsync(string json)
    {
        var sut = new JsonImportService();
        var path = Path.Combine(Path.GetTempPath(), $"birds-import-{Guid.NewGuid():N}.json");

        try
        {
            await File.WriteAllTextAsync(path, json);
            return await sut.ImportAsync(path, CancellationToken.None);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static object CreateBirdPayload(
        Guid? id = null,
        string name = "Sparrow",
        string? species = null,
        string? description = "note",
        DateOnly? arrival = null,
        DateOnly? departure = null,
        bool isAlive = true)
    {
        var resolvedId = id ?? Guid.NewGuid();
        var resolvedArrival = arrival ?? new DateOnly(2026, 4, 1);

        if (species is null)
            return new
            {
                id = resolvedId,
                name,
                description,
                arrival = resolvedArrival,
                departure,
                isAlive,
                createdAt = (DateTime?)null,
                updatedAt = (DateTime?)null
            };

        return new
        {
            id = resolvedId,
            name,
            species,
            description,
            arrival = resolvedArrival,
            departure,
            isAlive,
            createdAt = (DateTime?)null,
            updatedAt = (DateTime?)null
        };
    }
}
