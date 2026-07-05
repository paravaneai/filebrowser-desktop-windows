using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.IO;

namespace FileBrowserDesktop;

internal static class AssetImageLoader
{
    public static Image CreateImage(string fileName, double width, double height)
    {
        var image = new Image
        {
            Width = width,
            Height = height,
            Source = TryLoadBitmap(fileName),
            Stretch = Stretch.Uniform,
        };
        RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.HighQuality);
        return image;
    }

    public static Image CreateFillImage(string fileName)
    {
        var image = new Image
        {
            Source = TryLoadBitmap(fileName),
            Stretch = Stretch.UniformToFill,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
        };
        RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.HighQuality);
        return image;
    }

    public static WindowIcon? TryLoadWindowIcon(string fileName)
    {
        var path = GetAssetPath(fileName);
        return File.Exists(path) ? new WindowIcon(path) : null;
    }

    private static Bitmap? TryLoadBitmap(string fileName)
    {
        var path = GetAssetPath(fileName);
        return File.Exists(path) ? new Bitmap(path) : null;
    }

    private static string GetAssetPath(string fileName)
    {
        return Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
    }
}
