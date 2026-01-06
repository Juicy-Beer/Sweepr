# Sweepr

A free, open-source Windows uninstaller that removes programs **and** the leftovers they leave behind (folders and registry keys).

> Status: Phase 2 (safety + polish). See [docs/ROADMAP.md](docs/ROADMAP.md).
> Not affiliated with any commercial uninstaller product.

## What works today (Phase 2)

- Lists installed programs (HKLM, HKCU, 32-bit and 64-bit) with icons
- Search by name or company, sortable columns, details panel
- Uninstall: runs the program's own uninstaller, then scans for leftovers
- Force remove: scan for leftovers of an already-removed program
- Three scan modes: Safe / Moderate / Advanced
- Review screen: you choose what gets deleted (folders + registry keys)
- Registry keys are backed up to .reg before deletion, with a restore screen
- System Restore point is created before removal
- Program icons in the list, Icons/Details view switch
- Protected paths blocklist (never touches Windows, Program Files root, etc.)

## Build

Requirements: Windows 10/11, [.NET 8 SDK](https://dotnet.microsoft.com/download).

```
dotnet build -c Release
dotnet test
dotnet publish src/Sweepr.App -c Release -r win-x64 --self-contained false -o publish
```

The app requests administrator rights on launch (needed to read and remove system-wide entries). Test it in a Windows VM first.

## Installer

Install [Inno Setup](https://jrsoftware.org/isinfo.php), publish the app (command above), then compile `installer/Sweepr.iss`. The installer is written to `dist/`.

## Project layout

```
src/Sweepr.Core    scanning + uninstall logic (no UI)
src/Sweepr.App     WinForms UI
src/Sweepr.Tests   unit tests
installer/         Inno Setup script
docs/              roadmap
```

## Safety

- Nothing is deleted without a confirmation screen.
- Components published by Microsoft are never scanned for leftovers.
- Use at your own risk. Back up important data first.

## License

GNU General Public License v3.0 (GPLv3). See [LICENSE](LICENSE). Copyright (c) 2026 Juicy-Beer.
