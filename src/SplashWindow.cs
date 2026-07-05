using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace FileBrowserDesktop;

internal sealed class SplashWindow : Window
{
    private readonly AppImageConfig _imageConfig;

    public SplashWindow(AppImageConfig imageConfig, AppSettings settings)
    {
        _imageConfig = imageConfig;
        Title = "File Browser Desktop";
        Width = imageConfig.SplashWindowWidth;
        Height = imageConfig.SplashWindowHeight;
        MinWidth = Math.Min(520, imageConfig.SplashWindowWidth);
        MinHeight = Math.Min(320, imageConfig.SplashWindowHeight);
        CanResize = false;
        ShowInTaskbar = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        WindowDecorations = Avalonia.Controls.WindowDecorations.None;
        Icon = AssetImageLoader.TryLoadWindowIcon(imageConfig.WindowIconFileName);
        Background = BrushFor(settings.DarkTheme ? "#0B1120" : "#EEF3F8");
        Content = BuildContent(imageConfig, settings.DarkTheme);
        Opened += (_, _) => ApplyScreenAwarePlacement();
    }

    private void ApplyScreenAwarePlacement()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null)
        {
            return;
        }

        var scale = screen.Scaling > 0 ? screen.Scaling : 1;
        var workingArea = screen.WorkingArea;
        var availableWidth = workingArea.Width / scale;
        var availableHeight = workingArea.Height / scale;

        Width = Math.Min(_imageConfig.SplashWindowWidth, Math.Max(360, availableWidth - 48));
        Height = Math.Min(_imageConfig.SplashWindowHeight, Math.Max(240, availableHeight - 72));
        MinWidth = Math.Min(520, Width);
        MinHeight = Math.Min(320, Height);

        var windowWidth = (int)Math.Round(Width * scale);
        var windowHeight = (int)Math.Round(Height * scale);
        var x = workingArea.X + Math.Max(0, (workingArea.Width - windowWidth) / 2);
        var y = workingArea.Y + Math.Max(0, (workingArea.Height - windowHeight) / 2);

        Position = new PixelPoint(x, y);
    }

    private static Control BuildContent(AppImageConfig imageConfig, bool dark)
    {
        var overlay = BuildSplashOverlay(imageConfig, dark);

        var cardContent = imageConfig.UseSplashCardImage && !string.IsNullOrWhiteSpace(imageConfig.SplashCardImageFileName)
            ? BuildImageCardContent(imageConfig, dark, overlay)
            : overlay;

        return new Border
        {
            Background = BrushFor(dark ? imageConfig.SplashCardBackgroundColorDark : imageConfig.SplashCardBackgroundColorLight),
            BorderBrush = imageConfig.ShowSplashCardBorder
                ? BrushFor(dark ? imageConfig.SplashCardBorderColorDark : imageConfig.SplashCardBorderColorLight)
                : Brushes.Transparent,
            BorderThickness = imageConfig.ShowSplashCardBorder
                ? new Thickness(imageConfig.SplashCardBorderThickness)
                : new Thickness(0),
            CornerRadius = new CornerRadius(imageConfig.SplashCardCornerRadius),
            Margin = new Thickness(imageConfig.SplashCardOuterMargin),
            ClipToBounds = true,
            Child = cardContent,
        };
    }

    private static Control BuildSplashOverlay(AppImageConfig imageConfig, bool dark)
    {
        var overlay = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        if (imageConfig.ShowSplashLogo)
        {
            var logo = AssetImageLoader.CreateImage(imageConfig.SplashLogoFileName, imageConfig.SplashLogoSize, imageConfig.SplashLogoSize);
            PositionOverlayChild(logo, imageConfig.SplashLogoOffsetX, imageConfig.SplashLogoOffsetY - 90);
            overlay.Children.Add(logo);
        }

        if (imageConfig.ShowSplashTitle)
        {
            var title = new TextBlock
            {
                Text = imageConfig.SplashTitleText,
                FontSize = 26,
                FontWeight = FontWeight.SemiBold,
                Foreground = BrushFor(dark ? "#F8FAFC" : "#FFFFFF"),
                TextAlignment = TextAlignment.Center,
            };
            PositionOverlayChild(title, imageConfig.SplashTitleOffsetX, imageConfig.SplashTitleOffsetY);
            overlay.Children.Add(title);
        }

        var status = new TextBlock
        {
            Text = imageConfig.SplashStatusText,
            FontSize = 14,
            Foreground = BrushFor(dark ? "#CBD5E1" : "#FFFFFF"),
            TextAlignment = TextAlignment.Center,
        };
        PositionOverlayChild(status, imageConfig.SplashStatusOffsetX, imageConfig.SplashStatusOffsetY + 36);
        overlay.Children.Add(status);

        var progress = new ProgressBar
        {
            Width = imageConfig.SplashProgressWidth,
            Height = 6,
            IsIndeterminate = true,
        };
        PositionOverlayChild(progress, imageConfig.SplashProgressOffsetX, imageConfig.SplashProgressOffsetY + 72);
        overlay.Children.Add(progress);

        return overlay;
    }

    private static void PositionOverlayChild(Control child, double offsetX, double offsetY)
    {
        child.HorizontalAlignment = HorizontalAlignment.Center;
        child.VerticalAlignment = VerticalAlignment.Center;
        child.Margin = new Thickness(offsetX, offsetY, -offsetX, -offsetY);
    }

    private static Control BuildImageCardContent(AppImageConfig imageConfig, bool dark, Control overlayContent)
    {
        var card = new Grid();

        var background = AssetImageLoader.CreateFillImage(imageConfig.SplashCardImageFileName);
        background.Opacity = imageConfig.SplashCardImageOpacity;
        card.Children.Add(background);

        if (imageConfig.UseSplashCardTint)
        {
            card.Children.Add(new Border
            {
                Background = BrushFor(dark ? imageConfig.SplashCardTintColorDark : imageConfig.SplashCardTintColorLight),
            });
        }

        if (overlayContent is Control content)
        {
            content.HorizontalAlignment = HorizontalAlignment.Center;
            content.VerticalAlignment = VerticalAlignment.Center;
            card.Children.Add(content);
        }

        return card;
    }

    private static IBrush BrushFor(string color)
    {
        return SolidColorBrush.Parse(color);
    }
}
