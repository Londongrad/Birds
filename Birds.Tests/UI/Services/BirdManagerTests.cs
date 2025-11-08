using Birds.Application.Commands.CreateBird;
using Birds.Application.Commands.DeleteBird;
using Birds.Application.Commands.UpdateBird;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.Queries.GetAllBirds;
using Birds.Domain.Enums;
using Birds.Tests.Helpers;
using Birds.UI.Enums;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Stores.BirdStore;
using FluentAssertions;
using MediatR;
using Moq;

namespace Birds.Tests.UI.Services
{
    public class BirdManagerTests
    {
        private static BirdStoreInitializer Init(IBirdStore store, IMediator mediator) =>
            TestHelpers.MakeInitializer(store, mediator, out _, out _);

        private static BirdManager MakeManager(IBirdStore store, BirdStoreInitializer init, IMediator mediator) =>
            new(store, init, mediator, new InlineUiDispatcher());

        [Fact]
        public async Task AddAsync_WhenStoreLoaded_SendsCreate_AndAddsToStore()
        {
            // Arrange
            var store = new BirdStore(); store.CompleteLoading();
            var mediator = new Mock<IMediator>();

            var created = TestHelpers.Bird(name: "Воробей", desc: "desc");
            mediator.Setup(m => m.Send(It.IsAny<CreateBirdCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<BirdDTO>.Success(created));

            var sut = MakeManager(store, Init(store, mediator.Object), mediator.Object);
            var dto = new BirdCreateDTO(BirdsName.Воробей, "desc", TestHelpers.Today(), null, true);

            // Act
            var result = await sut.AddAsync(dto, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            store.Birds.Should().ContainSingle(b => b.Id == created.Id);
            mediator.Verify(m => m.Send(It.IsAny<CreateBirdCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AddAsync_WhenUninitialized_Reloads_ThenCreates_AndAdds()
        {
            // Arrange
            var store = new BirdStore(); // Uninitialized
            var mediator = new Mock<IMediator>();

            mediator.Setup(m => m.Send(It.IsAny<GetAllBirdsQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<IReadOnlyList<BirdDTO>>.Success(Array.Empty<BirdDTO>()));

            var created = TestHelpers.Bird(name: "Гайка");
            mediator.Setup(m => m.Send(It.IsAny<CreateBirdCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<BirdDTO>.Success(created));

            var sut = MakeManager(store, Init(store, mediator.Object), mediator.Object);
            var dto = new BirdCreateDTO(BirdsName.Гайка, null, TestHelpers.Today(), null, true);

            // Act
            var result = await sut.AddAsync(dto, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            store.LoadState.Should().Be(LoadState.Loaded);
            store.Birds.Should().ContainSingle(b => b.Id == created.Id);

            mediator.Verify(m => m.Send(It.IsAny<GetAllBirdsQuery>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            mediator.Verify(m => m.Send(It.IsAny<CreateBirdCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AddAsync_WhenReloadEndsFailed_ReturnsFailure_AndDoesNotCreate()
        {
            // Arrange
            var store = new BirdStore();
            var mediator = new Mock<IMediator>();

            mediator.Setup(m => m.Send(It.IsAny<GetAllBirdsQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<IReadOnlyList<BirdDTO>>.Failure("db down"));

            var sut = MakeManager(store, Init(store, mediator.Object), mediator.Object);
            var dto = new BirdCreateDTO(BirdsName.Воробей, "x", TestHelpers.Today(), null, true);

            // Act
            var result = await sut.AddAsync(dto, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Bird store cannot be loaded.");
            store.LoadState.Should().Be(LoadState.Failed);
            mediator.Verify(m => m.Send(It.IsAny<CreateBirdCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddAsync_WhenLoading_WaitsUntilLoaded_ThenCreates()
        {
            // Arrange
            var store = new BirdStore(); store.BeginLoading();
            var mediator = new Mock<IMediator>();

            var created = TestHelpers.Bird(name: "Воробей");
            mediator.Setup(m => m.Send(It.IsAny<CreateBirdCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<BirdDTO>.Success(created));

            var sut = MakeManager(store, Init(store, mediator.Object), mediator.Object);

            _ = Task.Run(async () =>
            {
                await Task.Delay(10);
                store.CompleteLoading();
            });

            var dto = new BirdCreateDTO(BirdsName.Воробей, null, TestHelpers.Today(), null, true);

            // Act
            var result = await sut.AddAsync(dto, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            store.Birds.Should().ContainSingle(b => b.Id == created.Id);
        }

        [Fact]
        public async Task UpdateAsync_Success_ReplacesItemInStore()
        {
            // Arrange
            var store = new BirdStore(); store.CompleteLoading();
            var mediator = new Mock<IMediator>();

            var id = Guid.NewGuid();
            store.Birds.Add(new BirdDTO(id, "Воробей", "old",
                TestHelpers.Today().AddDays(-1), null, true, null, null));

            var updated = new BirdDTO(id, "Синица", "new", TestHelpers.Today(), null, true, null, null);
            mediator.Setup(m => m.Send(It.IsAny<UpdateBirdCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<BirdDTO>.Success(updated));

            var sut = MakeManager(store, Init(store, mediator.Object), mediator.Object);
            var dto = new BirdUpdateDTO(id, "Синица", "new", TestHelpers.Today(), null, true);

            // Act
            var result = await sut.UpdateAsync(dto, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            store.Birds.Should().ContainSingle(b => b.Id == id && b.Name == "Синица" && b.Description == "new");
        }

        [Fact]
        public async Task UpdateAsync_Failure_DoesNotChangeStore()
        {
            // Arrange
            var store = new BirdStore(); store.CompleteLoading();
            var mediator = new Mock<IMediator>();

            var id = Guid.NewGuid();
            var original = new BirdDTO(id, "Воробей", "old",
                TestHelpers.Today().AddDays(-1), null, true, null, null);
            store.Birds.Add(original);

            mediator.Setup(m => m.Send(It.IsAny<UpdateBirdCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<BirdDTO>.Failure("boom"));

            var sut = MakeManager(store, Init(store, mediator.Object), mediator.Object);
            var dto = new BirdUpdateDTO(id, "Синица", "new", TestHelpers.Today(), null, true);

            // Act
            var result = await sut.UpdateAsync(dto, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            store.Birds.Single().Should().BeEquivalentTo(original);
        }

        [Fact]
        public async Task DeleteAsync_Success_RemovesFromStore()
        {
            // Arrange
            var store = new BirdStore(); store.CompleteLoading();
            var mediator = new Mock<IMediator>();

            var id = Guid.NewGuid();
            store.Birds.Add(new BirdDTO(id, "Воробей", null, TestHelpers.Today(), null, true, null, null));

            mediator.Setup(m => m.Send(It.IsAny<DeleteBirdCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success());

            var sut = MakeManager(store, Init(store, mediator.Object), mediator.Object);

            // Act
            var result = await sut.DeleteAsync(id, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            store.Birds.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteAsync_Failure_DoesNotRemoveFromStore()
        {
            // Arrange
            var store = new BirdStore(); store.CompleteLoading();
            var mediator = new Mock<IMediator>();

            var id = Guid.NewGuid();
            store.Birds.Add(new BirdDTO(id, "Воробей", null, TestHelpers.Today(), null, true, null, null));

            mediator.Setup(m => m.Send(It.IsAny<DeleteBirdCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Failure("nope"));

            var sut = MakeManager(store, Init(store, mediator.Object), mediator.Object);

            // Act
            var result = await sut.DeleteAsync(id, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            store.Birds.Should().ContainSingle(b => b.Id == id);
        }
    }
}
