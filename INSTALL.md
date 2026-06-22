# Install

File Browser Desktop is a Windows desktop client. It runs on your PC and connects to File Browser on a server through SSH.

## Windows Requirements

Install these on the Windows machine that runs the desktop app:

- .NET 8 Desktop Runtime: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
- Microsoft Edge WebView2 Runtime: https://developer.microsoft.com/en-us/microsoft-edge/webview2/
- OpenSSH Client (`ssh.exe`)

The framework-dependent release zip expects .NET 8 Desktop Runtime to be installed. `RunFileBrowserDesktop.cmd` checks for it before launching.

The app also checks for WebView2 at startup and shows the official Microsoft WebView2 install link if it is missing.

## Install From Zip

1. Download the release zip.
2. Extract it somewhere writable, for example:

   ```text
   C:\Tools\FileBrowserDesktop
   ```

3. Run:

   ```cmd
   RunFileBrowserDesktop.cmd
   ```

4. Follow the first-run wizard.

## First-Run Wizard

The wizard supports two paths:

- Connect to an existing File Browser instance.
- Help install/configure File Browser on a server over SSH.

The wizard can:

- Test SSH
- Run the safe server setup script
- Test the SSH tunnel
- Save the profile and open File Browser

## Server Side

File Browser runs on the server. It should bind to localhost only:

```text
127.0.0.1:8080
```

Do not expose File Browser directly to the public internet. Use the desktop app's SSH tunnel.

See:

```text
SERVER_SETUP.md
```

## Framework-Dependent vs Self-Contained

The default release zip is framework-dependent:

- Smaller download
- Requires .NET 8 Desktop Runtime

A self-contained build is possible later:

```cmd
dotnet publish src\FileBrowserDesktop.csproj -c Release -r win-x64 --self-contained true
```

WebView2 is still a separate runtime either way.
