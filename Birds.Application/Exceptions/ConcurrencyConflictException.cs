using Birds.Shared.Constants;

namespace Birds.Application.Exceptions;

public sealed class ConcurrencyConflictException(string name, object key, Exception? innerException = null)
    : Exception(ErrorMessages.BirdConcurrencyConflict, innerException)
{
    public string EntityName { get; } = name;
    public object EntityKey { get; } = key;
}
