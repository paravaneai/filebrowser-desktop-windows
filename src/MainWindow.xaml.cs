using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using System.IO;
using System.Text.Json;

namespace FileBrowserDesktop;

public sealed class MainWindow : Window
{
    private readonly SshTunnelService _tunnel = new();
    private readonly ProfileStore _profileStore = ProfileStore.Load();
    private readonly CancellationTokenSource _shutdown = new();

    private readonly ComboBox _profileComboBox = new();
    private readonly Button _newProfileButton = new();
    private readonly Button _editProfileButton = new();
    private readonly Button _backButton = new();
    private readonly Button _forwardButton = new();
    private readonly Button _reloadButton = new();
    private readonly Button _homeButton = new();
    private readonly Button _settingsButton = new();
    private readonly SettingsButtonConfig _settingsButtonConfig = SettingsButtonConfig.Load();
    private readonly AppImageConfig _imageConfig = AppImageConfig.Load();
    private readonly Button _reconnectButton = new();
    private readonly TextBlock _addressText = new();
    private readonly TextBlock _startupTitleText = new();
    private readonly TextBlock _startupStatusText = new();
    private readonly TextBlock _statusText = new();
    private readonly Border _statusDot = new();
    private readonly Border _topBar = new();
    private readonly Border _statusBar = new();
    private readonly Border _addressBox = new();
    private readonly Border _startupOverlay = new();
    private readonly Border _startupCard = new();
    private readonly Grid _root = new();
    private readonly Grid _contentGrid = new();
    private readonly LayoutTransformControl _zoomHost = new();
    private readonly SettingsPanel _settingsPanel = new();
    private readonly SetupPage _setupPage;
    private readonly Button _openSetupButton = new();
    private readonly ProgressBar _startupProgress = new();
    private NativeWebView? _browser;

    private ConnectionProfile? _activeProfile;
    private bool _isDarkTheme;
    private double _zoomScale = 1.0;
    private bool _suppressProfileSelectionChanged;
    private bool _browserWasVisibleBeforeSetup;
    private bool _browserWasVisibleBeforeSettings;
    private bool _startupWasVisibleBeforeSetup;
    private bool _settingsButtonIsHovered;

    public MainWindow()
    {
        Title = "File Browser Desktop";
        Width = 1180;
        Height = 760;
        MinWidth = 860;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        _setupPage = new SetupPage(_imageConfig);
        Icon = AssetImageLoader.TryLoadWindowIcon(_imageConfig.WindowIconFileName);
        Content = BuildLayout();

        _setupPage.SaveOpenRequested += SetupPage_SaveOpenRequested;
        _setupPage.CancelRequested += SetupPage_CancelRequested;
        _settingsPanel.CloseRequested += SettingsPanel_CloseRequested;
        ApplySettings(AppSettingsStore.Current);
        SetChromeEnabled(false);
        ShowLoadingScreen("Starting File Browser Desktop...");

        AppSettingsStore.SettingsChanged += AppSettingsChanged;
        Opened += async (_, _) =>
        {
            ApplyScreenAwarePlacement();
            await OnOpenedAsync();
        };
        KeyDown += MainWindow_KeyDown;
        Closing += (_, _) =>
        {
            AppSettingsStore.SettingsChanged -= AppSettingsChanged;
            _setupPage.SaveOpenRequested -= SetupPage_SaveOpenRequested;
            _setupPage.CancelRequested -= SetupPage_CancelRequested;
            _settingsPanel.CloseRequested -= SettingsPanel_CloseRequested;
            _shutdown.Cancel();
            _browser?.Stop();
            _tunnel.Dispose();
        };
    }

    private Control BuildLayout()
    {
        _root.RowDefinitions = new RowDefinitions("Auto,*,Auto");

        _topBar.Child = BuildToolbar();
        Grid.SetRow(_topBar, 0);
        _root.Children.Add(_topBar);

        _contentGrid.Background = BrushFor("#EEF3F8");

        _startupOverlay.Child = BuildStartupOverlay();
        _contentGrid.Children.Add(_startupOverlay);

        _setupPage.HorizontalAlignment = HorizontalAlignment.Stretch;
        _setupPage.VerticalAlignment = VerticalAlignment.Stretch;
        _contentGrid.Children.Add(_setupPage);

        Grid.SetRow(_contentGrid, 1);
        _root.Children.Add(_contentGrid);

        _statusBar.Child = BuildStatusBar();
        Grid.SetRow(_statusBar, 2);
        _root.Children.Add(_statusBar);

        Grid.SetRowSpan(_settingsPanel, 3);
        _settingsPanel.ZIndex = 100;
        _root.Children.Add(_settingsPanel);

        _zoomHost.Child = _root;
        return _zoomHost;
    }

