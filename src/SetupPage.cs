using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using System.IO;
using System.Net.Http;

namespace FileBrowserDesktop;

public sealed class SetupProfileEventArgs(ConnectionProfile profile, bool isNewProfile) : EventArgs
{
    public ConnectionProfile Profile { get; } = profile;
    public bool IsNewProfile { get; } = isNewProfile;
}

public sealed class SetupPage : UserControl
{
    private CancellationTokenSource _cancellation = new();

    private readonly Grid _root = new();
    private readonly Border _headerBar = new();
    private readonly Border _footerBar = new();
    private readonly Border _headerIconFrame = new();
    private readonly ScrollViewer _setupScrollViewer = new();
    private readonly TextBlock _headerTitleText = new();
    private readonly TextBlock _headerSubtitleText = new();
    private readonly StackPanel _guideStack = new();

    private readonly RadioButton _existingRadioButton = new();
    private readonly RadioButton _installRadioButton = new();
    private readonly CheckBox _alreadyInstalledCheckBox = new();
    private readonly TextBox _nameTextBox = new();
    private readonly TextBox _sshUserTextBox = new();
    private readonly TextBox _sshHostTextBox = new();
    private readonly TextBox _sshPortTextBox = new();
    private readonly TextBox _sshIdentityFileTextBox = new();
    private readonly TextBox _localPortTextBox = new();
    private readonly TextBox _remoteHostTextBox = new();
    private readonly TextBox _remotePortTextBox = new();
    private readonly TextBox _serverRootTextBox = new();
    private readonly CheckBox _saveFileBrowserCredentialCheckBox = new();
    private readonly TextBox _fileBrowserUsernameTextBox = new();
    private readonly TextBox _fileBrowserPasswordBox = new();
    private readonly TextBlock _credentialStatusText = new();
    private readonly Button _deleteCredentialButton = new();
    private readonly TextBox _logTextBox = new();
    private readonly TextBlock _errorText = new();
    private readonly Button _testSshButton = new();
    private readonly Button _installButton = new();
    private readonly Button _testTunnelButton = new();
    private readonly Button _cancelButton = new();
    private readonly Button _saveOpenButton = new();
    private readonly Border _setupPathCard = new();
    private readonly Border _profileCard = new();
    private readonly Border _credentialCard = new();
    private readonly Border _serverSetupCard = new();
    private readonly Border _actionsCard = new();

    private readonly List<Border> _cards = [];
    private readonly List<Border> _guideNumberBadges = [];
    private readonly List<TextBlock> _primaryTexts = [];
    private readonly List<TextBlock> _secondaryTexts = [];
    private readonly List<TextBlock> _labelTexts = [];
    private readonly List<Button> _neutralButtons = [];
    private readonly List<Button> _primaryButtons = [];
    private readonly List<Button> _guideButtons = [];
    private readonly List<TextBox> _textBoxes = [];
    private readonly List<RadioButton> _radioButtons = [];
    private readonly List<CheckBox> _checkBoxes = [];
    private readonly AppImageConfig _imageConfig;

    private StoredCredential? _existingFileBrowserCredential;
    private ConnectionProfile _workingProfile = new();
    private bool _isNewProfile = true;
    private bool _isDarkTheme;

    public event EventHandler<SetupProfileEventArgs>? SaveOpenRequested;
    public event EventHandler? CancelRequested;

    public SetupPage(AppImageConfig imageConfig)
    {
        _imageConfig = imageConfig;
        Focusable = true;
        IsVisible = false;
        Content = BuildLayout();

        _existingRadioButton.IsChecked = true;
        _fileBrowserPasswordBox.PasswordChar = '*';

        AppSettingsStore.SettingsChanged += AppSettingsChanged;
        DetachedFromVisualTree += (_, _) =>
        {
            AppSettingsStore.SettingsChanged -= AppSettingsChanged;
            _cancellation.Cancel();
            _cancellation.Dispose();
        };

        ApplySettings(AppSettingsStore.Current);
        SetSetupModeState();
        StartNewProfile();
    }

