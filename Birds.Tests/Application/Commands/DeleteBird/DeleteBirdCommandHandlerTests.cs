using Birds.Application.Commands.DeleteBird;
using Birds.Application.Exceptions;
using Birds.Application.Interfaces;
using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Shared.Constants;
using FluentAssertions;
using Moq;

namespace Birds.Tests.Application.Commands.DeleteBird
{
    public class DeleteBirdCommandHandlerTests
    {
        private readonly Mock<IBirdRepository> _repo = new();

        [Fact]
        public async Task Handle_Should_Remove_Bird_And_Return_Success()
        {
            // Arrange
            var id = Guid.NewGuid();
            var stored = Bird.Restore(id, BirdsName.Воробей, "desc",
                DateOnly.FromDateTime(DateTime.Now.AddDays(-10)), null, true);

            _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(stored);

            var handler = new DeleteBirdCommandHandler(_repo.Object);
            var cmd = new DeleteBirdCommand(id);

            // Act
            var result = await handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _repo.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
            _repo.Verify(r => r.RemoveAsync(stored, It.IsAny<CancellationToken>()), Times.Once);
            _repo.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_Request_Is_Null()
        {
            var handler = new DeleteBirdCommandHandler(_repo.Object);

            var result = await handler.Handle(null!, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(ErrorMessages.RequestCannotBeNull);
            _repo.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_Should_Throw_NotFound_When_Repo_Throws_NotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new NotFoundException(nameof(Bird), id));

            var handler = new DeleteBirdCommandHandler(_repo.Object);
            var cmd = new DeleteBirdCommand(id);

            // Act
            Func<Task> act = async () => await handler.Handle(cmd, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
            _repo.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
            _repo.Verify(r => r.RemoveAsync(It.IsAny<Bird>(), It.IsAny<CancellationToken>()), Times.Never);
            _repo.VerifyNoOtherCalls();
        }
    }
}