using Sweepr.Core.Services;

namespace Sweepr.App;

internal sealed class InstallMonitorDialog : Form
{
    private readonly TextBox _name = new() { Width = 300 };
    private readonly Button _start = new() { Text = "Start monitoring", Width = 130 };
    private readonly Button _stop = new() { Text = "Stop & save trace", Width = 130 };
    private readonly Button _close = new() { Text = "Close", Width = 80, DialogResult = DialogResult.Cancel };
    private readonly Label _status = new() { Dock = DockStyle.Top, Height = 40, Padding = new Padding(8, 8, 8, 0) };

    public InstallMonitorDialog()
    {
        Text = "Install Monitor";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 160);
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(16) };
        var row = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
        row.Controls.Add(new Label { Text = "Program name:", AutoSize = true, Padding = new Padding(0, 6, 4, 0) });
        row.Controls.Add(_name);
        layout.Controls.Add(row);
        var btns = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
        btns.Controls.AddRange(new Control[] { _start, _stop });
        layout.Controls.Add(btns);
        Controls.Add(layout);
        Controls.Add(_status);
        CancelButton = _close;

        _start.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_name.Text)) { MessageBox.Show(this, "Enter a program name.", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            try { InstallMonitor.Start(_name.Text.Trim()); _status.Text = $"Monitoring '{_name.Text.Trim()}'... Install the program, then click Stop."; }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        };
        _stop.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_name.Text)) return;
            var trace = InstallMonitor.Stop(_name.Text.Trim());
            if (trace is null) { MessageBox.Show(this, "No active monitoring for that name.", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            _status.Text = $"Saved trace: {trace.Files.Count} files, {trace.RegistryKeys.Count} registry keys.";
            MessageBox.Show(this, $"Trace saved.\n{trace.Files.Count} new files\n{trace.RegistryKeys.Count} new registry keys", "Sweepr", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
    }
}