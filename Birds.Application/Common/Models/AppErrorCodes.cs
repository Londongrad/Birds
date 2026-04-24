namespace Birds.Application.Common.Models;

public static class AppErrorCodes
{
    public const string ApplicationFailure = "Application.Failure";
    public const string ApplicationInvalidRequest = "Application.InvalidRequest";
    public const string ApplicationUnexpected = "Application.Unexpected";
    public const string ApplicationValidationFailed = "Application.ValidationFailed";

    public const string BirdNotFound = "Bird.NotFound";
    public const string BirdConcurrencyConflict = "Bird.ConcurrencyConflict";
    public const string BirdStoreUnavailable = "Bird.StoreUnavailable";

    public const string SyncRemoteFailure = "Sync.RemoteFailure";

    public const string ImportInvalidFile = "Import.InvalidFile";
    public const string ImportInvalidPayload = "Import.InvalidPayload";
    public const string ImportUnsupportedVersion = "Import.UnsupportedVersion";
    public const string ImportValidationFailed = "Import.ValidationFailed";
    public const string ImportTransactionFailed = "Import.TransactionFailed";
    public const string ImportDuplicateIds = "Import.DuplicateIds";
    public const string ImportInvalidSpecies = "Import.InvalidSpecies";

    public const string ExportFailure = "Export.Failure";
}
