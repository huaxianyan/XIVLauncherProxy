# XIVLauncher Proxy

[简体中文](README.md) | English

XIVLauncher Proxy provides per-shortcut local proxy settings and automatic OTP login for XIVLauncher. The configuration application only manages settings and creates shortcuts; daily launches are performed through those shortcuts.

## Features

Each launch profile can enable:

- Proxy only: disable auto-login for this launch and show account selection
- Quick login only: temporarily enable auto-login for one account
- Proxy and quick login together

Quick login waits until XIVLauncher requests an OTP, generates the current code, submits it locally, and restores the previous auto-login setting. Multiple accounts can have separate encrypted TOTP secrets and shortcuts.

Secret import supports Base32, standard `otpauth://` URIs, and Google Authenticator's private `otpauth-migration://` export format. PNG, JPEG, BMP, GIF, and WebP QR images can be selected directly. Multi-account and multi-image exports are combined for account selection. Images are decoded locally and are never uploaded or copied into the configuration directory.

## Usage

1. Run `XIVLauncherProxy.exe` to open configuration.
2. Confirm the `XIVLauncher.exe` path and configure an optional local proxy.
3. Create a launch profile and enable proxy, quick login, or both.
4. For quick login, select an XIVLauncher account with OTP enabled and enter its Base32 secret, an `otpauth://` URI, or choose authenticator export images with **Import QR**. Select all images at once when Google Authenticator creates a multi-image export.
5. If the export contains multiple OTP accounts, select the entry matching the current XIVLauncher account.
6. Save the profile and create its shortcut.
7. Launch through `XIVLauncher - Profile name.lnk`.

The XIVLauncher account must have One-Time Password enabled, and `Enable XL Authenticator app/OTP macro support` must be enabled. XIVLauncher auto-login does not need to remain enabled globally.

> [!NOTE]
> This application does not enroll, bind, remove, or migrate Square Enix OTP authenticators. Complete the official Square Enix enrollment process first and confirm that the official authenticator generates valid codes before saving the same TOTP secret here. Keep the official recovery codes in a safe place. Removing a secret from this application does not remove OTP from the Square Enix account.

## TOTP storage and security

> [!IMPORTANT]
> Zero-interaction login requires the application to generate a code without asking for another master password. A copied credential file cannot normally be used on another computer, but malware already running as the current Windows user may still obtain the secret. Enable this only on a trusted computer you control.

Ordinary settings are stored at:

```text
%LocalAppData%\XIVLauncherProxy\config.json
```

This file contains proxy settings, account names, XIVLauncher account associations, and feature flags. It does not contain TOTP secrets, Square Enix passwords, or generated verification codes.

Encrypted TOTP secrets are stored separately at:

```text
%LocalAppData%\XIVLauncherProxy\totp-credentials.json
```

The Base64 values in this file are Windows DPAPI ciphertext, not Base32 secrets that can be imported into an authenticator. Shortcuts contain no secret or generated code.

Secrets use Windows DPAPI with `CurrentUser` scope. Under normal conditions, copying only the credential file to another computer does not make it decryptable, and another Windows user on the same computer cannot directly decrypt it. The application briefly decrypts a secret in memory only when generating a code and does not intentionally log secrets, `otpauth://` URIs, or generated codes.

DPAPI protects the secret at the Windows user boundary, not behind an independent password vault. Malware already running as the same user may call DPAPI, inspect process memory, or replace application files. This is an inherent limit of unattended login and does not protect a compromised Windows session.

Recommended safeguards:

- Protect the Windows account with a strong password or Windows Hello
- Enable BitLocker for the system drive
- Do not run untrusted software or grant unnecessary administrator access
- Keep Square Enix recovery codes offline
- Re-enroll the authenticator if the computer may have been compromised

Clearing a secret removes its current encrypted record. Backups or filesystem snapshots may retain old ciphertext; after an actual compromise, re-enroll the authenticator rather than only deleting local files.

## Proxy scope

Proxy-enabled profiles set `HTTP_PROXY`, `HTTPS_PROXY`, and optionally `ALL_PROXY` and `NO_PROXY` for XIVLauncher and its child processes. User and system proxy settings are not changed. Local OTP submission always connects directly to `127.0.0.1`.

**Use this proxy for software updates** affects only update checks and downloads. When disabled, updates use a direct connection without changing launch-profile proxy behavior.

## Software updates

The configuration application checks the latest stable GitHub Release at startup, and updates can also be checked manually from **General settings**. When a newer version is available, it can be downloaded and installed directly.

Only `XIVLauncherProxy.exe` and `XIVLauncherProxy.exe.sha256` from the Release are accepted. Download progress is displayed, the SHA-256 digest is verified, and a temporary updater replaces the current executable before restarting the application. A failed replacement or failed startup restores the previous version.

Automatic installation supports the self-contained Windows single-file release. The currently running `XIVLauncherProxy.exe` is updated in place, so any writable installation directory is supported. Development builds can check for updates but cannot replace themselves. Staged files are stored under `%LocalAppData%\XIVLauncherProxy\updates` and removed after a successful restart.

## Architecture and platform scope

The project uses .NET 10, Avalonia UI, MVVM, and a modular monolith structure:

```text
XIVLauncherProxy.Domain
XIVLauncherProxy.Application
XIVLauncherProxy.Infrastructure
XIVLauncherProxy.Platform.Windows
XIVLauncherProxy.Desktop
```

Windows-specific DPAPI, shortcut creation, and path discovery are isolated in `Platform.Windows`. Windows is the only currently supported platform; future ports require corresponding platform implementations.

UI text uses `.resx` resources. English is the fallback language and Simplified Chinese is currently included.

## Build

.NET 10 SDK is required:

```powershell
dotnet build .\XIVLauncherProxy.sln -c Release
dotnet test .\XIVLauncherProxy.sln -c Release
```

Publish a self-contained Windows single-file executable:

```powershell
dotnet publish .\src\XIVLauncherProxy.Desktop\XIVLauncherProxy.Desktop.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None -o .\dist
```
