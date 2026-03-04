# Sweepr roadmap

## Phase 1 (this repo)
- [x] Installed programs list, search, sort, details panel
- [x] Uninstall + leftover scan (folders), registry leftovers review-only
- [x] Protected paths, unit tests, CI build

## Phase 2: safety + polish
- [x] Create a System Restore point before removal
- [x] Export registry keys to .reg before deleting, restore screen
- [x] Delete registry leftovers (with backup)
- [x] Program icons in the list, Icons/Details view switch
- [x] Install date / size accuracy fixes

## Phase 3: more uninstall targets
- [x] Windows Apps (Store/UWP) via PowerShell Get-AppxPackage
- [x] Browser extensions (Chrome, Edge, Firefox)
- [x] Multiple/quick uninstall queue
- [x] Target Picker (drag a target onto a window/tray icon)

## Phase 4: install tracking
- [x] Install monitor: snapshot files + registry before/after install
- [x] Tracked Programs view
- [x] Logs database (SQLite)

## Phase 5: registry cleaner
- [x] 15+ scan categories, 4 scan modes
- [x] Include/exclude custom keys
- [x] Auto + full registry backup
- [x] Registry tweaks (50), scheduled auto clean
- [x] Registry Watcher, review/save/export changes

## Phase 6: extra tools
- [x] Autorun manager, junk files cleaner, browser cleaner
- [x] Office cleaner, Windows cleaner, history cleaner
- [x] Secure delete

## Phase 7: release
- [ ] Inno Setup installer + portable zip on GitHub Releases
- [ ] Code signing
