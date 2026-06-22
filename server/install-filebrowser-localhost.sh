#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="/"
ADDRESS="127.0.0.1"
PORT="8080"
DB_PATH="/var/lib/filebrowser/filebrowser.db"
SERVICE_PATH="/etc/systemd/system/filebrowser.service"
SKIP_INSTALL="0"

usage() {
  cat <<'USAGE'
Install or configure File Browser for private SSH-tunnel access.

Defaults:
  - Binds File Browser to 127.0.0.1:8080
  - Creates a systemd service
  - Does not open any firewall ports

Usage:
  sudo bash install-filebrowser-localhost.sh
  sudo bash install-filebrowser-localhost.sh --root /srv
  sudo bash install-filebrowser-localhost.sh --port 8081
  sudo bash install-filebrowser-localhost.sh --already-installed

Options:
  --root PATH          File Browser root directory. Default: /
  --address ADDRESS   Bind address. Default: 127.0.0.1
  --port PORT         Bind port. Default: 8080
  --db PATH           Database path. Default: /var/lib/filebrowser/filebrowser.db
  --already-installed Do not install File Browser; configure the existing binary
  -h, --help          Show help
USAGE
}

install_curl() {
  if command -v apt-get >/dev/null 2>&1; then
    apt-get update
    apt-get install -y curl
  elif command -v dnf >/dev/null 2>&1; then
    dnf install -y curl
  elif command -v yum >/dev/null 2>&1; then
    yum install -y curl
  elif command -v apk >/dev/null 2>&1; then
    apk add --no-cache curl
  else
    echo "curl is required to install File Browser. Install curl, then rerun this script." >&2
    exit 1
  fi
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --root)
      ROOT_DIR="$2"
      shift 2
      ;;
    --address)
      ADDRESS="$2"
      shift 2
      ;;
    --port)
      PORT="$2"
      shift 2
      ;;
    --db)
      DB_PATH="$2"
      shift 2
      ;;
    --already-installed)
      SKIP_INSTALL="1"
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

if [[ "$(id -u)" != "0" ]]; then
  echo "Please run this script with sudo or as root." >&2
  exit 1
fi

if [[ "$ADDRESS" != "127.0.0.1" && "$ADDRESS" != "::1" && "$ADDRESS" != "localhost" ]]; then
  echo "Refusing to bind File Browser to a non-localhost address: $ADDRESS" >&2
  echo "Use an SSH tunnel from the desktop app instead of exposing File Browser publicly." >&2
  exit 1
fi

if ! command -v filebrowser >/dev/null 2>&1; then
  if [[ "$SKIP_INSTALL" == "1" ]]; then
    echo "filebrowser was not found on PATH, and --already-installed was provided." >&2
    exit 1
  fi

  if ! command -v curl >/dev/null 2>&1; then
    install_curl
  fi

  echo "Installing File Browser..."
  curl -fsSL https://raw.githubusercontent.com/filebrowser/get/master/get.sh | bash
fi

FILEBROWSER_BIN="$(command -v filebrowser)"
DB_DIR="$(dirname "$DB_PATH")"
mkdir -p "$DB_DIR"
chmod 0750 "$DB_DIR"

NEW_DB="0"
if [[ ! -f "$DB_PATH" ]]; then
  NEW_DB="1"
  filebrowser config init -d "$DB_PATH" >/dev/null
fi

filebrowser config set \
  -d "$DB_PATH" \
  --address "$ADDRESS" \
  --port "$PORT" \
  --root "$ROOT_DIR" >/dev/null

if [[ "$NEW_DB" == "1" ]]; then
  ADMIN_PASSWORD="$(python3 - <<'PY'
import secrets, string
alphabet = string.ascii_letters + string.digits
print(''.join(secrets.choice(alphabet) for _ in range(24)))
PY
)"
  filebrowser users add admin "$ADMIN_PASSWORD" -d "$DB_PATH" --perm.admin >/dev/null
fi

cat > "$SERVICE_PATH" <<EOF
[Unit]
Description=File Browser
After=network.target

[Service]
Type=simple
ExecStart=$FILEBROWSER_BIN -d $DB_PATH
Restart=on-failure
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

chmod 0644 "$SERVICE_PATH"
systemctl daemon-reload
systemctl enable --now filebrowser.service >/dev/null

echo
echo "File Browser is configured."
echo "Service: filebrowser.service"
echo "Bind: http://$ADDRESS:$PORT"
echo "Root: $ROOT_DIR"
echo
echo "This script did not open any firewall ports."
echo "Use the desktop app or an SSH tunnel to access File Browser."

if [[ "$NEW_DB" == "1" ]]; then
  echo
  echo "Initial File Browser login:"
  echo "  username: admin"
  echo "  password: $ADMIN_PASSWORD"
  echo
  echo "Store this password in a password manager or Windows Credential Manager."
fi
