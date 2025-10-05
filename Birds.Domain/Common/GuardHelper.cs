namespace Birds.Domain.Common
{
    public static class GuardHelper
    {
        public static void AgainstNullOrEmpty(string value, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainValidationException($"{argumentName} cannot be null or empty");
        }

        public static void AgainstInvalidDate(DateOnly value, string argumentName, bool allowFuture = false)
        {
            if (value == default)
                throw new DomainValidationException($"{argumentName} cannot be default");

            if (!allowFuture && value > DateOnly.FromDateTime(DateTime.UtcNow))
                throw new DomainValidationException($"{argumentName} cannot be in the future");

            if (value < DateOnly.FromDateTime(new DateTime(2020, 1, 1)))
                throw new DomainValidationException($"{argumentName} is too far in the past");
        }

        public static void AgainstInvalidDate(DateTime value, string argumentName, bool allowFuture = false)
        {
            if (value == default)
                throw new DomainValidationException($"{argumentName} cannot be default");

            if (!allowFuture && value > DateTime.UtcNow)
                throw new DomainValidationException($"{argumentName} cannot be in the future");

            if (value < new DateTime(2020, 1, 1))
                throw new DomainValidationException($"{argumentName} is too far in the past");
        }

        public static void AgainstEmptyGuid(Guid id, string argumentName)
        {
            if (id == Guid.Empty)
                throw new DomainValidationException($"{argumentName} cannot be empty Guid");
        }

        public static void AgainstNull<T>(T obj, string argumentName)
        {
            if (obj is null)
                throw new DomainValidationException($"{argumentName} cannot be null");
        }

        public static void AgainstInvalidEnum<TEnum>(TEnum value, string argumentName) where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(value))
                throw new DomainValidationException($"{argumentName} has invalid value: {value}");
        }

        public static void AgainstInvalidStringToEnum<TEnum>(string value, string argumentName) where TEnum : struct, Enum
        {
            if (!Enum.TryParse<TEnum>(value, true, out _))
                throw new DomainValidationException($"{argumentName} has invalid value: {value}");
        }

        public static void AgainstInvalidDateRange(DateOnly from, DateOnly to, string parameterName)
        {
            if (from > to)
                throw new DomainValidationException("Date range is invalid");
        }
    }
}