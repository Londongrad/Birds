using Birds.Application.Behaviors;
using Birds.Application.Common.Models;
using Birds.Application.Exceptions;
using Birds.Domain.Common.Exceptions;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Birds.Tests.Application.Behaviors.ExceptionHandlingBehavior;

public class ExceptionHandlingBehaviorGenericTests
{
    private readonly Mock<ILogger<ExceptionHandlingBehavior<DummyRequest, Result<string>>>> _loggerMock;

    public ExceptionHandlingBehaviorGenericTests()
    {
        _loggerMock = new Mock<ILogger<ExceptionHandlingBehavior<DummyRequest, Result<string>>>>();
    }

    [Fact]
    public async Task Handle_ShouldReturnGenericResult_WhenNoExceptionThrown()
    {
        // Arrange
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result<string>>(_loggerMock.Object);
        RequestHandlerDelegate<Result<string>> next = (cancellationToken) => Task.FromResult(Result<string>.Success("ok"));

        // Act
        var result = await behavior.Handle(new DummyRequest(), next, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_ShouldReturnGenericFailureAndLogWarning_WhenValidationExceptionThrown()
    {
        // Arrange
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result<string>>(_loggerMock.Object);
        RequestHandlerDelegate<Result<string>> next = (cancellationToken) => throw new FluentValidation.ValidationException("Invalid value!");

        // Act
        var result = await behavior.Handle(new DummyRequest(), next, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Validation error");
    }

    [Fact]
    public async Task Handle_ShouldReturnGenericFailureAndLogWarning_WhenDomainValidationExceptionThrown()
    {
        // Arrange
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result<string>>(_loggerMock.Object);
        RequestHandlerDelegate<Result<string>> next = (cancellationToken) => throw new DomainValidationException("Broken rule");

        // Act
        var result = await behavior.Handle(new DummyRequest(), next, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Broken rule");
    }

    [Fact]
    public async Task Handle_ShouldReturnGenericFailureAndLogWarning_WhenNotFoundExceptionThrown()
    {
        // Arrange
        var id = Guid.NewGuid();
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result<string>>(_loggerMock.Object);
        RequestHandlerDelegate<Result<string>> next = (cancellationToken) => throw new NotFoundException(nameof(id), id);

        // Act
        var result = await behavior.Handle(new DummyRequest(), next, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Not found");
    }

    [Fact]
    public async Task Handle_ShouldReturnGenericFailureAndLogError_WhenUnhandledExceptionThrown()
    {
        // Arrange
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result<string>>(_loggerMock.Object);
        RequestHandlerDelegate<Result<string>> next = (cancellationToken) => throw new InvalidOperationException("Something went wrong");

        // Act
        var result = await behavior.Handle(new DummyRequest(), next, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Unexpected error");
    }

    public sealed class DummyRequest
    { }
}