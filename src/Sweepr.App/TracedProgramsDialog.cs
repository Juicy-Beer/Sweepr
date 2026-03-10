using Sweepr.Core.Models;
using Sweepr.Core.Services;

namespace Sweepr.App;

internal sealed class TracedProgramsDialog : Form
{
    private readonly ListView _list = new() { View = View.Details, FullRowSelect = true, GridLines = true, Dock = DockStyle.Fill };
    private readonly ListView _details = new() { View = View.Details, FullRowSelect = true, GridLines = true, Dock = DockStyle.Fill };
    private readonly Button _refresh = new() { Text = "Refresh", Width = 90 };
    private readonly Button _remove = new() { Text = "Remove traced", Width = 120 };
    private readonly Button _close = new() { Text = "Close", Width = 80, DialogResult = DialogResult.Cancel };
    private readonly SplitContainer _split = new() { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 180 };
    private readonly Label _status = new() { Dock = DockStyle.Top, Height = 24, Padding = new Padding(8, 4, 8, 0) };

    public TracedProgramsDialog()
    {
        Text = "Traced Programs (Install Monitor)";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(820, 560);
        MinimumSize = new Size(600, 400);

        _list.Columns.Add("Name", 260);
        _list.Columns.Add("Started", 160);
        _list.Columns.Add("Completed", 160);
        _list.Columns.Add("Files", 70);
        _list.Columns.Add("Reg keys", 70);

        _details.Columns.Add("Captured path", 780);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        buttons.Controls.AddRange(new Control[] { _close, _remove, _refresh });
        _refresh.Click += (_, _) => LoadTraces();
        _remove.Click += (_, _) => RemoveSelected();
        _list.SelectedIndexChanged += (_, _) => ShowDetails();

        _split.Panel1.Controls.Add(_list);
        _split.Panel2.Controls.Add(_details);
        Controls.Add(_split);
        Controls.Add(_status);
        Controls.Add(buttons);
        CancelButton = _close;
        LoadTraces();
    }

    private void LoadTraces()
    {
        _list.Items.Clear();
        _details.Items.Clear();
        foreach (var t in TraceLogDatabase.GetAll())
        {
            _list.Items.Add(new ListViewItem(new[] { t.Name, t.StartedAt.ToString("g"), t.CompletedAt.ToString("g"), t.Files.Count.ToString(), t.RegistryKeys.Count.ToString() }) { Tag = t });
        }
        _status.Text = $"{_list.Items.Count} traced program(s).";
    }

    private void ShowDetails()
    {
        _details.Items.Clear();
        if (_list.SelectedItems.Count == 0) return;
        var t = (InstallTrace)_list.SelectedItems[0].Tag!;
        foreach (var f in t.Files) _details.Items.Add(new ListViewItem(f));
        foreach (var k in t.RegistryKeys) _details.Items.Add(new ListViewItem(k));
    }

    private void RemoveSelected()
    {
        if (_list.SelectedItems.Count == 0) return;
        var t = (InstallTrace)_list.SelectedItems[0].Tag!;
        var confirm = MessageBox.Show(this, $"Remove traced program '{t.Name}' and delete its captured files/keys?", "Sweepr", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;
        var (deletedFiles, fileErrors) = LeftoverCleaner.DeleteFiles(t.Files);
        var regLeftovers = t.RegistryKeys.Select(k => new Leftover(LeftoverKind.RegistryKey, k)).ToList();
        var (deletedReg, regErrors, backups) = LeftoverCleaner.DeleteRegistryKeys(regLeftovers, null);
        TraceLogDatabase.Delete(t.Name);
        var msg = $"Deleted {deletedFiles} file(s) and {deletedReg} registry key(s).";
        if (fileErrors.Count > 0 || regErrors.Count > 0) msg += "\n\nProblems:\n" + string.Join("\n", fileErrors.Concat(regErrors).Take(10));
        MessageBox.Show(this, msg, "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information);
        LoadTraces();
    }
}