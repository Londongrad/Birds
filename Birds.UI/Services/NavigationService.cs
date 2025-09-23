using Birds.UI.Common;
using System.Windows.Input;

namespace Birds.UI.Services
{
    public class NavigationService : ViewModelBase, INavigationService
    {
        private readonly Dictionary<Type, Func<ViewModelBase>> _typeCreators = new();

        private ViewModelBase? _current;
        public ViewModelBase? Current
        {
            get => _current;
            protected set => SetProperty(ref _current, value);
        }

        public ICommand NavigateToCommand { get; }
        public ICommand NavigateToTypeCommand { get; }

        public NavigationService()
        {
            NavigateToCommand = new RelayCommand(vm =>
            {
                if (vm is ViewModelBase target)
                    NavigateTo(target);
            });
            NavigateToTypeCommand = new RelayCommand<Type>(t =>
            {
                if (t != null)
                    NavigateToType(t);
            });
        }

        public void NavigateTo(ViewModelBase viewModel) => Current = viewModel;

        public void NavigateToType(Type type)
        {
            if (_typeCreators.TryGetValue(type, out var creator))
            {
                Current = creator();
            }
            else
            {
                Current = null;
            }
        }

        public void AddCreator(Type type, Func<ViewModelBase> creator)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(creator);

            _typeCreators[type] = creator;
        }
    }
}
