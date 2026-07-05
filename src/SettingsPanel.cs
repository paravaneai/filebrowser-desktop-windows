using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace FileBrowserDesktop;

public sealed class SettingsPanel : Grid
{
    private const double SettingsActionButtonWidth = 84;
    private const double SettingsButtonHeight = 34;

    private readonly Border _scrim = new();
    private readonly Border _card = new();
    private readonly TextBlock _titleText = new();
    private readonly TextBlock _subtitleText = new();
    private readonly TextBlock _themeLabel = new();
    private readonly TextBlock _themeHelp = new();
    private readonly TextBlock _zoomLabel = new();
    private readonly TextBlock _zoomHelp = new();
    private readonly TextBlock _zoomValueText = new();
    private readonly ToggleSwitch _darkThemeSwitch = new();
    private readonly Button _zoomOutButton = new();
    private readonly Button _zoomInButton = new();
    private readonly Button _resetZoomButton = new();
    private readonly Button _closeButton = new();

    private bool _suppressEvents;

    public event EventHandler? CloseRequested;

    public SettingsPanel()
    {
        IsVisible = false;
        Focusable = true;
        Children.Add(BuildLayout());

        AppSettingsStore.SettingsChanged += AppSettingsChanged;
        DetachedFromVisualTree += (_, _) => AppSettingsStore.SettingsChanged -= AppSettingsChanged;
        KeyDown += SettingsPanel_KeyDown;

        ApplySettings(AppSettingsStore.Current);
    }

    public void Show()
    {
        IsVisible = true;
        Focus();
    }

    public void Hide()
    {
        IsVisible = false;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private Control BuildLayout()
    {
        _scrim.Background = new SolidColorBrush(Color.FromArgb(130, 15, 23, 42));
        _scrim.Child = BuildCard();
        return _scrim;
    }

    private Control BuildCard()
    {
        var content = new StackPanel();

        _titleText.Text = "Settings";
        _titleText.FontSize = 24;
        _titleText.FontWeight = FontWeight.SemiBold;

        _subtitleText.Text = "Appearance settings apply to every open File Browser Desktop view.";
        _subtitleText.Margin = new Thickness(0, 6, 0, 16);
        _subtitleText.TextWrapping = TextWrapping.Wrap;

        content.Children.Add(_titleText);
        content.Children.Add(_subtitleText);
        content.Children.Add(BuildThemeRow());
        content.Children.Add(BuildZoomRow());

        ConfigureActionButton(_closeButton, "Close");
        _closeButton.HorizontalAlignment = HorizontalAlignment.Right;
        _closeButton.Margin = new Thickness(0, 16, 0, 0);
        _closeButton.Click += (_, _) => Hide();
        content.Children.Add(_closeButton);

        _card.Width = 460;
        _card.Padding = new Thickness(18);
        _card.BorderThickness = new Thickness(1);
        _card.CornerRadius = new CornerRadius(12);
        _card.HorizontalAlignment = HorizontalAlignment.Center;
        _card.VerticalAlignment = VerticalAlignment.Center;
        _card.Child = content;
        return _card;
    }

    private Control BuildThemeRow()
    {
        _themeLabel.Text = "Theme";
        _themeLabel.FontWeight = FontWeight.SemiBold;

        _themeHelp.Text = "Switch between the light and dark application theme.";
        _themeHelp.Margin = new Thickness(0, 4, 0, 0);
        _themeHelp.TextWrapping = TextWrapping.Wrap;

        _darkThemeSwitch.OnContent = "Dark";
        _darkThemeSwitch.OffContent = "Light";
        _darkThemeSwitch.VerticalAlignment = VerticalAlignment.Center;
        _darkThemeSwitch.IsCheckedChanged += (_, _) =>
        {
            if (!_suppressEvents)
            {
                AppSettingsStore.SetDarkTheme(_darkThemeSwitch.IsChecked == true);
            }
        };

        return SettingsRow(_themeLabel, _themeHelp, _darkThemeSwitch);
    }

    private Control BuildZoomRow()
    {
        _zoomLabel.Text = "Zoom";
        _zoomLabel.FontWeight = FontWeight.SemiBold;

        _zoomHelp.Text = "Use Ctrl+Plus and Ctrl+Minus anywhere in the app.";
        _zoomHelp.Margin = new Thickness(0, 4, 0, 0);
        _zoomHelp.TextWrapping = TextWrapping.Wrap;

        ConfigureSmallButton(_zoomOutButton, "-");
        _zoomOutButton.Click += (_, _) => AppSettingsStore.AdjustZoom(-0.1);

        _zoomValueText.Width = 56;
        _zoomValueText.TextAlignment = TextAlignment.Center;
        _zoomValueText.VerticalAlignment = VerticalAlignment.Center;

        ConfigureSmallButton(_zoomInButton, "+");
        _zoomInButton.Click += (_, _) => AppSettingsStore.AdjustZoom(0.1);

        ConfigureActionButton(_resetZoomButton, "Reset");
        _resetZoomButton.Margin = new Thickness(8, 0, 0, 0);
        _resetZoomButton.Click += (_, _) => AppSettingsStore.ResetZoom();

        var controls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                _zoomOutButton,
                _zoomValueText,
                _zoomInButton,
                _resetZoomButton,
            },
        };

