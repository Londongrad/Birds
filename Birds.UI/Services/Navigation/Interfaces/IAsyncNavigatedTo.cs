namespace Birds.UI.Services.Navigation.Interfaces
{
    /// <summary>
    /// Defines a contract for view models that require asynchronous initialization
    /// when navigated to their corresponding view.
    ///
    /// Used by the navigation service to trigger data loading or other
    /// preparation logic that should run either once or every time
    /// the view is navigated to.
    /// </summary>
    public interface IAsyncNavigatedTo
    {
        /// <summary>
        /// Called by the navigation service after navigating to this view model.
        /// Should contain asynchronous initialization logic (e.g., data loading).
        /// </summary>
        Task OnNavigatedToAsync();
    }
}