namespace Birds.Domain.Common.Exceptions
{
    /// <summary>
    /// Represents a business rule violation inside the domain model.
    /// </summary>
    public class DomainValidationException(string message) : Exception(message);
}
