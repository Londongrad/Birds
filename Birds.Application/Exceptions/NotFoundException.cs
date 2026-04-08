using Birds.Shared.Constants;

namespace Birds.Application.Exceptions
{
    public class NotFoundException(string name, object key)
        : Exception(ExceptionMessages.NotFound(name, key));
}
