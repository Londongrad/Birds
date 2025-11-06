using Birds.Application.DTOs;
using Birds.UI.Services.Factories.BirdViewModelFactory;
using Birds.UI.Services.Managers.Bird;
using Birds.UI.Services.Notification.Interfaces;
using Birds.UI.ViewModels;
using FluentAssertions;
using Moq;

namespace Birds.Tests.UI.Services
{
    public class BirdViewModelFactoryTests
    {
        private readonly Mock<IBirdManager> _manager = new();
        private readonly Mock<INotificationService> _notify = new();

        [Fact]
        public void Ctor_Throws_When_Dependencies_Null()
        {
            Action a1 = () => new BirdViewModelFactory(null!, _notify.Object);
            Action a2 = () => new BirdViewModelFactory(_manager.Object, null!);

            a1.Should().Throw<ArgumentNullException>().WithParameterName("birdManager");
            a2.Should().Throw<ArgumentNullException>().WithParameterName("notificationService");
        }

        [Fact]
        public void Create_Returns_ViewModel_And_Does_Not_Call_Services()
        {
            var factory = new BirdViewModelFactory(_manager.Object, _notify.Object);

            var dto = new BirdDTO(
                Id: Guid.NewGuid(),
                Name: "Sparrow",
                Description: "desc",
                Arrival: DateOnly.FromDateTime(DateTime.Now.AddDays(-2)),
                Departure: null,
                IsAlive: true,
                CreatedAt: DateTime.Now.AddDays(-2),
                UpdatedAt: null);

            var vm = factory.Create(dto);

            vm.Should().NotBeNull().And.BeOfType<BirdViewModel>();
            vm.Dto.Should().Be(dto);

            _manager.VerifyNoOtherCalls();
            _notify.VerifyNoOtherCalls();
        }

        [Fact]
        public void Create_Throws_When_Dto_Null()
        {
            var factory = new BirdViewModelFactory(_manager.Object, _notify.Object);
            Action act = () => factory.Create(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("dto");
        }
    }
}