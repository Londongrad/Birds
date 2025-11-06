using AutoMapper;
using Birds.Application.Commands.UpdateBird;
using Birds.Application.DTOs;
using Birds.Application.Exceptions;
using Birds.Application.Interfaces;
using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Shared.Constants;
using FluentAssertions;
using Moq;

namespace Birds.Tests.Application.Commands.UpdateBird
{
    public class UpdateBirdCommandHandlerTests
    {
        private readonly Mock<IBirdRepository> _repo = new();
        private readonly Mock<IMapper> _mapper = new();

        [Fact]
        public async Task Handle_Should_Update_And_Return_Dto_Success()
        {
            // Arrange
            var id = Guid.NewGuid();
            var existing = Bird.Restore(id, BirdsName.Воробей, "old",
                DateOnly.FromDateTime(DateTime.Now.AddDays(-30)), null, true);

            _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existing);

            var cmd = new UpdateBirdCommand(
                Id: id,
                Name: BirdsName.Гайка,
                Description: "new",
                Arrival: DateOnly.FromDateTime(DateTime.Now.AddDays(-20)),
                Departure: null,
                IsAlive: true
            );

            var dto = new BirdDTO(
                Id: id,
                Name: cmd.Name.ToString(),
                Description: cmd.Description,
                Arrival: cmd.Arrival,
                Departure: cmd.Departure,
                IsAlive: cmd.IsAlive,
                CreatedAt: existing.CreatedAt,
                UpdatedAt: existing.UpdatedAt
            );

            _mapper.Setup(m => m.Map<BirdDTO>(existing)).Returns(dto);

            var handler = new UpdateBirdCommandHandler(_repo.Object, _mapper.Object);

            // Act
            var result = await handler.Handle(cmd, CancellationToken.None);

            // Assert (Result)
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(dto);

            // Assert (Entity mutated)
            existing.Name.Should().Be(cmd.Name);
            existing.Description.Should().Be(cmd.Description);
            existing.Arrival.Should().Be(cmd.Arrival);
            existing.Departure.Should().Be(cmd.Departure);
            existing.IsAlive.Should().BeTrue();

            _repo.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
            _repo.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
            _mapper.Verify(m => m.Map<BirdDTO>(existing), Times.Once);
            _repo.VerifyNoOtherCalls();
            _mapper.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_Request_Is_Null()
        {
            var handler = new UpdateBirdCommandHandler(_repo.Object, _mapper.Object);

            var result = await handler.Handle(null!, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(ErrorMessages.RequestCannotBeNull);
            _repo.VerifyNoOtherCalls();
            _mapper.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_Should_Throw_NotFound_When_Repo_Throws_NotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new NotFoundException(nameof(Bird), id));

            var handler = new UpdateBirdCommandHandler(_repo.Object, _mapper.Object);

            var cmd = new UpdateBirdCommand(
                Id: id,
                Name: BirdsName.Дубонос,
                Description: "new",
                Arrival: DateOnly.FromDateTime(DateTime.Now.AddDays(-20)),
                Departure: null,
                IsAlive: true
            );

            // Act
            Func<Task> act = async () => await handler.Handle(cmd, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
            _repo.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<Bird>(), It.IsAny<CancellationToken>()), Times.Never);
            _mapper.Verify(m => m.Map<BirdDTO>(It.IsAny<Bird>()), Times.Never);
            _repo.VerifyNoOtherCalls();
            _mapper.VerifyNoOtherCalls();
        }
    }
}