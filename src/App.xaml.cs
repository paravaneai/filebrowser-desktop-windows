using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using System.Diagnostics;

namespace FileBrowserDesktop;

public sealed class App : Application
{
    public static string[] StartupArgs { get; set; } = [];

    public override void Initialize()
    {
        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        RequestedThemeVariant = ThemeVariant.Light;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var imageConfig = AppImageConfig.Load();
            var splash = new SplashWindow(imageConfig, AppSettingsStore.Current);
            desktop.MainWindow = splash;

            if (IsSplashPreview())
            {
                var parentPid = GetSplashPreviewParentPid();
                if (parentPid is not null)
                {
                    splash.Opened += (_, _) => WatchPreviewParentProcess(splash, parentPid.Value);
                }

                return;
            }

            splash.Opened += async (_, _) => await ShowMainWindowAfterSplashAsync(desktop, splash, imageConfig);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task ShowMainWindowAfterSplashAsync(
        IClassicDesktopStyleApplicationLifetime desktop,
        SplashWindow splash,
        AppImageConfig imageConfig)
    {
        var started = DateTimeOffset.UtcNow;

        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
        var mainWindow = new MainWindow();

        var elapsed = DateTimeOffset.UtcNow - started;
        var remaining = imageConfig.SplashMinimumMilliseconds - (int)elapsed.TotalMilliseconds;
        if (remaining > 0)
        {
            await Task.Delay(remaining);
        }

        desktop.MainWindow = mainWindow;
        mainWindow.Show();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
        splash.Close();
    }

    private static bool IsSplashPreview()
    {
        return StartupArgs.Any(arg =>
            string.Equals(arg, "--splash-preview", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "/splash-preview", StringComparison.OrdinalIgnoreCase));
    }

    private static int? GetSplashPreviewParentPid()
    {
        for (var index = 0; index < StartupArgs.Length - 1; index++)
        {
            if ((string.Equals(StartupArgs[index], "--preview-parent-pid", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(StartupArgs[index], "/preview-parent-pid", StringComparison.OrdinalIgnoreCase)) &&
                int.TryParse(StartupArgs[index + 1], out var pid))
            {
                return pid;
            }
        }

        return null;
    }

    private static void WatchPreviewParentProcess(Window splash, int parentPid)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var parent = Process.GetProcessById(parentPid);
                await parent.WaitForExitAsync();
            }
            catch
            {
                // If the parent cannot be found, treat it as closed.
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (splash.IsVisible)
                {
                    splash.Close();
                }
            });
        });
    }
}