    private Control BuildToolbar()
    {
        var grid = new Grid
        {
            Margin = new Thickness(14, 10),
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,Auto,*,Auto,Auto"),
            VerticalAlignment = VerticalAlignment.Center,
        };

        _profileComboBox.Width = 180;
        _profileComboBox.Height = 32;
        _profileComboBox.MinHeight = 32;
        _profileComboBox.Margin = new Thickness(0, 0, 6, 0);
        _profileComboBox.Padding = new Thickness(10, 0);
        _profileComboBox.VerticalAlignment = VerticalAlignment.Center;
        _profileComboBox.VerticalContentAlignment = VerticalAlignment.Center;
        _profileComboBox.SelectionChanged += async (_, _) => await ProfileSelectionChangedAsync();
        AddToolbarChild(grid, _profileComboBox, 0);

        ConfigureButton(_newProfileButton, "New", 54, UiButtonStyle.Primary);
        _newProfileButton.Click += async (_, _) => await NewProfileAsync();
        AddToolbarChild(grid, _newProfileButton, 1, new Thickness(0, 0, 6, 0));

        ConfigureButton(_editProfileButton, "Edit", 52);
        _editProfileButton.Click += async (_, _) => await EditProfileAsync();
        AddToolbarChild(grid, _editProfileButton, 2, new Thickness(0, 0, 12, 0));

        ConfigureButton(_backButton, "<", 38);
        _backButton.Click += (_, _) =>
        {
            if (_browser?.CanGoBack == true)
            {
                _browser.GoBack();
            }
        };
        AddToolbarChild(grid, _backButton, 3, new Thickness(0, 0, 6, 0));

        ConfigureButton(_forwardButton, ">", 38);
        _forwardButton.Click += (_, _) =>
        {
            if (_browser?.CanGoForward == true)
            {
                _browser.GoForward();
            }
        };
        AddToolbarChild(grid, _forwardButton, 4, new Thickness(0, 0, 6, 0));

        ConfigureButton(_reloadButton, "Reload", 72);
        _reloadButton.Click += async (_, _) =>
        {
            _browser?.Refresh();
            await TryPrefillFileBrowserCredentialsWithRetryAsync();
        };
        AddToolbarChild(grid, _reloadButton, 5, new Thickness(0, 0, 6, 0));

        ConfigureButton(_homeButton, "Home", 64);
        _homeButton.Click += (_, _) =>
        {
            if (_activeProfile is not null)
            {
                EnsureBrowser().Navigate(new Uri(_activeProfile.LocalUri));
                _addressText.Text = _activeProfile.LocalUri;
                _ = TryPrefillFileBrowserCredentialsWithRetryAsync();
            }
        };
        AddToolbarChild(grid, _homeButton, 6, new Thickness(0, 0, 12, 0));

        _addressBox.Height = 32;
        _addressBox.CornerRadius = new CornerRadius(6);
        _addressBox.BorderThickness = new Thickness(1);
        _addressBox.VerticalAlignment = VerticalAlignment.Center;
        _addressBox.Child = _addressText;
        _addressText.Margin = new Thickness(10, 0);
        _addressText.VerticalAlignment = VerticalAlignment.Center;
        _addressText.TextTrimming = TextTrimming.CharacterEllipsis;
        AddToolbarChild(grid, _addressBox, 7);

        ConfigureButton(_reconnectButton, "Reconnect", 98);
        _reconnectButton.Click += async (_, _) => await ReconnectAsync();
        AddToolbarChild(grid, _reconnectButton, 8, new Thickness(12, 0, 0, 0));

        ConfigureButton(_settingsButton, "", _settingsButtonConfig.ButtonSize);
        _settingsButton.Width = _settingsButtonConfig.ButtonSize;
        _settingsButton.Height = _settingsButtonConfig.ButtonSize;
        RefreshSettingsButtonIcon();
        ToolTip.SetTip(_settingsButton, "Settings");
        _settingsButton.Click += (_, _) => ShowSettings();
        _settingsButton.PointerEntered += (_, _) =>
        {
            _settingsButtonIsHovered = true;
            ApplySettingsIconSize();
        };
        _settingsButton.PointerExited += (_, _) =>
        {
            _settingsButtonIsHovered = false;
            ApplySettingsIconSize();
        };
        AddToolbarChild(grid, _settingsButton, 9, new Thickness(12, 0, 0, 0));

        return grid;
    }

