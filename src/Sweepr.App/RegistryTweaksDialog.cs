using Sweepr.Core.Services;

namespace Sweepr.App;

internal sealed class RegistryTweaksDialog : Form
{
    private readonly ListView _list = new() { View = View.Details, FullRowSelect = true, GridLines = true, Dock = DockStyle.Fill };
    private readonly Button _apply = new() { Text = "Apply", Width = 80 };
    private readonly Button _revert = new() { Text = "Revert", Width = 80 };
    private readonly Button _close = new() { Text = "Close", Width = 80, DialogResult = DialogResult.Cancel };
    private readonly Label _status = new() { Dock = DockStyle.Top, Height = 24, Padding = new Padding(8, 4, 8, 0) };

    public RegistryTweaksDialog()
    {
        Text = "Registry Tweaks";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(760, 480);
        MinimumSize = new Size(600, 360);

        _list.Columns.Add("Name", 200);
        _list.Columns.Add("Category", 110);
        _list.Columns.Add("Description", 420);

        foreach (var t in RegistryTweaks.All)
            _list.Items.Add(new ListViewItem(new[] { t.Name, t.Category, t.Description }) { Tag = t });

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        buttons.Controls.AddRange(new Control[] { _close, _revert, _apply });

        _apply.Click += (_, _) =>
        {
            if (_list.SelectedItems.Count == 0) return;
            var t = (RegistryTweak)_list.SelectedItems[0].Tag!;
            var ok = RegistryTweaks.Apply(t);
            MessageBox.Show(this, ok ? "Applied." : "Failed.", "Sweepr", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        };
        _revert.Click += (_, _) =>
        {
            if (_list.SelectedItems.Count == 0) return;
            var t = (RegistryTweak)_list.SelectedItems[0].Tag!;
            var ok = RegistryTweaks.Revert(t);
            MessageBox.Show(this, ok ? "Reverted." : "Failed.", "Sweepr", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        };

        Controls.Add(_list);
        Controls.Add(_status);
        Controls.Add(buttons);
        CancelButton = _close;
        _status.Text = $"{RegistryTweaks.All.Count} tweaks available.";
    }
}