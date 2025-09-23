using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;

namespace Birds.UI.Services
{
    public class NavigationService : ObservableObject, INavigationService
    {
        private readonly Dictionary<Type, Func<ObservableObject>> _creators = new();

        private ObservableObject? _current;
        public ObservableObject? Current
        {
            get => _current;
            private set => SetProperty(ref _current, value);
        }

        public ICommand NavigateToCommand { get; }
        public ICommand NavigateToTypeCommand { get; }

        public NavigationService()
        {
            NavigateToCommand = new RelayCommand<object>(vm =>
            {
                if (vm is ObservableObject target)
                    NavigateTo(target);
            });

            NavigateToTypeCommand = new RelayCommand<Type>(t =>
            {
                if (t != null)
                    NavigateToType(t);
            });
        }

        public void NavigateTo(ObservableObject viewModel)
        {
            Current = viewModel;
        }

        public void NavigateToType(Type type)
        {
            if (_creators.TryGetValue(type, out var creator))
                Current = creator();
            else
                Current = null;
        }

        public void AddCreator(Type type, Func<ObservableObject> creator)
        {
            _creators[type] = creator ?? throw new ArgumentNullException(nameof(creator));
        }
    }
}
