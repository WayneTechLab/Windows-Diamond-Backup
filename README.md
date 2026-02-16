# Windows Diamond File

Windows Diamond File is a professional multi-drive backup, indexing, and smart organization app for Windows 7, 10, 11, and newer systems.

## Apps included

1. **GUI desktop app** (`WindowsDiamondFile.Gui.exe`) for one-click interactive operation.
2. **CLI app** (`WindowsDiamondFile.exe`) for automation/scripting.

## Production-grade capabilities

- Multi-drive scanning across HDD/SSD/NVMe/USB sources.
- Full-file indexing with metadata and categorization.
- Merge backup into one destination while preserving source structure.
- Photo mirror database by year/month.
- Duplicate policies:
  - `SkipBySizeAndName`
  - `SkipOnlyWhenContentMatches` (safe default)
  - `KeepBothWithRename`
- File verification (size + optional SHA-256).
- Timestamp preservation.
- Access-denied resilient traversal.
- Executable quarantine logic.
- Dry-run mode.

## GUI UX improvements

- Add/remove/clear source folders.
- Output folder picker.
- Duplicate policy selector.
- Parallel copy tuning.
- Photo DB root customization.
- Live status and progress bar.
- Cancel running job.
- Save/load JSON profiles.

## CLI hardening

- Detects placeholder profile paths and exits with friendly guidance.
- Creates profile parent directories if missing.
- Writes starter profile for first run when path is valid.

## Project layout

- `src/WindowsDiamondFile.Core` - backup/index/categorization engine.
- `src/WindowsDiamondFile.Cli` - command-line executable.
- `src/WindowsDiamondFile.Gui` - WPF GUI executable.
- `docs/RELEASE.md` - build, publish, installer, and store workflow.
- `scripts/sample-backup-profile.json` - production-ready example profile.
