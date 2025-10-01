using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace Birds.UI.Services.Navigation
{
    /// <summary>
    /// Сервис навигации для управления текущей ViewModel и открытия отдельных окон.
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Текущая активная ViewModel, отображаемая во View (например, в ContentControl).
        /// </summary>
        ObservableObject? Current { get; }

        /// <summary>
        /// Навигация к переданной ViewModel внутри текущего окна.
        /// </summary>
        Task NavigateTo(object? viewModel);

        /// <summary>
        /// Навигация к ViewModel по её типу (создаётся через зарегистрированный фабричный метод)
        /// и отображается внутри текущего окна.
        /// </summary>
        Task NavigateToType(Type? type);

        /// <summary>
        /// Внутренняя реализация навигации для ViewModel (установка Current + вызов хуков OnNavigatedToAsync).
        /// </summary>
        Task NavigateToInternal(ObservableObject viewModel);

        /// <summary>
        /// Команда для навигации по конкретному экземпляру ViewModel (для биндинга в XAML).
        /// </summary>
        IAsyncRelayCommand<object?> NavigateToCommand { get; }

        /// <summary>
        /// Команда для навигации по типу ViewModel (для биндинга в XAML).
        /// </summary>
        IAsyncRelayCommand<Type?> NavigateToTypeCommand { get; }

        /// <summary>
        /// Открывает отдельное окно для указанной ViewModel.
        /// ViewModel отображается в новом экземпляре Window.
        /// </summary>
        Task OpenWindow(ObservableObject viewModel);

        /// <summary>
        /// Регистрирует фабрику для создания ViewModel по её типу.
        /// Используется при вызове NavigateToType.
        /// </summary>
        void AddCreator<TViewModel>(Func<ObservableObject> creator) where TViewModel : ObservableObject;

        /// <summary>
        /// Регистрирует фабрику для создания окна по типу ViewModel.
        /// Используется при вызове OpenWindow.
        /// </summary>
        void AddWindow<TViewModel>(Func<Window> windowFactory) where TViewModel : ObservableObject;
    }
}