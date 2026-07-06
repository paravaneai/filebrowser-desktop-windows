# Security

File Browser Desktop is designed to keep the File Browser web UI private. The intended model is:

```text
Windows desktop app -> SSH tunnel -> server localhost File Browser
```

Do not expose File Browser directly to the public internet.

## Server Exposure

File Browser should bind to localhost only, usually:

```text
127.0.0.1:8080
```

The desktop app then forwards a local Windows port to that private server port through SSH.

Good:

```text
server File Browser: 127.0.0.1:8080
desktop tunnel:      127.0.0.1:18080 -> server 127.0.0.1:8080
```

Avoid:

```text
server File Browser: 0.0.0.0:8080
public firewall:     allow 8080/tcp
```

The included server setup script configures File Browser for localhost binding and does not open firewall ports.

## SSH Tunnel Only

The app uses the user's installed `ssh.exe`.

Supported authentication paths:

- Existing OpenSSH config
- Default SSH keys
- ssh-agent
- Optional per-profile identity-file path

The app does not store SSH passwords or SSH key passphrases. Use ssh-agent or your normal OpenSSH configuration for passphrase-protected keys.

The app does not disable host-key checking. First-time host trust and host-key changes are handled by OpenSSH.

## Stored Profile Data

Connection profiles are stored in:

```text
%APPDATA%\FileBrowserDesktop\profiles.json
```

Profiles may contain:

- Profile name
- SSH username
- SSH host
- SSH port
- Optional SSH identity-file path
- Local tunnel host/port
- Remote File Browser host/port

Profiles must not contain:

- Passwords
- SSH key passphrases
- Private key contents
- File Browser login credentials

## File Browser Credentials

Optional File Browser login credentials are stored in Windows Credential Manager under:

```text
FileBrowserDesktop/FileBrowser/<profile-id>
```

The app reads those credentials only to prefill the File Browser login form. It does not auto-submit login.

## Revoke Or Remove Saved Credentials

Preferred in-app method:

1. Open File Browser Desktop.
2. Select the profile.
3. Click `Edit`.
4. Click `Delete saved credential`.
5. Save the profile.

Windows Credential Manager method:

1. Open Credential Manager:

   ```cmd
   control /name Microsoft.CredentialManager
   ```

2. Go to `Windows Credentials`.
3. Find the generic credential named:

   ```text
   FileBrowserDesktop/FileBrowser/<profile-id>
   ```

4. Remove it.

Command-line method:

```cmd
cmdkey /delete:FileBrowserDesktop/FileBrowser/<profile-id>
```

## Remove Local App Data

Remove connection profiles:

```text
%APPDATA%\FileBrowserDesktop\profiles.json
```

Remove WebView2 browser data, including File Browser cookies/sessions:

```text
%LOCALAPPDATA%\FileBrowserDesktop\WebView2
```

## Server Cleanup

On a systemd-based Linux server, stop and disable File Browser:

```bash
sudo systemctl disable --now filebrowser.service
```

If you used the included setup script, the default database path is:

```text
/var/lib/filebrowser/filebrowser.db
```

Do not delete server data unless you understand what File Browser root and database paths are being used.
