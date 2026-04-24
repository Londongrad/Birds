namespace Birds.Infrastructure.Services;

public sealed class RemoteSyncSchemaException(string message, Exception innerException)
    : Exception(message, innerException);
