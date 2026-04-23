using Birds.Application.Commands.UpdateBird;
using Birds.Application.DTOs;
using Birds.Application.DTOs.Helpers;
using Birds.Application.Exceptions;
using Birds.Application.Interfaces;
using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Shared.Constants;
using FluentAssertions;
using Moq;

namespace Birds.Tests.Application.Commands.UpdateBird;

public class UpdateBirdCommandHandlerTests
{
    private readonly Mock<IBirdRepository> _repo = new();

    [Fact]
    public async Task Handle_Should_Update_And_Return_Dto_Success()
    {
        var id = Guid.NewGuid();
        var existing = Bird.Restore(id, (BirdsName)1, "old",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-30)), null, true);

        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var cmd = new UpdateBirdCommand(
            id,
            (BirdsName)6,
            "new",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-20)),
            null,
            true
        );

        var handler = new UpdateBirdCommandHandler(_repo.Object);

        var result = await handler.Handle(cmd, CancellationToken.None);

        var expectedDto = new BirdDTO(
            existing.Id,
            BirdNameDisplayNames.GetDisplayName(existing.Name),
            existing.Description,
            existing.Arrival,
            existing.Departure,
            existing.IsAlive,
            existing.CreatedAt,
            existing.UpdatedAt);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedDto);

        existing.Name.Should().Be(cmd.Name);
        existing.Description.Should().Be(cmd.Description);
        existing.Arrival.Should().Be(cmd.Arrival);
        existing.Departure.Should().Be(cmd.Departure);
        existing.IsAlive.Should().BeTrue();

        _repo.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Request_Is_Null()
    {
        var handler = new UpdateBirdCommandHandler(_repo.Object);

        var result = await handler.Handle(null!, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.RequestCannotBeNull);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_Should_Throw_NotFound_When_Repo_Throws_NotFound()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(nameof(Bird), id));

        var handler = new UpdateBirdCommandHandler(_repo.Object);

        var cmd = new UpdateBirdCommand(
            id,
            (BirdsName)4,
            "new",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-20)),
            null,
            true
        );

        Func<Task> act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repo.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Bird>(), It.IsAny<CancellationToken>()), Times.Never);
        _repo.VerifyNoOtherCalls();
    }
}
