using Sweepr.Core.Services;

namespace Sweepr.App;

internal sealed class ExtraToolsDialog : Form
{
    public ExtraToolsDialog()
    {
        Text = "Extra Tools";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 320);
        MinimumSize = new Size(360, 280);

        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(16), WrapContents = false };
        var buttons = new[]
        {
            Make("Autorun Manager", () => new AutorunManagerDialog().ShowDialog(this)),
            Make("Junk Files Cleaner", () => new JunkFilesCleanerDialog().ShowDialog(this)),
            Make("Browser Cleaner", () => { var (d, f) = BrowserCleaner.Clean(); MessageBox.Show(this, $"Deleted {d} files, freed {f / 1024} KB.", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information); }),
            Make("Secure Delete (pick a file)", () => SecureDeletePick()),
        };
        foreach (var b in buttons) layout.Controls.Add(b);
        Controls.Add(layout);
    }

    private static Button Make(string text, Action onClick)
    {
        var b = new Button { Text = text, Width = 380, Height = 40, Margin = new Padding(0, 0, 0, 8) };
        b.Click += (_, _) => onClick();
        return b;
    }

    private void SecureDeletePick()
    {
        using var dlg = new OpenFileDialog { Title = "Select a file to wipe securely" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var ok = SecureDelete.Wipe(dlg.FileName);
        MessageBox.Show(this, ok ? "File wiped." : "Failed to wipe file.", "Sweepr", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }
}

internal sealed class AutorunManagerDialog : Form
{
    private readonly ListView _list = new() { View = View.Details, FullRowSelect = true, GridLines = true, Dock = DockStyle.Fill, MultiSelect = true };
    private readonly Button _refresh = new() { Text = "Refresh", Width = 90 };
    private readonly Button _disable = new() { Text = "Disable selected", Width = 120 };
    private readonly Button _close = new() { Text = "Close", Width = 80, DialogResult = DialogResult.Cancel };

    public AutorunManagerDialog()
    {
        Text = "Autorun Manager";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(760, 460);
        MinimumSize = new Size(600, 360);
        _list.Columns.Add("Source", 130);
        _list.Columns.Add("Name", 200);
        _list.Columns.Add("Command", 400);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        buttons.Controls.AddRange(new Control[] { _close, _disable, _refresh });
        _refresh.Click += (_, _) => LoadEntries();
        _disable.Click += (_, _) => DisableSelected();
        Controls.Add(_list);
        Controls.Add(buttons);
        CancelButton = _close;
        LoadEntries();
    }

    private void LoadEntries()
    {
        _list.Items.Clear();
        foreach (var e in AutorunManager.GetAll())
            _list.Items.Add(new ListViewItem(new[] { e.Source, e.Name, e.Command }) { Tag = e });
    }

    private void DisableSelected()
    {
        int ok = 0;
        foreach (ListViewItem row in _list.SelectedItems)
            if (row.Tag is AutorunEntry e && AutorunManager.Disable(e)) ok++;
        MessageBox.Show(this, $"Disabled {ok} entry/entries.", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information);
        LoadEntries();
    }
}

internal sealed class JunkFilesCleanerDialog : Form
{
    private readonly ListView _list = new() { View = View.Details, CheckBoxes = true, FullRowSelect = true, GridLines = true, Dock = DockStyle.Fill, MultiSelect = true };
    private readonly Button _scan = new() { Text = "Scan", Width = 80 };
    private readonly Button _clean = new() { Text = "Clean selected", Width = 120 };
    private readonly Button _close = new() { Text = "Close", Width = 80, DialogResult = DialogResult.Cancel };
    private readonly Label _status = new() { Dock = DockStyle.Top, Height = 24, Padding = new Padding(8, 4, 8, 0) };

    public JunkFilesCleanerDialog()
    {
        Text = "Junk Files Cleaner";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(760, 460);
        MinimumSize = new Size(600, 360);
        _list.Columns.Add("Path", 620);
        _list.Columns.Add("Size (KB)", 110);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        buttons.Controls.AddRange(new Control[] { _close, _clean, _scan });
        _scan.Click += (_, _) => Scan();
        _clean.Click += (_, _) => Clean();
        Controls.Add(_list);
        Controls.Add(_status);
        Controls.Add(buttons);
        CancelButton = _close;
    }

    private void Scan()
    {
        _list.Items.Clear();
        _status.Text = "Scanning...";
        UseWaitCursor = true;
        List<JunkFilesCleaner.JunkItem> items;
        try { items = JunkFilesCleaner.Scan(); }
        finally { UseWaitCursor = false; }
        foreach (var it in items)
            _list.Items.Add(new ListViewItem(new[] { it.Path, (it.SizeBytes / 1024).ToString() }) { Tag = it, Checked = true });
        _status.Text = $"Found {items.Count} junk files.";
    }

    private void Clean()
    {
        var selected = _list.CheckedItems.Cast<ListViewItem>().Select(i => (JunkFilesCleaner.JunkItem)i.Tag!).ToList();
        if (selected.Count == 0) return;
        var (deleted, freed, errors) = JunkFilesCleaner.Delete(selected);
        var msg = $"Deleted {deleted} files, freed {freed / 1024} KB.";
        if (errors.Count > 0) msg += "\n\nProblems:\n" + string.Join("\n", errors.Take(10));
        MessageBox.Show(this, msg, "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Scan();
    }
}