    public void StartNewProfile()
    {
        ResetCancellation();
        _isNewProfile = true;
        var profile = new ConnectionProfile
        {
            Name = "My Server",
            SshPort = 22,
            LocalHost = "127.0.0.1",
            LocalPort = 18080,
            RemoteHost = "127.0.0.1",
            RemotePort = 8080,
        };

        LoadProfile(profile);
        _existingRadioButton.IsChecked = true;
        _alreadyInstalledCheckBox.IsChecked = false;
        _headerTitleText.Text = "Set Up File Browser Desktop";
        _headerSubtitleText.Text = "Create a private SSH tunnel profile, test the connection, and open your server's File Browser instance.";
        _saveOpenButton.Content = "Connect";
        _logTextBox.Text = "";
        _errorText.Text = "";
        SetSetupModeState();
        Focus();
    }

    public void EditProfile(ConnectionProfile profile)
    {
        ResetCancellation();
        _isNewProfile = false;
        LoadProfile(profile);
        _existingRadioButton.IsChecked = true;
        _headerTitleText.Text = "Edit Connection Profile";
        _headerSubtitleText.Text = "Update SSH tunnel settings, saved login details, and server setup options from the same desktop surface.";
        _saveOpenButton.Content = "Connect";
        _logTextBox.Text = "";
        _errorText.Text = "";
        SetSetupModeState();
        Focus();
    }

    private void LoadProfile(ConnectionProfile profile)
    {
        _workingProfile = profile.Clone();
        _nameTextBox.Text = _workingProfile.Name;
        _sshUserTextBox.Text = _workingProfile.SshUser;
        _sshHostTextBox.Text = _workingProfile.SshHost;
        _sshPortTextBox.Text = _workingProfile.SshPort.ToString();
        _sshIdentityFileTextBox.Text = _workingProfile.SshIdentityFile;
        _localPortTextBox.Text = _workingProfile.LocalPort.ToString();
        _remoteHostTextBox.Text = _workingProfile.RemoteHost;
        _remotePortTextBox.Text = _workingProfile.RemotePort.ToString();
        _serverRootTextBox.Text = "/";
        LoadSavedCredential(_workingProfile.Id);
    }

    private Control BuildLayout()
    {
        _root.RowDefinitions = new RowDefinitions("Auto,*,Auto");

        _headerBar.Child = BuildHeader();
        _headerBar.BorderThickness = new Thickness(0, 0, 0, 1);
        Grid.SetRow(_headerBar, 0);
        _root.Children.Add(_headerBar);

        var body = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("250,*"),
        };

        var guidePanel = BuildGuidePanel();
        guidePanel.Margin = new Thickness(14, 18, 0, 18);
        Grid.SetColumn(guidePanel, 0);
        body.Children.Add(guidePanel);

        var panel = new StackPanel();
        panel.MaxWidth = 1120;
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        panel.Margin = new Thickness(0, 0, 16, 0);
        panel.Children.Add(BuildSetupPathSection());
        panel.Children.Add(BuildProfileSection());
        panel.Children.Add(BuildCredentialSection());
        panel.Children.Add(BuildServerSetupSection());
        panel.Children.Add(BuildActionsSection());

        _errorText.Margin = new Thickness(0, 12, 0, 0);
        _errorText.TextWrapping = TextWrapping.Wrap;
        panel.Children.Add(_errorText);
        panel.Children.Add(new Border { Height = 72 });

        _setupScrollViewer.Content = panel;
        _setupScrollViewer.Margin = new Thickness(16, 18, 0, 18);
        Grid.SetColumn(_setupScrollViewer, 1);
        body.Children.Add(_setupScrollViewer);

        Grid.SetRow(body, 1);
        _root.Children.Add(body);

        _footerBar.Child = BuildFooter();
        _footerBar.BorderThickness = new Thickness(0, 1, 0, 0);
        Grid.SetRow(_footerBar, 2);
        _root.Children.Add(_footerBar);

