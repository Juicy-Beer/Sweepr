using System.Runtime.Versioning;

namespace Sweepr.Core.Services;

// clears browser caches for chrome, edge and firefox (best effort).
[SupportedOSPlatform("windows")]
public static class BrowserCleaner
{
    public static List<string> GetCachePaths()
    {
        var paths = new List<string>();
        var roots = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data")
        };
        foreach (var root in roots)
        {
            if (!Directory.Exists(root)) continue;
            try
            {
                foreach (var profile in Directory.EnumerateDirectories(root))
                {
                    var cache = Path.Combine(profile, "Cache");
                    if (Directory.Exists(cache)) paths.Add(cache);
                    var codeCache = Path.Combine(profile, "Code Cache");
                    if (Directory.Exists(codeCache)) paths.Add(codeCache);
                }
            }
            catch { }
        }
        // firefox
        var ffProfiles = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla", "Firefox", "Profiles");
        if (Directory.Exists(ffProfiles))
        {
            try
            {
                foreach (var profile in Directory.EnumerateDirectories(ffProfiles))
                {
                    var cache = Path.Combine(profile, "cache2");
                    if (Directory.Exists(cache)) paths.Add(cache);
                }
            }
            catch { }
        }
        return paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static (int Deleted, long FreedBytes) Clean()
    {
        int deleted = 0; long freed = 0;
        foreach (var path in GetCachePaths())
        {
            try
            {
                foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { var len = new FileInfo(f).Length; File.Delete(f); deleted++; freed += len; } catch { }
                }
            }
            catch { }
        }
        return (deleted, freed);
    }
}