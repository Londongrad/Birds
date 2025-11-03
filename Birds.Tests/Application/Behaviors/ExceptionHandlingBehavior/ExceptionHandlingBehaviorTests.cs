using Birds.Application.Behaviors;
using Birds.Application.Common.Models;
using Birds.Application.Exceptions;
using Birds.Domain.Common.Exceptions;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Birds.Tests.Application.Behaviors.ExceptionHandlingBehavior;

public class ExceptionHandlingBehaviorTests
{
    private readonly Mock<ILogger<ExceptionHandlingBehavior<DummyRequest, Result>>> _loggerMock;

    public ExceptionHandlingBehaviorTests()
    {
        _loggerMock = new Mock<ILogger<ExceptionHandlingBehavior<DummyRequest, Result>>>();
    }

    [Fact]
    public async Task Handle_ShouldReturnResult_WhenNoExceptionThrown()
    {
        // Arrange
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result>(_loggerMock.Object);
        var request = new DummyRequest();
        RequestHandlerDelegate<Result> next = (cancellationToken) => Task.FromResult(Result.Success());

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndLogWarning_WhenValidationExceptionThrown()
    {
        // Arrange
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result>(_loggerMock.Object);
        var request = new DummyRequest();
        RequestHandlerDelegate<Result> next = (cancellationToken) => throw new FluentValidation.ValidationException("Invalid!");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Validation error");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndLogWarning_WhenDomainValidationExceptionThrown()
    {
        // Arrange
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result>(_loggerMock.Object);
        var request = new DummyRequest();
        RequestHandlerDelegate<Result> next = (cancellationToken) => throw new DomainValidationException("Rule broken");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Rule broken");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndLogWarning_WhenNotFoundExceptionThrown()
    {
        // Arrange
        var id = Guid.NewGuid();
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result>(_loggerMock.Object);
        var request = new DummyRequest();
        RequestHandlerDelegate<Result> next = (cancellationToken) => throw new NotFoundException(nameof(id), id);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Not found");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndLogError_WhenUnhandledExceptionThrown()
    {
        // Arrange
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result>(_loggerMock.Object);
        var request = new DummyRequest();
        RequestHandlerDelegate<Result> next = (cancellationToken) => throw new InvalidOperationException("Something failed");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Unexpected error");
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperation_WhenResponseIsNotResult()
    {
        // Arrange
        var logger = new Mock<ILogger<ExceptionHandlingBehavior<DummyRequest, string>>>();
        var behavior = new ExceptionHandlingBehavior<DummyRequest, string>(logger.Object);
        var request = new DummyRequest();
        RequestHandlerDelegate<string> next = (cancellationToken) => throw new Exception("Boom");

        // Act
        Func<Task> act = async () => await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*can only handle responses of type Result*");
    }

    public sealed class DummyRequest
    { }
}