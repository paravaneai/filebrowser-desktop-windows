using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace FileBrowserDesktop;

internal enum UiButtonStyle
{
    Neutral,
    Primary,
}

internal static class UiTheme
{
    private const string PrimaryBackground = "#2563EB";
    private const string PrimaryBorder = "#1D4ED8";
    private const string PrimaryForeground = "#FFFFFF";
    private const string NeutralDarkBackground = "#172033";
    private const string NeutralDarkBorder = "#40516D";
    private const string NeutralDarkForeground = "#F8FAFC";
    private const string NeutralLightBackground = "#F8FAFC";
    private const string NeutralLightBorder = "#CBD5E1";
    private const string NeutralLightForeground = "#1E293B";

    public static void ConfigureToolbarButton(
        Button button,
        string content,
        double minWidth,
        UiButtonStyle style = UiButtonStyle.Neutral)
    {
        button.Content = content;
        button.MinWidth = minWidth;
        button.Height = 32;
        button.Padding = new Thickness(10, 0);
        button.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        button.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        button.VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center;
        button.BorderThickness = new Thickness(1);
        button.CornerRadius = new CornerRadius(6);

        ApplyButtonTheme(button, style, darkTheme: false);
    }

    public static void ConfigureSetupButton(Button button, string content, double width)
    {
        button.Content = content;
        button.Width = width;
        button.Height = 34;
        button.Margin = new Thickness(0, 0, 8, 8);
        button.Padding = new Thickness(12, 0);
        button.BorderThickness = new Thickness(1);
        button.CornerRadius = new CornerRadius(7);
        button.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        button.VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center;
    }

    public static void ApplyButtonTheme(Button button, UiButtonStyle style, bool darkTheme)
    {
        if (style == UiButtonStyle.Primary)
        {
            button.Background = BrushFor(PrimaryBackground);
            button.BorderBrush = BrushFor(PrimaryBorder);
            button.Foreground = BrushFor(PrimaryForeground);
            ApplyIconForeground(button);
            return;
        }

        button.Background = BrushFor(darkTheme ? NeutralDarkBackground : NeutralLightBackground);
        button.BorderBrush = BrushFor(darkTheme ? NeutralDarkBorder : NeutralLightBorder);
        button.Foreground = BrushFor(darkTheme ? NeutralDarkForeground : NeutralLightForeground);
        ApplyIconForeground(button);
    }

    private static void ApplyIconForeground(Button button)
    {
        if (button.Content is PathIcon icon)
        {
            icon.Foreground = button.Foreground;
        }
    }

    private static IBrush BrushFor(string color)
    {
        return new SolidColorBrush(Color.Parse(color));
    }
}
