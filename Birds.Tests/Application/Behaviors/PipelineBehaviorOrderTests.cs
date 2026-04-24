using Birds.Application;
using Birds.Application.Behaviors;
using Birds.Application.Commands.CreateBird;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Application.Interfaces;
using Birds.Domain.Common;
using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Shared.Constants;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Birds.Tests.Application.Behaviors;

public class PipelineBehaviorOrderTests
{
    [Fact]
    public void AddApplication_Should_Register_Pipeline_Behaviors_In_Intended_Order()
    {
        var repository = new Mock<IBirdRepository>();
        using var provider = BuildProvider(repository.Object, out _);

        var behaviors = provider
            .GetServices<IPipelineBehavior<CreateBirdCommand, Result<BirdDTO>>>()
            .Select(behavior => behavior.GetType().GetGenericTypeDefinition())
            .ToList();

        behaviors.Should().Equal(
            typeof(ExceptionHandlingBehavior<,>),
            typeof(LoggingBehavior<,>),
            typeof(ValidationBehavior<,>));
    }

    [Fact]
    public async Task Send_InvalidRequest_Should_Return_Failure_Without_Executing_Handler()
    {
        var repository = new Mock<IBirdRepository>(MockBehavior.Strict);
        using var provider = BuildProvider(repository.Object, out var loggerProvider);
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new CreateBirdCommand(
            BirdSpecies.Sparrow,
            new string('x', BirdValidationRules.DescriptionMaxLength + 1),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)));

        var result = await mediator.Send(command);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AppErrorCodes.ApplicationValidationFailed);
        result.Error.Should().Contain("Arrival date cannot be in the future");
        result.Error.Should().Contain(
            $"Description must not exceed {BirdValidationRules.DescriptionMaxLength} characters");
        result.AppError!.ValidationErrors.Should().ContainKey(nameof(CreateBirdCommand.Arrival));
        result.AppError.ValidationErrors.Should().ContainKey(nameof(CreateBirdCommand.Description));
        repository.Verify(
            x => x.AddAsync(It.IsAny<Bird>(), It.IsAny<CancellationToken>()),
            Times.Never);
        loggerProvider.Entries.Should().Contain(entry =>
            entry.Category.Contains(nameof(LoggingBehavior<CreateBirdCommand, Result<BirdDTO>>)) &&
            entry.Message.Contains("Handling", StringComparison.OrdinalIgnoreCase));
        loggerProvider.Entries.Should().Contain(entry =>
            entry.Category.Contains(nameof(LoggingBehavior<CreateBirdCommand, Result<BirdDTO>>)) &&
            entry.Message.Contains("Failed", StringComparison.OrdinalIgnoreCase));
        loggerProvider.Entries.Should().NotContain(entry =>
            entry.Category.Contains(nameof(LoggingBehavior<CreateBirdCommand, Result<BirdDTO>>)) &&
            entry.Message.Contains("Handled", StringComparison.OrdinalIgnoreCase));
        loggerProvider.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Warning &&
            entry.Category.Contains(nameof(ExceptionHandlingBehavior<CreateBirdCommand, Result<BirdDTO>>)) &&
            entry.Message.Contains("Validation failed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Send_HandlerException_Should_Return_Safe_Failure_And_Log_Technical_Details()
    {
        var repository = new Mock<IBirdRepository>();
        repository
            .Setup(x => x.AddAsync(It.IsAny<Bird>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("secret connection detail"));
        using var provider = BuildProvider(repository.Object, out var loggerProvider);
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new CreateBirdCommand(
            BirdSpecies.Sparrow,
            "ok",
            DateOnly.FromDateTime(DateTime.Today));

        var result = await mediator.Send(command);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(AppErrorCodes.ApplicationUnexpected);
        result.Error.Should().Be(ErrorMessages.UnexpectedError);
        result.Error.Should().NotContain("secret connection detail");
        repository.Verify(
            x => x.AddAsync(It.IsAny<Bird>(), It.IsAny<CancellationToken>()),
            Times.Once);
        loggerProvider.Entries.Should().Contain(entry =>
            entry.Category.Contains(nameof(LoggingBehavior<CreateBirdCommand, Result<BirdDTO>>)) &&
            entry.Message.Contains("Failed", StringComparison.OrdinalIgnoreCase));
        loggerProvider.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Error &&
            entry.Category.Contains(nameof(ExceptionHandlingBehavior<CreateBirdCommand, Result<BirdDTO>>)) &&
            entry.Exception is InvalidOperationException &&
            entry.Exception.Message == "secret connection detail");
    }

    [Fact]
    public async Task Send_ValidRequest_Should_Log_Start_And_Success()
    {
        var repository = new Mock<IBirdRepository>();
        repository
            .Setup(x => x.AddAsync(It.IsAny<Bird>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        using var provider = BuildProvider(repository.Object, out var loggerProvider);
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new CreateBirdCommand(
            BirdSpecies.Sparrow,
            "ok",
            DateOnly.FromDateTime(DateTime.Today));

        var result = await mediator.Send(command);

        result.IsSuccess.Should().BeTrue();
        loggerProvider.Entries.Should().Contain(entry =>
            entry.Category.Contains(nameof(LoggingBehavior<CreateBirdCommand, Result<BirdDTO>>)) &&
            entry.Message.Contains("Handling", StringComparison.OrdinalIgnoreCase));
        loggerProvider.Entries.Should().Contain(entry =>
            entry.Category.Contains(nameof(LoggingBehavior<CreateBirdCommand, Result<BirdDTO>>)) &&
            entry.Message.Contains("Handled", StringComparison.OrdinalIgnoreCase));
        loggerProvider.Entries.Should().NotContain(entry =>
            entry.Category.Contains(nameof(LoggingBehavior<CreateBirdCommand, Result<BirdDTO>>)) &&
            entry.Message.Contains("Failed", StringComparison.OrdinalIgnoreCase));
    }

    private static ServiceProvider BuildProvider(
        IBirdRepository repository,
        out TestLoggerProvider loggerProvider)
    {
        var services = new ServiceCollection();
        var provider = new TestLoggerProvider();

        services.AddSingleton(repository);
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(provider);
            builder.SetMinimumLevel(LogLevel.Trace);
        });
        services.AddApplication();

        loggerProvider = provider;
        return services.BuildServiceProvider();
    }

    private sealed record LogEntry(
        LogLevel Level,
        string Category,
        string Message,
        Exception? Exception);

    private sealed class TestLoggerProvider : ILoggerProvider
    {
        private readonly List<LogEntry> _entries = new();

        public IReadOnlyList<LogEntry> Entries
        {
            get
            {
                lock (_entries)
                {
                    return _entries.ToArray();
                }
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(categoryName, _entries);
        }

        public void Dispose()
        {
        }
    }

    private sealed class TestLogger(
        string category,
        List<LogEntry> entries) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (entries)
            {
                entries.Add(new LogEntry(logLevel, category, formatter(state, exception), exception));
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
