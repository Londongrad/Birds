using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace Birds.UI.Services.Navigation
{
    /// <summary>
    /// Navigation service for managing the current ViewModel and opening separate windows.
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// The currently active ViewModel displayed in the View (e.g., inside a ContentControl).
        /// </summary>
        ObservableObject? Current { get; }

        /// <summary>
        /// Navigates to the specified ViewModel within the current window.
        /// </summary>
        Task NavigateTo(object? viewModel);

        /// <summary>
        /// Navigates to a ViewModel by its type (created via a registered factory method)
        /// and displays it within the current window.
        /// </summary>
        Task NavigateToType(Type? type);

        /// <summary>
        /// Internal navigation logic for ViewModels (sets <see cref="Current"/> and triggers <c>OnNavigatedToAsync</c> hooks).
        /// </summary>
        Task NavigateToInternal(ObservableObject viewModel);

        /// <summary>
        /// Command for navigating to a specific ViewModel instance (for XAML binding).
        /// </summary>
        IAsyncRelayCommand<object?> NavigateToCommand { get; }

        /// <summary>
        /// Command for navigating by ViewModel type (for XAML binding).
        /// </summary>
        IAsyncRelayCommand<Type?> NavigateToTypeCommand { get; }

        /// <summary>
        /// Opens a separate window for the specified ViewModel.
        /// The ViewModel is displayed in a new <see cref="Window"/> instance.
        /// </summary>
        Task OpenWindow(ObservableObject viewModel);

        /// <summary>
        /// Registers a factory for creating a ViewModel by its type.
        /// Used when calling <see cref="NavigateToType"/>.
        /// </summary>
        void AddCreator<TViewModel>(Func<ObservableObject> creator) where TViewModel : ObservableObject;

        /// <summary>
        /// Registers a factory for creating a window based on the ViewModel type.
        /// Used when calling <see cref="OpenWindow"/>.
        /// </summary>
        void AddWindow<TViewModel>(Func<Window> windowFactory) where TViewModel : ObservableObject;
    }
}