using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace FileBrowserDesktop;

public partial class ProfileEditorWindow : Window
{
    private StoredCredential? _existingFileBrowserCredential;

    public ConnectionProfile Profile { get; }

    public ProfileEditorWindow(ConnectionProfile profile)
    {
        InitializeComponent();
        Profile = profile.Clone();

        NameTextBox.Text = Profile.Name;
        SshUserTextBox.Text = Profile.SshUser;
        SshHostTextBox.Text = Profile.SshHost;
        SshPortTextBox.Text = Profile.SshPort.ToString();
        SshIdentityFileTextBox.Text = Profile.SshIdentityFile;
        LocalHostTextBox.Text = Profile.LocalHost;
        LocalPortTextBox.Text = Profile.LocalPort.ToString();
        RemoteHostTextBox.Text = Profile.RemoteHost;
        RemotePortTextBox.Text = Profile.RemotePort.ToString();

        LoadSavedCredential();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";

        if (!TryReadPort(SshPortTextBox.Text, "SSH port", out var sshPort) ||
            !TryReadPort(LocalPortTextBox.Text, "Local port", out var localPort) ||
            !TryReadPort(RemotePortTextBox.Text, "Remote port", out var remotePort))
        {
            return;
        }

        var name = NameTextBox.Text.Trim();
        var sshUser = SshUserTextBox.Text.Trim();
        var sshHost = SshHostTextBox.Text.Trim();
        var sshIdentityFile = SshIdentityFileTextBox.Text.Trim();
        var localHost = LocalHostTextBox.Text.Trim();
        var remoteHost = RemoteHostTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorText.Text = "Profile name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(sshHost))
        {
            ErrorText.Text = "SSH host is required.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(sshIdentityFile) && !File.Exists(sshIdentityFile))
        {
            ErrorText.Text = "SSH key file does not exist.";
            return;
        }

        if (string.IsNullOrWhiteSpace(localHost))
        {
            ErrorText.Text = "Local host is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(remoteHost))
        {
            ErrorText.Text = "Remote host is required.";
            return;
        }

        try
        {
            SaveFileBrowserCredential();
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Could not save credential: {ex.Message}";
            return;
        }

        Profile.Name = name;
        Profile.SshUser = sshUser;
        Profile.SshHost = sshHost;
        Profile.SshPort = sshPort;
        Profile.SshIdentityFile = sshIdentityFile;
        Profile.LocalHost = localHost;
        Profile.LocalPort = localPort;
        Profile.RemoteHost = remoteHost;
        Profile.RemotePort = remotePort;

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

    private void DeleteCredentialButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            CredentialManager.DeleteFileBrowserCredential(Profile.Id);
            _existingFileBrowserCredential = null;
            SaveFileBrowserCredentialCheckBox.IsChecked = false;
            FileBrowserUsernameTextBox.Text = "";
            FileBrowserPasswordBox.Password = "";
            CredentialStatusText.Text = "No File Browser credential is saved for this profile.";
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"Could not delete credential: {ex.Message}";
        }
    }

    private void LoadSavedCredential()
    {
        try
        {
            _existingFileBrowserCredential = CredentialManager.ReadFileBrowserCredential(Profile.Id);
            if (_existingFileBrowserCredential is null)
            {
                SaveFileBrowserCredentialCheckBox.IsChecked = false;
                CredentialStatusText.Text = "No File Browser credential is saved for this profile.";
                return;
            }

            SaveFileBrowserCredentialCheckBox.IsChecked = true;
            FileBrowserUsernameTextBox.Text = _existingFileBrowserCredential.Username;
            CredentialStatusText.Text = "A File Browser credential is saved. Leave password blank to keep the current saved password.";
        }
        catch (Exception ex)
        {
            SaveFileBrowserCredentialCheckBox.IsChecked = false;
            CredentialStatusText.Text = $"Could not read saved credential: {ex.Message}";
        }
    }

    private void SaveFileBrowserCredential()
    {
        if (SaveFileBrowserCredentialCheckBox.IsChecked != true)
        {
            CredentialManager.DeleteFileBrowserCredential(Profile.Id);
            return;
        }

        var username = FileBrowserUsernameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("File Browser username is required when credential saving is enabled.");
        }

        var password = FileBrowserPasswordBox.Password;
        if (string.IsNullOrEmpty(password))
        {
            if (_existingFileBrowserCredential is null)
            {
                throw new InvalidOperationException("File Browser password is required the first time you save credentials.");
            }

            password = _existingFileBrowserCredential.Password;
        }

        CredentialManager.WriteFileBrowserCredential(Profile.Id, username, password);
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
}
