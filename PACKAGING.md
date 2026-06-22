# Packaging

The current release package is a framework-dependent Windows zip.

Build it with:

```cmd
package-release.cmd
```

Output:

```text
dist\FileBrowserDesktop-win-x64-framework-dependent.zip
```

The zip includes:

- `FileBrowserDesktop.exe`
- WebView2 loader/runtime support files from the WebView2 NuGet package
- `RunFileBrowserDesktop.cmd`
- `server/install-filebrowser-localhost.sh`
- README/install/security/server setup docs

## Runtime Decision

This project currently ships as framework-dependent.

Required on the user's Windows machine:

- .NET 8 Desktop Runtime
- Microsoft Edge WebView2 Runtime
- OpenSSH Client (`ssh.exe`)

Reasons for framework-dependent packaging:

- Smaller release zip
- Easier to inspect
- Uses standard Windows/OpenSSH components
- Keeps SSH host-key handling in OpenSSH

`RunFileBrowserDesktop.cmd` checks for .NET 8 Desktop Runtime before launching.

The app checks for missing WebView2 at startup and shows the official Microsoft WebView2 install link.

## Official Runtime Links

- .NET 8 Desktop Runtime: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
- WebView2 Runtime: https://developer.microsoft.com/en-us/microsoft-edge/webview2/

## Self-Contained Option

For users who do not have .NET installed, publish a larger self-contained build:

```cmd
dotnet publish src\FileBrowserDesktop.csproj -c Release -r win-x64 --self-contained true
```

WebView2 is still separate; self-contained .NET does not bundle WebView2.

## Installer Options Later

Reasonable installer paths:

- MSIX for Microsoft Store-style packaging
- Inno Setup for a traditional Windows installer
- WiX for enterprise-style MSI packaging
- winget manifest once releases are stable

Recommended next step:

1. Keep the zip release for early testers.
2. Add an Inno Setup installer that checks/downloads .NET Desktop Runtime and WebView2 Runtime.
3. Add a winget manifest once release URLs are stable.
