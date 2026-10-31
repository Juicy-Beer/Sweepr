using System.Diagnostics;
using Sweepr.Core.Models;
using Sweepr.Core.Services;
using Sweepr.Core.Settings;

namespace Sweepr.App;

// plain, classic main window: toolbar, search box, program list, details panel, status bar.
public sealed class MainForm : Form
{
    private readonly ListView _list = new()
    {
        View = View.Details,
        FullRowSelect = true,
        GridLines = true,
        HideSelection = false,
        MultiSelect = false,
        Dock = DockStyle.Fill,
        SmallImageList = null
    };

    private readonly ImageList _smallImages = new() { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
    private readonly ImageList _largeImages = new() { ImageSize = new Size(32, 32), ColorDepth = ColorDepth.Depth32Bit };
    private readonly Dictionary<string, int> _iconIndex = new(StringComparer.OrdinalIgnoreCase);

    private readonly ToolStrip _toolbar = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _statusLabel = new();
    private readonly Panel _searchPanel = new() { Dock = DockStyle.Top, Height = 34 };
    private readonly TextBox _search = new() { Left = 64, Top = 6, Width = 260 };
    private readonly ComboBox _searchBy = new() { Left = 410, Top = 6, Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _details = new()
    {
        Dock = DockStyle.Bottom,
        Height = 72,
        Padding = new Padding(8, 6, 8, 6),
        BorderStyle = BorderStyle.FixedSingle
    };

    private List<InstalledProgram> _all = new();
    private readonly AppSettings _settings = AppSettings.Load();
    private readonly UninstallQueue _queue = new();
    private ToolStripButton? _viewButton;
    private ToolStripButton? _targetPickerButton;
    private int _sortColumn;
    private bool _sortAscending = true;

    public MainForm()
    {
        Text = "Sweepr";
        ClientSize = new Size(1100, 680);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        BuildUi();
        Load += (_, _) => RefreshPrograms();
        KeyDown += OnKeyDown;

        // first-run: show license, then settings.
        if (!_settings.FirstRunCompleted)
        {
            using var license = new LicenseDialog();
            if (license.ShowDialog(this) != DialogResult.OK)
            {
                // declined license so close the app.
                Close();
                return;
            }
            using var settings = new SettingsDialog(_settings);
            settings.ShowDialog(this);
            _settings.FirstRunCompleted = true;
            _settings.Save();
        }

        // load a custom window icon if present next to the exe.
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
            if (File.Exists(iconPath)) Icon = new Icon(iconPath);
        }
        catch
        {
            // fall back to the default window icon.
        }
    }

    // ---------- ui ----------

    private void BuildUi()
    {
        _list.SmallImageList = _smallImages;
        _list.LargeImageList = _largeImages;

        // toolbar
        _toolbar.Items.Add(MakeButton("Uninstall", async () => await UninstallSelectedAsync()));
        _toolbar.Items.Add(MakeButton("Force Remove", async () => await ForcedUninstallSelectedAsync()));
        _toolbar.Items.Add(new ToolStripSeparator());
        _toolbar.Items.Add(MakeButton("Refresh", () => RefreshPrograms()));
        _toolbar.Items.Add(new ToolStripSeparator());
        _viewButton = MakeButton("View: Details", () => ToggleView());
        _toolbar.Items.Add(_viewButton);
        _toolbar.Items.Add(new ToolStripSeparator());
        _toolbar.Items.Add(MakeButton("Restore backup", () => OpenRestoreDialog()));
        _toolbar.Items.Add(new ToolStripSeparator());
        _toolbar.Items.Add(MakeButton("Options", () => OpenSettingsDialog()));
        _toolbar.Items.Add(new ToolStripSeparator());
        _toolbar.Items.Add(MakeButton("Registry Cleaner", () => OpenRegistryCleanerDialog()));
        _toolbar.Items.Add(MakeButton("Tweaks", () => OpenRegistryTweaksDialog()));
        _toolbar.Items.Add(MakeButton("Watcher", () => ToggleWatcher()));
        _toolbar.Items.Add(new ToolStripSeparator());
        _toolbar.Items.Add(MakeButton("Traced", () => OpenTracedProgramsDialog()));
        _toolbar.Items.Add(MakeButton("Monitor", () => OpenInstallMonitorDialog()));
        _toolbar.Items.Add(new ToolStripSeparator());
        _toolbar.Items.Add(MakeButton("Windows Apps", () => OpenWindowsAppsDialog()));
        _toolbar.Items.Add(MakeButton("Browser Ext", () => OpenBrowserExtensionsDialog()));
        _toolbar.Items.Add(MakeButton("Queue", () => OpenQueueDialog()));
        _targetPickerButton = MakeButton("Target Picker", () => OpenTargetPicker());
        _toolbar.Items.Add(_targetPickerButton);
        _toolbar.Items.Add(new ToolStripSeparator());
        _toolbar.Items.Add(MakeButton("Tools", () => OpenExtraToolsDialog()));

        // search row
        _searchBy.Items.AddRange(new object[] { "Name", "Company" });
        _searchBy.SelectedIndex = 0;
        _search.TextChanged += (_, _) => ApplyFilter();
        _searchBy.SelectedIndexChanged += (_, _) => ApplyFilter();
        _searchPanel.Controls.Add(new Label { Text = "Search:", AutoSize = true, Left = 8, Top = 10 });
        _searchPanel.Controls.Add(_search);
        _searchPanel.Controls.Add(new Label { Text = "Search by:", AutoSize = true, Left = 340, Top = 10 });
        _searchPanel.Controls.Add(_searchBy);

        // list
        _list.Columns.Add("Application", 280);
        _list.Columns.Add("Size", 90, HorizontalAlignment.Right);
        _list.Columns.Add("Version", 110);
        _list.Columns.Add("Type", 60);
        _list.Columns.Add("Company", 170);
        _list.Columns.Add("Install Date", 90);
        _list.Columns.Add("Website", 200);
        _list.Columns.Add("Comment", 220);
        _list.ColumnClick += (_, e) =>
        {
            if (_sortColumn == e.Column) _sortAscending = !_sortAscending;
            else { _sortColumn = e.Column; _sortAscending = true; }
            ApplyFilter();
        };
        _list.SelectedIndexChanged += (_, _) => UpdateDetails();
        _list.DoubleClick += async (_, _) => await UninstallSelectedAsync();
        _list.ContextMenuStrip = BuildContextMenu();

        // status bar
        _status.Items.Add(_statusLabel);

        // order matters for docking: fill first, edge panels after (last added = outermost).
        Controls.Add(_list);
        Controls.Add(_details);
        Controls.Add(_searchPanel);
        Controls.Add(_status);
        Controls.Add(_toolbar);
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Uninstall", null, async (_, _) => await UninstallSelectedAsync());
        menu.Items.Add("Force Remove", null, async (_, _) => await ForcedUninstallSelectedAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Open install folder", null, (_, _) => OpenInstallFolder());
        menu.Items.Add("Search online", null, (_, _) => SearchOnline());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Add to queue", null, (_, _) => { var p = Selected; if (p is not null) _queue.Add(p); });
        return menu;
    }

    private static ToolStripButton MakeButton(string text, Action onClick)
    {
        var button = new ToolStripButton(text) { DisplayStyle = ToolStripItemDisplayStyle.Text };
        button.Click += (_, _) => onClick();
        return button;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F5) { RefreshPrograms(); e.Handled = true; }
        else if (e.Control && e.KeyCode == Keys.F) { _search.Focus(); e.Handled = true; }
        else if (e.KeyCode == Keys.Delete && _list.Focused) { _ = UninstallSelectedAsync(); e.Handled = true; }
    }

    // ---------- data ----------

    private void RefreshPrograms()
    {
        UseWaitCursor = true;
        try { _all = ProgramScanner.GetInstalledPrograms(_settings.ShowSystemComponents); }
        finally { UseWaitCursor = false; }
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var term = _search.Text.Trim();
        IEnumerable<InstalledProgram> query = _all;

        if (term.Length > 0)
        {
            query = _searchBy.SelectedIndex == 1
                ? query.Where(p => p.Publisher.Contains(term, StringComparison.OrdinalIgnoreCase))
                : query.Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        query = _sortAscending
            ? query.OrderBy(p => SortKey(p, _sortColumn))
            : query.OrderByDescending(p => SortKey(p, _sortColumn));

        var items = query.Select(ToItem).ToArray();

        _list.BeginUpdate();
        _list.Items.Clear();
        _list.Items.AddRange(items);
        _list.EndUpdate();

        UpdateDetails();
    }

    private static object SortKey(InstalledProgram p, int column) => column switch
    {
        1 => p.SizeBytes,
        2 => p.Version.ToLowerInvariant(),
        3 => p.Architecture,
        4 => p.Publisher.ToLowerInvariant(),
        5 => p.InstallDate,
        6 => p.Website.ToLowerInvariant(),
        7 => p.Comments.ToLowerInvariant(),
        _ => p.Name.ToLowerInvariant()
    };

    private ListViewItem ToItem(InstalledProgram p) => new(new[]
    {
        p.Name, FormatSize(p.SizeBytes), p.Version, p.Architecture,
        p.Publisher, p.InstallDate, p.Website, p.Comments
    })
    { Tag = p, ImageIndex = GetIconIndex(p.DisplayIcon) };

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0) return "";
        string[] units = { "KB", "MB", "GB", "TB" };
        double value = bytes / 1024.0;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
        return $"{value:0.##} {units[unit]}";
    }

