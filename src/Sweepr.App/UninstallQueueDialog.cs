using Sweepr.Core.Models;
using Sweepr.Core.Services;

namespace Sweepr.App;

internal sealed class UninstallQueueDialog : Form
{
    private readonly UninstallQueue _queue;
    private readonly ListView _list = new() { View = View.Details, FullRowSelect = true, GridLines = true, Dock = DockStyle.Fill, MultiSelect = true };
    private readonly Button _run = new() { Text = "Run queue", Width = 100 };
    private readonly Button _remove = new() { Text = "Remove selected", Width = 120 };
    private readonly Button _clear = new() { Text = "Clear", Width = 70 };
    private readonly Button _close = new() { Text = "Close", Width = 80, DialogResult = DialogResult.Cancel };
    private readonly Label _status = new() { Dock = DockStyle.Top, Height = 24, Padding = new Padding(8, 4, 8, 0) };

    public UninstallQueueDialog(UninstallQueue queue)
    {
        _queue = queue;
        Text = "Uninstall Queue";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(700, 420);
        MinimumSize = new Size(500, 300);

        _list.Columns.Add("Application", 280);
        _list.Columns.Add("Company", 170);
        _list.Columns.Add("Version", 110);
        RefreshList();

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        buttons.Controls.AddRange(new Control[] { _close, _clear, _remove, _run });
        _run.Click += async (_, _) => await RunQueueAsync();
        _remove.Click += (_, _) => RemoveSelected();
        _clear.Click += (_, _) => { _queue.Clear(); RefreshList(); };

        Controls.Add(_list);
        Controls.Add(_status);
        Controls.Add(buttons);
        CancelButton = _close;
    }

    private void RefreshList()
    {
        _list.Items.Clear();
        foreach (var p in _queue.Items)
            _list.Items.Add(new ListViewItem(new[] { p.Name, p.Publisher, p.Version }) { Tag = p });
        _status.Text = $"{_queue.Items.Count} item(s) queued.";
    }

    private void RemoveSelected()
    {
        foreach (ListViewItem row in _list.SelectedItems)
            if (row.Tag is InstalledProgram p) _queue.Remove(p);
        RefreshList();
    }

    private async Task RunQueueAsync()
    {
        if (_queue.Items.Count == 0) { MessageBox.Show(this, "Queue is empty.", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        var confirm = MessageBox.Show(this, $"Run uninstall queue for {_queue.Items.Count} program(s)?", "Sweepr", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        Enabled = false;
        UseWaitCursor = true;
        try
        {
            var snapshot = _queue.Items.ToList();
            foreach (var program in snapshot)
            {
                _status.Text = $"Uninstalling {program.Name}...";
                Application.DoEvents();
                try
                {
                    var rpOk = await Task.Run(() => SystemRestore.CreateRestorePoint($"Sweepr: {program.Name} (queue)"));
                    if (!rpOk) { }
                    await UninstallRunner.RunAsync(program);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"{program.Name}: {ex.Message}", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }

                var leftovers = await Task.Run(() => LeftoverScanner.Scan(program, ScanMode.Moderate));
                if (leftovers.Count > 0)
                {
                    using var dlg = new LeftoverDialog(program.Name, leftovers, preCheckFolders: true);
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        var selected = dlg.SelectedFolders.ToList();
                        var selectedReg = dlg.SelectedRegistryKeys.ToList();
                        if (selected.Count > 0) LeftoverCleaner.DeleteFolders(selected);
                        if (selectedReg.Count > 0) LeftoverCleaner.DeleteRegistryKeys(selectedReg, null);
                    }
                }
                _queue.Remove(program);
                RefreshList();
            }
            MessageBox.Show(this, "Queue finished.", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            UseWaitCursor = false;
            Enabled = true;
            RefreshList();
        }
    }
}