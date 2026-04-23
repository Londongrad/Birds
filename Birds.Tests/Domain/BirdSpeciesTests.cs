using Birds.Domain.Enums;
using FluentAssertions;

namespace Birds.Tests.Domain;

public sealed class BirdSpeciesTests
{
    [Fact]
    public void BirdSpecies_Should_Preserve_Legacy_Numeric_Values()
    {
        ((int)BirdSpecies.Sparrow).Should().Be(1);
        ((int)BirdSpecies.Goldfinch).Should().Be(2);
        ((int)BirdSpecies.Amadin).Should().Be(3);
        ((int)BirdSpecies.Hawfinch).Should().Be(4);
        ((int)BirdSpecies.GreatTit).Should().Be(5);
        ((int)BirdSpecies.BlackCappedChickadee).Should().Be(6);
        ((int)BirdSpecies.Nuthatch).Should().Be(7);
    }

    [Theory]
    [InlineData("Воробей", BirdSpecies.Sparrow)]
    [InlineData("Щегол", BirdSpecies.Goldfinch)]
    [InlineData("Амадин", BirdSpecies.Amadin)]
    [InlineData("Дубонос", BirdSpecies.Hawfinch)]
    [InlineData("Большак", BirdSpecies.GreatTit)]
    [InlineData("Гайка", BirdSpecies.BlackCappedChickadee)]
    [InlineData("Поползень", BirdSpecies.Nuthatch)]
    public void BirdSpeciesCodes_Should_Parse_Legacy_Russian_Enum_Names(
        string legacyCode,
        BirdSpecies expected)
    {
        BirdSpeciesCodes.Parse(legacyCode).Should().Be(expected);
    }
}
