using Sweepr.Core.Models;
using Sweepr.Core.Services;

namespace Sweepr.App;

internal sealed class WindowsAppsDialog : Form
{
    private readonly ListView _list = new() { View = View.Details, CheckBoxes = true, FullRowSelect = true, GridLines = true, Dock = DockStyle.Fill, MultiSelect = true };
    private readonly Button _refresh = new() { Text = "Refresh", Width = 90 };
    private readonly Button _uninstall = new() { Text = "Uninstall selected", Width = 130 };
    private readonly Button _close = new() { Text = "Close", Width = 80, DialogResult = DialogResult.Cancel };
    private readonly Label _status = new() { Dock = DockStyle.Top, Height = 24, Padding = new Padding(8, 4, 8, 0) };

    public WindowsAppsDialog()
    {
        Text = "Windows Apps (Store/UWP)";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(820, 520);
        MinimumSize = new Size(600, 360);

        _list.Columns.Add("Name", 260);
        _list.Columns.Add("Version", 110);
        _list.Columns.Add("Architecture", 90);
        _list.Columns.Add("Publisher", 200);
        _list.Columns.Add("Package", 300);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        buttons.Controls.AddRange(new Control[] { _close, _uninstall, _refresh });
        _refresh.Click += (_, _) => LoadApps();
        _uninstall.Click += (_, _) => UninstallSelected();

        Controls.Add(_list);
        Controls.Add(_status);
        Controls.Add(buttons);
        CancelButton = _close;
        LoadApps();
    }

    private void LoadApps()
    {
        _list.Items.Clear();
        _status.Text = "Loading Windows Apps...";
        UseWaitCursor = true;
        List<WindowsApp> apps;
        try { apps = WindowsAppScanner.GetInstalledApps(allUsers: true); }
        finally { UseWaitCursor = false; }
        foreach (var a in apps)
        {
            if (a.IsFramework || a.IsBundle) continue;
            var row = new ListViewItem(new[] { a.Name, a.Version, a.Architecture, a.Publisher, a.PackageFullName }) { Tag = a };
            _list.Items.Add(row);
        }
        _status.Text = $"Found {_list.Items.Count} apps.";
    }

    private void UninstallSelected()
    {
        var selected = _list.CheckedItems.Cast<ListViewItem>().Select(i => (WindowsApp)i.Tag!).ToList();
        if (selected.Count == 0) { MessageBox.Show(this, "Tick at least one app.", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        var confirm = MessageBox.Show(this, $"Uninstall {selected.Count} Windows app(s)?", "Sweepr", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        int ok = 0, fail = 0;
        foreach (var a in selected)
        {
            if (WindowsAppUninstaller.Remove(a.PackageFullName)) ok++; else fail++;
        }
        MessageBox.Show(this, $"Uninstalled {ok}, failed {fail}.", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information);
        LoadApps();
    }
}