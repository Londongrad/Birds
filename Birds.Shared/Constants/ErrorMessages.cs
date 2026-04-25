using Birds.Shared.Localization;

namespace Birds.Shared.Constants;

public static class ErrorMessages
{
    public static string BirdLoadFailed => AppText.Get("Error.BirdLoadFailed");

    public static string UnknownError => AppText.Get("Error.UnknownError");
    public static string UnexpectedError => AppText.Get("Error.UnexpectedError");

    public static string CannotSaveBird => AppText.Get("Error.CannotSaveBird");
    public static string CannotDeleteBird => AppText.Get("Error.CannotDeleteBird");
    public static string CannotUpdateBird => AppText.Get("Error.CannotUpdateBird");
    public static string BirdConcurrencyConflict => AppText.Get("Error.BirdConcurrencyConflict");

    public static string RequestCannotBeNull => AppText.Get("Error.RequestCannotBeNull");
    public static string QueryCannotBeNull => AppText.Get("Error.QueryCannotBeNull");

    public static string ConnectionStringNotFound => AppText.Get("Error.ConnectionStringNotFound");

    public static string RemoteSyncConfigurationMissing => AppText.Get("Error.RemoteSyncConfigurationMissing");

    public static string RemoteSyncHostMissing => AppText.Get("Error.RemoteSyncHostMissing");

    public static string RemoteSyncPortInvalid => AppText.Get("Error.RemoteSyncPortInvalid");

    public static string RemoteSyncDatabaseMissing => AppText.Get("Error.RemoteSyncDatabaseMissing");

    public static string RemoteSyncUsernameMissing => AppText.Get("Error.RemoteSyncUsernameMissing");

    public static string RemoteSyncPasswordMissing => AppText.Get("Error.RemoteSyncPasswordMissing");

    public static string RemoteSyncConnectionTestFailed => AppText.Get("Error.RemoteSyncConnectionTestFailed");

    public static string RemoteSyncConnectionFailed(string detail)
    {
        return AppText.Format("Error.RemoteSyncConnectionFailedWithDetail", detail);
    }

    public static string RemoteSyncSettingsSaveFailed => AppText.Get("Error.RemoteSyncSettingsSaveFailed");

    public static string ImportPathCannotBeEmpty
        => AppText.Get("Error.ImportPathCannotBeEmpty");

    public static string ImportPayloadCannotBeNull
        => AppText.Get("Error.ImportPayloadCannotBeNull");

    public static string InvalidImportFileFormat
        => AppText.Get("Error.InvalidImportFileFormat");

    public static string ImportFailed
        => AppText.Get("Error.ImportFailed");

    public static string ImportValidationFailed
        => AppText.Get("Error.ImportValidationFailed");

    public static string ImportTransactionFailed
        => AppText.Get("Error.ImportTransactionFailed");

    public static string InvalidImportedBirdSpecies
        => AppText.Get("Error.InvalidImportedBirdSpecies");

    public static string ExportFailed
        => AppText.Get("Error.ExportFailed");

    public static string CannotClearBirdRecords
        => AppText.Get("Error.CannotClearBirdRecords");

    public static string CannotResetLocalDatabase
        => AppText.Get("Error.CannotResetLocalDatabase");

    public static string FailedToDisplayErrorNotification
        => AppText.Get("Error.FailedToDisplayErrorNotification");

    public static string StartupErrorTitle => AppText.Get("Error.StartupErrorTitle");

    public static string ShotdownWarningTitle => AppText.Get("Error.ShutdownWarningTitle");

    public static string ConnectionStringNotFoundFor(params string[] names)
    {
        return AppText.Format("Error.ConnectionStringNotFoundFor", string.Join(", ", names));
    }

    public static string RemoteSyncMissingEnvironmentVariables(string names)
    {
        return AppText.Format("Error.RemoteSyncMissingEnvironmentVariables", names);
    }

    public static string InvalidDatabaseProvider(string provider)
    {
        return AppText.Format("Error.InvalidDatabaseProvider", provider);
    }

    public static string InvalidDatabaseSeedingMode(string mode)
    {
        return AppText.Format("Error.InvalidDatabaseSeedingMode", mode);
    }

    public static string ImportFileNotFound(string path)
    {
        return AppText.Format("Error.ImportFileNotFound", path);
    }

    public static string UnsupportedImportVersion(int version)
    {
        return AppText.Format("Error.UnsupportedImportVersion", version);
    }

    public static string ImportContainsDuplicateIds(Guid id)
    {
        return AppText.Format("Error.ImportContainsDuplicateIds", id);
    }

    public static string InvalidImportedBirdName(string name)
    {
        return AppText.Format("Error.InvalidImportedBirdName", name);
    }

    public static string StartupError(string message)
    {
        return AppText.Format("Error.StartupError", message);
    }

    public static string StartupFailure(string logDirectory)
    {
        return AppText.Format("Error.StartupFailure", logDirectory);
    }

    public static string GlobalCrashMessage(string logDirectory)
    {
        return AppText.Format("Error.GlobalCrashMessage", logDirectory);
    }

    public static string ShotdownError(string message)
    {
        return AppText.Format("Error.ShutdownError", message);
    }

    public static string ShutdownFailure(string logDirectory)
    {
        return AppText.Format("Error.ShutdownFailure", logDirectory);
    }
}
