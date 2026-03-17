using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Sweepr.Core.Services;

// watches registry software keys for changes and keeps a timeline.
[SupportedOSPlatform("windows")]
public static class RegistryWatcher
{
    private static System.Threading.Timer? _timer;
    private static Dictionary<string, List<string>> _baseline = new();
    public static readonly List<string> Timeline = new();
    public static event Action<string>? Changed;

    public static void Start(TimeSpan interval)
    {
        Stop();
        _baseline = Snapshot();
        _timer = new System.Threading.Timer(_ => Tick(), null, interval, interval);
        Timeline.Add($"{DateTime.Now:HH:mm:ss} watcher started");
    }

    public static void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private static void Tick()
    {
        var current = Snapshot();
        foreach (var kv in current)
        {
            if (!_baseline.TryGetValue(kv.Key, out var old)) continue;
            var added = kv.Value.Except(old).ToList();
            var removed = old.Except(kv.Value).ToList();
            foreach (var a in added) { var msg = $"{DateTime.Now:HH:mm:ss} added {kv.Key}\\{a}"; Timeline.Add(msg); Changed?.Invoke(msg); }
            foreach (var r in removed) { var msg = $"{DateTime.Now:HH:mm:ss} removed {kv.Key}\\{r}"; Timeline.Add(msg); Changed?.Invoke(msg); }
        }
        _baseline = current;
    }

    private static Dictionary<string, List<string>> Snapshot()
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (hive, hiveName) in new[] { (Registry.CurrentUser, "HKCU"), (Registry.LocalMachine, "HKLM") })
        {
            try
            {
                using var software = hive.OpenSubKey("Software");
                if (software is null) continue;
                result[$"{hiveName}\\Software"] = software.GetSubKeyNames().ToList();
            }
            catch { }
        }
        return result;
    }
}