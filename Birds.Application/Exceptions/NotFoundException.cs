using Birds.Shared.Constants;

namespace Birds.Application.Exceptions
{
    public class NotFoundException(string name, object key)
        : Exception(string.Format(ExceptionMessages.NotFound, name, key));
}