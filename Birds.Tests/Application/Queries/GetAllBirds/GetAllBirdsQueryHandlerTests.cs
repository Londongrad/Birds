using AutoMapper;
using Birds.Application.DTOs;
using Birds.Application.Interfaces;
using Birds.Application.Queries.GetAllBirds;
using Birds.Domain.Entities;
using Birds.Domain.Enums;
using Birds.Shared.Constants;
using FluentAssertions;
using Moq;

namespace Birds.Tests.Application.Queries.GetAllBirds
{
    public class GetAllBirdsQueryHandlerTests
    {
        private readonly Mock<IBirdRepository> _repo = new();
        private readonly Mock<IMapper> _mapper = new();

        [Fact]
        public async Task Handle_Should_Return_Success_With_Mapped_List()
        {
            // Arrange
            var birds = new List<Bird>
        {
            Bird.Restore(Guid.NewGuid(), BirdsName.Воробей, "sparrow",
                DateOnly.FromDateTime(DateTime.Now.AddDays(-10)), null, true),
            Bird.Restore(Guid.NewGuid(), BirdsName.Большак, "tit",
                DateOnly.FromDateTime(DateTime.Now.AddDays(-20)), null, true),
        }.AsReadOnly();

            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(birds);

            var mapped = new List<BirdDTO>
        {
            new(birds[0].Id, birds[0].Name.ToString(), birds[0].Description, birds[0].Arrival, birds[0].Departure, birds[0].IsAlive, birds[0].CreatedAt, birds[0].UpdatedAt),
            new(birds[1].Id, birds[1].Name.ToString(), birds[1].Description, birds[1].Arrival, birds[1].Departure, birds[1].IsAlive, birds[1].CreatedAt, birds[1].UpdatedAt),
        }.AsReadOnly();

            _mapper.Setup(m => m.Map<IReadOnlyList<BirdDTO>>(birds))
                   .Returns(mapped);

            var handler = new GetAllBirdsQueryHandler(_repo.Object, _mapper.Object);

            // Act
            var result = await handler.Handle(new GetAllBirdsQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Should().HaveCount(2);
            result.Value.Should().BeEquivalentTo(mapped);

            _repo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mapper.Verify(m => m.Map<IReadOnlyList<BirdDTO>>(birds), Times.Once);
            _repo.VerifyNoOtherCalls();
            _mapper.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_Query_Is_Null()
        {
            // Arrange
            var handler = new GetAllBirdsQueryHandler(_repo.Object, _mapper.Object);

            // Act
            var result = await handler.Handle(null!, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(ErrorMessages.QueryCannotBeNull);

            _repo.VerifyNoOtherCalls();
            _mapper.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Handle_Should_Return_Failure_When_No_Birds()
        {
            // Arrange
            _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync((IReadOnlyList<Bird>)new List<Bird>().AsReadOnly());

            var handler = new GetAllBirdsQueryHandler(_repo.Object, _mapper.Object);

            // Act
            var result = await handler.Handle(new GetAllBirdsQuery(), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(ErrorMessages.NoBirdsFound);

            _repo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mapper.Verify(m => m.Map<IReadOnlyList<BirdDTO>>(It.IsAny<object>()), Times.Never);
            _repo.VerifyNoOtherCalls();
            _mapper.VerifyNoOtherCalls();
        }
    }
}