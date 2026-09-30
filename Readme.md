# audiobookshelf-windows

Installs and manages the [audiobookshelf](https://github.com/advplyr/audiobookshelf) server on Windows as a native **Windows Background Service** with **Network Drive Mapping** and a companion System Tray manager.

## Key Features

- **Runs as a 24/7 Windows Background Service**: Starts automatically on system boot (before user login).
- **Network Drive Mapping**: Automatically mounts UNC shares (e.g. `\\NAS\audiobooks` to `Z:`) before server startup with configurable retry attempts and delays.
- **Watchdog & Auto-Restart**: Automatically recovers and restarts the server process if it exits unexpectedly.
- **Interactive System Tray**:
  - Start, stop, and restart the Windows Service.
  - Open the Audiobookshelf web interface in your default browser.
  - View live streaming server logs (tailing `C:\ProgramData\Audiobookshelf\logs\server.log`).
  - Configure server port, data directory, and network drive mappings.
  - Automatic update checks and installation from GitHub releases.

## System Requirements

- Windows 10/11 64-bit or Windows Server 2016+
- .NET Framework 4.6.1 (pre-installed on modern Windows)
- Administrator privileges (for installing the Windows Service)

## Installation

Download the latest installer from the [Releases](https://github.com/binyaminyblatt/audiobookshelf-windows/releases/latest) page and run `AudiobookshelfInstaller.exe`.

The installer will:
1. Install `audiobookshelf.exe`, `AudiobookshelfService.exe`, and `AudiobookshelfTray.exe`.
2. Register and start the `AudiobookshelfService` Windows Service.
3. Launch the Tray application.

## Configuration

Configuration is saved in `C:\ProgramData\Audiobookshelf\config.json`. You can manage settings via the Tray App:
- Right-click the Audiobookshelf tray icon -> **Settings**.
- **Server Settings**: Change HTTP Port (default: `13378`) and Data Directory.
- **Network Drives**: Add, edit, or remove mapped drive letters (`Z:`, `Y:`, etc.) pointing to UNC network shares (`\\192.168.1.50\audiobooks`), with optional user credentials and auto-remount retry options.

## Architecture

- **`Audiobookshelf.Common`**: Shared library containing `DriveMap` (Win32 `mpr.dll` `WNetAddConnection2`), `CryptographicVerifier` (RSA-SHA256 provenance and checksum verification), `UpdateManager` (self-updater & automated rollback), `SettingsHandler` (JSON config), and `ServiceControllerHelper`.
- **`AudiobookshelfService.exe`**: The Windows Service that mounts network drives, supervises `audiobookshelf.exe` in Session 0, and handles scheduled midnight updates.
- **`AudiobookshelfTray.exe`**: WinForms system tray application running in the user session.

## Release Signing & Supply Chain Security

The GitHub Actions CI/CD pipeline cryptographically signs release assets using RSA-SHA256:
- **Private Key**: Kept exclusively in GitHub Actions encrypted secrets (`RELEASE_SIGNING_PRIVATE_KEY`).
- **Public Key**: Optionally provided in `RELEASE_SIGNING_PUBLIC_KEY` secret (or auto-derived from the private key secret) and dynamically injected into the binaries at build time before compilation.
- **Key Rotation Detection**: If the signing key is ever rotated or changed, the build workflow automatically detects the change compared to prior releases and publishes a release warning notifying users that automatic updates cannot verify the new key and manual reinstallation is required.
- **Self-Updater Verification**: The client verifies digital signatures and SHA-256 hashes before staging and applying any updates.

### Setting up Release Signing in GitHub:
1. Generate an RSA 2048-bit keypair (e.g. via PowerShell `(New-Object System.Security.Cryptography.RSACryptoServiceProvider 2048).ToXmlString($true)`).
2. Store the **Private Key** in your repository: **Settings -> Secrets and variables -> Actions -> Repository secrets -> `RELEASE_SIGNING_PRIVATE_KEY`**.
3. (Optional) Store the matching **Public Key** in **`RELEASE_SIGNING_PUBLIC_KEY`** (if omitted, the build script will automatically derive the public key from the private key secret).
4. No code edits are required to rotate keys—simply update the repository secrets and trigger a build.

