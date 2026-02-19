using Sweepr.Core.Models;
using Sweepr.Core.Services;

namespace Sweepr.App;

internal sealed class BrowserExtensionsDialog : Form
{
    private readonly ListView _list = new() { View = View.Details, CheckBoxes = true, FullRowSelect = true, GridLines = true, Dock = DockStyle.Fill, MultiSelect = true };
    private readonly Button _refresh = new() { Text = "Refresh", Width = 90 };
    private readonly Button _remove = new() { Text = "Remove selected", Width = 130 };
    private readonly Button _close = new() { Text = "Close", Width = 80, DialogResult = DialogResult.Cancel };
    private readonly Label _status = new() { Dock = DockStyle.Top, Height = 24, Padding = new Padding(8, 4, 8, 0) };

    public BrowserExtensionsDialog()
    {
        Text = "Browser Extensions";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(820, 520);
        MinimumSize = new Size(600, 360);

        _list.Columns.Add("Browser", 100);
        _list.Columns.Add("Name", 260);
        _list.Columns.Add("Version", 110);
        _list.Columns.Add("Id", 260);
        _list.Columns.Add("Path", 300);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        buttons.Controls.AddRange(new Control[] { _close, _remove, _refresh });
        _refresh.Click += (_, _) => LoadExtensions();
        _remove.Click += (_, _) => RemoveSelected();

        Controls.Add(_list);
        Controls.Add(_status);
        Controls.Add(buttons);
        CancelButton = _close;
        LoadExtensions();
    }

    private void LoadExtensions()
    {
        _list.Items.Clear();
        _status.Text = "Scanning browser extensions...";
        UseWaitCursor = true;
        List<BrowserExtension> exts;
        try { exts = BrowserExtensionScanner.ScanAll(); }
        finally { UseWaitCursor = false; }
        foreach (var e in exts)
        {
            var row = new ListViewItem(new[] { e.Browser, e.Name, e.Version, e.Id, e.Path }) { Tag = e };
            _list.Items.Add(row);
        }
        _status.Text = $"Found {_list.Items.Count} extensions.";
    }

    private void RemoveSelected()
    {
        var selected = _list.CheckedItems.Cast<ListViewItem>().Select(i => (BrowserExtension)i.Tag!).ToList();
        if (selected.Count == 0) { MessageBox.Show(this, "Tick at least one extension.", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        var confirm = MessageBox.Show(this, $"Remove {selected.Count} extension(s)? Browsers may need to be closed first.", "Sweepr", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        int ok = 0, fail = 0;
        var errors = new List<string>();
        foreach (var e in selected)
        {
            var (success, error) = BrowserExtensionScanner.Remove(e);
            if (success) ok++; else { fail++; if (!string.IsNullOrWhiteSpace(error)) errors.Add(error); }
        }
        var msg = $"Removed {ok}, failed {fail}.";
        if (errors.Count > 0) msg += "\n\n" + string.Join("\n", errors.Take(10));
        MessageBox.Show(this, msg, "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information);
        LoadExtensions();
    }
}