using System.IO;
using System.Security.Cryptography;
using System.Text;
using Birds.UI.Services.Preferences.Interfaces;
using Microsoft.Extensions.Logging;

namespace Birds.App.Services;

internal sealed class DpapiRemoteSyncCredentialStore(
    IAppPreferencesPathProvider preferencesPathProvider,
    ILogger<DpapiRemoteSyncCredentialStore> logger) : IRemoteSyncCredentialStore
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Birds.RemoteSync.Password.v1");

    private readonly ILogger<DpapiRemoteSyncCredentialStore> _logger = logger;
    private readonly IAppPreferencesPathProvider _preferencesPathProvider = preferencesPathProvider;

    public bool HasPassword()
    {
        return File.Exists(GetPasswordPath());
    }

    public string? TryLoadPassword()
    {
        var path = GetPasswordPath();
        try
        {
            if (!File.Exists(path))
                return null;

            var protectedBytes = File.ReadAllBytes(path);
            var passwordBytes = ProtectedData.Unprotect(
                protectedBytes,
                Entropy,
                DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(passwordBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load remote sync password from {Path}.", path);
            return null;
        }
    }

    public void SavePassword(string password)
    {
        var path = GetPasswordPath();
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var protectedBytes = ProtectedData.Protect(
                passwordBytes,
                Entropy,
                DataProtectionScope.CurrentUser);

            File.WriteAllBytes(path, protectedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save remote sync password to {Path}.", path);
            throw;
        }
    }

    public void ClearPassword()
    {
        var path = GetPasswordPath();
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear remote sync password at {Path}.", path);
        }
    }

    private string GetPasswordPath()
    {
        var preferencesPath = _preferencesPathProvider.GetPreferencesPath();
        var directory = Path.GetDirectoryName(preferencesPath)
                        ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return Path.Combine(directory, "remote-sync-password.dat");
    }
}
