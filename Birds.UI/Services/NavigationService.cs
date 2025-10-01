using Birds.UI.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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

        public IAsyncRelayCommand<object?> NavigateToCommand { get; }
        public IAsyncRelayCommand<Type?> NavigateToTypeCommand { get; }

        public NavigationService()
        {
            NavigateToCommand = new AsyncRelayCommand<object?>(NavigateTo);
            NavigateToTypeCommand = new AsyncRelayCommand<Type?>(NavigateToType);
        }

        public async Task NavigateTo(object? vm)
        {
            if (vm is ObservableObject target)
                await NavigateToInternal(target);
        }

        public async Task NavigateToType(Type? type)
        {
            if (type == null)
                return;

            if (_creators.TryGetValue(type, out var creator))
            {
                var vm = creator();
                await NavigateToInternal(vm);
            }
        }

        public async Task NavigateToInternal(ObservableObject viewModel)
        {
            Current = viewModel;

            if (viewModel is IAsyncNavigatedTo asyncVm)
            {
                await asyncVm.OnNavigatedToAsync();
            }
        }

        public void AddCreator(Type type, Func<ObservableObject> creator)
        {
            _creators[type] = creator ?? throw new ArgumentNullException(nameof(creator));
        }
    }
}