# Steam Toolkit MRM

**A native Windows application for creating, managing, and restoring Steam and Lua Tools backups.**

Steam Toolkit MRM replaces the former Steam plugin with a standalone WPF desktop app. It preserves the legacy `BackupSteamMRM-*` backup format and provides a clean interface for backups, restore, retention, and automation.

**GitHub description:** A native Windows app for creating, managing, and restoring Steam and Lua Tools backups.

## Download

Download the latest Windows installer from [GitHub Releases](https://github.com/TheKillerMRM/SteamToolkitMRM/releases/latest). The installer is self-contained and does not require a separate .NET installation.

## Features

- Backs up Steam `userdata`, `appcache/stats`, `depotcache`, and `config/stplug-in`.
- Restores legacy backups while preserving unrelated files.
- Lists, pins, renames, restores, and deletes backups.
- Optional automatic backups when a game starts or closes.
- Configurable backup location and retention limit.
- Purple-by-default accent color with a color picker.
- Portuguese, English, Spanish, and Russian interface.
- Optional Windows startup and system-tray mode.
- Opens the separately licensed Ludusavi utility.

## Build and test

Requirements: .NET 10 SDK, Inno Setup 6, and internet access to download the pinned Ludusavi release.

```powershell
dotnet run --project .\native\SteamToolkitMRM.csproj
dotnet run --project .\native.tests\SteamToolkitMRM.Tests.csproj
.\build-distributions.ps1
```

The build script verifies Ludusavi release checksums, publishes the installable package, and also creates a portable build for local testing. Generated binaries and local settings are intentionally excluded from source control; the installer is distributed through GitHub Releases.

See [native/README.md](./native/README.md) for detailed build and backup-test instructions. Licensing information for the application and bundled dependency is in [LICENSE](./LICENSE) and [THIRD-PARTY-NOTICES.md](./THIRD-PARTY-NOTICES.md).

Steam and Lua Tools are trademarks of their respective owners. This project is independent and is not affiliated with or endorsed by Valve or Lua Tools.
