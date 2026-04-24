namespace Birds.Application.Common.Models;

/// <summary>
///     Represents a structured application error that can be handled by code and shown to users.
/// </summary>
public sealed record AppError(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null)
{
    public override string ToString()
    {
        return Message;
    }
}
