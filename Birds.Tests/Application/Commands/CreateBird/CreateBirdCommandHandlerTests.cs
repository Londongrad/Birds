using AutoMapper;
using Birds.Application.Commands.CreateBird;
using Birds.Application.DTOs;
using Birds.Application.Interfaces;
using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Domain.Extensions;
using FluentAssertions;
using Moq;

namespace Birds.Tests.Application.Commands.CreateBird
{
    public class CreateBirdCommandHandlerTests
    {
        private readonly Mock<IBirdRepository> _birdRepositoryMock = new();
        private readonly Mock<IMapper> _mapperMock = new();

        [Fact]
        public async Task Handle_Should_Return_SuccessResult_When_Command_Is_Valid()
        {
            // Arrange
            var command = new CreateBirdCommand(
                BirdsName.Воробей,
                "Nice bird",
                DateOnly.FromDateTime(DateTime.Now)
            );

            var bird = Bird.Create(command.Name, command.Description, command.Arrival, command.Departure, command.IsAlive);
            var birdDto = new BirdDTO(
                bird.Id,
                bird.Name.ToDisplayName(),
                bird.Description,
                bird.Arrival,
                bird.Departure,
                bird.IsAlive,
                bird.CreatedAt,
                bird.UpdatedAt);

            _mapperMock.Setup(m => m.Map<Bird>(command)).Returns(bird);
            _mapperMock.Setup(m => m.Map<BirdDTO>(bird)).Returns(birdDto);

            var handler = new CreateBirdCommandHandler(_birdRepositoryMock.Object, _mapperMock.Object);

            // Act
            var result = await handler.Handle(command, default);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(birdDto);

            _birdRepositoryMock.Verify(r => r.AddAsync(bird, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_Request_Is_Null()
        {
            var handler = new CreateBirdCommandHandler(_birdRepositoryMock.Object, _mapperMock.Object);

            var result = await handler.Handle(null!, default);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Request cannot be null.");
        }
    }
}