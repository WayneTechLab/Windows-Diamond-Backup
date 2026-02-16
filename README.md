# Windows Diamond File

Windows Diamond File is a professional multi-drive backup, indexing, and smart organization app concept for Windows 7, 10, 11, and newer systems.

This repository contains a production-focused starter implementation that can be built as:

1. **Classic desktop EXE** (Windows installer path).
2. **Modern package candidate** (can be wrapped into MSIX for Microsoft Store distribution).

## What the app does

- Scans multiple source drives (HDD/SSD/NVMe/M.2/USB).
- Creates a full file index and metadata table.
- Backs up all files into one output location.
- Uses duplicate policies to avoid accidental data loss:
  - skip by size+name,
  - skip only when content matches,
  - keep both with safe rename.
- Verifies copied files (size + optional SHA-256).
- Preserves original working folder structures for software/projects.
- Builds a photo mirror database grouped by year/month.
- Applies smart category rules for non-photo files.
- Quarantines executables/installers when enabled.
- Preserves timestamps and continues safely on access-denied paths.

## 20 professional toolsets included in this design

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

## Project layout

- `src/WindowsDiamondFile.Core` - backup/index/categorization engine.
- `src/WindowsDiamondFile.Cli` - executable entry point (`WindowsDiamondFile.exe`).
- `docs/RELEASE.md` - build, publish, installer, and store workflow.
- `scripts/sample-backup-profile.json` - production-ready example profile.

## One-click usage flow

1. Edit a backup profile JSON with source drives and output drive.
2. Run `WindowsDiamondFile.exe backup-profile.json`.
3. App scans all source drives, merges into output, preserves project paths.
4. Photos also copy into `Photo-Database/<year>/<month>`.
5. Existing files are deduplicated by your selected policy.

## Production review status

The app has been reviewed and updated for production-readiness concerns in these areas:

- safer duplicate decisions (content-aware mode),
- collision-safe rename strategy,
- access-denied traversal resilience,
- drive-root naming consistency,
- timestamp preservation,
- deterministic CLI exit codes for automation.

## Status

This is a strong **starter implementation + architecture** designed for extension into:

- WPF/WinUI UI with one-click UX.
- Scheduling service.
- AI model-based semantic tagging.
- Installer + signed enterprise release pipeline.
