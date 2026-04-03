using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Domain.Enums;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.ViewModels;
using FluentAssertions;
using Moq;

namespace Birds.Tests.UI.ViewModels
{
    public class BirdViewModelTests
    {
        private readonly Mock<IBirdManager> _birdManager = new();
        private readonly Mock<INotificationService> _notification = new();

        [Fact]
        public async Task SaveAsync_Uses_SelectedBirdName_When_Updating()
        {
            // Arrange
            var original = CreateBirdDto(BirdsName.Воробей);
            var updated = original with
            {
                Name = BirdsName.Гайка.ToString(),
                UpdatedAt = DateTime.Now
            };

            BirdUpdateDTO? sentDto = null;
            _birdManager.Setup(x => x.UpdateAsync(It.IsAny<BirdUpdateDTO>(), It.IsAny<CancellationToken>()))
                .Callback<BirdUpdateDTO, CancellationToken>((dto, _) => sentDto = dto)
                .ReturnsAsync(Result<BirdDTO>.Success(updated));

            var sut = new BirdViewModel(original, _birdManager.Object, _notification.Object);
            sut.EditCommand.Execute(null);
            sut.SelectedBirdName = BirdsName.Гайка;

            // Act
            await sut.SaveCommand.ExecuteAsync(null);

            // Assert
            sentDto.Should().NotBeNull();
            sentDto!.Name.Should().Be(BirdsName.Гайка.ToString());
            sut.Name.Should().Be(BirdsName.Гайка.ToString());
            sut.Dto.Name.Should().Be(BirdsName.Гайка.ToString());
            sut.IsEditing.Should().BeFalse();
        }

        [Fact]
        public async Task CancelEdit_After_Successful_Save_Restores_Latest_Saved_State()
        {
            // Arrange
            var original = CreateBirdDto(BirdsName.Воробей);
            var updated = original with
            {
                Name = BirdsName.Гайка.ToString(),
                Description = "updated",
                UpdatedAt = DateTime.Now
            };

            _birdManager.Setup(x => x.UpdateAsync(It.IsAny<BirdUpdateDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<BirdDTO>.Success(updated));

            var sut = new BirdViewModel(original, _birdManager.Object, _notification.Object);
            sut.EditCommand.Execute(null);
            sut.SelectedBirdName = BirdsName.Гайка;
            sut.Description = "updated";
            await sut.SaveCommand.ExecuteAsync(null);

            sut.EditCommand.Execute(null);
            sut.SelectedBirdName = BirdsName.Дубонос;
            sut.Description = "draft";

            // Act
            sut.CancelEditCommand.Execute(null);

            // Assert
            sut.Name.Should().Be(BirdsName.Гайка.ToString());
            sut.SelectedBirdName.Should().Be(BirdsName.Гайка);
            sut.Description.Should().Be("updated");
            sut.Dto.Name.Should().Be(BirdsName.Гайка.ToString());
        }

        private static BirdDTO CreateBirdDto(BirdsName name) =>
            new(
                Id: Guid.NewGuid(),
                Name: name.ToString(),
                Description: "initial",
                Arrival: DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
                Departure: null,
                IsAlive: true,
                CreatedAt: DateTime.Today.AddDays(-5),
                UpdatedAt: null);
    }
}
