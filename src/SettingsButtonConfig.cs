using System.IO;
using System.Text.Json;

namespace FileBrowserDesktop;

internal sealed class SettingsButtonConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public string IconFileName { get; set; } = "settings-icon.png";
    public string LightIconFileName { get; set; } = "";
    public string DarkIconFileName { get; set; } = "";
    public string FallbackIconFileName { get; set; } = "settings-knobs-svgrepo-com.svg";
    public double IconSize { get; set; } = 18;
    public bool ShrinkIconOnHover { get; set; }
    public double HoverIconSize { get; set; } = 16;
    public double ButtonSize { get; set; } = 42;
    public bool RemoveButtonBorder { get; set; }
    public bool RemoveButtonBackground { get; set; }

    public static SettingsButtonConfig Load()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "Assets", "settings-button.json");
        try
        {
            if (!File.Exists(configPath))
            {
                return new SettingsButtonConfig();
            }

            var config = JsonSerializer.Deserialize<SettingsButtonConfig>(File.ReadAllText(configPath), JsonOptions) ?? new SettingsButtonConfig();
            config.IconSize = Clamp(config.IconSize, 12, 32, 18);
            config.HoverIconSize = Math.Min(config.IconSize, Clamp(config.HoverIconSize, 8, 32, Math.Max(8, config.IconSize - 4)));
            config.ButtonSize = Clamp(config.ButtonSize, 28, 56, 42);
            config.LightIconFileName = config.LightIconFileName.Trim();
            config.DarkIconFileName = config.DarkIconFileName.Trim();
            return config;
        }
        catch
        {
            return new SettingsButtonConfig();
        }
    }

    private static double Clamp(double value, double min, double max, double fallback)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return fallback;
        }

        return Math.Clamp(value, min, max);
    }
}
