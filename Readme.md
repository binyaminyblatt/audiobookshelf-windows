# audiobookshelf-windows

Installs and manages the [audiobookshelf](https://github.com/advplyr/audiobookshelf) server on Windows as a native **Windows Background Service** with **Network Drive Mapping** and a companion System Tray manager.

## Key Features

- **Runs as a 24/7 Windows Background Service**: Starts automatically on system boot (before user login) with non-elevated service control permissions for the configured user.
- **Network Drive Mapping**: Automatically mounts UNC shares (e.g. `\\NAS\audiobooks` to `Z:`) before server startup with configurable retry attempts and delays, and automatically hides mapped drives in "This PC".
- **Host Binding & Network Interfaces**: Bind to `0.0.0.0` (all network adapters / LAN access), `127.0.0.1` (localhost only), or select any specific active local network adapter IPv4 address on multi-IP machines.
- **Automated Windows Firewall Management**: Automatically configures and updates Windows Defender Firewall inbound rules on the server port, and safely deletes them upon uninstallation.
- **HTTP Health Checking & Live Status Badge**: Proactively polls the server (`/ping`) every 2.5s and updates the taskbar tray icon with a **green dot** when healthy and receiving ping responses, and a **red dot** if the server is stopped or pings are failing (along with tooltip status `Ready` vs `Initializing...`).
- **Quick Access Tray Actions**:
  - Open Data Folder (`C:\ProgramData\Audiobookshelf`)
  - Open Logs Folder (`C:\ProgramData\Audiobookshelf\logs`)
  - Backup Database Now (on-demand database snapshot via ABS API)
- **Data Safety & Integrity**:
  - Graceful process termination sending EOF signals, `CTRL_BREAK_EVENT`, and `CloseMainWindow` with a 5-second graceful window to allow SQLite WAL checkpoints to flush cleanly to disk before shutdown.
  - Automated pre-update database backups before applying software updates.
  - Log rotation with 10MB file caps and 5 rolling archives to prevent disk exhaustion.
- **Watchdog & Auto-Recovery**: Automatically monitors and restarts the server process if it exits unexpectedly.
- **Interactive System Tray**:
  - Start, stop, and restart the Windows Service / standalone server process.
  - Open the Audiobookshelf web interface in your default browser.
  - View live streaming server logs (tailing `server.log`).
  - Configure server port, host binding, data directory, and network drive mappings.
  - Automatic update checks and installation from GitHub releases.

## System Requirements

- Windows 10/11 64-bit or Windows Server 2016+
- .NET Framework 4.6.1 (pre-installed on modern Windows)
- Administrator privileges (for installing the Windows Service and configuring firewall rules)

## Installation

### GUI Installation
Download the latest installer from the [Releases](https://github.com/binyaminyblatt/audiobookshelf-windows/releases/latest) page and run `AudiobookshelfInstaller.exe`.

The installer will:
1. Install `audiobookshelf.exe`, `AudiobookshelfService.exe`, and `AudiobookshelfTray.exe`.
2. Configure Windows Defender Firewall inbound rule on TCP port `13378` (or custom port).
3. Register and start the `AudiobookshelfService` Windows Service with your Windows user credentials.
4. Launch the Tray application.

### Silent / Headless / Automation Installation
The installer supports fully unattended command-line parameters:

```cmd
AudiobookshelfInstaller.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /PORT=13378 /HOST=0.0.0.0 /DATADIR="C:\AudiobookshelfData" /SERVICE_USER=".\Administrator" /SERVICE_PASS="Password123"
```

#### Available CLI Switches & Parameters:
| Parameter | Default | Description |
|---|---|---|
| `/PORT=<port>` | `13378` | TCP port for the server |
| `/HOST=<host>` | `0.0.0.0` | Host / interface binding (`0.0.0.0` or `127.0.0.1`) |
| `/DATADIR="<path>"` | `C:\ProgramData\Audiobookshelf` | Directory for databases, config, metadata, and cache |
| `/SERVICE_USER="<user>"` | Current Windows User | Windows account used to run the service and mount network drives |
| `/SERVICE_PASS="<pass>"` | *(empty)* | Password for the Windows service account |
| `/NOFIREWALL` | *(disabled)* | Skip creating the Windows Firewall inbound rule |
| `/INSTALLSERVICE=0` | `1` (enabled) | Set to `0` or `no` to install in standalone portable tray mode without Windows Service |

## Configuration

Configuration is saved in `C:\ProgramData\Audiobookshelf\config.json`. You can manage settings via the Tray App:
- Right-click the Audiobookshelf tray icon -> **Settings**.
- **Server Settings**: Change HTTP Port, Host Binding, Data Directory, and toggle Firewall Management / Auto-Open Browser on startup.
- **Network Drives**: Add, edit, or remove mapped drive letters (`Z:`, `Y:`, etc.) pointing to UNC network shares (`\\192.168.1.50\audiobooks`), with optional user credentials and auto-remount retry options.
- **Custom Environment Variables**: Additional environment variables can be placed in `config.json` under the `"envs"` dictionary.

## Architecture

- **`Audiobookshelf.Common`**: Shared library containing `DriveMap` (Win32 `mpr.dll` `WNetAddConnection2`), `CryptographicVerifier` (RSA-SHA256 provenance and checksum verification), `UpdateManager` (self-updater & automated rollback), `FirewallHelper` (`netsh` rule manager), `ServerHealthHelper` (`/ping` and `/api/backup`), `ProcessUtils` (graceful SQLite WAL process shutdown), and `ServiceControllerHelper`.
- **`AudiobookshelfService.exe`**: The Windows Service that mounts network drives, supervises `audiobookshelf.exe` in Session 0, and handles scheduled midnight updates and pre-update backups.
- **`AudiobookshelfTray.exe`**: WinForms system tray application running in the user session with quick access shortcuts, live logs viewer, health polling, and configuration UI.

## Release Signing & Supply Chain Security

The GitHub Actions CI/CD pipeline cryptographically signs release assets using RSA-SHA256:
- **Private Key**: Kept exclusively in GitHub Actions encrypted secrets (`RELEASE_SIGNING_PRIVATE_KEY`).
- **Public Key**: Optionally provided in `RELEASE_SIGNING_PUBLIC_KEY` secret (or auto-derived from the private key secret) and dynamically injected into the binaries at build time before compilation.
- **Key Rotation Detection**: If the signing key is ever rotated or changed, the build workflow automatically detects the change compared to prior releases and publishes a release warning notifying users that automatic updates cannot verify the new key and manual reinstallation is required.
- **Self-Updater Verification**: The client verifies digital signatures and SHA-256 hashes before staging and applying any updates.


