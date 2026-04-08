using Birds.Application.Behaviors;
using Birds.Application.Common.Models;
using Birds.Application.Exceptions;
using Birds.Domain.Common.Exceptions;
using Birds.Shared.Constants;
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
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result>(_loggerMock.Object);
        var request = new DummyRequest();
        RequestHandlerDelegate<Result> next = cancellationToken => Task.FromResult(Result.Success());

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndLogWarning_WhenValidationExceptionThrown()
    {
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result>(_loggerMock.Object);
        var request = new DummyRequest();
        RequestHandlerDelegate<Result> next = cancellationToken => throw new FluentValidation.ValidationException("Invalid!");

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ExceptionMessages.ValidationFailure("Invalid!"));
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndLogWarning_WhenDomainValidationExceptionThrown()
    {
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result>(_loggerMock.Object);
        var request = new DummyRequest();
        RequestHandlerDelegate<Result> next = cancellationToken => throw new DomainValidationException("Rule broken");

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Rule broken");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndLogWarning_WhenNotFoundExceptionThrown()
    {
        var id = Guid.NewGuid();
        var notFound = new NotFoundException(nameof(id), id);
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result>(_loggerMock.Object);
        var request = new DummyRequest();
        RequestHandlerDelegate<Result> next = cancellationToken => throw notFound;

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ExceptionMessages.NotFoundFailure(notFound.Message));
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndLogError_WhenUnhandledExceptionThrown()
    {
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result>(_loggerMock.Object);
        var request = new DummyRequest();
        RequestHandlerDelegate<Result> next = cancellationToken => throw new InvalidOperationException("Something failed");

        var result = await behavior.Handle(request, next, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ExceptionMessages.UnexpectedFailure("Something failed"));
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperation_WhenResponseIsNotResult()
    {
        var logger = new Mock<ILogger<ExceptionHandlingBehavior<DummyRequest, string>>>();
        var behavior = new ExceptionHandlingBehavior<DummyRequest, string>(logger.Object);
        var request = new DummyRequest();
        RequestHandlerDelegate<string> next = cancellationToken => throw new Exception("Boom");

        Func<Task> act = async () => await behavior.Handle(request, next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(ExceptionMessages.InvalidOperation("String"));
    }

    public sealed class DummyRequest
    { }
}
