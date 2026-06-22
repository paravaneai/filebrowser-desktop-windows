# Contributing

Thank you for considering a contribution to File Browser Desktop.

## Development Setup

Required on Windows:

- .NET 8 SDK
- Microsoft Edge WebView2 Runtime
- OpenSSH Client

Build the app:

```cmd
dotnet build src\FileBrowserDesktop.csproj
```

Create the release zip:

```cmd
package-release.cmd
```

## Pull Requests

Please keep pull requests focused and include:

- A clear description of the change.
- Any security impact, especially for SSH, credentials, profile storage, or server setup.
- Manual verification steps.
- Screenshots for user-interface changes when helpful.

## Security-Sensitive Changes

Treat these areas with extra care:

- SSH command construction and host-key behavior.
- Credential Manager usage.
- File Browser login prefilling.
- Server installation and systemd service configuration.
- Any change that could expose File Browser beyond localhost.

See `SECURITY.md` before proposing changes in these areas.
