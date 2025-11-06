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
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.Services.Stores.BirdStore;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using Polly.Retry;

namespace Birds.Tests.UI.Services
{
    public class BirdManagerTests
    {
        private static AsyncRetryPolicy<Result<IReadOnlyList<BirdDTO>>> RetryNoDelay(int retries) =>
        Policy.HandleResult<Result<IReadOnlyList<BirdDTO>>>(r => !r.IsSuccess)
              .WaitAndRetryAsync(retries, _ => TimeSpan.Zero);

        private static BirdStoreInitializer MakeInitializer(IBirdStore store, IMediator mediator)
        {
            var logger = new Mock<ILogger<BirdStoreInitializer>>();
            var notify = new Mock<INotificationService>();
            return new BirdStoreInitializer(
                store, mediator, logger.Object, notify.Object,
                retryPolicy: RetryNoDelay(4),
                uiDispatcher: new InlineUiDispatcher());
        }

        private static BirdManager MakeManager(IBirdStore store, BirdStoreInitializer init, IMediator mediator) =>
            new(store, init, mediator, new InlineUiDispatcher());

        // ========== tests ==========

        [Fact]
        public async Task AddAsync_WhenStoreLoaded_SendsCreate_AndAddsToStore()
        {
            // Arrange
            var store = new BirdStore();
            store.CompleteLoading(); // Loaded
            var mediator = new Mock<IMediator>();
            var created = new BirdDTO(Guid.NewGuid(), "Воробей", "desc",
                DateOnly.FromDateTime(DateTime.Now), null, true, null, null);

            mediator.Setup(m => m.Send(It.IsAny<CreateBirdCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<BirdDTO>.Success(created));

            var init = MakeInitializer(store, mediator.Object); // не будет вызван
            var sut = MakeManager(store, init, mediator.Object);

            var dto = new BirdCreateDTO(BirdsName.Воробей, "desc",
                DateOnly.FromDateTime(DateTime.Now), null, true);

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

            // Перезагрузка успешна (пустой список → Loaded)
            mediator.Setup(m => m.Send(It.IsAny<GetAllBirdsQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<IReadOnlyList<BirdDTO>>.Success(Array.Empty<BirdDTO>()));

            var created = new BirdDTO(Guid.NewGuid(), "Гайка", null,
                DateOnly.FromDateTime(DateTime.Now), null, true, null, null);

            mediator.Setup(m => m.Send(It.IsAny<CreateBirdCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<BirdDTO>.Success(created));

            var init = MakeInitializer(store, mediator.Object);
            var sut = MakeManager(store, init, mediator.Object);

            var dto = new BirdCreateDTO(BirdsName.Гайка, null,
                DateOnly.FromDateTime(DateTime.Now), null, true);

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
            var store = new BirdStore(); // Uninitialized → Fail
            var mediator = new Mock<IMediator>();

            mediator.Setup(m => m.Send(It.IsAny<GetAllBirdsQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<IReadOnlyList<BirdDTO>>.Failure("db down"));

            var init = MakeInitializer(store, mediator.Object);
            var sut = MakeManager(store, init, mediator.Object);

            var dto = new BirdCreateDTO(BirdsName.Воробей, "x",
                DateOnly.FromDateTime(DateTime.Now), null, true);

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
            var store = new BirdStore();
            store.BeginLoading(); // Loading
            var mediator = new Mock<IMediator>();

            var created = new BirdDTO(Guid.NewGuid(), "Воробей", null,
                DateOnly.FromDateTime(DateTime.Now), null, true, null, null);

            mediator.Setup(m => m.Send(It.IsAny<CreateBirdCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<BirdDTO>.Success(created));

            var init = MakeInitializer(store, mediator.Object); // не нужен в этом сценарии
            var sut = MakeManager(store, init, mediator.Object);

            // Через короткую задержку переведём в Loaded, чтобы менеджер «дождался»
            _ = Task.Run(async () =>
            {
                await Task.Delay(10);
                store.CompleteLoading();
            });

            var dto = new BirdCreateDTO(BirdsName.Воробей, null,
                DateOnly.FromDateTime(DateTime.Now), null, true);

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
            var store = new BirdStore();
            store.CompleteLoading();
            var mediator = new Mock<IMediator>();

            var id = Guid.NewGuid();
            store.Birds.Add(new BirdDTO(id, "Воробей", "old",
                DateOnly.FromDateTime(DateTime.Now.AddDays(-1)), null, true, null, null));

            var updated = new BirdDTO(id, "Синица", "new",
                DateOnly.FromDateTime(DateTime.Now), null, true, null, null);

            mediator.Setup(m => m.Send(It.IsAny<UpdateBirdCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<BirdDTO>.Success(updated));

            var init = MakeInitializer(store, mediator.Object);
            var sut = MakeManager(store, init, mediator.Object);

            var dto = new BirdUpdateDTO(id, "Синица", "new",
                DateOnly.FromDateTime(DateTime.Now), null, true);

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
            var store = new BirdStore();
            store.CompleteLoading();
            var mediator = new Mock<IMediator>();

            var id = Guid.NewGuid();
            var original = new BirdDTO(id, "Воробей", "old",
                DateOnly.FromDateTime(DateTime.Now.AddDays(-1)), null, true, null, null);
            store.Birds.Add(original);

            mediator.Setup(m => m.Send(It.IsAny<UpdateBirdCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<BirdDTO>.Failure("boom"));

            var init = MakeInitializer(store, mediator.Object);
            var sut = MakeManager(store, init, mediator.Object);

            var dto = new BirdUpdateDTO(id, "Синица", "new",
                DateOnly.FromDateTime(DateTime.Now), null, true);

            // Act
            var result = await sut.UpdateAsync(dto, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            store.Birds.Should().ContainSingle(b => b.Id == id && b.Name == original.Name && b.Description == original.Description);
        }

        [Fact]
        public async Task DeleteAsync_Success_RemovesFromStore()
        {
            // Arrange
            var store = new BirdStore();
            store.CompleteLoading();
            var mediator = new Mock<IMediator>();

            var id = Guid.NewGuid();
            store.Birds.Add(new BirdDTO(id, "Воробей", null,
                DateOnly.FromDateTime(DateTime.Now), null, true, null, null));

            mediator.Setup(m => m.Send(It.IsAny<DeleteBirdCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Success());

            var init = MakeInitializer(store, mediator.Object);
            var sut = MakeManager(store, init, mediator.Object);

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
            var store = new BirdStore();
            store.CompleteLoading();
            var mediator = new Mock<IMediator>();

            var id = Guid.NewGuid();
            store.Birds.Add(new BirdDTO(id, "Воробей", null,
                DateOnly.FromDateTime(DateTime.Now), null, true, null, null));

            mediator.Setup(m => m.Send(It.IsAny<DeleteBirdCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result.Failure("nope"));

            var init = MakeInitializer(store, mediator.Object);
            var sut = MakeManager(store, init, mediator.Object);

            // Act
            var result = await sut.DeleteAsync(id, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            store.Birds.Should().ContainSingle(b => b.Id == id);
        }
    }
}