using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FileBrowserDesktop;

public partial class MainWindow : Window
{
    private readonly SshTunnelService _tunnel = new();
    private readonly ProfileStore _profileStore = ProfileStore.Load();
    private readonly CancellationTokenSource _shutdown = new();
    private ConnectionProfile? _activeProfile;
    private bool _webViewReady;
    private bool _isDarkTheme;
    private bool _suppressProfileSelectionChanged;

    public MainWindow()
    {
        InitializeComponent();
        SetWindowIcon();
        ApplyTheme(false);
        SetChromeEnabled(false);
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        ApplyNativeTitleBarTheme();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        LoadProfilesIntoComboBox();

        if (_profileStore.Profiles.Count == 0)
        {
            if (!CreateProfileFromWizard())
            {
                ShowNoProfileScreen();
                return;
            }
        }

        await StartSelectedProfileAsync("Starting SSH tunnel...");
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _shutdown.Cancel();
        Browser.Dispose();
        _tunnel.Dispose();
    }

    private async Task StartSelectedProfileAsync(string message)
    {
        var profile = ProfileComboBox.SelectedItem as ConnectionProfile ?? _profileStore.GetSelectedProfile();
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
            ReconnectButton.IsEnabled = false;
            EditProfileButton.IsEnabled = false;

            await _tunnel.StartAsync(profile, _shutdown.Token);

            SetStatus("Tunnel connected. Loading File Browser...", "#22C55E");
            StartupStatusText.Text = $"Tunnel connected. Loading File Browser for {profile.Name}...";

            await InitializeWebViewAsync();
            Browser.Source = new Uri(profile.LocalUri);
            AddressText.Text = profile.LocalUri;
        }
        catch (Exception ex)
        {
            SetStatus("Connection failed", "#EF4444");
            StartupStatusText.Text = ex.Message;
            StartupOverlay.Visibility = Visibility.Visible;
            Browser.Visibility = Visibility.Hidden;
            SetChromeEnabled(true);
            ReconnectButton.IsEnabled = true;
            EditProfileButton.IsEnabled = true;
        }
    }

    private async Task InitializeWebViewAsync()
    {
        if (_webViewReady)
        {
            return;
        }

        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FileBrowserDesktop",
            "WebView2");

        Directory.CreateDirectory(userDataFolder);

        try
        {
            var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await Browser.EnsureCoreWebView2Async(environment);
        }
        catch (WebView2RuntimeNotFoundException ex)
        {
            throw new InvalidOperationException(
                "Microsoft Edge WebView2 Runtime is not installed. Install it from https://developer.microsoft.com/microsoft-edge/webview2/",
                ex);
        }

        Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
#if DEBUG
        Browser.CoreWebView2.Settings.AreDevToolsEnabled = true;
#else
        Browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
