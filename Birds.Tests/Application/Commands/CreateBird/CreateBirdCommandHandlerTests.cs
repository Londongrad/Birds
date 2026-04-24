using Birds.Application.Commands.CreateBird;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.DTOs.Helpers;
using Birds.Application.Interfaces;
using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Shared.Constants;
using FluentAssertions;
using Moq;

namespace Birds.Tests.Application.Commands.CreateBird;

public class CreateBirdCommandHandlerTests
{
    private readonly Mock<IBirdRepository> _birdRepositoryMock = new();

    [Fact]
    public async Task Handle_Should_Return_SuccessResult_When_Command_Is_Valid()
    {
        var command = new CreateBirdCommand(
            (BirdSpecies)1,
            "Nice bird",
            DateOnly.FromDateTime(DateTime.Now)
        );

        var expectedBird = Bird.Create(command.Name, command.Description, command.Arrival, command.Departure,
            command.IsAlive);
        var expectedDto = new BirdDTO(
            expectedBird.Id,
            BirdNameDisplayNames.GetDisplayName(expectedBird.Name),
            expectedBird.Description,
            expectedBird.Arrival,
            expectedBird.Departure,
            expectedBird.IsAlive,
            expectedBird.CreatedAt,
            expectedBird.UpdatedAt);

        Bird? savedBird = null;
        _birdRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Bird>(), It.IsAny<CancellationToken>()))
            .Callback<Bird, CancellationToken>((bird, _) => savedBird = bird)
            .Returns(Task.CompletedTask);

        var handler = new CreateBirdCommandHandler(_birdRepositoryMock.Object);

        var result = await handler.Handle(command, default);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        savedBird.Should().NotBeNull();
        savedBird!.Name.Should().Be(command.Name);
        savedBird.Description.Should().Be(command.Description);
        savedBird.Arrival.Should().Be(command.Arrival);
        savedBird.Departure.Should().Be(command.Departure);
        savedBird.IsAlive.Should().Be(command.IsAlive);
        result.Value.Should().BeEquivalentTo(expectedDto,
            options => options.Excluding(dto => dto.Id).Excluding(dto => dto.CreatedAt));
        result.Value.Id.Should().Be(savedBird.Id);
        result.Value.CreatedAt.Should().Be(savedBird.CreatedAt);

        _birdRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Bird>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Request_Is_Null()
    {
        var handler = new CreateBirdCommandHandler(_birdRepositoryMock.Object);

        var result = await handler.Handle(null!, default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.RequestCannotBeNull);
        result.ErrorCode.Should().Be(AppErrorCodes.ApplicationInvalidRequest);
    }
}
