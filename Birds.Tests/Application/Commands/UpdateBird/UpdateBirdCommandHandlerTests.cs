using Birds.Application.Commands.UpdateBird;
using Birds.Application.Common.Models;
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
        var existing = Bird.Restore(id, (BirdSpecies)1, "old",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-30)), null, true);
        var updated = Bird.Restore(id, (BirdSpecies)6, "new",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-20)), null, true, version: 2);

        _repo.Setup(r => r.UpdateAsync(
                id,
                existing.Version,
                (BirdSpecies)6,
                "new",
                It.IsAny<DateOnly>(),
                null,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var cmd = new UpdateBirdCommand(
            id,
            (BirdSpecies)6,
            "new",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-20)),
            null,
            true
        );

        var handler = new UpdateBirdCommandHandler(_repo.Object);

        var result = await handler.Handle(cmd, CancellationToken.None);

        var expectedDto = new BirdDTO(
            updated.Id,
            BirdNameDisplayNames.GetDisplayName(updated.Name),
            updated.Description,
            updated.Arrival,
            updated.Departure,
            updated.IsAlive,
            updated.CreatedAt,
            updated.UpdatedAt)
        {
            Species = updated.Name,
            Version = updated.Version
        };

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedDto);

        _repo.Verify(r => r.UpdateAsync(
            id,
            cmd.Version,
            cmd.Name,
            cmd.Description,
            cmd.Arrival,
            cmd.Departure,
            cmd.IsAlive,
            It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Request_Is_Null()
    {
        var handler = new UpdateBirdCommandHandler(_repo.Object);

        var result = await handler.Handle(null!, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.RequestCannotBeNull);
        result.ErrorCode.Should().Be(AppErrorCodes.ApplicationInvalidRequest);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_Should_Throw_NotFound_When_Repo_Throws_NotFound()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.UpdateAsync(
                id,
                It.IsAny<long>(),
                It.IsAny<BirdSpecies>(),
                It.IsAny<string?>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(nameof(Bird), id));

        var handler = new UpdateBirdCommandHandler(_repo.Object);

        var cmd = new UpdateBirdCommand(
            id,
            (BirdSpecies)4,
            "new",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-20)),
            null,
            true
        );

        Func<Task> act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repo.Verify(r => r.UpdateAsync(
            id,
            cmd.Version,
            cmd.Name,
            cmd.Description,
            cmd.Arrival,
            cmd.Departure,
            cmd.IsAlive,
            It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_Should_Return_Concurrency_Failure_When_Repository_Detects_Stale_Version()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.UpdateAsync(
                id,
                1,
                It.IsAny<BirdSpecies>(),
                It.IsAny<string?>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException(nameof(Bird), id));

        var handler = new UpdateBirdCommandHandler(_repo.Object);
        var cmd = new UpdateBirdCommand(
            id,
            (BirdSpecies)4,
            "stale",
            DateOnly.FromDateTime(DateTime.Now.AddDays(-20)),
            null,
            true);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.BirdConcurrencyConflict);
        result.ErrorCode.Should().Be(AppErrorCodes.BirdConcurrencyConflict);
    }
}
