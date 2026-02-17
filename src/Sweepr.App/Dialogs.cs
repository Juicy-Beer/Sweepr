using Sweepr.Core.Services;
using System.Diagnostics;

namespace Sweepr.App;

// lets the user pick how deep the leftover scan goes.
internal sealed class ScanOptionsDialog : Form
{
    private readonly RadioButton _safe = new() { Text = "Safe - exact name matches only (recommended)", Checked = true, AutoSize = true, Left = 20, Top = 44 };
    private readonly RadioButton _moderate = new() { Text = "Moderate - names that contain the program name", AutoSize = true, Left = 20, Top = 74 };
    private readonly RadioButton _advanced = new() { Text = "Advanced - broader matching, review carefully", AutoSize = true, Left = 20, Top = 104 };

    public ScanOptionsDialog()
    {
        Text = "Leftover scan mode";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 190);

        var ok = new Button { Text = "Next", DialogResult = DialogResult.OK, Left = 270, Top = 140, Width = 80 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 360, Top = 140, Width = 80 };

        Controls.Add(new Label { Text = "How thoroughly should Sweepr look for leftovers?", AutoSize = true, Left = 20, Top = 16 });
        Controls.AddRange(new Control[] { _safe, _moderate, _advanced, ok, cancel });
        AcceptButton = ok;
        CancelButton = cancel;
    }

    public ScanMode Mode =>
        _advanced.Checked ? ScanMode.Advanced :
        _moderate.Checked ? ScanMode.Moderate :
        ScanMode.Safe;
}

// shows found leftovers, the user ticks what should be deleted.
internal sealed class LeftoverDialog : Form
{
    private readonly ListView _list = new()
    {
        View = View.Details,
        CheckBoxes = true,
        FullRowSelect = true,
        GridLines = true,
        Dock = DockStyle.Fill
    };

    public LeftoverDialog(string programName, IReadOnlyList<Leftover> leftovers, bool preCheckFolders)
    {
        Text = $"Leftovers - {programName}";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(820, 480);
        MinimumSize = new Size(600, 300);

        _list.Columns.Add("Type", 110);
        _list.Columns.Add("Path", 680);

        foreach (var item in leftovers)
        {
            var isFolder = item.Kind == LeftoverKind.Folder;
            var row = new ListViewItem(new[] { isFolder ? "Folder" : "Registry (review only)", item.Path })
            {
                Tag = item,
                Checked = isFolder && preCheckFolders
            };
            _list.Items.Add(row);
        }

        var hasFolders = leftovers.Any(l => l.Kind == LeftoverKind.Folder);
        var hasRegistry = leftovers.Any(l => l.Kind == LeftoverKind.RegistryKey);
        var labelText = (hasFolders, hasRegistry) switch
        {
            (true, false) => "Review the folders below. Ticked folders will be deleted.",
            (false, true) => "Review the registry keys below. Ticked keys will be backed up to .reg, then deleted.",
            _ => "Review the items below. Ticked folders will be deleted. Ticked registry keys will be backed up to .reg, then deleted."
        };

        var top = new Label
        {
            Text = labelText,
            Dock = DockStyle.Top,
            Height = 36,
            Padding = new Padding(8, 8, 8, 0)
        };

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
        var delete = new Button { Text = "Delete selected", DialogResult = DialogResult.OK, Width = 120 };
        var none = new Button { Text = "Select none", Width = 90 };
        var all = new Button { Text = "Select all folders", Width = 120 };
        none.Click += (_, _) => SetAll(false);
        all.Click += (_, _) => SetAll(true);
        buttons.Controls.AddRange(new Control[] { cancel, delete, none, all });

        Controls.Add(_list);
        Controls.Add(top);
        Controls.Add(buttons);
        AcceptButton = delete;
        CancelButton = cancel;
    }

    public IEnumerable<Leftover> SelectedFolders =>
        _list.CheckedItems.Cast<ListViewItem>()
             .Select(i => (Leftover)i.Tag!)
             .Where(l => l.Kind == LeftoverKind.Folder);


/// <summary>restores a saved .reg backup.</summary>
internal sealed class RestoreBackupDialog : Form
{
    private readonly string _folder;
    private readonly ListView _list = new() { View = View.Details, FullRowSelect = true, Dock = DockStyle.Fill };
    private readonly Button _restore = new() { Text = "Restore selected", Width = 130 };
    private readonly Button _open = new() { Text = "Open folder", Width = 100 };
    private readonly Button _close = new() { Text = "Close", Width = 80, DialogResult = DialogResult.Cancel };

    public RestoreBackupDialog(string folder)
    {
        _folder = folder;
        Text = "Restore registry backup";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(700, 420);
        MinimumSize = new Size(500, 300);

        _list.Columns.Add("Backup file", 660);
        RefreshList();

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        buttons.Controls.AddRange(new Control[] { _close, _open, _restore });
        _restore.Click += (_, _) => RestoreSelected();
        _open.Click += (_, _) => { try { Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_folder}\"") { UseShellExecute = false }); } catch { } };

        Controls.Add(_list);
        Controls.Add(buttons);
        CancelButton = _close;
    }

    private void RefreshList()
    {
        _list.Items.Clear();
        if (!Directory.Exists(_folder)) return;
        foreach (var f in Directory.EnumerateFiles(_folder, "*.reg").OrderByDescending(File.GetLastWriteTime))
            _list.Items.Add(new ListViewItem(Path.GetFileName(f)) { Tag = f });
    }

    private void RestoreSelected()
    {
        if (_list.SelectedItems.Count == 0) { MessageBox.Show(this, "Select a backup first.", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        var file = _list.SelectedItems[0].Tag as string;
        if (file is null) return;
        var confirm = MessageBox.Show(this, $"Import {Path.GetFileName(file)} back into the registry?", "Sweepr", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;
        try
        {
            var psi = new ProcessStartInfo("reg.exe", $"import \"{file}\"") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            using var p = Process.Start(psi)!;
            var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode == 0) MessageBox.Show(this, "Restore complete.", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else MessageBox.Show(this, $"Restore failed:\n{output}", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
    public IEnumerable<Leftover> SelectedRegistryKeys =>
        _list.CheckedItems.Cast<ListViewItem>()
             .Select(i => (Leftover)i.Tag!)
             .Where(l => l.Kind == LeftoverKind.RegistryKey);

    private void SetAll(bool value)
    {
        foreach (ListViewItem item in _list.Items)
            item.Checked = value && item.Tag is Leftover { Kind: LeftoverKind.Folder };
    }
}
