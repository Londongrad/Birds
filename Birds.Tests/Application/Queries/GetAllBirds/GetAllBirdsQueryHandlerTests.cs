using Birds.Application.DTOs;
using Birds.Application.Interfaces;
using Birds.Application.Queries.GetAllBirds;
using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Domain.Extensions;
using Birds.Shared.Constants;
using FluentAssertions;
using Moq;

namespace Birds.Tests.Application.Queries.GetAllBirds
{
    public class GetAllBirdsQueryHandlerTests
    {
        private readonly Mock<IBirdRepository> _repo = new();

        [Fact]
        public async Task Handle_Should_Return_Success_With_Mapped_List()
        {
            var birds = new List<Bird>
            {
                Bird.Restore(Guid.NewGuid(), (BirdsName)1, "sparrow",
                    DateOnly.FromDateTime(DateTime.Now.AddDays(-10)), null, true),
                Bird.Restore(Guid.NewGuid(), (BirdsName)5, "tit",
                    DateOnly.FromDateTime(DateTime.Now.AddDays(-20)), null, true),
            }.AsReadOnly();

            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(birds);

            var expected = new List<BirdDTO>
            {
                new(birds[0].Id, birds[0].Name.ToDisplayName(), birds[0].Description, birds[0].Arrival, birds[0].Departure, birds[0].IsAlive, birds[0].CreatedAt, birds[0].UpdatedAt),
                new(birds[1].Id, birds[1].Name.ToDisplayName(), birds[1].Description, birds[1].Arrival, birds[1].Departure, birds[1].IsAlive, birds[1].CreatedAt, birds[1].UpdatedAt),
            }.AsReadOnly();

            var handler = new GetAllBirdsQueryHandler(_repo.Object);

            var result = await handler.Handle(new GetAllBirdsQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Should().HaveCount(2);
            result.Value.Should().BeEquivalentTo(expected);

            _repo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _repo.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_Query_Is_Null()
        {
            var handler = new GetAllBirdsQueryHandler(_repo.Object);

            var result = await handler.Handle(null!, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(ErrorMessages.QueryCannotBeNull);

            _repo.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_Should_Return_Success_With_Empty_List_When_No_Birds()
        {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync((IReadOnlyList<Bird>)new List<Bird>().AsReadOnly());

            var handler = new GetAllBirdsQueryHandler(_repo.Object);

            var result = await handler.Handle(new GetAllBirdsQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Should().BeEmpty();

            _repo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _repo.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_Repository_Returns_Null()
        {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync((IReadOnlyList<Bird>)null!);

            var handler = new GetAllBirdsQueryHandler(_repo.Object);

            var result = await handler.Handle(new GetAllBirdsQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(ErrorMessages.UnexpectedError);

            _repo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _repo.VerifyNoOtherCalls();
        }
    }
}
