using System.Runtime.Versioning;
using System.Text.Json;
using Sweepr.Core.Models;

namespace Sweepr.Core.Services;

// captures a before/after snapshot of files and registry keys around an install.
[SupportedOSPlatform("windows")]
public static class InstallMonitor
{
    private static string MonitorDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sweepr", "monitor");

    private static readonly string[] FileRoots = new[]
    {
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs")
    };

    private class Baseline { public List<string> Files { get; set; } = new(); public List<string> RegKeys { get; set; } = new(); }

    public static void Start(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name required", nameof(name));
        Directory.CreateDirectory(MonitorDir);
        var baseline = new Baseline { Files = SnapshotFiles(), RegKeys = SnapshotRegistry() };
        File.WriteAllText(Path.Combine(MonitorDir, Sanitize(name) + ".json"), JsonSerializer.Serialize(baseline));
    }

    public static InstallTrace? Stop(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var path = Path.Combine(MonitorDir, Sanitize(name) + ".json");
        if (!File.Exists(path)) return null;
        var baseline = JsonSerializer.Deserialize<Baseline>(File.ReadAllText(path)) ?? new Baseline();

        var afterFiles = SnapshotFiles();
        var afterReg = SnapshotRegistry();

        var newFiles = afterFiles.Except(baseline.Files, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        var newReg = afterReg.Except(baseline.RegKeys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

        try { File.Delete(path); } catch { }

        var trace = new InstallTrace
        {
            Name = name,
            StartedAt = DateTime.Now.AddMinutes(-1),
            CompletedAt = DateTime.Now,
            Files = newFiles,
            RegistryKeys = newReg
        };
        TraceLogDatabase.Add(trace);
        return trace;
    }

    public static bool IsMonitoring(string name) =>
        !string.IsNullOrWhiteSpace(name) && File.Exists(Path.Combine(MonitorDir, Sanitize(name) + ".json"));

    private static List<string> SnapshotFiles()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in FileRoots)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) continue;
            try
            {
                foreach (var f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                    set.Add(f);
            }
            catch { }
        }
        return set.ToList();
    }

    private static List<string> SnapshotRegistry()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (hive, hiveName) in new[] { (Microsoft.Win32.Registry.CurrentUser, "HKCU"), (Microsoft.Win32.Registry.LocalMachine, "HKLM") })
        {
            try
            {
                using var software = hive.OpenSubKey("Software");
                if (software is null) continue;
                foreach (var name in software.GetSubKeyNames())
                    set.Add($"{hiveName}\\Software\\{name}");
            }
            catch { }
        }
        return set.ToList();
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name;
    }
}