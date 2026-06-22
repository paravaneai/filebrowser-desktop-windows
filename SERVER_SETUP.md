# Server Setup

File Browser should not be exposed directly to the internet. Configure it to listen on localhost and connect through the desktop app's SSH tunnel.

## New Install

Copy or download:

```text
server/install-filebrowser-localhost.sh
```

Run on the server:

```bash
sudo bash install-filebrowser-localhost.sh
```

Defaults:

- File Browser root: `/`
- Bind address: `127.0.0.1`
- Bind port: `8080`
- Service: `filebrowser.service`
- No firewall ports opened

The bundled script is intended for Linux servers that use systemd. If `curl` is missing, it tries common package managers including `apt-get`, `dnf`, `yum`, and `apk`.

## I Already Have File Browser Installed

Use:

```bash
sudo bash install-filebrowser-localhost.sh --already-installed
```

The script will configure the existing `filebrowser` binary, create/update the systemd service, and keep it bound to `127.0.0.1`.

## Safer Root Directory

To limit File Browser to a directory instead of the whole server:

```bash
sudo bash install-filebrowser-localhost.sh --root /srv
```

## Security Notes

- Keep File Browser bound to `127.0.0.1`.
- Do not open port `8080` publicly.
- Use SSH keys or ssh-agent from your desktop.
- Store File Browser login credentials in Windows Credential Manager through the desktop app.
