using Sweepr.Core.Settings;

namespace Sweepr.App;

internal sealed class SettingsDialog : Form
{
    private readonly AppSettings _settings;
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly Button _ok = new() { Text = "OK", DialogResult = DialogResult.OK, Width = 80 };
    private readonly Button _cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };

    private readonly ComboBox _language = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly TextBox _translator = new() { Width = 200 };
    private readonly CheckBox _checkUpdate = new() { Text = "Check for update on startup", AutoSize = true };
    private readonly CheckBox _skipInfo = new() { Text = "Skip information messages", AutoSize = true };
    private readonly CheckBox _skipWarn = new() { Text = "Skip warning messages", AutoSize = true };

    private readonly CheckBox _showSystem = new() { Text = "Show system components", AutoSize = true };
    private readonly CheckBox _useProgramData = new() { Text = "Use program's uninstall data for Install Date", AutoSize = true };
    private readonly CheckBox _fastLoading = new() { Text = "Enable fast loading mode", AutoSize = true };
    private readonly CheckBox _onlyBuiltIn = new() { Text = "Only run built-in uninstaller", AutoSize = true };
    private readonly CheckBox _restorePoint = new() { Text = "Make a System Restore Point before uninstall", AutoSize = true };
    private readonly CheckBox _maximized = new() { Text = "Open Uninstall Wizard maximized", AutoSize = true };
    private readonly CheckBox _checkAllLeftovers = new() { Text = "Check mark all leftovers by default", AutoSize = true };
    private readonly CheckBox _stopExecutables = new() { Text = "Forcibly stop running executables during their uninstall", AutoSize = true };
    private readonly CheckBox _recycleLeftovers = new() { Text = "Delete leftover files and folders to the Recycle Bin", AutoSize = true };

    private readonly CheckBox _junkRecycle = new() { Text = "Delete files to Recycle Bin", AutoSize = true };
    private readonly CheckBox _ignore24h = new() { Text = "Ignore files accessed in last 24 hours", AutoSize = true };

    public SettingsDialog(AppSettings settings)
    {
        _settings = settings;
        Text = "Options";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 480);
        MinimumSize = new Size(520, 440);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _language.Items.AddRange(new object[] { "English", "Spanish", "French", "German", "Italian", "Portuguese", "Russian", "Chinese", "Japanese" });
        _language.SelectedItem = settings.Language;
        if (_language.SelectedIndex < 0) _language.SelectedIndex = 0;
        _translator.Text = settings.Translator;

        _checkUpdate.Checked = settings.CheckForUpdateOnStartup;
        _skipInfo.Checked = settings.SkipInformationMessages;
        _skipWarn.Checked = settings.SkipWarningMessages;

        _showSystem.Checked = settings.ShowSystemComponents;
        _useProgramData.Checked = settings.UseProgramUninstallDataForInstallDate;
        _fastLoading.Checked = settings.FastLoadingMode;
        _onlyBuiltIn.Checked = settings.OnlyRunBuiltInUninstaller;
        _restorePoint.Checked = settings.CreateRestorePointBeforeUninstall;
        _maximized.Checked = settings.OpenUninstallWizardMaximized;
        _checkAllLeftovers.Checked = settings.CheckMarkAllLeftoversByDefault;
        _stopExecutables.Checked = settings.ForciblyStopRunningExecutables;
        _recycleLeftovers.Checked = settings.DeleteLeftoversToRecycleBin;

        _junkRecycle.Checked = settings.DeleteJunkToRecycleBin;
        _ignore24h.Checked = settings.IgnoreFilesAccessedLast24Hours;

        BuildTabs();

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        buttons.Controls.AddRange(new Control[] { _cancel, _ok });
        _ok.Click += (_, _) => { SaveSettings(); DialogResult = DialogResult.OK; };

        Controls.Add(_tabs);
        Controls.Add(buttons);
        AcceptButton = _ok;
        CancelButton = _cancel;
    }

    private void BuildTabs()
    {
        var generalTab = new TabPage("General");
        var generalLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(12) };
        generalLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        generalLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        generalLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        generalLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        generalLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        generalLayout.Controls.Add(new Label { Text = "Current language:", AutoSize = true }, 0, 0);
        generalLayout.Controls.Add(_language, 1, 0);
        generalLayout.Controls.Add(new Label { Text = "Translator:", AutoSize = true }, 0, 1);
        generalLayout.Controls.Add(_translator, 1, 1);
        var generalChecks = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Dock = DockStyle.Fill };
        generalChecks.Controls.Add(_checkUpdate);
        generalChecks.Controls.Add(_skipInfo);
        generalChecks.Controls.Add(_skipWarn);
        generalLayout.Controls.Add(generalChecks, 0, 2);
        generalLayout.SetColumnSpan(generalChecks, 2);
        generalTab.Controls.Add(generalLayout);

        var uninstallerTab = new TabPage("Uninstaller");
        var uninstallerLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12), AutoScroll = true };
        uninstallerLayout.Controls.Add(_showSystem);
        uninstallerLayout.Controls.Add(_useProgramData);
        uninstallerLayout.Controls.Add(_fastLoading);
        uninstallerLayout.Controls.Add(_onlyBuiltIn);
        uninstallerLayout.Controls.Add(_restorePoint);
        uninstallerLayout.Controls.Add(_maximized);
        uninstallerLayout.Controls.Add(_checkAllLeftovers);
        uninstallerLayout.Controls.Add(_stopExecutables);
        uninstallerLayout.Controls.Add(_recycleLeftovers);
        uninstallerTab.Controls.Add(uninstallerLayout);

        var junkTab = new TabPage("Junk Files Cleaner");
        var junkLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12), AutoScroll = true };
        junkLayout.Controls.Add(_junkRecycle);
        junkLayout.Controls.Add(_ignore24h);
        junkTab.Controls.Add(junkLayout);

        _tabs.TabPages.Add(generalTab);
        _tabs.TabPages.Add(uninstallerTab);
        _tabs.TabPages.Add(junkTab);
    }

    private void SaveSettings()
    {
        _settings.Language = _language.SelectedItem?.ToString() ?? "English";
        _settings.Translator = _translator.Text;
        _settings.CheckForUpdateOnStartup = _checkUpdate.Checked;
        _settings.SkipInformationMessages = _skipInfo.Checked;
        _settings.SkipWarningMessages = _skipWarn.Checked;

        _settings.ShowSystemComponents = _showSystem.Checked;
        _settings.UseProgramUninstallDataForInstallDate = _useProgramData.Checked;
        _settings.FastLoadingMode = _fastLoading.Checked;
        _settings.OnlyRunBuiltInUninstaller = _onlyBuiltIn.Checked;
        _settings.CreateRestorePointBeforeUninstall = _restorePoint.Checked;
        _settings.OpenUninstallWizardMaximized = _maximized.Checked;
        _settings.CheckMarkAllLeftoversByDefault = _checkAllLeftovers.Checked;
        _settings.ForciblyStopRunningExecutables = _stopExecutables.Checked;
        _settings.DeleteLeftoversToRecycleBin = _recycleLeftovers.Checked;

        _settings.DeleteJunkToRecycleBin = _junkRecycle.Checked;
        _settings.IgnoreFilesAccessedLast24Hours = _ignore24h.Checked;

        _settings.Save();
    }
}