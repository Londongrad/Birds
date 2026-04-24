using System.Data.Common;
using System.Text.RegularExpressions;

namespace Birds.App.Services;

internal static partial class DiagnosticRedactor
{
    public const string RedactedValue = "***REDACTED***";

    public static string RedactConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return string.Empty;

        try
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            foreach (var key in builder.Keys.Cast<string>().ToArray())
            {
                if (!string.IsNullOrWhiteSpace(key) && IsSecretKey(key))
                    builder[key] = RedactedValue;
            }

            return builder.ConnectionString;
        }
        catch (ArgumentException)
        {
            return SecretAssignmentRegex().Replace(
                connectionString,
                match => $"{match.Groups["key"].Value}={RedactedValue}");
        }
    }

    public static string? TryGetSqliteDataSource(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        try
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            foreach (var key in builder.Keys.Cast<string>())
            {
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                var normalizedKey = NormalizeKey(key);
                if (normalizedKey is "datasource" or "filename")
                    return builder[key]?.ToString();
            }
        }
        catch (ArgumentException)
        {
            return null;
        }

        return null;
    }

    private static bool IsSecretKey(string key)
    {
        var normalizedKey = NormalizeKey(key);

        return normalizedKey.Contains("password", StringComparison.Ordinal)
               || normalizedKey == "pwd"
               || normalizedKey.Contains("secret", StringComparison.Ordinal)
               || normalizedKey.Contains("token", StringComparison.Ordinal)
               || normalizedKey.Contains("apikey", StringComparison.Ordinal);
    }

    private static string NormalizeKey(string key)
    {
        return key.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }

    [GeneratedRegex(
        @"(?<key>password|pwd|secret|token|access\s*token|api\s*key)\s*=\s*[^;]*",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SecretAssignmentRegex();
}
