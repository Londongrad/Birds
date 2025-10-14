namespace Birds.UI.Enums
{
    /// <summary>
    /// Represents the current data loading state of the <see cref="IBirdStore"/>.
    /// Used to determine whether the bird collection is initialized, successfully loaded, or failed.
    /// </summary>
    public enum LoadState
    {
        /// <summary>
        /// The store has not been initialized yet.
        /// </summary>
        Uninitialized = 0,

        /// <summary>
        /// The store is currently loading data from the database.
        /// </summary>
        Loading = 1,

        /// <summary>
        /// The store has successfully loaded all bird data.
        /// </summary>
        Loaded = 2,

        /// <summary>
        /// The last loading attempt failed.
        /// </summary>
        Failed = 3
    }
}
