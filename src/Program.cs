using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.Runtime.InteropServices;
using System.Threading;

namespace FileBrowserDesktop;

internal static class Program
{
    private const string SingleInstanceMutexName = @"Local\ParavaneLabs.FileBrowserDesktop";

    private static Mutex? singleInstanceMutex;
    private static bool ownsSingleInstanceMutex;

    [STAThread]
    public static void Main(string[] args)
    {
        App.StartupArgs = args;

        if (!IsSplashPreview(args) && !TryAcquireSingleInstance())
        {
            return;
        }

        _ = SetCurrentProcessExplicitAppUserModelID("ParavaneLabs.FileBrowserDesktop");

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            ReleaseSingleInstance();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

    private static bool IsSplashPreview(string[] args)
    {
        return args.Any(arg =>
            string.Equals(arg, "--splash-preview", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "/splash-preview", StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryAcquireSingleInstance()
    {
        singleInstanceMutex = new Mutex(false, SingleInstanceMutexName);

        try
        {
            ownsSingleInstanceMutex = singleInstanceMutex.WaitOne(0);
            return ownsSingleInstanceMutex;
        }
        catch (AbandonedMutexException)
        {
            ownsSingleInstanceMutex = true;
            return true;
        }
    }

    private static void ReleaseSingleInstance()
    {
        if (ownsSingleInstanceMutex)
        {
            singleInstanceMutex?.ReleaseMutex();
        }

        singleInstanceMutex?.Dispose();
    }
}