    private Control BuildStartupOverlay()
    {
        var panel = new StackPanel
        {
            Width = 460,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var startupLogo = AssetImageLoader.CreateImage(_imageConfig.StartupLogoFileName, _imageConfig.StartupLogoSize, _imageConfig.StartupLogoSize);
        startupLogo.Margin = new Thickness(0, 0, 0, 16);
        startupLogo.HorizontalAlignment = HorizontalAlignment.Center;
        panel.Children.Add(startupLogo);

        _startupTitleText.Text = "File Browser Desktop";
        _startupTitleText.FontSize = 30;
        _startupTitleText.FontWeight = FontWeight.SemiBold;
        _startupTitleText.HorizontalAlignment = HorizontalAlignment.Center;
        panel.Children.Add(_startupTitleText);

        _startupStatusText.Margin = new Thickness(0, 14, 0, 0);
        _startupStatusText.Text = "Starting SSH tunnel...";
        _startupStatusText.FontSize = 15;
        _startupStatusText.TextAlignment = TextAlignment.Center;
        _startupStatusText.TextWrapping = TextWrapping.Wrap;
        panel.Children.Add(_startupStatusText);

        _startupProgress.Margin = new Thickness(0, 22, 0, 0);
        _startupProgress.Height = 6;
        _startupProgress.IsIndeterminate = true;
        panel.Children.Add(_startupProgress);

        ConfigureButton(_openSetupButton, "Open setup", 128, UiButtonStyle.Primary);
        _openSetupButton.HorizontalAlignment = HorizontalAlignment.Center;
        _openSetupButton.Margin = new Thickness(0, 22, 0, 0);
        _openSetupButton.IsVisible = false;
        _openSetupButton.Click += async (_, _) => await NewProfileAsync();
        panel.Children.Add(_openSetupButton);

        _startupCard.Width = 560;
        _startupCard.Padding = new Thickness(38, 34);
        _startupCard.CornerRadius = new CornerRadius(16);
        _startupCard.BorderThickness = new Thickness(1);
        _startupCard.HorizontalAlignment = HorizontalAlignment.Center;
        _startupCard.VerticalAlignment = VerticalAlignment.Center;
        _startupCard.Child = panel;
        return _startupCard;
    }

    private Control BuildStatusBar()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(12, 7),
            VerticalAlignment = VerticalAlignment.Center,
        };

        _statusDot.Width = 9;
        _statusDot.Height = 9;
        _statusDot.CornerRadius = new CornerRadius(5);
        _statusDot.Margin = new Thickness(0, 0, 8, 0);
        _statusDot.VerticalAlignment = VerticalAlignment.Center;
        panel.Children.Add(_statusDot);

        _statusText.FontSize = 12;
        _statusText.VerticalAlignment = VerticalAlignment.Center;
        _statusText.Text = "Starting...";
        panel.Children.Add(_statusText);

