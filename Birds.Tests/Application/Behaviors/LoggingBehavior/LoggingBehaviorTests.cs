using Birds.Application.Behaviors;
using Birds.Application.Commands.CreateBird;
using Birds.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Birds.Tests.Application.Behaviors.LoggingBehavior
{
    public class LoggingBehaviorTests
    {
        [Fact]
        public async Task Logs_Before_And_After_And_CallsNext()
        {
            // Arrange
            var logger = new Mock<ILogger<LoggingBehavior<CreateBirdCommand, string>>>();
            var behavior = new LoggingBehavior<CreateBirdCommand, string>(logger.Object);

            var cmd = new CreateBirdCommand(
                BirdsName.Воробей,
                "ok",
                DateOnly.FromDateTime(DateTime.Now)
            );

            RequestHandlerDelegate<string> next = (cancellationToken) => Task.FromResult("OK");

            // Act
            var result = await behavior.Handle(cmd, next, CancellationToken.None);

            // Assert
            result.Should().Be("OK");

            // "Handling {RequestName}"
            logger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v != null &&
                    // матчим и слово, и имя запроса
                    v.ToString()!.IndexOf("Handling", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    v.ToString()!.IndexOf(nameof(CreateBirdCommand), StringComparison.OrdinalIgnoreCase) >= 0),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // "Handled {RequestName}"
            logger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v != null &&
                    v.ToString()!.IndexOf("Handled", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    v.ToString()!.IndexOf(nameof(CreateBirdCommand), StringComparison.OrdinalIgnoreCase) >= 0),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Logs_Before_And_Rethrows_When_Next_Throws_No_AfterLog()
        {
            // Arrange
            var logger = new Mock<ILogger<LoggingBehavior<CreateBirdCommand, string>>>();
            var behavior = new LoggingBehavior<CreateBirdCommand, string>(logger.Object);

            var cmd = new CreateBirdCommand(
                BirdsName.Воробей,
                "ok",
                DateOnly.FromDateTime(DateTime.Now)
            );

            RequestHandlerDelegate<string> next = (cancellationToken) => throw new InvalidOperationException("boom");

            // Act
            Func<Task> act = async () => await behavior.Handle(cmd, next, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();

            // Был "Handling ..."
            logger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v != null &&
                    v.ToString()!.IndexOf("Handling", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    v.ToString()!.IndexOf(nameof(CreateBirdCommand), StringComparison.OrdinalIgnoreCase) >= 0),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // Не было "Handled ..." (упали раньше)
            logger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v != null &&
                    v.ToString()!.IndexOf("Handled", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    v.ToString()!.IndexOf(nameof(CreateBirdCommand), StringComparison.OrdinalIgnoreCase) >= 0),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }
    }
}