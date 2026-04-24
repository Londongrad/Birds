using Birds.Application.Commands.ImportBirds;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.Interfaces;
using Birds.Domain.Entities;
using Birds.Domain.Enums;
using FluentAssertions;
using Moq;

namespace Birds.Tests.Application.Commands.ImportBirds;

public class ImportBirdsCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Upsert_Birds_And_Return_Snapshot()
    {
        var repository = new Mock<IBirdRepository>();
        var species = Enum.GetValues<BirdSpecies>()[0];
        var importedBird = new BirdDTO(
            Guid.NewGuid(),
            "Sparrow",
            "note",
            new DateOnly(2026, 4, 1),
            null,
            true,
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow.AddDays(-1))
        {
            Version = 5
        };

        IReadOnlyCollection<Bird>? savedBirds = null;
        repository.Setup(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<Bird>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Bird>, CancellationToken>((birds, _) => savedBirds = birds)
            .ReturnsAsync(new UpsertBirdsResult(1, 0));
        repository.Setup(x =>
                x.ReplaceWithSnapshotAsync(It.IsAny<IReadOnlyCollection<Bird>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpsertBirdsResult(1, 0));
        repository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                Bird.Restore(
                    importedBird.Id,
                    species,
                    importedBird.Description,
                    importedBird.Arrival,
                    importedBird.Departure,
                    importedBird.IsAlive,
                    importedBird.CreatedAt,
                    importedBird.UpdatedAt)
            });

        var sut = new ImportBirdsCommandHandler(repository.Object);

        var result = await sut.Handle(new ImportBirdsCommand(new[] { importedBird }), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Added.Should().Be(1);
        result.Value.Updated.Should().Be(0);
        result.Value.Removed.Should().Be(0);
        result.Value.Snapshot.Should().ContainSingle(x => x.Id == importedBird.Id);
        savedBirds.Should().ContainSingle(x => x.Version == importedBird.Version);
        repository.Verify(
            x => x.UpsertAsync(
                It.Is<IReadOnlyCollection<Bird>>(items => items.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Prefer_Stable_Species_Over_Display_Name()
    {
        var repository = new Mock<IBirdRepository>();
        var importedBird = new BirdDTO(
            Guid.NewGuid(),
            "Unknown display text",
            null,
            new DateOnly(2026, 4, 1),
            null,
            true,
            null,
            null)
        {
            Species = BirdSpecies.Goldfinch
        };

        IReadOnlyCollection<Bird>? savedBirds = null;
        repository.Setup(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<Bird>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Bird>, CancellationToken>((birds, _) => savedBirds = birds)
            .ReturnsAsync(new UpsertBirdsResult(1, 0));
        repository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Bird>());

        var sut = new ImportBirdsCommandHandler(repository.Object);

        var result = await sut.Handle(new ImportBirdsCommand(new[] { importedBird }), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        savedBirds.Should().ContainSingle();
        savedBirds!.Single().Name.Should().Be(BirdSpecies.Goldfinch);
    }

    [Fact]
    public async Task Handle_Should_Fail_For_Unknown_Bird_Name()
    {
        var repository = new Mock<IBirdRepository>();
        var sut = new ImportBirdsCommandHandler(repository.Object);

        var result = await sut.Handle(
            new ImportBirdsCommand(new[]
            {
                new BirdDTO(Guid.NewGuid(), "UnknownBird", null, new DateOnly(2026, 4, 1), null, true, null, null)
            }),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AppErrorCodes.ImportInvalidSpecies);
        repository.Verify(x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<Bird>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Replace_Snapshot_When_Mode_Is_Replace()
    {
        var repository = new Mock<IBirdRepository>();
        var importedBird = new BirdDTO(
            Guid.NewGuid(),
            "Sparrow",
            "note",
            new DateOnly(2026, 4, 1),
            null,
            true,
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow.AddDays(-1));

        repository.Setup(x =>
                x.ReplaceWithSnapshotAsync(It.IsAny<IReadOnlyCollection<Bird>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpsertBirdsResult(1, 0, 3));
        repository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Bird>());

        var sut = new ImportBirdsCommandHandler(repository.Object);

        var result = await sut.Handle(
            new ImportBirdsCommand(new[] { importedBird }, BirdImportMode.Replace),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Removed.Should().Be(3);
        repository.Verify(
            x => x.ReplaceWithSnapshotAsync(
                It.Is<IReadOnlyCollection<Bird>>(items => items.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(
            x => x.UpsertAsync(It.IsAny<IReadOnlyCollection<Bird>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
