using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Sweepr.Core.Services;

public sealed class AutorunEntry
{
    public string Source { get; init; } = "";
    public string Name { get; init; } = "";
    public string Command { get; init; } = "";
    public string Location { get; init; } = "";
    public bool IsRegistry { get; init; }
    public RegistryKey? Key { get; init; }
}

[SupportedOSPlatform("windows")]
public static class AutorunManager
{
    public static List<AutorunEntry> GetAll()
    {
        var results = new List<AutorunEntry>();
        var runPaths = new[] { @"Software\Microsoft\Windows\CurrentVersion\Run", @"Software\Microsoft\Windows\CurrentVersion\RunOnce" };
        foreach (var (hive, hiveName) in new[] { (Registry.CurrentUser, "HKCU"), (Registry.LocalMachine, "HKLM") })
        {
            foreach (var p in runPaths)
            {
                try
                {
                    using var key = hive.OpenSubKey(p);
                    if (key is null) continue;
                    foreach (var name in key.GetValueNames())
                    {
                        results.Add(new AutorunEntry
                        {
                            Source = hiveName,
                            Name = name,
                            Command = key.GetValue(name) as string ?? "",
                            Location = $@"{hiveName}\{p}",
                            IsRegistry = true
                        });
                    }
                }
                catch { }
            }
        }

        // startup folders
        foreach (var folder in new[] { Environment.GetFolderPath(Environment.SpecialFolder.Startup), Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup) })
        {
            if (!Directory.Exists(folder)) continue;
            foreach (var file in Directory.EnumerateFiles(folder))
            {
                results.Add(new AutorunEntry
                {
                    Source = "Startup folder",
                    Name = Path.GetFileName(file),
                    Command = file,
                    Location = folder,
                    IsRegistry = false
                });
            }
        }
        return results;
    }

    public static bool Disable(AutorunEntry entry)
    {
        try
        {
            if (entry.IsRegistry)
            {
                var hive = entry.Source == "HKCU" ? Registry.CurrentUser : Registry.LocalMachine;
                var subPath = entry.Location.Substring(entry.Source.Length + 1);
                using var key = hive.OpenSubKey(subPath, writable: true);
                if (key is null) return false;
                key.DeleteValue(entry.Name, throwOnMissingValue: false);
                return true;
            }
            else
            {
                if (!File.Exists(entry.Command)) return false;
                var disabledDir = Path.Combine(Path.GetDirectoryName(entry.Command)!, "Disabled");
                Directory.CreateDirectory(disabledDir);
                File.Move(entry.Command, Path.Combine(disabledDir, Path.GetFileName(entry.Command)), overwrite: true);
                return true;
            }
        }
        catch { return false; }
    }
}