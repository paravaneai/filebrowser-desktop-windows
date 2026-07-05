using System.IO;
using System.Text.Json;

namespace FileBrowserDesktop;

public sealed class AppImageConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public string WindowIconFileName { get; set; } = "filebrowser.ico";
    public string ToolbarLogoFileName { get; set; } = "filebrowser.png";
    public double ToolbarLogoSize { get; set; } = 28;
    public double ToolbarLogoMarginRight { get; set; } = 12;
    public string StartupLogoFileName { get; set; } = "filebrowser.png";
    public double StartupLogoSize { get; set; } = 64;
    public string SplashLogoFileName { get; set; } = "filebrowser.png";
    public bool ShowSplashLogo { get; set; } = true;
    public double SplashLogoSize { get; set; } = 72;
    public double SplashLogoOffsetX { get; set; }
    public double SplashLogoOffsetY { get; set; }
    public bool ShowSplashTitle { get; set; } = true;
    public string SplashTitleText { get; set; } = "File Browser Desktop";
    public double SplashTitleOffsetX { get; set; }
    public double SplashTitleOffsetY { get; set; }
    public string SplashStatusText { get; set; } = "Starting private file access...";
    public double SplashStatusOffsetX { get; set; }
    public double SplashStatusOffsetY { get; set; }
    public double SplashProgressWidth { get; set; } = 360;
    public double SplashProgressOffsetX { get; set; }
    public double SplashProgressOffsetY { get; set; }
    public int SplashMinimumMilliseconds { get; set; } = 1200;
    public double SplashWindowWidth { get; set; } = 520;
    public double SplashWindowHeight { get; set; } = 320;
    public double SplashCardOuterMargin { get; set; } = 12;
    public double SplashCardCornerRadius { get; set; } = 18;
    public bool ShowSplashCardBorder { get; set; } = true;
    public double SplashCardBorderThickness { get; set; } = 1;
    public string SplashCardBorderColorDark { get; set; } = "#334155";
    public string SplashCardBorderColorLight { get; set; } = "#D9E2EF";
    public string SplashCardBackgroundColorDark { get; set; } = "#0F172A";
    public string SplashCardBackgroundColorLight { get; set; } = "#FFFFFF";
    public bool UseSplashCardImage { get; set; }
    public string SplashCardImageFileName { get; set; } = "";
    public double SplashCardImageOpacity { get; set; } = 1;
    public bool UseSplashCardTint { get; set; } = true;
    public string SplashCardTintColorDark { get; set; } = "#990B1120";
    public string SplashCardTintColorLight { get; set; } = "#66FFFFFF";
    public string SetupHeaderLogoFileName { get; set; } = "filebrowser.png";
    public double SetupHeaderLogoSize { get; set; } = 28;
    public double SetupHeaderLogoOffsetX { get; set; }
    public double SetupHeaderLogoOffsetY { get; set; }
    public double SetupHeaderFrameSize { get; set; } = 44;
    public bool ShowSetupHeaderIconFrame { get; set; } = true;
    public bool ShowSetupHeaderIconBackground { get; set; } = true;
    public bool ShowSetupHeaderIconBorder { get; set; } = true;

    public static AppImageConfig Load()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app-images.json");
        try
        {
            if (!File.Exists(configPath))
            {
                return new AppImageConfig();
            }

            var config = JsonSerializer.Deserialize<AppImageConfig>(File.ReadAllText(configPath), JsonOptions) ?? new AppImageConfig();
            config.ToolbarLogoSize = Clamp(config.ToolbarLogoSize, 12, 96, 28);
            config.ToolbarLogoMarginRight = Clamp(config.ToolbarLogoMarginRight, 0, 48, 12);
            config.StartupLogoSize = Clamp(config.StartupLogoSize, 24, 160, 64);
            config.SplashLogoSize = Clamp(config.SplashLogoSize, 24, 220, 72);
            config.SplashLogoOffsetX = Clamp(config.SplashLogoOffsetX, -1000, 1000, 0);
            config.SplashLogoOffsetY = Clamp(config.SplashLogoOffsetY, -1000, 1000, 0);
            config.SplashTitleOffsetX = Clamp(config.SplashTitleOffsetX, -1000, 1000, 0);
            config.SplashTitleOffsetY = Clamp(config.SplashTitleOffsetY, -1000, 1000, 0);
            config.SplashStatusOffsetX = Clamp(config.SplashStatusOffsetX, -1000, 1000, 0);
            config.SplashStatusOffsetY = Clamp(config.SplashStatusOffsetY, -1000, 1000, 0);
            config.SplashProgressWidth = Clamp(config.SplashProgressWidth, 80, 1000, 360);
            config.SplashProgressOffsetX = Clamp(config.SplashProgressOffsetX, -1000, 1000, 0);
            config.SplashProgressOffsetY = Clamp(config.SplashProgressOffsetY, -1000, 1000, 0);
            config.SplashMinimumMilliseconds = (int)Clamp(config.SplashMinimumMilliseconds, 500, 10000, 1200);
            config.SplashWindowWidth = Clamp(config.SplashWindowWidth, 360, 1200, 520);
            config.SplashWindowHeight = Clamp(config.SplashWindowHeight, 240, 720, 320);
            config.SplashCardOuterMargin = Clamp(config.SplashCardOuterMargin, 0, 48, 12);
            config.SplashCardCornerRadius = Clamp(config.SplashCardCornerRadius, 0, 48, 18);
            config.SplashCardBorderThickness = Clamp(config.SplashCardBorderThickness, 0, 8, 1);
            config.SplashCardImageOpacity = Clamp(config.SplashCardImageOpacity, 0.1, 1, 1);
            config.SetupHeaderLogoSize = Clamp(config.SetupHeaderLogoSize, 16, 96, 28);
            config.SetupHeaderLogoOffsetX = Clamp(config.SetupHeaderLogoOffsetX, -120, 120, 0);
            config.SetupHeaderLogoOffsetY = Clamp(config.SetupHeaderLogoOffsetY, -120, 120, 0);
            config.SetupHeaderFrameSize = Clamp(config.SetupHeaderFrameSize, 24, 120, 44);
            if (!config.ShowSetupHeaderIconFrame)
            {
                config.ShowSetupHeaderIconBackground = false;
                config.ShowSetupHeaderIconBorder = false;
            }
            if (string.IsNullOrWhiteSpace(config.SplashLogoFileName))
            {
                config.SplashLogoFileName = config.StartupLogoFileName;
            }

            if (string.IsNullOrWhiteSpace(config.SplashTitleText))
            {
                config.SplashTitleText = "File Browser Desktop";
            }

            if (string.IsNullOrWhiteSpace(config.SplashStatusText))
            {
                config.SplashStatusText = "Starting private file access...";
            }

            if (string.IsNullOrWhiteSpace(config.SplashCardTintColorDark))
            {
                config.SplashCardTintColorDark = "#990B1120";
            }

            if (string.IsNullOrWhiteSpace(config.SplashCardTintColorLight))
            {
                config.SplashCardTintColorLight = "#66FFFFFF";
            }

            if (string.IsNullOrWhiteSpace(config.SplashCardBorderColorDark))
            {
                config.SplashCardBorderColorDark = "#334155";
            }

            if (string.IsNullOrWhiteSpace(config.SplashCardBorderColorLight))
            {
                config.SplashCardBorderColorLight = "#D9E2EF";
            }

            if (string.IsNullOrWhiteSpace(config.SplashCardBackgroundColorDark))
            {
                config.SplashCardBackgroundColorDark = "#0F172A";
            }

            if (string.IsNullOrWhiteSpace(config.SplashCardBackgroundColorLight))
            {
                config.SplashCardBackgroundColorLight = "#FFFFFF";
            }

            return config;
        }
        catch
        {
            return new AppImageConfig();
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