    private void OpenSettingsDialog()
    {
        using var dlg = new SettingsDialog(_settings);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _settings.Save();
            RefreshPrograms();
        }
    }

    private void OpenRestoreDialog()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sweepr", "Backups");
        using var dlg = new LeftoverDialog.RestoreBackupDialog(folder);
        dlg.ShowDialog(this);
    }

    private void OpenWindowsAppsDialog()
    {
        using var dlg = new WindowsAppsDialog();
        dlg.ShowDialog(this);
    }

    private void OpenBrowserExtensionsDialog()
    {
        using var dlg = new BrowserExtensionsDialog();
        dlg.ShowDialog(this);
    }

    private void OpenQueueDialog()
    {
        using var dlg = new UninstallQueueDialog(_queue);
        dlg.ShowDialog(this);
    }

    private void OpenTargetPicker()
    {
        using var dlg = new TargetPickerForm();
        dlg.Show(this);
    }

    private void OpenRegistryCleanerDialog() { using var d = new RegistryCleanerDialog(); d.ShowDialog(this); }
    private void OpenRegistryTweaksDialog() { using var d = new RegistryTweaksDialog(); d.ShowDialog(this); }
    private void OpenTracedProgramsDialog() { using var d = new TracedProgramsDialog(); d.ShowDialog(this); }
    private void OpenInstallMonitorDialog() { using var d = new InstallMonitorDialog(); d.ShowDialog(this); }
    private void OpenExtraToolsDialog() { using var d = new ExtraToolsDialog(); d.ShowDialog(this); }

    private void ToggleWatcher()
    {
        if (_watcherOn) { RegistryWatcher.Stop(); _watcherOn = false; MessageBox.Show(this, "Watcher stopped.", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        else { RegistryWatcher.Start(TimeSpan.FromSeconds(5)); _watcherOn = true; MessageBox.Show(this, "Watcher started (5s interval).", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information); }
    }

    private bool _watcherOn;

    private void ToggleView()
    {
        if (_list.View == View.Details)
        {
            _list.View = View.LargeIcon;
            if (_viewButton is not null) _viewButton.Text = "View: Icons";
        }
        else
        {
            _list.View = View.Details;
            if (_viewButton is not null) _viewButton.Text = "View: Details";
        }
    }

    private int GetIconIndex(string displayIcon)
    {
        if (string.IsNullOrWhiteSpace(displayIcon)) return -1;
        var path = displayIcon.Split(',')[0].Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return -1;
        if (_iconIndex.TryGetValue(path, out var existing)) return existing;
        try
        {
            using var ico = Icon.ExtractAssociatedIcon(path);
            if (ico is null) return -1;
            var idx = _smallImages.Images.Count;
            _smallImages.Images.Add(ico);
            _largeImages.Images.Add(new Icon(ico, 32, 32));
            _iconIndex[path] = idx;
            return idx;
        }
        catch
        {
            return -1;
        }
    }

    private InstalledProgram? Selected =>
        _list.SelectedItems.Count > 0 ? _list.SelectedItems[0].Tag as InstalledProgram : null;

    private void UpdateDetails()
    {
        var p = Selected;
        _details.Text = p is null
            ? "Select a program to see its details."
            : $"{p.Name}\r\n" +
              $"Install date: {p.InstallDate}    Version: {p.Version}    Size: {FormatSize(p.SizeBytes)}    Company: {p.Publisher}\r\n" +
              $"Location: {p.InstallLocation}";

        _statusLabel.Text = $"Installations: {_list.Items.Count}    |    {_list.SelectedItems.Count} selected";
    }

    // ---------- actions ----------

    private async Task UninstallSelectedAsync()
    {
        var program = Selected;
        if (program is null) return;

        var confirm = MessageBox.Show(this,
            $"Uninstall \"{program.Name}\"?\n\nSweepr will run the program's own uninstaller, then scan for leftovers.",
            "Sweepr", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        using var options = new ScanOptionsDialog();
        if (options.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            Enabled = false;
            if (_settings.CreateRestorePointBeforeUninstall)
            {
                // try to create a restore point first; warn but continue if it fails.
                var rpOk = await Task.Run(() => SystemRestore.CreateRestorePoint($"Sweepr: {program.Name}"));
                if (!rpOk)
                    MessageBox.Show(this, "Could not create a System Restore point. Continuing anyway.", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            await UninstallRunner.RunAsync(program, _settings.ForciblyStopRunningExecutables);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            // still offer the leftover scan even if the uninstaller failed
        }
        finally
        {
            Enabled = true;
        }

        // scan for leftovers and show the review dialog
        await ScanAndReviewAsync(program, options.Mode);
        RefreshPrograms();
    }

    private async Task ForcedUninstallSelectedAsync()
    {
        var program = Selected;
        if (program is null) return;

        var confirm = MessageBox.Show(this,
            $"Force remove of \"{program.Name}\" skips the program's own uninstaller and only scans for leftovers.\n\nContinue?",
            "Sweepr", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        using var options = new ScanOptionsDialog();
        if (options.ShowDialog(this) != DialogResult.OK) return;

        if (_settings.CreateRestorePointBeforeUninstall)
        {
            var rpOk = await Task.Run(() => SystemRestore.CreateRestorePoint($"Sweepr: {program.Name} (forced)"));
            if (!rpOk)
                MessageBox.Show(this, "Could not create a System Restore point. Continuing anyway.", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        await ScanAndReviewAsync(program, options.Mode);
        RefreshPrograms();
    }

    private async Task ScanAndReviewAsync(InstalledProgram program, ScanMode mode)
    {
        UseWaitCursor = true;
        List<Leftover> leftovers;
        try { leftovers = await Task.Run(() => LeftoverScanner.Scan(program, mode)); }
        finally { UseWaitCursor = false; }

        if (leftovers.Count == 0)
        {
            MessageBox.Show(this, "No leftovers found.", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // one dialog for both folders and registry
        using var dialog = new LeftoverTreeViewDialog(program.Name, leftovers);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var selectedFolders = dialog.SelectedFolders.ToList();
        var selectedReg = dialog.SelectedRegistryKeys.ToList();

        // delete folders
        if (selectedFolders.Count > 0)
        {
            var (deleted, errors) = await Task.Run(() =>
                LeftoverCleaner.DeleteFolders(selectedFolders, _settings.DeleteLeftoversToRecycleBin));

            var msg = $"Deleted {deleted} folder(s).";
            if (errors.Count > 0) msg += "\n\nProblems:\n" + string.Join("\n", errors.Take(10));
            MessageBox.Show(this, msg, "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // delete registry keys (backed up before deletion)
        if (selectedReg.Count > 0)
        {
            var (deleted, errors, backups) = await Task.Run(() =>
                LeftoverCleaner.DeleteRegistryKeys(selectedReg, null));

            var msg = $"Deleted {deleted} registry key(s), backed up to {backups.Count} .reg file(s).";
            if (errors.Count > 0) msg += "\n\nProblems:\n" + string.Join("\n", errors.Take(10));
            if (backups.Count > 0) msg += "\n\nBackups: " + string.Join(", ", backups);
            MessageBox.Show(this, msg, "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void OpenInstallFolder()
    {
        var p = Selected;
        if (p is null || !Directory.Exists(p.InstallLocation)) return;
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{p.InstallLocation}\"") { UseShellExecute = false });
    }

    private void SearchOnline()
    {
        var p = Selected;
        if (p is null) return;
        Process.Start(new ProcessStartInfo($"https://www.google.com/search?q={Uri.EscapeDataString(p.Name)}") { UseShellExecute = true });
    }
}
