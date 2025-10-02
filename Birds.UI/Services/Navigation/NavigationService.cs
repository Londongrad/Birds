using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.Windows;

namespace Birds.UI.Services.Navigation
{
    public class NavigationService : ObservableObject, INavigationService
    {
        #region [ Fields ]

        private readonly Dictionary<Type, Func<ObservableObject>> _creators = new();
        private readonly Dictionary<Type, Func<Window>> _windowCreators = new();
        private readonly IMediator _mediator;
        private ObservableObject? _current;

        #endregion [ Fields ]

        public NavigationService(IMediator mediator)
        {
            NavigateToCommand = new AsyncRelayCommand<object?>(NavigateTo);
            NavigateToTypeCommand = new AsyncRelayCommand<Type?>(NavigateToType);
            _mediator = mediator;
        }

        #region [ Properties ]

        public ObservableObject? Current
        {
            get => _current;
            private set => SetProperty(ref _current, value);
        }

        public IAsyncRelayCommand<object?> NavigateToCommand { get; }
        public IAsyncRelayCommand<Type?> NavigateToTypeCommand { get; }

        #endregion [ Properties ]

        #region [ Methods ]

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

        public async Task OpenWindow(ObservableObject viewModel)
        {
            if (_windowCreators.TryGetValue(viewModel.GetType(), out var windowFactory))
            {
                var window = windowFactory();
                window.DataContext = viewModel;
                window.Show();

                await _mediator.Publish(new NavigatedEvent(window, viewModel));
            }
        }

        public void AddCreator<TViewModel>(Func<ObservableObject> creator)
            where TViewModel : ObservableObject
        {
            _creators[typeof(TViewModel)] = creator ?? throw new ArgumentNullException(nameof(creator));
        }

        public void AddWindow<TViewModel>(Func<Window> windowFactory)
            where TViewModel : ObservableObject
        {
            _windowCreators[typeof(TViewModel)] = windowFactory ?? throw new ArgumentNullException(nameof(windowFactory));
        }

        #endregion [ Methods ]
    }
}