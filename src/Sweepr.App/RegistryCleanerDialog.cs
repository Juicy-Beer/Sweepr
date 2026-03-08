using Sweepr.Core.Models;
using Sweepr.Core.Services;
using Microsoft.Win32;

namespace Sweepr.App;

internal sealed class RegistryCleanerDialog : Form
{
    private readonly CheckedListBox _categories = new() { Dock = DockStyle.Left, Width = 220 };
    private readonly ListView _list = new() { View = View.Details, CheckBoxes = true, FullRowSelect = true, GridLines = true, Dock = DockStyle.Fill };
    private readonly ComboBox _mode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
    private readonly Button _scan = new() { Text = "Scan", Width = 80 };
    private readonly Button _fix = new() { Text = "Fix selected", Width = 110 };
    private readonly Button _close = new() { Text = "Close", Width = 80, DialogResult = DialogResult.Cancel };
    private readonly Label _status = new() { Dock = DockStyle.Top, Height = 24, Padding = new Padding(8, 4, 8, 0) };

    public RegistryCleanerDialog()
    {
        Text = "Registry Cleaner";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 560);
        MinimumSize = new Size(700, 400);

        foreach (var c in RegistryCleaner.AllCategories) _categories.Items.Add(c, true);
        _mode.Items.AddRange(new object[] { "Light", "Moderate", "Deep" });
        _mode.SelectedIndex = 1;
        _mode.SelectedIndexChanged += (_, _) => ApplyMode();

        _list.Columns.Add("Category", 150);
        _list.Columns.Add("Hive", 60);
        _list.Columns.Add("Key", 420);
        _list.Columns.Add("Value", 140);
        _list.Columns.Add("Reason", 120);

        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8) };
        top.Controls.Add(new Label { Text = "Mode:", AutoSize = true, Padding = new Padding(0, 6, 4, 0) });
        top.Controls.Add(_mode);
        top.Controls.Add(_scan);
        top.Controls.Add(_fix);
        top.Controls.Add(_close);

        Controls.Add(_list);
        Controls.Add(_categories);
        Controls.Add(_status);
        Controls.Add(top);
        CancelButton = _close;

        _scan.Click += (_, _) => DoScan();
        _fix.Click += (_, _) => DoFix();
        ApplyMode();
    }

    private void ApplyMode()
    {
        var mode = _mode.SelectedIndex switch { 0 => RegistryCleanMode.Light, 1 => RegistryCleanMode.Moderate, _ => RegistryCleanMode.Deep };
        var cats = new HashSet<string>(RegistryCleaner.CategoriesFor(mode), StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < _categories.Items.Count; i++)
            _categories.SetItemChecked(i, cats.Contains(_categories.Items[i]!.ToString()!));
    }

    private void DoScan()
    {
        _list.Items.Clear();
        _status.Text = "Scanning...";
        UseWaitCursor = true;
        var cats = _categories.CheckedItems.Cast<object>().Select(o => o.ToString()!).ToList();
        List<RegistryCleanEntry> entries;
        try { entries = RegistryCleaner.Scan(RegistryCleanMode.Custom, cats); }
        finally { UseWaitCursor = false; }
        foreach (var e in entries)
        {
            var row = new ListViewItem(new[] { e.Category, e.Hive, e.KeyPath, e.ValueName, e.Reason }) { Tag = e, Checked = true };
            _list.Items.Add(row);
        }
        _status.Text = $"Found {entries.Count} invalid entries.";
    }

    private void DoFix()
    {
        var selected = _list.CheckedItems.Cast<ListViewItem>().Select(i => (RegistryCleanEntry)i.Tag!).ToList();
        if (selected.Count == 0) { MessageBox.Show(this, "Tick at least one entry.", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        var confirm = MessageBox.Show(this, $"Remove {selected.Count} registry entries? Backups will be created.", "Sweepr", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        // backup all affected keys first
        var paths = selected.Select(e => e.KeyPath).Distinct().ToList();
        RegistryExporter.ExportToFile(paths, Path.Combine(Path.GetTempPath(), $"sweepr-cleaner-{DateTime.Now:yyyyMMddHHmmss}.reg"));

        int removed = 0;
        foreach (var e in selected)
        {
            try
            {
                if (!RegistryPath.TryParse(e.KeyPath, out var hive, out var segments) || segments.Count == 0) continue;
                var keyName = segments[^1];
                var parent = segments.Take(segments.Count - 1).ToList();
                using var root = Microsoft.Win32.RegistryKey.OpenBaseKey(hive, Microsoft.Win32.RegistryView.Registry64);
                using var pk = parent.Count == 0 ? root : root.OpenSubKey(string.Join('\\', parent), writable: true);
                if (pk is null) continue;
                if (string.IsNullOrWhiteSpace(e.ValueName) || e.ValueName == "(Default)")
                    pk.DeleteSubKeyTree(keyName, throwOnMissingSubKey: false);
                else
                    pk.DeleteValue(e.ValueName, throwOnMissingValue: false);
                removed++;
            }
            catch { }
        }
        MessageBox.Show(this, $"Removed {removed} of {selected.Count}.", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information);
        DoScan();
    }
}