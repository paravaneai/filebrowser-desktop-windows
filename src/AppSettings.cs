using System.IO;
using System.Text.Json;

namespace FileBrowserDesktop;

internal sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public bool DarkTheme { get; set; }
    public double ZoomScale { get; set; } = 1.0;

    public static string FilePath => Path.Combine(ProfileStore.DirectoryPath, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new AppSettings();
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), JsonOptions) ?? new AppSettings();
            settings.ZoomScale = ClampZoom(settings.ZoomScale);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        ZoomScale = ClampZoom(ZoomScale);
        Directory.CreateDirectory(ProfileStore.DirectoryPath);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
    }

    public static double ClampZoom(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 1.0;
        }

        return Math.Clamp(Math.Round(value, 2), 0.8, 1.4);
    }

    public AppSettings Clone()
    {
        return new AppSettings
        {
            DarkTheme = DarkTheme,
            ZoomScale = ZoomScale,
        };
    }
}
