using Microsoft.Win32;
using System.IO;
using System.Net.Http;
using System.Windows;

namespace FileBrowserDesktop;

public partial class OnboardingWizardWindow : Window
{
    private readonly CancellationTokenSource _cancellation = new();

    public ConnectionProfile? Profile { get; private set; }

    public OnboardingWizardWindow()
    {
        InitializeComponent();
        SetSetupPathState();
    }

    protected override void OnClosed(EventArgs e)
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
        base.OnClosed(e);
    }

    private async void TestSshButton_Click(object sender, RoutedEventArgs e)
    {
        await RunWizardActionAsync("Testing SSH...", async profile =>
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

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        await RunWizardActionAsync("Installing/configuring File Browser...", async profile =>
        {
            var serverRoot = ServerRootTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(serverRoot))
            {
                serverRoot = "/";
            }

            var alreadyInstalled = AlreadyInstalledCheckBox.IsChecked == true;
            var result = await SshCommandService.InstallOrConfigureFileBrowserAsync(profile, serverRoot, alreadyInstalled, _cancellation.Token);
            AppendLog(result.Output);
            if (!result.Success)
            {
                throw new InvalidOperationException("Server setup failed. If you are not logging in as root, passwordless sudo is required for the install/configure step.");
            }

            AppendLog("Server setup OK.");
        });
    }

    private async void TestTunnelButton_Click(object sender, RoutedEventArgs e)
    {
        await RunWizardActionAsync("Testing SSH tunnel...", async profile =>
        {
            using var tunnel = new SshTunnelService();
            await tunnel.StartAsync(profile, _cancellation.Token);

            using var http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(8),
            };

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

    private void SaveOpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildProfile(out var profile))
        {
            return;
        }

        Profile = profile;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void BrowseIdentityFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select SSH private key",
            CheckFileExists = true,
            Filter = "SSH private keys|id_*;*.pem;*.key;*.ppk|All files|*.*",
        };

        if (dialog.ShowDialog(this) == true)
        {
            SshIdentityFileTextBox.Text = dialog.FileName;
        }
    }

    private void SetupPathRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        SetSetupPathState();
    }

    private async Task RunWizardActionAsync(string heading, Func<ConnectionProfile, Task> action)
    {
        ErrorText.Text = "";
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
            ErrorText.Text = "Operation cancelled.";
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            AppendLog(ex.Message);
        }
        finally
        {
            SetActionsEnabled(true);
        }
    }

    private bool TryBuildProfile(out ConnectionProfile profile)
    {
        profile = new ConnectionProfile();
        ErrorText.Text = "";

        if (!TryReadPort(SshPortTextBox.Text, "SSH port", out var sshPort) ||
            !TryReadPort(LocalPortTextBox.Text, "Local port", out var localPort) ||
            !TryReadPort(RemotePortTextBox.Text, "Remote File Browser port", out var remotePort))
        {
            return false;
        }

        var name = NameTextBox.Text.Trim();
        var sshUser = SshUserTextBox.Text.Trim();
        var sshHost = SshHostTextBox.Text.Trim();
        var identityFile = SshIdentityFileTextBox.Text.Trim();
        var remoteHost = RemoteHostTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorText.Text = "Profile name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(sshHost))
        {
            ErrorText.Text = "SSH host is required.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(identityFile) && !File.Exists(identityFile))
        {
            ErrorText.Text = "SSH key file does not exist.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(remoteHost))
        {
            ErrorText.Text = "Remote File Browser host is required.";
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
            ErrorText.Text = $"{label} must be a number from 1 to 65535.";
            return false;
        }

        return true;
    }

    private void SetActionsEnabled(bool enabled)
    {
        TestSshButton.IsEnabled = enabled;
        InstallButton.IsEnabled = enabled && InstallRadioButton.IsChecked == true;
        AlreadyInstalledCheckBox.IsEnabled = enabled && InstallRadioButton.IsChecked == true;
        TestTunnelButton.IsEnabled = enabled;
        SaveOpenButton.IsEnabled = enabled;
    }

    private void SetSetupPathState()
    {
        var installMode = InstallRadioButton?.IsChecked == true;
        if (InstallButton is not null)
        {
            InstallButton.IsEnabled = installMode;
        }

        if (AlreadyInstalledCheckBox is not null)
        {
            AlreadyInstalledCheckBox.IsEnabled = installMode;
        }
    }

    private void AppendLog(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            LogTextBox.AppendText(text + Environment.NewLine);
        }

        LogTextBox.ScrollToEnd();
    }
}
