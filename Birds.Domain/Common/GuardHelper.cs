namespace Birds.Domain.Common
{
    public static class GuardHelper
    {
        public static void AgainstNullOrEmpty(string value, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{argumentName} cannot be null or empty", argumentName);
        }

        public static void AgainstInvalidDate(DateOnly value, string argumentName, bool allowFuture = false)
        {
            if (value == default)
                throw new ArgumentException($"{argumentName} cannot be default", argumentName);

            if (!allowFuture && value > DateOnly.FromDateTime(DateTime.UtcNow))
                throw new ArgumentException($"{argumentName} cannot be in the future", argumentName);

            if (value < DateOnly.FromDateTime(new DateTime(2020, 1, 1)))
                throw new ArgumentException($"{argumentName} is too far in the past", argumentName);
        }

        public static void AgainstInvalidDate(DateTime value, string argumentName, bool allowFuture = false)
        {
            if (value == default)
                throw new ArgumentException($"{argumentName} cannot be default", argumentName);

            if (!allowFuture && value > DateTime.UtcNow)
                throw new ArgumentException($"{argumentName} cannot be in the future", argumentName);

            if (value < new DateTime(1900, 1, 1))
                throw new ArgumentException($"{argumentName} is too far in the past", argumentName);
        }

        public static void AgainstEmptyGuid(Guid id, string argumentName)
        {
            if (id == Guid.Empty)
                throw new ArgumentException($"{argumentName} cannot be empty Guid", argumentName);
        }

        public static void AgainstNull<T>(T obj, string argumentName)
        {
            if (obj is null)
                throw new ArgumentException($"{argumentName} cannot be null", argumentName);
        }

        public static void AgainstInvalidEnum<TEnum>(TEnum value, string argumentName) where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentException($"{argumentName} has invalid value: {value}", argumentName);
        }

        public static void AgainstInvalidDateRange(DateOnly from, DateOnly to, string parameterName)
        {
            if (from > to)
                throw new ArgumentException("Date range is invalid", parameterName);
        }
    }
}
