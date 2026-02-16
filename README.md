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

## 20 professional toolsets included

1. Multi-drive scanner
2. File metadata indexer
3. Duplicate detector with policy engine
4. Optional checksum validator (SHA-256)
5. Parallel fast copy engine
6. Memory-safe stream copy
7. Photo database mirror (year/month)
8. Project/work directory preservation
9. Smart file categorization engine
10. Software/install file quarantine
11. Hidden/system file filtering options
12. Ignore directory rules
13. Dry-run mode
14. Profile-based job configuration
15. Error collection and reporting
16. Merge-by-project destination structure
17. Job naming and repeatable automation
18. Legacy + modern target frameworks
19. MSIX-ready packaging pathway
20. Security settings profile

## GUI UX features

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
- Deterministic exit codes for automation (0 success, 2 partial failures, 1 fatal).

## Project layout

- `src/WindowsDiamondFile.Core` - backup/index/categorization engine.
- `src/WindowsDiamondFile.Cli` - command-line executable.
- `src/WindowsDiamondFile.Gui` - WPF GUI executable.
- `docs/RELEASE.md` - build, publish, installer, and store workflow.
- `scripts/sample-backup-profile.json` - production-ready example profile.

## One-click usage flow

1. Edit a backup profile JSON with source drives and output drive.
2. Run `WindowsDiamondFile.exe backup-profile.json` (CLI) or use the GUI.
3. App scans all source drives, merges into output, preserves project paths.
4. Photos also copy into `Photo-Database/<year>/<month>`.
5. Existing files are deduplicated by your selected policy.

## Production review status

The app has been reviewed and updated for production-readiness:

- Safer duplicate decisions (content-aware mode)
- Collision-safe rename strategy
- Access-denied traversal resilience
- Drive-root naming consistency
- Timestamp preservation
- WPF GUI with real-time feedback
- CLI first-run experience improvements
