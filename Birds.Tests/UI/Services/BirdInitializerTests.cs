using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.Queries.GetAllBirds;
using Birds.Shared.Constants;
using Birds.Tests.Helpers;
using Birds.UI.Enums;
using Birds.UI.Services.Notification;
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
    public class BirdInitializerTests
    {
        [Fact]
        public async Task StartAsync_Success_PopulatesStoreAndLogs()
        {
            // Arrange
            var store = new BirdStore();
            var mediator = new Mock<IMediator>();
            var logger = new Mock<ILogger<BirdStoreInitializer>>();
            var notify = new Mock<INotificationService>();

            var birds = new List<BirdDTO> {
                new(Guid.NewGuid(), "Sparrow", "d", DateOnly.FromDateTime(DateTime.UtcNow), null, true, null, null),
                new(Guid.NewGuid(), "Tit", "d", DateOnly.FromDateTime(DateTime.UtcNow), null, true, null, null)
            }.AsReadOnly();

            mediator.Setup(m => m.Send(It.IsAny<GetAllBirdsQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<IReadOnlyList<BirdDTO>>.Success(birds));

            var sut = new BirdStoreInitializer(
                store, mediator.Object, logger.Object, notify.Object,
                retryPolicy: RetryNoDelay(4),
                uiDispatcher: new InlineUiDispatcher());

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            store.LoadState.Should().Be(LoadState.Loaded);
            store.Birds.Should().HaveCount(2);

            notify.Verify(n => n.ShowInfo(InfoMessages.LoadingBirdData), Times.Once);
            notify.Verify(n => n.ShowInfo(InfoMessages.LoadedSuccessfully), Times.Once);
        }

        [Fact]
        public async Task StartAsync_FailsAfterRetries_SetsFailedAndNotifies()
        {
            // Arrange
            var store = new BirdStore();
            var mediator = new Mock<IMediator>();
            var logger = new Mock<ILogger<BirdStoreInitializer>>();
            var notify = new Mock<INotificationService>();

            mediator.Setup(m => m.Send(It.IsAny<GetAllBirdsQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<IReadOnlyList<BirdDTO>>.Failure("db down"));

            var sut = new BirdStoreInitializer(
                store, mediator.Object, logger.Object, notify.Object,
                retryPolicy: RetryNoDelay(4),               // 1 + 4 ретрая = 5 вызовов
                uiDispatcher: new InlineUiDispatcher());

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            store.LoadState.Should().Be(LoadState.Failed);
            store.Birds.Should().BeEmpty();

            mediator.Verify(m => m.Send(It.IsAny<GetAllBirdsQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
            notify.Verify(n => n.Show(ErrorMessages.BirdLoadFailed,
                It.Is<NotificationOptions>(o => o.Type == NotificationType.Error)), Times.Once);
        }

        [Fact]
        public async Task StartAsync_FailsThenSucceeds_LoadsAndKeepsWarnings()
        {
            // Arrange
            var store = new BirdStore();
            var mediator = new Mock<IMediator>();
            var logger = new Mock<ILogger<BirdStoreInitializer>>();
            var notify = new Mock<INotificationService>();

            var birds = new List<BirdDTO> {
                new(Guid.NewGuid(), "Sparrow", null, DateOnly.FromDateTime(DateTime.UtcNow), null, true, null, null)
            }.AsReadOnly();

            mediator.SetupSequence(m => m.Send(It.IsAny<GetAllBirdsQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<IReadOnlyList<BirdDTO>>.Failure("temp"))
                    .ReturnsAsync(Result<IReadOnlyList<BirdDTO>>.Failure("temp"))
                    .ReturnsAsync(Result<IReadOnlyList<BirdDTO>>.Success(birds));

            var sut = new BirdStoreInitializer(
                store, mediator.Object, logger.Object, notify.Object,
                retryPolicy: RetryNoDelay(4),
                uiDispatcher: new InlineUiDispatcher());

            // Act
            await sut.StartAsync(CancellationToken.None);

            // Assert
            store.LoadState.Should().Be(LoadState.Loaded);
            store.Birds.Should().HaveCount(1);
            mediator.Verify(m => m.Send(It.IsAny<GetAllBirdsQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Fact]
        public async Task StartAsync_CanceledEarly_DoesNothing()
        {
            // Arrange
            var store = new BirdStore();
            var mediator = new Mock<IMediator>();
            var logger = new Mock<ILogger<BirdStoreInitializer>>();
            var notify = new Mock<INotificationService>();
            var cts = new CancellationTokenSource(); cts.Cancel();

            var sut = new BirdStoreInitializer(
                store, mediator.Object, logger.Object, notify.Object,
                retryPolicy: RetryNoDelay(4),
                uiDispatcher: new InlineUiDispatcher());

            // Act
            await sut.StartAsync(cts.Token);

            // Assert
            store.LoadState.Should().Be(LoadState.Uninitialized);
            mediator.VerifyNoOtherCalls();
            notify.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Helper.
        /// </summary>
        private static AsyncRetryPolicy<Result<IReadOnlyList<BirdDTO>>> RetryNoDelay(int retries)
        {
            // 1 attempt + retries
            return Policy
                .HandleResult<Result<IReadOnlyList<BirdDTO>>>(r => !r.IsSuccess)
                .WaitAndRetryAsync(retries, _ => TimeSpan.Zero);
        }
    }
}