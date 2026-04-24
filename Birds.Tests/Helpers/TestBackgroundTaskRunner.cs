using Birds.UI.Services.Background;
using Microsoft.Extensions.Logging.Abstractions;

namespace Birds.Tests.Helpers;

public static class TestBackgroundTaskRunner
{
    public static IBackgroundTaskRunner Create()
    {
        return new BackgroundTaskRunner(NullLogger<BackgroundTaskRunner>.Instance);
    }
}
