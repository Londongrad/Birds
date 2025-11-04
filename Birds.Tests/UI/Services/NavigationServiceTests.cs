using Birds.Tests.Helpers;
using Birds.UI.Services.Navigation;
using FluentAssertions;
using MediatR;
using Moq;

namespace Birds.Tests.UI.Services
{
    public class NavigationServiceTests
    {
        [Fact]
        public async Task NavigateTo_SetsCurrent_And_Calls_OnNavigatedToAsync()
        {
            // Arrange
            var mediator = new Mock<IMediator>();
            var nav = new NavigationService(mediator.Object);
            var vm = new DummyVm();

            // Act
            await nav.NavigateTo(vm);

            // Assert
            nav.Current.Should().BeSameAs(vm);
            vm.Calls.Should().Be(1);
        }

        [Fact]
        public async Task NavigateTo_Ignores_NonObservableObject()
        {
            // Arrange
            var mediator = new Mock<IMediator>();
            var nav = new NavigationService(mediator.Object);

            // Act
            await nav.NavigateTo(new object()); // not ObservableObject

            // Assert
            nav.Current.Should().BeNull();
        }

        [Fact]
        public async Task NavigateToType_Uses_Registered_Factory_And_Sets_Current()
        {
            // Arrange
            var mediator = new Mock<IMediator>();
            var nav = new NavigationService(mediator.Object);

            nav.AddCreator<DummyVm>(() => new DummyVm());

            // Act
            await nav.NavigateToType(typeof(DummyVm));

            // Assert
            nav.Current.Should().NotBeNull().And.BeOfType<DummyVm>();
            ((DummyVm)nav.Current!).Calls.Should().Be(1); // OnNavigatedToAsync called
        }

        [Fact]
        public async Task NavigateToType_NoFactory_DoesNothing()
        {
            // Arrange
            var mediator = new Mock<IMediator>();
            var nav = new NavigationService(mediator.Object);

            // Act
            await nav.NavigateToType(typeof(DummyVm)); // Factory not registered

            // Assert
            nav.Current.Should().BeNull();
        }

        [Fact]
        public async Task Commands_Invoke_Their_Methods()
        {
            // Arrange
            var mediator = new Mock<IMediator>();
            var nav = new NavigationService(mediator.Object);
            nav.AddCreator<DummyVm>(() => new DummyVm());
            var vm = new DummyVm();

            // Act
            await nav.NavigateToCommand.ExecuteAsync(vm);
            await nav.NavigateToTypeCommand.ExecuteAsync(typeof(DummyVm));

            // Assert
            nav.Current.Should().BeOfType<DummyVm>();
            ((DummyVm)nav.Current!).Calls.Should().Be(1);
        }
    }
}