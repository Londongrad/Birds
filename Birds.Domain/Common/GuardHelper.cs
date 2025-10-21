using Birds.Domain.Common.Exceptions;

namespace Birds.Domain.Common
{
    /// <summary>
    /// Provides helper methods to enforce domain invariants and guard against invalid values.
    /// </summary>
    public static class GuardHelper
    {
        /// <summary>
        /// Ensures that the provided string is not null, empty, or whitespace.
        /// </summary>
        /// <param name="value">The string to validate.</param>
        /// <param name="argumentName">The name of the argument being validated.</param>
        /// <exception cref="DomainValidationException">Thrown when the string is null, empty, or whitespace.</exception>
        public static void AgainstNullOrEmpty(string value, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainValidationException($"{argumentName} cannot be null or empty");
        }

        /// <summary>
        /// Ensures that the given <see cref="DateOnly"/> value is valid, not default, and within allowed bounds.
        /// </summary>
        /// <param name="value">The date to validate.</param>
        /// <param name="argumentName">The name of the argument being validated.</param>
        /// <param name="allowFuture">If false, disallows dates later than the current UTC date.</param>
        /// <exception cref="DomainValidationException">Thrown when the date is default, in the future, or too far in the past.</exception>
        public static void AgainstInvalidDateOnly(DateOnly? value, string argumentName, bool allowFuture = false)
        {
            if (value == null)
                return;

            if (value == default)
                throw new DomainValidationException($"{argumentName} cannot be default");

            if (!allowFuture && value > DateOnly.FromDateTime(DateTime.UtcNow))
                throw new DomainValidationException($"{argumentName} cannot be in the future");

            if (value < DateOnly.FromDateTime(new DateTime(2020, 1, 1)))
                throw new DomainValidationException($"{argumentName} is too far in the past");
        }

        /// <summary>
        /// Ensures that the given <see cref="DateTime"/> value is valid, not default, and within allowed bounds.
        /// </summary>
        /// <param name="value">The date to validate.</param>
        /// <param name="argumentName">The name of the argument being validated.</param>
        /// <param name="allowFuture">If false, disallows future dates beyond the current UTC time.</param>
        /// <exception cref="DomainValidationException">Thrown when the date is default, in the future, or too far in the past.</exception>
        public static void AgainstInvalidDateTime(DateTime value, string argumentName, bool allowFuture = false)
        {
            if (value == default)
                throw new DomainValidationException($"{argumentName} cannot be default");

            if (!allowFuture && value > DateTime.UtcNow)
                throw new DomainValidationException($"{argumentName} cannot be in the future");

            if (value < new DateTime(2020, 1, 1))
                throw new DomainValidationException($"{argumentName} is too far in the past");
        }

        /// <summary>
        /// Ensures that the provided <see cref="Guid"/> is not empty.
        /// </summary>
        /// <param name="id">The GUID to validate.</param>
        /// <param name="argumentName">The name of the argument being validated.</param>
        /// <exception cref="DomainValidationException">Thrown when the GUID is empty.</exception>
        public static void AgainstEmptyGuid(Guid id, string argumentName)
        {
            if (id == Guid.Empty)
                throw new DomainValidationException($"{argumentName} cannot be empty Guid");
        }

        /// <summary>
        /// Ensures that the given object reference is not null.
        /// </summary>
        /// <typeparam name="T">The type of the object to validate.</typeparam>
        /// <param name="obj">The object to validate.</param>
        /// <param name="argumentName">The name of the argument being validated.</param>
        /// <exception cref="DomainValidationException">Thrown when the object is null.</exception>
        public static void AgainstNull<T>(T obj, string argumentName)
        {
            if (obj is null)
                throw new DomainValidationException($"{argumentName} cannot be null");
        }

        /// <summary>
        /// Ensures that the provided enum value is defined in the enumeration.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enumeration.</typeparam>
        /// <param name="value">The enum value to validate.</param>
        /// <param name="argumentName">The name of the argument being validated.</param>
        /// <exception cref="DomainValidationException">Thrown when the enum value is not defined.</exception>
        public static void AgainstInvalidEnum<TEnum>(TEnum value, string argumentName) where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(value))
                throw new DomainValidationException($"{argumentName} has invalid value: {value}");
        }

        /// <summary>
        /// Ensures that the provided string can be successfully parsed into a valid enum value.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enumeration.</typeparam>
        /// <param name="value">The string to parse.</param>
        /// <param name="argumentName">The name of the argument being validated.</param>
        /// <exception cref="DomainValidationException">Thrown when the string cannot be parsed into a valid enum value.</exception>
        public static void AgainstInvalidStringToEnum<TEnum>(string value, string argumentName) where TEnum : struct, Enum
        {
            if (!Enum.TryParse<TEnum>(value, true, out _))
                throw new DomainValidationException($"{argumentName} has invalid value: {value}");
        }

        /// <summary>
        /// Ensures that the provided date range is valid, where the start date is not later than the end date.
        /// </summary>
        /// <param name="from">The start date of the range.</param>
        /// <param name="to">The end date of the range.</param>
        /// <exception cref="DomainValidationException">Thrown when the start date is later than the end date.</exception>
        public static void AgainstInvalidDateRange(DateOnly from, DateOnly? to)
        {
            if (to is null)
                return;

            if (from > to)
                throw new DomainValidationException("Date range is invalid");
        }

        /// <summary>
        /// Ensures that the bird's status update is valid.
        /// Throws when the bird is marked as dead but no departure date is provided.
        /// </summary>
        /// <param name="departure">The departure date of the bird (if any).</param>
        /// <param name="isAlive">The current alive status of the bird.</param>
        /// <param name="argumentName">The name of the parameter being validated.</param>
        /// <exception cref="DomainValidationException">
        /// Thrown when <paramref name="isAlive"/> is false but <paramref name="departure"/> is null.
        /// </exception>
        public static void AgainstInvalidStatusUpdate(DateOnly? departure, bool isAlive, string argumentName)
        {
            if (departure is null && isAlive == false)
                throw new DomainValidationException($"{argumentName} date must be set before marking the bird as dead.");
        }
    }
}