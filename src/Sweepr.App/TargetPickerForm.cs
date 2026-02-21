using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Sweepr.Core.Models;
using Sweepr.Core.Services;

namespace Sweepr.App;

internal sealed class TargetPickerForm : Form
{
    private readonly Label _crosshair = new() { Text = "+", Font = new Font("Segoe UI", 24), AutoSize = true, Cursor = Cursors.Cross };
    private readonly Label _hint = new() { Text = "drag onto a window, tray icon or shortcut", AutoSize = true };
    private bool _dragging;
    private Point _dragStart;

    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(POINT pt);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X; public int Y; }

    public TargetPickerForm()
    {
        Text = "Target Picker";
        FormBorderStyle = FormBorderStyle.SizableToolWindow;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(100, 100);
        ClientSize = new Size(260, 120);
        Controls.Add(_crosshair);
        Controls.Add(_hint);
        _crosshair.Location = new Point(10, 10);
        _hint.Location = new Point(10, 60);

        _crosshair.MouseDown += (_, e) => { _dragging = true; _dragStart = e.Location; _crosshair.Capture = true; };
        _crosshair.MouseMove += (_, e) => { if (_dragging) { _crosshair.Left += e.X - _dragStart.X; _crosshair.Top += e.Y - _dragStart.Y; } };
        _crosshair.MouseUp += (_, _) => { _dragging = false; _crosshair.Capture = false; HandleDrop(); };
    }

    private void HandleDrop()
    {
        var pt = Cursor.Position;
        var hWnd = WindowFromPoint(new POINT { X = pt.X, Y = pt.Y });
        if (hWnd == IntPtr.Zero) { MessageBox.Show(this, "No window found under the cursor.", "Target Picker", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        GetWindowThreadProcessId(hWnd, out var pid);
        if (pid == 0) { MessageBox.Show(this, "Could not identify the process.", "Target Picker", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        Process? proc = null;
        try { proc = Process.GetProcessById((int)pid); } catch { }
        if (proc is null) { MessageBox.Show(this, "Process not accessible.", "Target Picker", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        var sb = new StringBuilder(256);
        GetWindowText(hWnd, sb, 256);
        var title = sb.ToString();

        var menu = new ContextMenuStrip();
        menu.Items.Add($"Target: {proc.ProcessName} (pid {pid})").Enabled = false;
        if (!string.IsNullOrWhiteSpace(title)) menu.Items.Add($"Window: {title}").Enabled = false;
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Uninstall (run its uninstaller)", null, (_, _) => TryUninstall(proc));
        menu.Items.Add("Stop process", null, (_, _) => { try { proc.Kill(); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Target Picker", MessageBoxButtons.OK, MessageBoxIcon.Warning); } });
        menu.Items.Add("Disable autostart (remove Run entries)", null, (_, _) => DisableAutostart(proc.ProcessName));
        menu.Show(Cursor.Position);
    }

    private void TryUninstall(Process proc)
    {
        var programs = ProgramScanner.GetInstalledPrograms();
        var match = programs.FirstOrDefault(p =>
            !string.IsNullOrWhiteSpace(p.InstallLocation) && proc.MainModule is not null &&
            proc.MainModule.FileName.StartsWith(p.InstallLocation, StringComparison.OrdinalIgnoreCase)) ??
            programs.FirstOrDefault(p => p.Name.Contains(proc.ProcessName, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            MessageBox.Show(this, $"No installed program matched '{proc.ProcessName}'.", "Target Picker", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(this, $"Uninstall '{match.Name}'?", "Target Picker", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        try { UninstallRunner.RunAsync(match).GetAwaiter().GetResult(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Target Picker", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        using var options = new ScanOptionsDialog();
        if (options.ShowDialog(this) == DialogResult.OK)
        {
            var leftovers = LeftoverScanner.Scan(match, options.Mode);
            if (leftovers.Count > 0)
            {
                using var dlg = new LeftoverDialog(match.Name, leftovers, preCheckFolders: options.Mode == ScanMode.Safe);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var selected = dlg.SelectedFolders.ToList();
                    if (selected.Count > 0)
                    {
                        var (deleted, errors) = LeftoverCleaner.DeleteFolders(selected);
                        var msg = $"Deleted {deleted} folder(s).";
                        if (errors.Count > 0) msg += "\n\nProblems:\n" + string.Join("\n", errors.Take(10));
                        MessageBox.Show(this, msg, "Target Picker", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            else MessageBox.Show(this, "No leftovers found.", "Target Picker", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void DisableAutostart(string processName)
    {
        int removed = 0;
        foreach (var (hive, path) in new[] { (Microsoft.Win32.Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run"), (Microsoft.Win32.Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run") })
        {
            try
            {
                using var key = hive.OpenSubKey(path, writable: true);
                if (key is null) continue;
                foreach (var valueName in key.GetValueNames().ToList())
                {
                    var data = key.GetValue(valueName) as string ?? "";
                    if (data.Contains(processName, StringComparison.OrdinalIgnoreCase))
                    {
                        key.DeleteValue(valueName, throwOnMissingValue: false);
                        removed++;
                    }
                }
            }
            catch { }
        }
        MessageBox.Show(this, removed > 0 ? $"Removed {removed} autostart entry/entries." : "No matching autostart entries found.", "Target Picker", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}