        return panel;
    }

    private async Task OnOpenedAsync()
    {
        LoadProfilesIntoComboBox();

        if (_profileStore.Profiles.Count == 0)
        {
            ShowSetupPageForNewProfile();
            return;
        }

        await StartSelectedProfileAsync("Starting SSH tunnel...");
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

        var targetWidth = FitToScreen(availableWidth, preferredRatio: 0.82, defaultSize: 1180, minimumSize: 860, maximumSize: 1400, padding: 48);
        var targetHeight = FitToScreen(availableHeight, preferredRatio: 0.84, defaultSize: 760, minimumSize: 560, maximumSize: 920, padding: 72);

        MinWidth = Math.Min(860, targetWidth);
        MinHeight = Math.Min(560, targetHeight);
        Width = targetWidth;
        Height = targetHeight;

        CenterOnScreen(screen);
    }

    private void CenterOnScreen(Screen screen)
    {
        var scale = screen.Scaling > 0 ? screen.Scaling : 1;
        var workingArea = screen.WorkingArea;
        var windowWidth = (int)Math.Round(Width * scale);
        var windowHeight = (int)Math.Round(Height * scale);
        var x = workingArea.X + Math.Max(0, (workingArea.Width - windowWidth) / 2);
        var y = workingArea.Y + Math.Max(0, (workingArea.Height - windowHeight) / 2);

        Position = new PixelPoint(x, y);
    }

    private static double FitToScreen(
        double available,
        double preferredRatio,
        double defaultSize,
        double minimumSize,
        double maximumSize,
        double padding)
    {
        var upper = Math.Max(320, Math.Min(maximumSize, available - padding));
        var lower = Math.Min(minimumSize, upper);
        var preferred = Math.Max(defaultSize, available * preferredRatio);
        return Math.Clamp(preferred, lower, upper);
    }

    private async Task StartSelectedProfileAsync(string message)
    {
        var profile = _profileComboBox.SelectedItem as ConnectionProfile ?? _profileStore.GetSelectedProfile();
        if (profile is null)
        {
            ShowNoProfileScreen();
            return;
        }

        _activeProfile = profile;

        try
        {
            ShowLoadingScreen($"{message}\n{profile.Name}");
            SetChromeEnabled(false);
            _reconnectButton.IsEnabled = false;
            _editProfileButton.IsEnabled = false;

            await _tunnel.StartAsync(profile, _shutdown.Token);

            SetStatus("Tunnel connected. Loading File Browser...", "#22C55E");
            _startupStatusText.Text = $"Tunnel connected. Loading File Browser for {profile.Name}...";

            _addressText.Text = profile.LocalUri;
            EnsureBrowser().Source = new Uri(profile.LocalUri);

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await Task.Delay(1200, _shutdown.Token);
                ShowBrowser();
                await TryPrefillFileBrowserCredentialsWithRetryAsync();
            });
        }
        catch (Exception ex)
        {
            SetStatus("Connection failed", "#EF4444");
            _startupStatusText.Text = ex.Message;
            _startupOverlay.IsVisible = true;
            HideBrowser();
            SetChromeEnabled(true);
            _reconnectButton.IsEnabled = true;
            _editProfileButton.IsEnabled = true;
        }
    }

    private async Task ProfileSelectionChangedAsync()
    {
        if (_suppressProfileSelectionChanged || _profileComboBox.SelectedItem is not ConnectionProfile profile)
        {
            return;
        }

        _profileStore.SelectedProfileId = profile.Id;
        _profileStore.Save();

        ShowLoadingScreen($"Switching to {profile.Name}...");
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
        _tunnel.Stop();
        await StartSelectedProfileAsync("Starting SSH tunnel...");
    }

    private async Task NewProfileAsync()
    {
        ShowSetupPageForNewProfile();
        await Task.CompletedTask;
    }

    private async Task EditProfileAsync()
    {
        if (_profileComboBox.SelectedItem is not ConnectionProfile selected)
        {
            return;
        }

        ShowSetupPageForEditProfile(selected);
        await Task.CompletedTask;
    }

    private async Task ReconnectAsync()
    {
        if (_activeProfile is null && _profileStore.Profiles.Count == 0)
        {
            ShowSetupPageForNewProfile();
            return;
        }

        ShowLoadingScreen("Restarting SSH tunnel...");
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
        _tunnel.Stop();
        await StartSelectedProfileAsync("Starting SSH tunnel...");
    }

    private void ShowSettings()
    {
        _browserWasVisibleBeforeSettings = _browser?.IsVisible == true;
        HideBrowser();
        _settingsPanel.Show();
    }

    private void SettingsPanel_CloseRequested(object? sender, EventArgs e)
    {
        if (_browserWasVisibleBeforeSettings && _activeProfile is not null && !_setupPage.IsVisible && !_startupOverlay.IsVisible)
        {
            ShowBrowser();
        }

        _browserWasVisibleBeforeSettings = false;
    }

    private void ShowSetupPageForNewProfile()
    {
        CapturePreSetupView();
        _setupPage.StartNewProfile();
        ShowSetupPage("Setup: New connection profile");
    }

    private void ShowSetupPageForEditProfile(ConnectionProfile profile)
    {
        CapturePreSetupView();
        _setupPage.EditProfile(profile);
        ShowSetupPage($"Setup: {profile.Name}");
    }

    private void CapturePreSetupView()
    {
        _browserWasVisibleBeforeSetup = _browser?.IsVisible == true;
        _startupWasVisibleBeforeSetup = _startupOverlay.IsVisible;
    }

    private void ShowSetupPage(string addressText)
    {
        HideBrowser();
        _startupOverlay.IsVisible = false;
        _setupPage.IsVisible = true;
        _startupProgress.IsVisible = false;
        _openSetupButton.IsVisible = false;
        _addressText.Text = addressText;
        SetStatus("Editing setup", "#3B82F6");
        SetChromeEnabled(false);
        SetSetupToolbarMode(true);
        _profileComboBox.IsEnabled = false;
        _newProfileButton.IsEnabled = false;
        _editProfileButton.IsEnabled = false;
        _reconnectButton.IsEnabled = false;
        _setupPage.Focus();
    }

    private async void SetupPage_SaveOpenRequested(object? sender, SetupProfileEventArgs e)
    {
        var index = _profileStore.Profiles.FindIndex(profile => profile.Id == e.Profile.Id);
        if (index >= 0)
        {
            _profileStore.Profiles[index] = e.Profile;
        }
        else
        {
            _profileStore.Profiles.Add(e.Profile);
        }

        _profileStore.SelectedProfileId = e.Profile.Id;
        _profileStore.Save();
        LoadProfilesIntoComboBox();

        ShowLoadingScreen($"Starting {e.Profile.Name}...");
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);
        _tunnel.Stop();
        await StartSelectedProfileAsync("Starting SSH tunnel...");
    }

    private void SetupPage_CancelRequested(object? sender, EventArgs e)
    {
        _setupPage.IsVisible = false;
        _profileComboBox.IsEnabled = true;
        _newProfileButton.IsEnabled = true;
        SetSetupToolbarMode(false);

        if (_profileStore.Profiles.Count == 0)
        {
            ShowNoProfileScreen();
            return;
        }

        if (_browserWasVisibleBeforeSetup && _activeProfile is not null)
        {
            ShowBrowser();
            _addressText.Text = _activeProfile.LocalUri;
            return;
        }

        if (_startupWasVisibleBeforeSetup)
        {
            _startupOverlay.IsVisible = true;
            HideBrowser();
            _setupPage.IsVisible = false;
            SetSetupToolbarMode(false);
            _editProfileButton.IsEnabled = _profileComboBox.SelectedItem is ConnectionProfile;
            _reconnectButton.IsEnabled = _profileComboBox.SelectedItem is ConnectionProfile;
            return;
        }

        ShowNoProfileScreen();
    }

    private void LoadProfilesIntoComboBox()
    {
        _suppressProfileSelectionChanged = true;
        try
        {
            _profileComboBox.ItemsSource = null;
            _profileComboBox.ItemsSource = _profileStore.Profiles;
            _profileComboBox.SelectedItem = _profileStore.GetSelectedProfile();

            var hasProfile = _profileComboBox.SelectedItem is ConnectionProfile;
            _profileComboBox.IsEnabled = true;
            _newProfileButton.IsEnabled = true;
            _editProfileButton.IsEnabled = hasProfile;
            _reconnectButton.IsEnabled = hasProfile;
        }
        finally
        {
            _suppressProfileSelectionChanged = false;
        }
    }

    private void ShowNoProfileScreen()
    {
        _activeProfile = null;
        _tunnel.Stop();
        SetStatus("No connection profile configured", "#F59E0B");
        _startupStatusText.Text = "No connection profile is configured. Open setup to create a private SSH tunnel profile.";
        _startupOverlay.IsVisible = true;
        HideBrowser();
        _setupPage.IsVisible = false;
        _startupProgress.IsVisible = false;
        _openSetupButton.IsVisible = true;
        _addressText.Text = "";
        _profileComboBox.IsEnabled = true;
        _newProfileButton.IsEnabled = true;
        SetSetupToolbarMode(false);
        SetChromeEnabled(false);
        _editProfileButton.IsEnabled = false;
        _reconnectButton.IsEnabled = false;
    }

    private void ShowBrowser()
    {
        EnsureBrowser().IsVisible = true;
        _startupOverlay.IsVisible = false;
        _setupPage.IsVisible = false;
        _startupProgress.IsVisible = false;
        _openSetupButton.IsVisible = false;
        SetStatus("Connected", "#22C55E");
        _profileComboBox.IsEnabled = true;
        _newProfileButton.IsEnabled = true;
        SetSetupToolbarMode(false);
        SetChromeEnabled(true);
        _reconnectButton.IsEnabled = true;
        _editProfileButton.IsEnabled = _profileComboBox.SelectedItem is ConnectionProfile;
    }

    private NativeWebView EnsureBrowser()
    {
        if (_browser is not null)
        {
            return _browser;
        }

        _browser = new NativeWebView
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsVisible = false,
        };

        _contentGrid.Children.Insert(0, _browser);
        return _browser;
    }

    private void HideBrowser()
    {
        if (_browser is not null)
        {
            _browser.IsVisible = false;
        }
    }

    private void SetChromeEnabled(bool enabled)
    {
        _backButton.IsEnabled = enabled && _browser?.CanGoBack == true;
        _forwardButton.IsEnabled = enabled && _browser?.CanGoForward == true;
        _reloadButton.IsEnabled = enabled;
        _homeButton.IsEnabled = enabled && _activeProfile is not null;
    }

    private void SetSetupToolbarMode(bool setupMode)
    {
        _profileComboBox.IsVisible = !setupMode;
        _newProfileButton.IsVisible = !setupMode;
        _editProfileButton.IsVisible = !setupMode;
        _backButton.IsVisible = !setupMode;
        _forwardButton.IsVisible = !setupMode;
        _reloadButton.IsVisible = !setupMode;
        _homeButton.IsVisible = !setupMode;
        _reconnectButton.IsVisible = !setupMode;
    }

    private void SetStatus(string text, string color)
    {
        _statusText.Text = text;
        _statusDot.Background = BrushFor(color);
    }

    private void ShowLoadingScreen(string message)
    {
        SetStatus("Starting SSH tunnel...", "#F59E0B");
        _startupStatusText.Text = message;
        HideBrowser();
        _startupOverlay.IsVisible = true;
        _setupPage.IsVisible = false;
        _startupProgress.IsVisible = true;
        _openSetupButton.IsVisible = false;
        _profileComboBox.IsEnabled = true;
        _newProfileButton.IsEnabled = true;
        SetSetupToolbarMode(false);
    }

    private void ApplySettings(AppSettings settings)
    {
        ApplyTheme(settings.DarkTheme);
        ApplyZoom(settings.ZoomScale);
    }

    private void AppSettingsChanged(object? sender, AppSettings settings)
    {
        ApplySettings(settings);
    }

    private void ApplyTheme(bool dark)
    {
        _isDarkTheme = dark;
        RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;

        var windowBackground = BrushFor(dark ? "#101820" : "#EEF3F8");
        var surface = BrushFor(dark ? "#0B1120" : "#FFFFFF");
        var elevated = BrushFor(dark ? "#172033" : "#F2F4F7");
        var border = BrushFor(dark ? "#2E3B52" : "#D8DEE8");
        var innerBorder = BrushFor(dark ? "#40516D" : "#D4DAE4");
        var primaryText = BrushFor(dark ? "#F8FAFC" : "#111827");
        var secondaryText = BrushFor(dark ? "#CBD5E1" : "#475569");
        var addressText = BrushFor(dark ? "#E2E8F0" : "#334155");

        Background = windowBackground;
        _root.Background = windowBackground;
        _contentGrid.Background = windowBackground;
        _topBar.Background = surface;
        _topBar.BorderBrush = border;
        _topBar.BorderThickness = new Thickness(0, 0, 0, 1);
        _statusBar.Background = surface;
        _statusBar.BorderBrush = border;
        _statusBar.BorderThickness = new Thickness(0, 1, 0, 0);
        _startupOverlay.Background = windowBackground;
        _startupCard.Background = BrushFor(dark ? "#0F172A" : "#FFFFFF");
        _startupCard.BorderBrush = BrushFor(dark ? "#334155" : "#D9E2EF");
        _addressBox.Background = elevated;
        _addressBox.BorderBrush = innerBorder;
        _startupTitleText.Foreground = primaryText;
        _startupStatusText.Foreground = secondaryText;
        _addressText.Foreground = addressText;
        _statusText.Foreground = secondaryText;

        ApplyButtonTheme(_newProfileButton, primary: true);
        ApplyButtonTheme(_openSetupButton, primary: true);
        ApplyButtonTheme(_editProfileButton, primary: false);
        ApplyButtonTheme(_backButton, primary: false);
        ApplyButtonTheme(_forwardButton, primary: false);
        ApplyButtonTheme(_reloadButton, primary: false);
        ApplyButtonTheme(_homeButton, primary: false);
        ApplyButtonTheme(_settingsButton, primary: false);
        ApplyButtonTheme(_reconnectButton, primary: false);
        ApplySettingsButtonConfig();
    }

    private void ApplyZoom(double zoomScale)
    {
        _zoomScale = AppSettings.ClampZoom(zoomScale);
        _zoomHost.LayoutTransform = new ScaleTransform(_zoomScale, _zoomScale);
    }

    private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
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

    private async Task TryPrefillFileBrowserCredentialsWithRetryAsync()
    {
        for (var attempt = 0; attempt < 12; attempt++)
        {
            if (_shutdown.IsCancellationRequested || _browser is null || !_browser.IsVisible)
            {
                return;
            }

            await TryPrefillFileBrowserCredentialsAsync();

            try
            {
                await Task.Delay(500, _shutdown.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task TryPrefillFileBrowserCredentialsAsync()
    {
        if (_activeProfile is null || !CredentialManager.IsSupported)
        {
            return;
        }

        StoredCredential? credential;
        try
        {
            credential = CredentialManager.ReadFileBrowserCredential(_activeProfile.Id);
        }
        catch
        {
            return;
        }

        if (credential is null)
        {
            return;
        }

        var username = JsonSerializer.Serialize(credential.Username);
        var password = JsonSerializer.Serialize(credential.Password);
        var script = $$"""
            (() => {
              const username = {{username}};
              const password = {{password}};
              const passwordInput = document.querySelector('input[type="password"]') || document.querySelector('input[name="password"]') || document.querySelector('#password');
              if (!passwordInput) return false;
              const usernameInput = document.querySelector('input[name="username"]') || document.querySelector('input[name="user"]') || document.querySelector('#username') || document.querySelector('#user') || document.querySelector('input[type="text"]') || document.querySelector('input:not([type])');
              if (!usernameInput) return false;
              const setValue = (input, value) => {
                input.focus();
                input.value = value;
                input.dispatchEvent(new Event('input', { bubbles: true }));
                input.dispatchEvent(new Event('change', { bubbles: true }));
                input.dispatchEvent(new KeyboardEvent('keyup', { bubbles: true }));
              };
              setValue(usernameInput, username);
              setValue(passwordInput, password);
              return true;
            })();
            """;

        try
        {
            if (_browser is not null)
            {
                await _browser.InvokeScript(script);
            }
        }
        catch
        {
            // Platform WebView may not be ready or the page may not be the login form.
        }
    }

    private static void ConfigureButton(Button button, string content, double minWidth, UiButtonStyle style = UiButtonStyle.Neutral)
    {
        UiTheme.ConfigureToolbarButton(button, content, minWidth, style);
    }

    private void ApplyButtonTheme(Button button, bool primary)
    {
        UiTheme.ApplyButtonTheme(button, primary ? UiButtonStyle.Primary : UiButtonStyle.Neutral, _isDarkTheme);
    }

    private void ApplySettingsButtonConfig()
    {
        RefreshSettingsButtonIcon();

        if (_settingsButtonConfig.RemoveButtonBorder)
        {
            _settingsButton.BorderThickness = new Thickness(0);
            _settingsButton.BorderBrush = Brushes.Transparent;
        }

        if (_settingsButtonConfig.RemoveButtonBackground)
        {
            _settingsButton.Background = Brushes.Transparent;
        }

        ApplySettingsIconSize();
    }

    private void RefreshSettingsButtonIcon()
    {
        _settingsButton.Content = AssetIconLoader.CreateSettingsIcon(_settingsButtonConfig, _isDarkTheme);
        ApplySettingsIconSize();
    }

    private void ApplySettingsIconSize()
    {
        var iconSize = _settingsButtonConfig.ShrinkIconOnHover && _settingsButtonIsHovered
            ? _settingsButtonConfig.HoverIconSize
            : _settingsButtonConfig.IconSize;

        if (_settingsButton.Content is PathIcon icon)
        {
            icon.Width = iconSize;
            icon.Height = iconSize;
            icon.Foreground = _settingsButton.Foreground;
        }
        else if (_settingsButton.Content is Image image)
        {
            image.Width = iconSize;
            image.Height = iconSize;
        }
    }

    private static void AddToolbarChild(Grid grid, Control child, int column, Thickness? margin = null)
    {
        if (margin is not null)
        {
            child.Margin = margin.Value;
        }

        Grid.SetColumn(child, column);
        grid.Children.Add(child);
    }

    private static IBrush BrushFor(string color)
    {
        return SolidColorBrush.Parse(color);
    }

}
