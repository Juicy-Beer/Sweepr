using System.Text.Json;

namespace Sweepr.Core.Settings;

// application settings persisted to %LOCALAPPDATA%\Sweepr\settings.json.
public sealed class AppSettings
{
    public string Language { get; set; } = "English";
    public string Translator { get; set; } = "";
    public bool CheckForUpdateOnStartup { get; set; } = true;
    public bool SkipInformationMessages { get; set; } = false;
    public bool SkipWarningMessages { get; set; } = false;

    public bool ShowSystemComponents { get; set; } = false;
    public bool UseProgramUninstallDataForInstallDate { get; set; } = false;
    public bool FastLoadingMode { get; set; } = false;
    public bool OnlyRunBuiltInUninstaller { get; set; } = false;
    public bool CreateRestorePointBeforeUninstall { get; set; } = true;
    public bool OpenUninstallWizardMaximized { get; set; } = false;
    public bool CheckMarkAllLeftoversByDefault { get; set; } = false;
    public bool ForciblyStopRunningExecutables { get; set; } = false;
    public bool DeleteLeftoversToRecycleBin { get; set; } = false;

    public bool DeleteJunkToRecycleBin { get; set; } = false;
    public bool IgnoreFilesAccessedLast24Hours { get; set; } = false;
    public bool FirstRunCompleted { get; set; } = false;
    public List<string> JunkPatterns { get; set; } = new()
    {
        "*.tmp", "*.temp", "*.old", "*.bak", "*.log", "*.chk",
        "Thumbs.db", "desktop.ini", "*.~*", "*.gid"
    };

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Sweepr", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var s = JsonSerializer.Deserialize<AppSettings>(json);
                if (s is not null) return s;
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch { }
    }
}