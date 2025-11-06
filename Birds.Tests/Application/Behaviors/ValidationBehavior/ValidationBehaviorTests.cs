using Birds.Application.Behaviors;
using Birds.Application.Commands.CreateBird;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using MediatR;

namespace Birds.Tests.Application.Behaviors.ValidationBehavior
{
    public class ValidationBehaviorTests
    {
        [Fact]
        public async Task Handle_ValidRequest_PassesThroughAndCallsNext()
        {
            // Arrange
            var validators = new List<IValidator<CreateBirdCommand>> { new CreateBirdCommandValidator() };
            var behavior = new ValidationBehavior<CreateBirdCommand, Result<BirdDTO>>(validators);

            var cmd = new CreateBirdCommand(
                BirdsName.Воробей,
                "ok",
                DateOnly.FromDateTime(DateTime.Now));

            var nextCalled = false;
            RequestHandlerDelegate<Result<BirdDTO>> next = (cancellationToken) =>
            {
                nextCalled = true;
                return Task.FromResult(Result<BirdDTO>.Success(new BirdDTO(
                    Guid.NewGuid(), cmd.Name.ToString(), cmd.Description, cmd.Arrival, cmd.Departure, cmd.IsAlive, null, null)));
            };

            // Act
            var result = await behavior.Handle(cmd, next, CancellationToken.None);

            // Assert
            nextCalled.Should().BeTrue();
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_InvalidRequest_ThrowsValidationException_AndDoesNotCallNext()
        {
            // Arrange
            var validators = new List<IValidator<CreateBirdCommand>> { new CreateBirdCommandValidator() };
            var behavior = new ValidationBehavior<CreateBirdCommand, Result<BirdDTO>>(validators);

            var invalid = new CreateBirdCommand(
                BirdsName.Воробей,
                new string('x', 101),                         // слишком длинно
                DateOnly.FromDateTime(DateTime.Now.AddDays(1)) // будущее
            );

            var nextCalled = false;
            RequestHandlerDelegate<Result<BirdDTO>> next = (cancellationToken) =>
            {
                nextCalled = true;
                return Task.FromResult(Result<BirdDTO>.Success(default!));
            };

            // Act
            Func<Task> act = async () => await behavior.Handle(invalid, next, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationException>()
                .WithMessage("*Arrival date cannot be in the future*")
                .WithMessage("*Description must not exceed 100 characters*"); // обе ошибки агрегируются
            nextCalled.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_NoValidators_JustCallsNext()
        {
            // Arrange
            var behavior = new ValidationBehavior<CreateBirdCommand, Result<BirdDTO>>(Enumerable.Empty<IValidator<CreateBirdCommand>>());

            var cmd = new CreateBirdCommand(BirdsName.Воробей, null, DateOnly.FromDateTime(DateTime.Now));

            var nextCalled = false;
            RequestHandlerDelegate<Result<BirdDTO>> next = (cancellationToken) =>
            {
                nextCalled = true;
                return Task.FromResult(Result<BirdDTO>.Success(default!));
            };

            // Act
            var result = await behavior.Handle(cmd, next, CancellationToken.None);

            // Assert
            nextCalled.Should().BeTrue();
            result.IsSuccess.Should().BeTrue();
        }
    }
}