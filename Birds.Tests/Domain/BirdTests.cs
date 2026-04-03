using Birds.Domain.Common.Exceptions;
using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Shared.Constants;
using FluentAssertions;

namespace Birds.Tests.Domain
{
    public class BirdTests
    {
        [Fact]
        public void Create_ShouldReturnValidBird_WhenDataIsCorrect()
        {
            // Arrange
            var name = (BirdsName)1;
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
            bird.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Restore_ShouldRecreateBirdCorrectly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var name = (BirdsName)3;
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
            var bird = Bird.Create((BirdsName)6, "Tiny bird", new DateOnly(2024, 5, 1));
            var oldUpdatedAt = bird.UpdatedAt;

            // Act
            bird.Update((BirdsName)4, "Updated bird", new DateOnly(2024, 5, 5), null, true);

            // Assert
            bird.Name.Should().Be((BirdsName)4);
            bird.Description.Should().Be("Updated bird");
            bird.Arrival.Should().Be(new DateOnly(2024, 5, 5));
            bird.Departure.Should().BeNull();
            bird.IsAlive.Should().BeTrue();
            bird.UpdatedAt.Should().NotBeNull();
            bird.UpdatedAt.Should().NotBe(oldUpdatedAt);
        }

        [Fact]
        public void Create_ShouldThrow_When_Description_Exceeds_Max_Length()
        {
            Action act = () => Bird.Create(
                (BirdsName)1,
                new string('A', BirdValidationRules.DescriptionMaxLength + 1),
                new DateOnly(2024, 5, 10));

            act.Should().Throw<DomainValidationException>();
        }

        [Fact]
        public void Update_ShouldThrow_When_Departure_Earlier_Than_Arrival()
        {
            var bird = Bird.Create((BirdsName)6, "Tiny bird", new DateOnly(2024, 5, 1));

            Action act = () => bird.Update(
                (BirdsName)4,
                "Updated bird",
                new DateOnly(2024, 5, 5),
                new DateOnly(2024, 5, 4),
                false);

            act.Should().Throw<DomainValidationException>();
        }
    }
}