        return SettingsRow(_zoomLabel, _zoomHelp, controls);
    }

    private static Control SettingsRow(TextBlock label, TextBlock help, Control control)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(0, 0, 0, 18),
        };

        var text = new StackPanel();
        text.Children.Add(label);
        text.Children.Add(help);
        grid.Children.Add(text);

        Grid.SetColumn(control, 1);
        grid.Children.Add(control);
        return grid;
    }

    private void AppSettingsChanged(object? sender, AppSettings settings)
    {
        ApplySettings(settings);
    }

    private void ApplySettings(AppSettings settings)
    {
        _suppressEvents = true;
        _darkThemeSwitch.IsChecked = settings.DarkTheme;
        _suppressEvents = false;

        _zoomValueText.Text = $"{Math.Round(settings.ZoomScale * 100)}%";
        _zoomOutButton.IsEnabled = settings.ZoomScale > 0.81;
        _zoomInButton.IsEnabled = settings.ZoomScale < 1.39;

        var dark = settings.DarkTheme;
        var card = BrushFor(dark ? "#0F172A" : "#FFFFFF");
        var border = BrushFor(dark ? "#334155" : "#D9E2EF");
        var primary = BrushFor(dark ? "#F8FAFC" : "#111827");
        var secondary = BrushFor(dark ? "#CBD5E1" : "#475569");

        _card.Background = card;
        _card.BorderBrush = border;

        _titleText.Foreground = primary;
        _themeLabel.Foreground = primary;
        _zoomLabel.Foreground = primary;
        _subtitleText.Foreground = secondary;
        _themeHelp.Foreground = secondary;
        _zoomHelp.Foreground = secondary;
        _zoomValueText.Foreground = secondary;
        _darkThemeSwitch.Foreground = primary;

        ApplyButtonTheme(_zoomOutButton, dark);
        ApplyButtonTheme(_zoomInButton, dark);
        ApplyButtonTheme(_resetZoomButton, dark);
        ApplyButtonTheme(_closeButton, dark);
    }

    private void SettingsPanel_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
            return;
        }

        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        if (e.Key is Key.Add or Key.OemPlus)
        {
            AppSettingsStore.AdjustZoom(0.1);
            e.Handled = true;
        }
        else if (e.Key is Key.Subtract or Key.OemMinus)
        {
            AppSettingsStore.AdjustZoom(-0.1);
            e.Handled = true;
        }
        else if (e.Key is Key.D0 or Key.NumPad0)
        {
            AppSettingsStore.ResetZoom();
            e.Handled = true;
        }
    }

    private static void ConfigureSmallButton(Button button, string content)
    {
        button.Content = content;
        button.Width = 34;
        button.Height = SettingsButtonHeight;
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        button.VerticalContentAlignment = VerticalAlignment.Center;
        button.BorderThickness = new Thickness(1);
        button.CornerRadius = new CornerRadius(7);
    }

    private static void ConfigureActionButton(Button button, string content)
    {
        button.Content = content;
        button.Width = SettingsActionButtonWidth;
        button.Height = SettingsButtonHeight;
        button.Padding = new Thickness(12, 0);
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        button.VerticalContentAlignment = VerticalAlignment.Center;
        button.BorderThickness = new Thickness(1);
        button.CornerRadius = new CornerRadius(7);
    }

    private static void ApplyButtonTheme(Button button, bool dark)
    {
        button.Background = BrushFor(dark ? "#172033" : "#F8FAFC");
        button.BorderBrush = BrushFor(dark ? "#40516D" : "#CBD5E1");
        button.Foreground = BrushFor(dark ? "#F8FAFC" : "#1E293B");
    }

    private static IBrush BrushFor(string color)
    {
        return SolidColorBrush.Parse(color);
    }
}
