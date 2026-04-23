using Birds.Domain.Common;
using Birds.Domain.Common.Exceptions;
using Birds.Domain.Entities;
using Birds.Domain.Enums;
using FluentAssertions;

namespace Birds.Tests.Domain;

public class BirdTests
{
    [Fact]
    public void Create_ShouldReturnValidBird_WhenDataIsCorrect()
    {
        // Arrange
        var name = (BirdSpecies)1;
        var arrival = new DateOnly(2024, 5, 10);
        var description = "Small gray bird";
        var beforeLocal = DateTime.Now;
        var beforeUtc = DateTime.UtcNow;

        // Act
        var bird = Bird.Create(name, description, arrival);
        var afterLocal = DateTime.Now;
        var afterUtc = DateTime.UtcNow;

        // Assert
        bird.Should().NotBeNull();
        bird.Id.Should().NotBeEmpty();
        bird.Name.Should().Be(name);
        bird.Description.Should().Be(description);
        bird.Arrival.Should().Be(arrival);
        bird.Departure.Should().BeNull();
        bird.IsAlive.Should().BeTrue();
        bird.CreatedAt.Should().BeOnOrAfter(beforeLocal).And.BeOnOrBefore(afterLocal);
        bird.CreatedAt.Kind.Should().Be(DateTimeKind.Local);
        bird.UpdatedAt.Should().BeNull();
        bird.SyncStampUtc.Should().BeOnOrAfter(beforeUtc).And.BeOnOrBefore(afterUtc);
        bird.SyncStampUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Restore_ShouldRecreateBirdCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = (BirdSpecies)3;
        var arrival = new DateOnly(2023, 1, 15);
        var departure = new DateOnly(2024, 1, 15);
        var isAlive = false;
        var description = "Large bird of prey";

        // Act
        var bird = Bird.Restore(id, name, description, arrival, departure, isAlive);

        // Assert
        bird.Id.Should().Be(id);
        bird.Name.Should().Be(name);
        bird.Description.Should().Be(description);
        bird.Arrival.Should().Be(arrival);
        bird.Departure.Should().Be(departure);
        bird.IsAlive.Should().BeFalse();
    }

    [Fact]
    public void Update_ShouldModifyPropertiesAndUpdateTimestamp()
    {
        // Arrange
        var bird = Bird.Create((BirdSpecies)6, "Tiny bird", new DateOnly(2024, 5, 1));
        var oldUpdatedAt = bird.UpdatedAt;
        var beforeLocal = DateTime.Now;
        var beforeUtc = DateTime.UtcNow;

        // Act
        bird.Update((BirdSpecies)4, "Updated bird", new DateOnly(2024, 5, 5), null, true);
        var afterLocal = DateTime.Now;
        var afterUtc = DateTime.UtcNow;

        // Assert
        bird.Name.Should().Be((BirdSpecies)4);
        bird.Description.Should().Be("Updated bird");
        bird.Arrival.Should().Be(new DateOnly(2024, 5, 5));
        bird.Departure.Should().BeNull();
        bird.IsAlive.Should().BeTrue();
        bird.UpdatedAt.Should().NotBeNull();
        bird.UpdatedAt!.Value.Should().BeOnOrAfter(beforeLocal).And.BeOnOrBefore(afterLocal);
        bird.UpdatedAt.Value.Kind.Should().Be(DateTimeKind.Local);
        bird.UpdatedAt.Should().NotBe(oldUpdatedAt);
        bird.SyncStampUtc.Should().BeOnOrAfter(beforeUtc).And.BeOnOrBefore(afterUtc);
        bird.SyncStampUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Create_ShouldThrow_When_Description_Exceeds_Max_Length()
    {
        Action act = () => Bird.Create(
            (BirdSpecies)1,
            new string('A', BirdValidationRules.DescriptionMaxLength + 1),
            new DateOnly(2024, 5, 10));

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Update_ShouldThrow_When_Departure_Earlier_Than_Arrival()
    {
        var bird = Bird.Create((BirdSpecies)6, "Tiny bird", new DateOnly(2024, 5, 1));

        var act = () => bird.Update(
            (BirdSpecies)4,
            "Updated bird",
            new DateOnly(2024, 5, 5),
            new DateOnly(2024, 5, 4),
            false);

        act.Should().Throw<DomainValidationException>();
    }
}