        return _root;
    }

    private Control BuildHeader()
    {
        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            Margin = new Thickness(24, 20),
        };

        _headerIconFrame.Width = _imageConfig.SetupHeaderFrameSize;
        _headerIconFrame.Height = _imageConfig.SetupHeaderFrameSize;
        _headerIconFrame.BorderThickness = _imageConfig.ShowSetupHeaderIconBorder ? new Thickness(1) : new Thickness(0);
        _headerIconFrame.CornerRadius = new CornerRadius(12);
        var headerLogo = AssetImageLoader.CreateImage(
            _imageConfig.SetupHeaderLogoFileName,
            _imageConfig.SetupHeaderLogoSize,
            _imageConfig.SetupHeaderLogoSize);
        headerLogo.RenderTransform = new TranslateTransform(
            _imageConfig.SetupHeaderLogoOffsetX,
            _imageConfig.SetupHeaderLogoOffsetY);
        _headerIconFrame.Child = headerLogo;
        header.Children.Add(_headerIconFrame);

        var headerText = new StackPanel
        {
            Margin = new Thickness(14, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(headerText, 1);

        _headerTitleText.FontSize = 25;
        _headerTitleText.FontWeight = FontWeight.SemiBold;
        _headerTitleText.TextWrapping = TextWrapping.Wrap;
        _primaryTexts.Add(_headerTitleText);

        _headerSubtitleText.Margin = new Thickness(0, 5, 0, 0);
        _headerSubtitleText.TextWrapping = TextWrapping.Wrap;
        _secondaryTexts.Add(_headerSubtitleText);

        headerText.Children.Add(_headerTitleText);
        headerText.Children.Add(_headerSubtitleText);
        header.Children.Add(headerText);
        return header;
    }

    private Control BuildGuidePanel()
    {
        _guideStack.Children.Add(PrimaryText("Private by default", 18, FontWeight.SemiBold));
        _guideStack.Children.Add(SecondaryText("File Browser should stay bound to localhost on the server. This app reaches it through SSH.", new Thickness(0, 8, 0, 18)));
        _guideStack.Children.Add(GuideButton("1", "Setup Path", () => _setupPathCard));
        _guideStack.Children.Add(GuideButton("2", "Connection", () => _profileCard));
        _guideStack.Children.Add(GuideButton("3", "Login", () => _credentialCard));
        _guideStack.Children.Add(GuideButton("4", "Server", () => _serverSetupCard));
        _guideStack.Children.Add(GuideButton("5", "Test & Save", () => _actionsCard));
        return Card(_guideStack);
    }

    private Control GuideButton(string number, string text, Func<Control> target)
    {
        var badge = new Border
        {
            Width = 26,
            Height = 26,
            CornerRadius = new CornerRadius(13),
            Child = new TextBlock
            {
                Text = number,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeight.SemiBold,
            },
        };
        _guideNumberBadges.Add(badge);

        var label = new TextBlock
        {
            Text = text,
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };
        _secondaryTexts.Add(label);

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.Children.Add(badge);
        Grid.SetColumn(label, 1);
        row.Children.Add(label);

        var button = new Button
        {
            Content = row,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
        };
        button.Click += (_, _) => target().BringIntoView();
        _guideButtons.Add(button);
        return button;
    }

    private Control BuildSetupPathSection()
    {
        _existingRadioButton.Content = "Connect to an existing File Browser instance";
        _existingRadioButton.GroupName = "SetupPath";
        _existingRadioButton.Margin = new Thickness(0, 10, 0, 4);
        _existingRadioButton.IsCheckedChanged += (_, _) => SetSetupModeState();
        _radioButtons.Add(_existingRadioButton);

        _installRadioButton.Content = "Install or configure File Browser on a server";
        _installRadioButton.GroupName = "SetupPath";
        _installRadioButton.IsCheckedChanged += (_, _) => SetSetupModeState();
        _radioButtons.Add(_installRadioButton);

        var stack = new StackPanel();
        stack.Children.Add(PrimaryText("Setup Path", 15, FontWeight.SemiBold));
        stack.Children.Add(SecondaryText("Use an existing private File Browser instance, or run the bundled safe setup script over SSH.", new Thickness(0, 6, 0, 8)));
        stack.Children.Add(_existingRadioButton);
        stack.Children.Add(_installRadioButton);
        return Card(stack, _setupPathCard);
    }

    private Control BuildProfileSection()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("170,*,24,170,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto"),
        };

        var title = PrimaryText("Connection Profile", 15, FontWeight.SemiBold);
        Grid.SetRow(title, 0);
        Grid.SetColumnSpan(title, 5);
        grid.Children.Add(title);

        AddProfileRow(grid, 1, "Profile name", _nameTextBox);
        AddProfileRow(grid, 2, "SSH username", _sshUserTextBox);
        AddProfileRow(grid, 3, "SSH host", _sshHostTextBox);
        AddPairedProfileRow(grid, 4, "SSH port", _sshPortTextBox, "Local port", _localPortTextBox);

        AddProfileRow(grid, 5, "SSH key file", _sshIdentityFileTextBox, spanActionColumn: false);
        var browseButton = NeutralButton("Browse", 82);
        browseButton.Margin = new Thickness(0, 0, 0, 10);
        browseButton.Click += async (_, _) => await BrowseIdentityFileAsync();
        Grid.SetRow(browseButton, 5);
        Grid.SetColumn(browseButton, 4);
        grid.Children.Add(browseButton);

        AddProfileRow(grid, 6, "Remote File Browser host", _remoteHostTextBox);
        AddProfileRow(grid, 7, "Remote File Browser port", _remotePortTextBox);

        return Card(grid, _profileCard);
    }

    private Control BuildCredentialSection()
    {
        _saveFileBrowserCredentialCheckBox.Margin = new Thickness(0, 10, 0, 10);
        _saveFileBrowserCredentialCheckBox.Content = "Save File Browser login for this profile";
        _saveFileBrowserCredentialCheckBox.IsEnabled = CredentialManager.IsSupported;
        _checkBoxes.Add(_saveFileBrowserCredentialCheckBox);

        var form = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("205,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
        };

        AddProfileRow(form, 0, "Username", _fileBrowserUsernameTextBox, spanActionColumn: false);
        AddProfileRow(form, 1, "Password", _fileBrowserPasswordBox, spanActionColumn: false);

        ConfigureButton(_deleteCredentialButton, "Remove saved login", 170);
        _neutralButtons.Add(_deleteCredentialButton);
        _deleteCredentialButton.IsEnabled = CredentialManager.IsSupported;
        _deleteCredentialButton.Click += (_, _) => DeleteCredential();
        Grid.SetRow(_deleteCredentialButton, 2);
        Grid.SetColumn(_deleteCredentialButton, 1);
        form.Children.Add(_deleteCredentialButton);

        _credentialStatusText.Margin = new Thickness(0, 10, 0, 0);
        _credentialStatusText.TextWrapping = TextWrapping.Wrap;
        _secondaryTexts.Add(_credentialStatusText);

        var stack = new StackPanel();
        stack.Children.Add(PrimaryText("File Browser Login", 15, FontWeight.SemiBold));
        stack.Children.Add(SecondaryText(
            CredentialManager.IsSupported
                ? "Optional. Saved credentials live in Windows Credential Manager and are used to automatically prefill the File Browser login form."
                : "Credential saving is unavailable on this platform. Do not store File Browser passwords in profile JSON.",
            new Thickness(0, 6, 0, 0)));
        stack.Children.Add(_saveFileBrowserCredentialCheckBox);
        stack.Children.Add(form);
        stack.Children.Add(_credentialStatusText);
        return Card(stack, _credentialCard);
    }

    private Control BuildServerSetupSection()
    {
        _alreadyInstalledCheckBox.Margin = new Thickness(0, 10, 0, 0);
        _alreadyInstalledCheckBox.Content = "File Browser is already installed; only configure service and bind address";
        _checkBoxes.Add(_alreadyInstalledCheckBox);

        var form = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("205,*"),
            RowDefinitions = new RowDefinitions("Auto"),
        };
        AddProfileRow(form, 0, "Server root", _serverRootTextBox, spanActionColumn: false);

        var stack = new StackPanel();
        stack.Children.Add(PrimaryText("Server Setup", 15, FontWeight.SemiBold));
        stack.Children.Add(_alreadyInstalledCheckBox);
        stack.Children.Add(form);
        stack.Children.Add(SecondaryText("The install/configure step runs the bundled server script over SSH. It binds File Browser to localhost and does not open firewall ports. Non-root users need passwordless sudo for this step.", new Thickness(0, 8, 0, 0)));
        return Card(stack, _serverSetupCard);
    }

    private Control BuildActionsSection()
    {
        ConfigureButton(_testSshButton, "Test SSH", 110);
        _neutralButtons.Add(_testSshButton);
        _testSshButton.Click += async (_, _) => await TestSshAsync();

        ConfigureButton(_installButton, "Install / Configure", 154);
        _neutralButtons.Add(_installButton);
        _installButton.Click += async (_, _) => await InstallAsync();

        ConfigureButton(_testTunnelButton, "Test Tunnel", 116);
        _neutralButtons.Add(_testTunnelButton);
        _testTunnelButton.Click += async (_, _) => await TestTunnelAsync();

        var buttons = new WrapPanel();
        buttons.Children.Add(_testSshButton);
        buttons.Children.Add(_installButton);
        buttons.Children.Add(_testTunnelButton);

        _logTextBox.Height = 150;
        _logTextBox.Margin = new Thickness(0, 8, 0, 0);
        _logTextBox.IsReadOnly = true;
        _logTextBox.TextWrapping = TextWrapping.Wrap;
        _logTextBox.FontFamily = FontFamily.Parse("Consolas");
        _logTextBox.FontSize = 12;
        _logTextBox.AcceptsReturn = true;
        _textBoxes.Add(_logTextBox);

        var stack = new StackPanel();
        stack.Children.Add(buttons);
        stack.Children.Add(_logTextBox);
        return Card(stack, _actionsCard);
    }

    private Control BuildFooter()
    {
        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(18, 14),
        };

        ConfigureButton(_cancelButton, "Cancel", 92);
        _cancelButton.Margin = new Thickness(0, 0, 8, 0);
        _cancelButton.VerticalAlignment = VerticalAlignment.Center;
        _neutralButtons.Add(_cancelButton);
        _cancelButton.Click += (_, _) => Cancel();

        ConfigureButton(_saveOpenButton, "Connect", 124);
        _saveOpenButton.Margin = new Thickness(0);
        _saveOpenButton.VerticalAlignment = VerticalAlignment.Center;
        _primaryButtons.Add(_saveOpenButton);
        _saveOpenButton.Click += (_, _) => SaveOpen();

        footer.Children.Add(_cancelButton);
        footer.Children.Add(_saveOpenButton);
        return footer;
    }

    private async Task TestSshAsync()
    {
        await RunSetupActionAsync("Testing SSH...", async profile =>
        {
            var result = await SshCommandService.TestConnectionAsync(profile, _cancellation.Token);
            AppendLog(result.Output);
            if (!result.Success || !result.Output.Contains("FILEBROWSER_DESKTOP_SSH_OK", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("SSH test failed. Check host, username, key/agent, and host-key trust.");
            }

            AppendLog("SSH connection OK.");
        });
    }

    private async Task InstallAsync()
    {
        await RunSetupActionAsync("Installing/configuring File Browser...", async profile =>
        {
            var serverRoot = TextOf(_serverRootTextBox).Trim();
            if (string.IsNullOrWhiteSpace(serverRoot))
            {
                serverRoot = "/";
            }

            var alreadyInstalled = _alreadyInstalledCheckBox.IsChecked == true;
            var result = await SshCommandService.InstallOrConfigureFileBrowserAsync(profile, serverRoot, alreadyInstalled, _cancellation.Token);
            AppendLog(result.Output);
            if (!result.Success)
            {
                throw new InvalidOperationException("Server setup failed. If you are not logging in as root, passwordless sudo is required for the install/configure step.");
            }

            AppendLog("Server setup OK.");
        });
    }

    private async Task TestTunnelAsync()
    {
        await RunSetupActionAsync("Testing SSH tunnel...", async profile =>
        {
            using var tunnel = new SshTunnelService();
            await tunnel.StartAsync(profile, _cancellation.Token);

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            try
            {
                using var response = await http.GetAsync(profile.LocalUri, _cancellation.Token);
                AppendLog($"Tunnel HTTP response: {(int)response.StatusCode} {response.ReasonPhrase}");
                if ((int)response.StatusCode >= 500)
                {
                    throw new InvalidOperationException("File Browser responded with a server error through the tunnel.");
                }
            }
            finally
            {
                tunnel.Stop();
            }

            AppendLog("Tunnel test OK.");
        });
    }

    private async Task RunSetupActionAsync(string heading, Func<ConnectionProfile, Task> action)
    {
        _errorText.Text = "";
        ResetCancellation();
        if (!TryBuildProfile(out var profile))
        {
            return;
        }

        SetActionsEnabled(false);
        AppendLog("");
        AppendLog(heading);

        try
        {
            await action(profile);
        }
        catch (OperationCanceledException)
        {
            _errorText.Text = "Operation cancelled.";
        }
        catch (Exception ex)
        {
            _errorText.Text = ex.Message;
            AppendLog(ex.Message);
        }
        finally
        {
            SetActionsEnabled(true);
        }
    }

    private async Task BrowseIdentityFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select SSH private key",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("SSH private keys")
                {
                    Patterns = ["id_*", "*.pem", "*.key", "*.ppk"],
                },
                FilePickerFileTypes.All,
            ],
        });

        if (files.Count > 0)
        {
            _sshIdentityFileTextBox.Text = files[0].Path.LocalPath;
        }
    }

    private void SaveOpen()
    {
        if (!TryBuildProfile(out var profile))
        {
            return;
        }

        try
        {
            SaveFileBrowserCredential(profile.Id);
        }
        catch (Exception ex)
        {
            _errorText.Text = $"Could not save credential: {ex.Message}";
            return;
        }

        SaveOpenRequested?.Invoke(this, new SetupProfileEventArgs(profile, _isNewProfile));
    }

    private void Cancel()
    {
        _cancellation.Cancel();
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool TryBuildProfile(out ConnectionProfile profile)
    {
        profile = _workingProfile.Clone();
        _errorText.Text = "";

        if (!TryReadPort(TextOf(_sshPortTextBox), "SSH port", out var sshPort) ||
            !TryReadPort(TextOf(_localPortTextBox), "Local port", out var localPort) ||
            !TryReadPort(TextOf(_remotePortTextBox), "Remote File Browser port", out var remotePort))
        {
            return false;
        }

        var name = TextOf(_nameTextBox).Trim();
        var sshUser = TextOf(_sshUserTextBox).Trim();
        var sshHost = TextOf(_sshHostTextBox).Trim();
        var identityFile = TextOf(_sshIdentityFileTextBox).Trim();
        var remoteHost = TextOf(_remoteHostTextBox).Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            _errorText.Text = "Profile name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(sshHost))
        {
            _errorText.Text = "SSH host is required.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(identityFile) && !File.Exists(identityFile))
        {
            _errorText.Text = "SSH key file does not exist.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(remoteHost))
        {
            _errorText.Text = "Remote File Browser host is required.";
            return false;
        }

        profile.Name = name;
        profile.SshUser = sshUser;
        profile.SshHost = sshHost;
        profile.SshPort = sshPort;
        profile.SshIdentityFile = identityFile;
        profile.LocalHost = "127.0.0.1";
        profile.LocalPort = localPort;
        profile.RemoteHost = remoteHost;
        profile.RemotePort = remotePort;
        return true;
    }

    private bool TryReadPort(string value, string label, out int port)
    {
        if (!int.TryParse(value.Trim(), out port) || port < 1 || port > 65535)
        {
            _errorText.Text = $"{label} must be a number from 1 to 65535.";
            return false;
        }

        return true;
    }

    private void LoadSavedCredential(string profileId)
    {
        _fileBrowserUsernameTextBox.Text = "";
        _fileBrowserPasswordBox.Text = "";
        _existingFileBrowserCredential = null;

        if (!CredentialManager.IsSupported)
        {
            _saveFileBrowserCredentialCheckBox.IsChecked = false;
            _credentialStatusText.Text = "Credential saving is unavailable on this platform.";
            return;
        }

        try
        {
            _existingFileBrowserCredential = CredentialManager.ReadFileBrowserCredential(profileId);
            if (_existingFileBrowserCredential is null)
            {
                _saveFileBrowserCredentialCheckBox.IsChecked = false;
                _credentialStatusText.Text = "No File Browser credential is saved for this profile.";
                return;
            }

            _saveFileBrowserCredentialCheckBox.IsChecked = true;
            _fileBrowserUsernameTextBox.Text = _existingFileBrowserCredential.Username;
            _credentialStatusText.Text = "A File Browser credential is saved. Leave password blank to keep the current saved password.";
        }
        catch (Exception ex)
        {
            _saveFileBrowserCredentialCheckBox.IsChecked = false;
            _credentialStatusText.Text = $"Could not read saved credential: {ex.Message}";
        }
    }

    private void SaveFileBrowserCredential(string profileId)
    {
        if (!CredentialManager.IsSupported)
        {
            return;
        }

        if (_saveFileBrowserCredentialCheckBox.IsChecked != true)
        {
            CredentialManager.DeleteFileBrowserCredential(profileId);
            return;
        }

        var username = TextOf(_fileBrowserUsernameTextBox).Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("File Browser username is required when credential saving is enabled.");
        }

        var password = TextOf(_fileBrowserPasswordBox);
        if (string.IsNullOrEmpty(password))
        {
            if (_existingFileBrowserCredential is null)
            {
                throw new InvalidOperationException("File Browser password is required the first time you save credentials.");
            }

            password = _existingFileBrowserCredential.Password;
        }

        CredentialManager.WriteFileBrowserCredential(profileId, username, password);
    }

    private void DeleteCredential()
    {
        try
        {
            CredentialManager.DeleteFileBrowserCredential(_workingProfile.Id);
            _existingFileBrowserCredential = null;
            _saveFileBrowserCredentialCheckBox.IsChecked = false;
            _fileBrowserUsernameTextBox.Text = "";
            _fileBrowserPasswordBox.Text = "";
            _credentialStatusText.Text = "No File Browser credential is saved for this profile.";
        }
        catch (Exception ex)
        {
            _errorText.Text = $"Could not delete credential: {ex.Message}";
        }
    }

    private void SetActionsEnabled(bool enabled)
    {
        var installMode = _installRadioButton.IsChecked == true;
        _testSshButton.IsEnabled = enabled;
        _installButton.IsEnabled = enabled && installMode;
        _installButton.IsVisible = installMode;
        _alreadyInstalledCheckBox.IsEnabled = enabled && installMode;
        _testTunnelButton.IsEnabled = enabled;
        _saveOpenButton.IsEnabled = enabled;
        _cancelButton.IsEnabled = enabled;
    }

    private void SetSetupModeState()
    {
        var installMode = _installRadioButton.IsChecked == true;
        _installButton.IsEnabled = installMode;
        _installButton.IsVisible = installMode;
        _alreadyInstalledCheckBox.IsEnabled = installMode;
        _serverRootTextBox.IsEnabled = installMode;
        _serverSetupCard.IsVisible = installMode;
    }

    private void AppendLog(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            _logTextBox.Text += text + Environment.NewLine;
        }

        _logTextBox.CaretIndex = _logTextBox.Text?.Length ?? 0;
    }

    private void AppSettingsChanged(object? sender, AppSettings settings)
    {
        ApplySettings(settings);
    }

    private void ApplySettings(AppSettings settings)
    {
        ApplyTheme(settings.DarkTheme);
    }

    private void ApplyTheme(bool dark)
    {
        _isDarkTheme = dark;

        var background = BrushFor(dark ? "#101820" : "#EEF3F8");
        var surface = BrushFor(dark ? "#0B1120" : "#FFFFFF");
        var card = BrushFor(dark ? "#0F172A" : "#FFFFFF");
        var border = BrushFor(dark ? "#334155" : "#D9E2EF");
        var primaryText = BrushFor(dark ? "#F8FAFC" : "#111827");
        var secondaryText = BrushFor(dark ? "#CBD5E1" : "#475569");
        var labelText = BrushFor(dark ? "#DDE7F3" : "#334155");
        var inputBackground = BrushFor(dark ? "#111827" : "#FFFFFF");
        var inputBorder = BrushFor(dark ? "#475569" : "#CBD5E1");
        var inputText = BrushFor(dark ? "#F8FAFC" : "#111827");

        Background = background;
        _root.Background = background;
        _headerBar.Background = surface;
        _headerBar.BorderBrush = border;
        _footerBar.Background = surface;
        _footerBar.BorderBrush = border;
        _headerIconFrame.Background = _imageConfig.ShowSetupHeaderIconBackground
            ? BrushFor(dark ? "#172033" : "#EFF6FF")
            : Brushes.Transparent;
        _headerIconFrame.BorderBrush = _imageConfig.ShowSetupHeaderIconBorder
            ? BrushFor(dark ? "#40516D" : "#BFDBFE")
            : Brushes.Transparent;

        foreach (var cardBorder in _cards)
        {
            cardBorder.Background = card;
            cardBorder.BorderBrush = border;
        }

        foreach (var textBlock in _primaryTexts)
        {
            textBlock.Foreground = primaryText;
        }

        foreach (var textBlock in _secondaryTexts)
        {
            textBlock.Foreground = secondaryText;
        }

        foreach (var textBlock in _labelTexts)
        {
            textBlock.Foreground = labelText;
        }

        foreach (var textBox in _textBoxes)
        {
            textBox.Background = textBox == _logTextBox ? BrushFor("#0F172A") : inputBackground;
            textBox.Foreground = textBox == _logTextBox ? BrushFor("#D1E7FF") : inputText;
            textBox.BorderBrush = textBox == _logTextBox ? BrushFor("#1E293B") : inputBorder;
        }

        foreach (var radio in _radioButtons)
        {
            radio.Foreground = labelText;
        }

        foreach (var checkBox in _checkBoxes)
        {
            checkBox.Foreground = labelText;
        }

        foreach (var button in _neutralButtons)
        {
            ApplyButtonTheme(button, primary: false);
        }

        foreach (var button in _primaryButtons)
        {
            ApplyButtonTheme(button, primary: true);
        }

        foreach (var button in _guideButtons)
        {
            button.Background = Brushes.Transparent;
            button.BorderBrush = Brushes.Transparent;
            button.Foreground = secondaryText;
        }

        foreach (var badge in _guideNumberBadges)
        {
            badge.Background = BrushFor(dark ? "#1E3A5F" : "#DBEAFE");
            if (badge.Child is TextBlock badgeText)
            {
                badgeText.Foreground = BrushFor(dark ? "#BFDBFE" : "#1D4ED8");
            }
        }

        _errorText.Foreground = BrushFor(dark ? "#FCA5A5" : "#DC2626");
    }

    private Border Card(Control child)
    {
        return Card(child, new Border());
    }

    private Border Card(Control child, Border border)
    {
        border.Margin = new Thickness(0, 0, 0, 12);
        border.Padding = new Thickness(18);
        border.BorderThickness = new Thickness(1);
        border.CornerRadius = new CornerRadius(12);
        border.Child = child;
        _cards.Add(border);
        return border;
    }

    private TextBlock PrimaryText(string text, double fontSize, FontWeight weight)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = weight,
            TextWrapping = TextWrapping.Wrap,
        };
        _primaryTexts.Add(block);
        return block;
    }

    private TextBlock SecondaryText(string text, Thickness margin)
    {
        var block = new TextBlock
        {
            Text = text,
            Margin = margin,
            TextWrapping = TextWrapping.Wrap,
        };
        _secondaryTexts.Add(block);
        return block;
    }

    private void AddProfileRow(Grid grid, int row, string label, TextBox input, bool spanActionColumn = true)
    {
        var columnCount = grid.ColumnDefinitions.Count;
        var labelBlock = new TextBlock
        {
            Text = label,
            Margin = new Thickness(0, 0, 12, 10),
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };
        _labelTexts.Add(labelBlock);
        Grid.SetRow(labelBlock, row);
        Grid.SetColumn(labelBlock, 0);
        grid.Children.Add(labelBlock);

        input.Height = 34;
        input.Margin = new Thickness(0, 0, spanActionColumn ? 0 : 8, 10);
        _textBoxes.Add(input);
        Grid.SetRow(input, row);
        Grid.SetColumn(input, 1);
        if (columnCount >= 5)
        {
            Grid.SetColumnSpan(input, spanActionColumn ? 4 : 3);
        }
        else if (spanActionColumn && columnCount > 2)
        {
            Grid.SetColumnSpan(input, columnCount - 1);
        }

        grid.Children.Add(input);
    }

    private void AddPairedProfileRow(Grid grid, int row, string leftLabel, TextBox leftInput, string rightLabel, TextBox rightInput)
    {
        AddInlineProfileInput(grid, row, 0, leftLabel, leftInput);
        AddInlineProfileInput(grid, row, 3, rightLabel, rightInput);
    }

    private void AddInlineProfileInput(Grid grid, int row, int labelColumn, string label, TextBox input)
    {
        var labelBlock = new TextBlock
        {
            Text = label,
            Margin = new Thickness(0, 0, 12, 10),
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };
        _labelTexts.Add(labelBlock);
        Grid.SetRow(labelBlock, row);
        Grid.SetColumn(labelBlock, labelColumn);
        grid.Children.Add(labelBlock);

        input.Height = 34;
        input.Margin = new Thickness(0, 0, 0, 10);
        _textBoxes.Add(input);
        Grid.SetRow(input, row);
        Grid.SetColumn(input, labelColumn + 1);
        grid.Children.Add(input);
    }

    private Button NeutralButton(string content, double width)
    {
        var button = new Button();
        ConfigureButton(button, content, width);
        _neutralButtons.Add(button);
        return button;
    }

    private void ConfigureButton(Button button, string content, double width)
    {
        UiTheme.ConfigureSetupButton(button, content, width);
    }

    private void ApplyButtonTheme(Button button, bool primary)
    {
        UiTheme.ApplyButtonTheme(button, primary ? UiButtonStyle.Primary : UiButtonStyle.Neutral, _isDarkTheme);
    }

    private void ResetCancellation()
    {
        if (!_cancellation.IsCancellationRequested)
        {
            return;
        }

        _cancellation.Dispose();
        _cancellation = new CancellationTokenSource();
    }

    private static string TextOf(TextBox textBox)
    {
        return textBox.Text ?? "";
    }

    private static IBrush BrushFor(string color)
    {
        return SolidColorBrush.Parse(color);
    }

}
