using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.IO;
using System.Xml.Linq;

namespace FileBrowserDesktop;

internal static class AssetIconLoader
{
    private const string FallbackSettingsIconPathData = "M87 32v71h18V32H87zm160 0v345h18V32h-18zm160 0v167h18V32h-18zM50 121c-5.14 0-9 3.9-9 9v28c0 5.1 3.86 9 9 9h92c5.1 0 9-3.9 9-9v-28c0-5.1-3.9-9-9-9H50zm37 64v295h18V185H87zm283 32c-5.1 0-9 3.9-9 9v28c0 5.1 3.9 9 9 9h92c5.1 0 9-3.9 9-9v-28c0-5.1-3.9-9-9-9h-92zm37 64v199h18V281h-18zM210 395c-5.1 0-9 3.9-9 9v28c0 5.1 3.9 9 9 9h92c5.1 0 9-3.9 9-9v-28c0-5.1-3.9-9-9-9h-92zm37 64v21h18v-21h-18z";

    public static Control CreateSettingsIcon()
    {
        return CreateSettingsIcon(SettingsButtonConfig.Load());
    }

    public static Control CreateSettingsIcon(SettingsButtonConfig config)
    {
        return CreateIconControl(config.IconSize, config.IconFileName, config.FallbackIconFileName);
    }

    public static Control CreateSettingsIcon(SettingsButtonConfig config, bool darkTheme)
    {
        var themedIcon = darkTheme ? config.DarkIconFileName : config.LightIconFileName;
        return CreateIconControl(config.IconSize, themedIcon, config.IconFileName, config.FallbackIconFileName);
    }

    private static Control CreateIconControl(double iconSize, params string[] assetFileNames)
    {
        foreach (var assetFileName in assetFileNames)
        {
            if (string.IsNullOrWhiteSpace(assetFileName))
            {
                continue;
            }

            var path = Path.Combine(AppContext.BaseDirectory, "Assets", assetFileName);
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                if (IsBitmapAsset(path))
                {
                    return CreateBitmapIcon(path, iconSize);
                }

                var pathData = ReadSvgPathData(path);
                if (!string.IsNullOrWhiteSpace(pathData))
                {
                    return CreatePathIcon(pathData, iconSize);
                }
            }
            catch
            {
                // Fall back to the built-in settings icon if a trial SVG cannot be parsed.
            }
        }

        return CreatePathIcon(FallbackSettingsIconPathData, iconSize);
    }

    private static bool IsBitmapAsset(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".ico", StringComparison.OrdinalIgnoreCase);
    }

    private static Image CreateBitmapIcon(string filePath, double iconSize)
    {
        var image = new Image
        {
            Width = iconSize,
            Height = iconSize,
            Source = new Bitmap(filePath),
            Stretch = Stretch.Uniform,
        };
        RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.HighQuality);
        return image;
    }

    private static string ReadSvgPathData(string filePath)
    {
        var document = XDocument.Load(filePath);
        var pathData = document
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "path", StringComparison.OrdinalIgnoreCase))
            .Select(element => element.Attribute("d")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value));

        return string.Join(" ", pathData);
    }

    private static PathIcon CreatePathIcon(string pathData, double iconSize)
    {
        return new PathIcon
        {
            Width = iconSize,
            Height = iconSize,
            Data = StreamGeometry.Parse(pathData),
        };
    }
}
