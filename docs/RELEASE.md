# Build and Release Guide

## 1) Build all apps (Core + CLI + GUI)

Use Visual Studio 2022 Build Tools or newer on Windows:

```powershell
msbuild WindowsDiamondFile.sln /p:Configuration=Release
```

Key outputs:

- `src\WindowsDiamondFile.Cli\bin\Release\net48\WindowsDiamondFile.exe`
- `src\WindowsDiamondFile.Gui\bin\Release\net48\WindowsDiamondFile.Gui.exe`

## 2) Publish modern self-contained binaries

CLI:

```powershell
dotnet publish src\WindowsDiamondFile.Cli\WindowsDiamondFile.Cli.csproj -c Release -f net8.0-windows10.0.19041.0 -r win-x64 --self-contained true
```

GUI:

```powershell
dotnet publish src\WindowsDiamondFile.Gui\WindowsDiamondFile.Gui.csproj -c Release -f net8.0-windows10.0.19041.0 -r win-x64 --self-contained true
```

## 3) Installer options

- **Inno Setup** for classic EXE installer (supports Windows 7 via net48 target).
- **WiX Toolset** for enterprise MSI.
- **MSIX Packaging Tool** for modern Windows Store pathway.

## 4) Operational defaults for production

- Prefer `DuplicateHandling=SkipOnlyWhenContentMatches`.
- Keep `ContinueOnAccessDenied=true`.
- Keep `VerifyCopiedFiles=true`.
- Start with `DryRun=true` for first large migration.
- Use GUI for operations teams and CLI for scheduled automation.

## 5) CLI first-run behavior

- If profile JSON does not exist, CLI creates a starter profile.
- If provided path contains placeholder text (for example `path\to\your-backup-profile.json`), CLI exits with a friendly error.

## 6) Security and reliability checklist

- Code sign EXE/MSIX.
- Keep executable quarantine enabled.
- Validate automation via exit codes (`0` success, `2` partial failures, `1` fatal).
