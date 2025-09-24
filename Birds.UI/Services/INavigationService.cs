using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Birds.UI.Services
{
    public interface INavigationService
    {
        /// <summary>
        /// Текущая активная ViewModel, отображаемая во View.
        /// </summary>
        ObservableObject? Current { get; }

        /// <summary>
        /// Навигация к переданной ViewModel.
        /// </summary>
        Task NavigateTo(object? viewModel);

        /// <summary>
        /// Навигация по типу ViewModel (создаётся через зарегистрированный фабричный метод).
        /// </summary>
        Task NavigateToType(Type? type);

        /// <summary>
        /// Внутренняя реализация навигации для ViewModel (установка Current + вызов хуков).
        /// </summary>
        Task NavigateToInternal(ObservableObject viewModel);

        /// <summary>
        /// Регистрирует фабрику для создания ViewModel по её типу.
        /// </summary>
        void AddCreator(Type type, Func<ObservableObject> creator);

        /// <summary>
        /// Команда для навигации по конкретному экземпляру ViewModel.
        /// </summary>
        IAsyncRelayCommand<object?> NavigateToCommand { get; }

        /// <summary>
        /// Команда для навигации по типу ViewModel.
        /// </summary>
        IAsyncRelayCommand<Type?> NavigateToTypeCommand { get; }
    }
}
