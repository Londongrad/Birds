using System.Diagnostics;

namespace Birds.App;

public partial class App
{
    private Mutex? _mutex;
    private bool _mutexOwned;

    /// <summary>
    /// Tries to acquire a named system mutex to enforce a single instance.
    /// Returns true if this is the first (owning) instance; otherwise false.
    /// </summary>
    internal bool AcquireSingleInstanceGuard(string name)
    {
        // Create (or open) the named mutex and try to take initial ownership.
        _mutex = new Mutex(initiallyOwned: true, name, out bool isNew);
        // We only own the mutex when it was just created.
        _mutexOwned = isNew;
        return isNew;
    }

    /// <summary>
    /// Releases the mutex if owned and disposes it.
    /// Safe to call multiple times.
    /// </summary>
    internal void ReleaseSingleInstanceGuard()
    {
        if (_mutex is null) return;

        try
        {
            // Release only if we actually own it.
            if (_mutexOwned)
                _mutex.ReleaseMutex();
        }
        finally
        {
            _mutex.Dispose();
            _mutex = null;
            _mutexOwned = false;
        }
    }

    /// <summary>
    /// Attempts to bring an already running instance of the application to the foreground.
    /// </summary>
    /// <remarks>
    /// Finds another process with the same executable name and a different PID, restores its main
    /// window if minimized, and activates it. Best-effort: any interop failures are ignored.
    /// </remarks>
    private static void BringExistingInstanceToFront()
    {
        try
        {
            var current = Process.GetCurrentProcess();
            var existing = Process.GetProcessesByName(current.ProcessName)
                                  .FirstOrDefault(p => p.Id != current.Id);

            if (existing != null)
            {
                var handle = existing.MainWindowHandle;
                if (handle != IntPtr.Zero)
                {
                    NativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);
                    NativeMethods.SetForegroundWindow(handle);
                }
            }
        }
        catch
        {
            // Intentionally ignored: do not crash when interop fails.
        }
    }

    /// <summary>
    /// Win32 interop helpers used to restore and activate an existing window.
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>
        /// Brings the specified window to the foreground and activates it.
        /// </summary>
        /// <param name="hWnd">A handle to the target window.</param>
        /// <returns>
        /// <c>true</c> if the window was brought to the foreground; otherwise, <c>false</c>.
        /// </returns>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Sets the show state of the specified window.
        /// Used here with <see cref="SW_RESTORE"/> to un-minimize a window before activation.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="nCmdShow">
        /// One of the <c>SW_*</c> constants; typically <see cref="SW_RESTORE"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the window was previously visible; otherwise, <c>false</c>.
        /// </returns>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// Restores a minimized window and activates it.
        /// </summary>
        internal const int SW_RESTORE = 9;
    }
}
