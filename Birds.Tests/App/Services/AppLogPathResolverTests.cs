using Birds.App.Services;
using FluentAssertions;

namespace Birds.Tests.App.Services;

public sealed class AppLogPathResolverTests
{
    [Fact]
    public void ResolveLogsDirectory_WhenRepositoryRootExists_UsesRepositoryLogsFolder()
    {
        var root = CreateTempDirectory();
        var nested = Path.Combine(root, "Birds.App", "bin", "Debug", "net9.0-windows");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(root, "Birds.sln"), "fake solution");

        try
        {
            var logsDirectory = AppLogPathResolver.ResolveLogsDirectory(nested);

            logsDirectory.Should().Be(Path.Combine(root, "logs"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ResolveLogsDirectory_WhenRepositoryRootIsMissing_UsesUserLocalAppData()
    {
        var root = CreateTempDirectory();
        var nested = Path.Combine(root, "Birds.App", "bin");
        Directory.CreateDirectory(nested);

        try
        {
            var logsDirectory = AppLogPathResolver.ResolveLogsDirectory(nested);

            logsDirectory.Should().Be(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Birds",
                "Logs"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void ResolveLogsDirectory_WhenOverrideIsSet_UsesOverridePath()
    {
        var previous = Environment.GetEnvironmentVariable("BIRDS_LOG_DIR");
        var overridePath = Path.Combine(Path.GetTempPath(), "Birds.Tests", Guid.NewGuid().ToString("N"), "logs");

        try
        {
            Environment.SetEnvironmentVariable("BIRDS_LOG_DIR", overridePath);

            var logsDirectory = AppLogPathResolver.ResolveLogsDirectory();

            logsDirectory.Should().Be(Path.GetFullPath(overridePath));
        }
        finally
        {
            Environment.SetEnvironmentVariable("BIRDS_LOG_DIR", previous);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "Birds.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
