# Build and Release Guide

## 1) Build classic EXE (Windows 7/10/11)

Use Visual Studio 2022 Build Tools or newer on Windows:

```powershell
msbuild WindowsDiamondFile.sln /p:Configuration=Release
```

Output binary:

- `src\WindowsDiamondFile.Cli\bin\Release\net48\WindowsDiamondFile.exe`

## 2) Build modern EXE / self-contained

```powershell
dotnet publish src\WindowsDiamondFile.Cli\WindowsDiamondFile.Cli.csproj -c Release -f net8.0-windows10.0.19041.0 -r win-x64 --self-contained true
```

## 3) Create installer

Recommended options:

- **MSIX Packaging Tool** for Store-compatible package.
- **Inno Setup** for classic installer with Windows 7 support.
- **WiX Toolset** for enterprise MSI deployment.

## 4) Microsoft Store path

1. Package app as MSIX.
2. Add app capabilities and signing certificate.
3. Validate with Windows App Certification Kit.
4. Submit via Partner Center.

## 5) Security and reliability checklist

- Code sign EXE/MSIX.
- Enable Windows Defender reputation bootstrap.
- Run copy verification enabled in profile.
- Use dry-run on large migrations first.
- Keep quarantine enabled for executable files.
- Use `DuplicateHandling=SkipOnlyWhenContentMatches` for safest deduplication.
- Keep `ContinueOnAccessDenied=true` to avoid job failure on protected paths.
- Validate automation using process exit code (`0` success, `2` partial failures).
