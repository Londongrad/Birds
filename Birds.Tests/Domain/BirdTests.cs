using Birds.Domain.Entities;
using Birds.Domain.Enums;
using FluentAssertions;

namespace Birds.Tests.Domain
{
    public class BirdTests
    {
        [Fact]
        public void Create_ShouldReturnValidBird_WhenDataIsCorrect()
        {
            // Arrange
            var name = BirdsName.Воробей;
            var arrival = new DateOnly(2024, 5, 10);
            var description = "Small gray bird";

            // Act
            var bird = Bird.Create(name, description, arrival);

            // Assert
            bird.Should().NotBeNull();
            bird.Id.Should().NotBeEmpty();
            bird.Name.Should().Be(name);
            bird.Description.Should().Be(description);
            bird.Arrival.Should().Be(arrival);
            bird.Departure.Should().BeNull();
            bird.IsAlive.Should().BeTrue();
            bird.CreatedAt.Should().NotBeNull();
            bird.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Restore_ShouldRecreateBirdCorrectly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var name = BirdsName.Амадин;
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
            var bird = Bird.Create(BirdsName.Гайка, "Tiny bird", new DateOnly(2024, 5, 1));
            var oldUpdatedAt = bird.UpdatedAt;

            // Act
            bird.Update(BirdsName.Дубонос, "Updated bird", new DateOnly(2024, 5, 5), null, true);

            // Assert
            bird.Name.Should().Be(BirdsName.Дубонос);
            bird.Description.Should().Be("Updated bird");
            bird.Arrival.Should().Be(new DateOnly(2024, 5, 5));
            bird.Departure.Should().BeNull();
            bird.IsAlive.Should().BeTrue();
            bird.UpdatedAt.Should().NotBeNull();
            bird.UpdatedAt.Should().NotBe(oldUpdatedAt);
        }
    }
}