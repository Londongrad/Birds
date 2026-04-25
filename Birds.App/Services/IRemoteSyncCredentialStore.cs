namespace Birds.App.Services;

internal interface IRemoteSyncCredentialStore
{
    bool HasPassword();

    string? TryLoadPassword();

    void SavePassword(string password);

    void ClearPassword();
}
