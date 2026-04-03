using Birds.Application.Behaviors;
using Birds.Application.Commands.CreateBird;
using Birds.Application.Common.Models;
using Birds.Application.DTOs;
using Birds.Domain.Enums;
using Birds.Shared.Constants;
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
            var validators = new List<IValidator<CreateBirdCommand>> { new CreateBirdCommandValidator() };
            var behavior = new ValidationBehavior<CreateBirdCommand, Result<BirdDTO>>(validators);

            var cmd = new CreateBirdCommand(
                (BirdsName)1,
                "ok",
                DateOnly.FromDateTime(DateTime.Now));

            var nextCalled = false;
            RequestHandlerDelegate<Result<BirdDTO>> next = (cancellationToken) =>
            {
                nextCalled = true;
                return Task.FromResult(Result<BirdDTO>.Success(new BirdDTO(
                    Guid.NewGuid(), cmd.Name.ToString(), cmd.Description, cmd.Arrival, cmd.Departure, cmd.IsAlive, null, null)));
            };

            var result = await behavior.Handle(cmd, next, CancellationToken.None);

            nextCalled.Should().BeTrue();
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_InvalidRequest_ThrowsValidationException_AndDoesNotCallNext()
        {
            var validators = new List<IValidator<CreateBirdCommand>> { new CreateBirdCommandValidator() };
            var behavior = new ValidationBehavior<CreateBirdCommand, Result<BirdDTO>>(validators);

            var invalid = new CreateBirdCommand(
                (BirdsName)1,
                new string('x', BirdValidationRules.DescriptionMaxLength + 1),
                DateOnly.FromDateTime(DateTime.Now.AddDays(1))
            );

            var nextCalled = false;
            RequestHandlerDelegate<Result<BirdDTO>> next = (cancellationToken) =>
            {
                nextCalled = true;
                return Task.FromResult(Result<BirdDTO>.Success(default!));
            };

            Func<Task> act = async () => await behavior.Handle(invalid, next, CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>()
                .WithMessage("*Arrival date cannot be in the future*")
                .WithMessage($"*Description must not exceed {BirdValidationRules.DescriptionMaxLength} characters*");
            nextCalled.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_NoValidators_JustCallsNext()
        {
            var behavior = new ValidationBehavior<CreateBirdCommand, Result<BirdDTO>>(Enumerable.Empty<IValidator<CreateBirdCommand>>());

            var cmd = new CreateBirdCommand((BirdsName)1, null, DateOnly.FromDateTime(DateTime.Now));

            var nextCalled = false;
            RequestHandlerDelegate<Result<BirdDTO>> next = (cancellationToken) =>
            {
                nextCalled = true;
                return Task.FromResult(Result<BirdDTO>.Success(default!));
            };

            var result = await behavior.Handle(cmd, next, CancellationToken.None);

            nextCalled.Should().BeTrue();
            result.IsSuccess.Should().BeTrue();
        }
    }
}
