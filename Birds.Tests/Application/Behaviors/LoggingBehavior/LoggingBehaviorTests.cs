using Birds.Application.Behaviors;
using Birds.Application.Commands.CreateBird;
using Birds.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Birds.Tests.Application.Behaviors.LoggingBehavior;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task Logs_Before_And_After_And_CallsNext()
    {
        var logger = new Mock<ILogger<LoggingBehavior<CreateBirdCommand, string>>>();
        var behavior = new LoggingBehavior<CreateBirdCommand, string>(logger.Object);

        var cmd = new CreateBirdCommand(
            BirdSpecies.Sparrow,
            "ok",
            DateOnly.FromDateTime(DateTime.Now)
        );

        RequestHandlerDelegate<string> next = cancellationToken => Task.FromResult("OK");

        var result = await behavior.Handle(cmd, next, CancellationToken.None);

        result.Should().Be("OK");
        VerifyLog(logger, "Handling", Times.Once());
        VerifyLog(logger, "Handled", Times.Once());
        VerifyLog(logger, "Failed", Times.Never());
    }

    [Fact]
    public async Task Logs_Before_And_Failure_Then_Rethrows_When_Next_Throws()
    {
        var logger = new Mock<ILogger<LoggingBehavior<CreateBirdCommand, string>>>();
        var behavior = new LoggingBehavior<CreateBirdCommand, string>(logger.Object);

        var cmd = new CreateBirdCommand(
            BirdSpecies.Sparrow,
            "ok",
            DateOnly.FromDateTime(DateTime.Now)
        );

        RequestHandlerDelegate<string> next = cancellationToken => throw new InvalidOperationException("boom");

        Func<Task> act = async () => await behavior.Handle(cmd, next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        VerifyLog(logger, "Handling", Times.Once());
        VerifyLog(logger, "Handled", Times.Never());
        VerifyLog(logger, "Failed", Times.Once());
    }

    private static void VerifyLog(
        Mock<ILogger<LoggingBehavior<CreateBirdCommand, string>>> logger,
        string expectedText,
        Times times)
    {
        logger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v != null &&
                    v.ToString()!.IndexOf(expectedText, StringComparison.OrdinalIgnoreCase) >= 0 &&
                    v.ToString()!.IndexOf(nameof(CreateBirdCommand), StringComparison.OrdinalIgnoreCase) >= 0),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
}