#endif
        Browser.CoreWebView2.DocumentTitleChanged += (_, _) =>
        {
            Title = string.IsNullOrWhiteSpace(Browser.CoreWebView2.DocumentTitle)
                ? "File Browser Desktop"
                : $"{Browser.CoreWebView2.DocumentTitle} - File Browser Desktop";
        };

        _webViewReady = true;
    }

    private async void ProfileComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_suppressProfileSelectionChanged || ProfileComboBox.SelectedItem is not ConnectionProfile profile)
        {
            return;
        }

        _profileStore.SelectedProfileId = profile.Id;
        _profileStore.Save();

        ShowLoadingScreen($"Switching to {profile.Name}...");
        await Dispatcher.Yield(DispatcherPriority.Render);
        _tunnel.Stop();
        await StartSelectedProfileAsync("Starting SSH tunnel...");
    }

    private async void NewProfileButton_Click(object sender, RoutedEventArgs e)
    {
        if (!CreateProfileFromWizard())
        {
            return;
        }

        await StartSelectedProfileAsync("Starting SSH tunnel...");
    }

    private async void EditProfileButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileComboBox.SelectedItem is not ConnectionProfile selected)
        {
            return;
        }

        var editor = new ProfileEditorWindow(selected)
        {
            Owner = this,
        };

        if (editor.ShowDialog() != true)
        {
            return;
        }

        var index = _profileStore.Profiles.FindIndex(profile => profile.Id == selected.Id);
        if (index >= 0)
        {
            _profileStore.Profiles[index] = editor.Profile;
            _profileStore.SelectedProfileId = editor.Profile.Id;
            _profileStore.Save();
            LoadProfilesIntoComboBox();

            ShowLoadingScreen($"Restarting {editor.Profile.Name}...");
            await Dispatcher.Yield(DispatcherPriority.Render);
            _tunnel.Stop();
            await StartSelectedProfileAsync("Starting SSH tunnel...");
        }
    }

    private async void ReconnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is null)
        {
            if (!CreateProfileFromWizard())
            {
                return;
            }
        }

        ShowLoadingScreen("Restarting SSH tunnel...");
        await Dispatcher.Yield(DispatcherPriority.Render);
        _tunnel.Stop();
        await StartSelectedProfileAsync("Starting SSH tunnel...");
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Browser.CanGoBack)
        {
            Browser.GoBack();
        }
    }

    private void ForwardButton_Click(object sender, RoutedEventArgs e)
    {
        if (Browser.CanGoForward)
        {
            Browser.GoForward();
        }
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        Browser.Reload();
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeProfile is not null)
        {
            Browser.Source = new Uri(_activeProfile.LocalUri);
        }
    }

    private void ThemeToggleButton_Checked(object sender, RoutedEventArgs e)
    {
        ApplyTheme(true);
    }

    private void ThemeToggleButton_Unchecked(object sender, RoutedEventArgs e)
    {
        ApplyTheme(false);
    }

    private void Browser_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        AddressText.Text = e.Uri;
        SetStatus("Loading...", "#F59E0B");
    }

    private void Browser_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        Browser.Visibility = Visibility.Visible;
        StartupOverlay.Visibility = Visibility.Collapsed;
        SetStatus(e.IsSuccess ? "Connected" : $"Navigation failed: {e.WebErrorStatus}", e.IsSuccess ? "#22C55E" : "#EF4444");
        SetChromeEnabled(true);
        ReconnectButton.IsEnabled = true;
        EditProfileButton.IsEnabled = ProfileComboBox.SelectedItem is ConnectionProfile;

        if (e.IsSuccess)
        {
            _ = TryPrefillFileBrowserCredentialsAsync();
        }
    }

    private bool CreateProfileFromDialog()
    {
        var profile = new ConnectionProfile
        {
            Name = $"Server {_profileStore.Profiles.Count + 1}",
        };

        var editor = new ProfileEditorWindow(profile)
        {
            Owner = this,
        };

        if (editor.ShowDialog() != true)
        {
            return false;
        }

        _profileStore.Profiles.Add(editor.Profile);
        _profileStore.SelectedProfileId = editor.Profile.Id;
        _profileStore.Save();
        LoadProfilesIntoComboBox();
        return true;
    }

    private bool CreateProfileFromWizard()
    {
        var wizard = new OnboardingWizardWindow
        {
            Owner = this,
        };

        if (wizard.ShowDialog() != true || wizard.Profile is null)
        {
            return false;
        }

        _profileStore.Profiles.Add(wizard.Profile);
        _profileStore.SelectedProfileId = wizard.Profile.Id;
        _profileStore.Save();
        LoadProfilesIntoComboBox();
        return true;
    }

    private void LoadProfilesIntoComboBox()
    {
        _suppressProfileSelectionChanged = true;
        try
        {
            ProfileComboBox.ItemsSource = null;
            ProfileComboBox.ItemsSource = _profileStore.Profiles;
            ProfileComboBox.DisplayMemberPath = nameof(ConnectionProfile.Name);
            ProfileComboBox.SelectedItem = _profileStore.GetSelectedProfile();

            var hasProfile = ProfileComboBox.SelectedItem is ConnectionProfile;
            EditProfileButton.IsEnabled = hasProfile;
            ReconnectButton.IsEnabled = hasProfile;
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
        StartupStatusText.Text = "Create a connection profile to start. Profiles store only SSH host, ports, and tunnel settings.";
        StartupOverlay.Visibility = Visibility.Visible;
        Browser.Visibility = Visibility.Hidden;
        AddressText.Text = "";
        SetChromeEnabled(false);
        EditProfileButton.IsEnabled = false;
        ReconnectButton.IsEnabled = false;
    }

    private void SetChromeEnabled(bool enabled)
    {
        BackButton.IsEnabled = enabled && Browser.CanGoBack;
        ForwardButton.IsEnabled = enabled && Browser.CanGoForward;
        ReloadButton.IsEnabled = enabled;
        HomeButton.IsEnabled = enabled && _activeProfile is not null;
    }

    private void SetStatus(string text, string color)
    {
        StatusText.Text = text;
        StatusDot.Fill = BrushFor(color);
    }

    private void SetWindowIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "filebrowser.ico");
        if (!File.Exists(iconPath))
        {
            iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "filebrowser.ico");
        }

        if (File.Exists(iconPath))
        {
            Icon = BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
        }
    }

    private void ApplyTheme(bool dark)
    {
        _isDarkTheme = dark;

        SetThemeResource("PrimaryTextBrush", dark ? "#F8FAFC" : "#111827");
        SetThemeResource("DisabledTextBrush", dark ? "#8EA0B8" : "#94A3B8");
        SetThemeResource("ControlBackgroundBrush", dark ? "#172033" : "#F2F4F7");
        SetThemeResource("ControlHoverBackgroundBrush", dark ? "#22304A" : "#E7ECF3");
        SetThemeResource("ControlPressedBackgroundBrush", dark ? "#2C3A56" : "#DCE4EF");
        SetThemeResource("ControlBorderBrush", dark ? "#40516D" : "#D4DAE4");
        SetThemeResource("DisabledBackgroundBrush", dark ? "#111827" : "#EEF2F7");
        SetThemeResource("DisabledBorderBrush", dark ? "#263246" : "#DCE3EC");
        SetThemeResource("SwitchBackgroundBrush", dark ? "#334155" : "#D7DEE8");
        SetThemeResource("SwitchCheckedBackgroundBrush", "#2563EB");
        SetThemeResource("SwitchThumbBrush", dark ? "#E0F2FE" : "#FFFFFF");
        SetThemeResource("SwitchHoverBorderBrush", dark ? "#60A5FA" : "#93C5FD");

        var windowBackground = BrushFor(dark ? "#101820" : "#F6F7F9");
        var surface = BrushFor(dark ? "#0B1120" : "#FFFFFF");
        var elevated = BrushFor(dark ? "#172033" : "#F2F4F7");
        var border = BrushFor(dark ? "#2E3B52" : "#D8DEE8");
        var innerBorder = BrushFor(dark ? "#40516D" : "#D4DAE4");
        var primaryText = BrushFor(dark ? "#F8FAFC" : "#111827");
        var secondaryText = BrushFor(dark ? "#CBD5E1" : "#475569");
        var addressText = BrushFor(dark ? "#E2E8F0" : "#334155");

        Background = windowBackground;
        RootGrid.Background = windowBackground;
        TopBar.Background = surface;
        TopBar.BorderBrush = border;
        StatusBar.Background = surface;
        StatusBar.BorderBrush = border;
        StartupOverlay.Background = windowBackground;
        AddressBox.Background = elevated;
        AddressBox.BorderBrush = innerBorder;

        StartupTitleText.Foreground = primaryText;
        StartupStatusText.Foreground = secondaryText;
        AddressText.Foreground = addressText;
        StatusText.Foreground = secondaryText;
        ProfileComboBox.Background = elevated;
        ProfileComboBox.BorderBrush = innerBorder;
        ProfileComboBox.Foreground = primaryText;

        ApplyNativeTitleBarTheme();
    }

    private void ShowLoadingScreen(string message)
    {
        SetStatus("Starting SSH tunnel...", "#F59E0B");
        StartupStatusText.Text = message;
        Browser.Visibility = Visibility.Hidden;
        StartupOverlay.Visibility = Visibility.Visible;
        StartupOverlay.Background = BrushFor(_isDarkTheme ? "#101820" : "#F6F7F9");
    }

    private void SetThemeResource(string key, string color)
    {
        Resources[key] = BrushFor(color);
    }

    private void ApplyNativeTitleBarTheme()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var useDark = _isDarkTheme ? 1 : 0;
            _ = DwmSetWindowAttribute(handle, DwmUseImmersiveDarkMode, ref useDark, sizeof(int));

            var captionColor = _isDarkTheme ? ToColorRef(11, 17, 32) : ToColorRef(246, 247, 249);
            var textColor = _isDarkTheme ? ToColorRef(248, 250, 252) : ToColorRef(17, 24, 39);
            var borderColor = _isDarkTheme ? ToColorRef(46, 59, 82) : ToColorRef(216, 222, 232);

            _ = DwmSetWindowAttribute(handle, DwmCaptionColor, ref captionColor, sizeof(int));
            _ = DwmSetWindowAttribute(handle, DwmTextColor, ref textColor, sizeof(int));
            _ = DwmSetWindowAttribute(handle, DwmBorderColor, ref borderColor, sizeof(int));
        }
        catch
        {
            // Older Windows builds may ignore these DWM attributes.
        }
    }

    private static Brush BrushFor(string color)
    {
        return (Brush)new BrushConverter().ConvertFromString(color)!;
    }

    private static int ToColorRef(byte red, byte green, byte blue)
    {
        return red | (green << 8) | (blue << 16);
    }

    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmBorderColor = 34;
    private const int DwmCaptionColor = 35;
    private const int DwmTextColor = 36;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);

    private async Task TryPrefillFileBrowserCredentialsAsync()
    {
        if (_activeProfile is null || Browser.CoreWebView2 is null)
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

              const passwordInput =
                document.querySelector('input[type="password"]') ||
                document.querySelector('input[name="password"]') ||
                document.querySelector('#password');

              if (!passwordInput) return false;

              const usernameInput =
                document.querySelector('input[name="username"]') ||
                document.querySelector('#username') ||
                document.querySelector('input[type="text"]') ||
                document.querySelector('input:not([type])');

              if (!usernameInput) return false;

              const setValue = (input, value) => {
                input.focus();
                input.value = value;
                input.dispatchEvent(new Event('input', { bubbles: true }));
                input.dispatchEvent(new Event('change', { bubbles: true }));
              };

              setValue(usernameInput, username);
              setValue(passwordInput, password);
              return true;
            })();
            """;

        try
        {
            await Browser.CoreWebView2.ExecuteScriptAsync(script);
        }
        catch
        {
            // The page may not be the File Browser login page yet.
        }
    }
}
