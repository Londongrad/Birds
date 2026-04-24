using Birds.Application.Behaviors;
using Birds.Application.Common.Models;
using Birds.Application.Exceptions;
using Birds.Domain.Common.Exceptions;
using Birds.Shared.Constants;
using FluentAssertions;
using FluentValidation;
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
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result<string>>(_loggerMock.Object);
        RequestHandlerDelegate<Result<string>>
            next = cancellationToken => Task.FromResult(Result<string>.Success("ok"));

        var result = await behavior.Handle(new DummyRequest(), next, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_ShouldReturnGenericFailureAndLogWarning_WhenValidationExceptionThrown()
    {
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result<string>>(_loggerMock.Object);
        RequestHandlerDelegate<Result<string>> next = cancellationToken =>
            throw new ValidationException("Invalid value!");

        var result = await behavior.Handle(new DummyRequest(), next, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ExceptionMessages.ValidationFailure("Invalid value!"));
    }

    [Fact]
    public async Task Handle_ShouldReturnGenericFailureAndLogWarning_WhenDomainValidationExceptionThrown()
    {
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result<string>>(_loggerMock.Object);
        RequestHandlerDelegate<Result<string>> next = cancellationToken =>
            throw new DomainValidationException("Broken rule");

        var result = await behavior.Handle(new DummyRequest(), next, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Broken rule");
    }

    [Fact]
    public async Task Handle_ShouldReturnGenericFailureAndLogWarning_WhenNotFoundExceptionThrown()
    {
        var id = Guid.NewGuid();
        var notFound = new NotFoundException(nameof(id), id);
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result<string>>(_loggerMock.Object);
        RequestHandlerDelegate<Result<string>> next = cancellationToken => throw notFound;

        var result = await behavior.Handle(new DummyRequest(), next, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ExceptionMessages.NotFoundFailure(notFound.Message));
    }

    [Fact]
    public async Task Handle_ShouldReturnGenericFailureAndLogError_WhenUnhandledExceptionThrown()
    {
        var behavior = new ExceptionHandlingBehavior<DummyRequest, Result<string>>(_loggerMock.Object);
        RequestHandlerDelegate<Result<string>> next = cancellationToken =>
            throw new InvalidOperationException("Something went wrong");

        var result = await behavior.Handle(new DummyRequest(), next, CancellationToken.None);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ErrorMessages.UnexpectedError);
        result.Error.Should().NotContain("Something went wrong");
    }

    public sealed class DummyRequest
    {
    }
}
