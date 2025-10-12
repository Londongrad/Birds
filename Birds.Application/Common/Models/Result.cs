namespace Birds.Application.Common.Models
{
    /// <summary>
    /// Represents a basic operation result indicating success or failure.
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets an error message if the operation failed.
        /// </summary>
        public string? Error { get; }

        protected Result(bool isSuccess, string? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        /// <summary>
        /// Creates a successful result without a value.
        /// </summary>
        public static Result Success() => new(true, null);

        /// <summary>
        /// Creates a failed result with the specified error message.
        /// </summary>
        public static Result Failure(string error) => new(false, error);
    }

    /// <summary>
    /// Represents a result of an operation that can return a value of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type of the value returned if the operation succeeds.</typeparam>
    public class Result<T> : Result
    {
        /// <summary>
        /// Gets the value of the operation if it succeeded.
        /// </summary>
        public T? Value { get; }

        private Result(bool isSuccess, string? error, T? value)
            : base(isSuccess, error)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a successful result with the specified value.
        /// </summary>
        public static Result<T> Success(T value) => new(true, null, value);

        /// <summary>
        /// Creates a failed result with the specified error message.
        /// </summary>
        public static new Result<T> Failure(string error) => new(false, error, default);
    }
}
