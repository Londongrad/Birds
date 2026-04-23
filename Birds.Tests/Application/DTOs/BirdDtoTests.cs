using Birds.Application.DTOs;
using Birds.Domain.Enums;
using FluentAssertions;

namespace Birds.Tests.Application.DTOs;

public sealed class BirdDtoTests
{
    [Fact]
    public void ResolveSpecies_Should_Use_Stable_Species_When_Name_Differs()
    {
        var dto = new BirdDTO(
            Guid.NewGuid(),
            "Unknown display text",
            null,
            new DateOnly(2026, 4, 1),
            null,
            true,
            null,
            null)
        {
            Species = BirdsName.Щегол
        };

        dto.ResolveSpecies().Should().Be(BirdsName.Щегол);
        dto.Name.Should().Be("Unknown display text");
    }
}
