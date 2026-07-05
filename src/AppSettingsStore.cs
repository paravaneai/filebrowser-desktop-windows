namespace FileBrowserDesktop;

internal static class AppSettingsStore
{
    private static AppSettings _current = AppSettings.Load();

    public static event EventHandler<AppSettings>? SettingsChanged;

    public static AppSettings Current => _current.Clone();

    public static void SetDarkTheme(bool darkTheme)
    {
        Update(settings => settings.DarkTheme = darkTheme);
    }

    public static void SetZoomScale(double zoomScale)
    {
        Update(settings => settings.ZoomScale = AppSettings.ClampZoom(zoomScale));
    }

    public static void AdjustZoom(double delta)
    {
        SetZoomScale(_current.ZoomScale + delta);
    }

    public static void ResetZoom()
    {
        SetZoomScale(1.0);
    }

    public static void Reload()
    {
        _current = AppSettings.Load();
        SettingsChanged?.Invoke(null, Current);
    }

    private static void Update(Action<AppSettings> update)
    {
        update(_current);
        _current.ZoomScale = AppSettings.ClampZoom(_current.ZoomScale);
        _current.Save();
        SettingsChanged?.Invoke(null, Current);
    }
}
