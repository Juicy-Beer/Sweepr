using System.Runtime.Versioning;

namespace Sweepr.Core.Services;

// scans common junk/temp locations and reports file sizes.
[SupportedOSPlatform("windows")]
public static class JunkFilesCleaner
{
    public sealed class JunkItem { public string Path { get; init; } = ""; public long SizeBytes { get; init; } }

    public static List<JunkItem> Scan()
    {
        var results = new List<JunkItem>();
        var roots = new[]
        {
            Path.GetTempPath(),
            Environment.GetFolderPath(Environment.SpecialFolder.InternetCache),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")
        };
        foreach (var root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(root)) continue;
            try
            {
                foreach (var f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    try { results.Add(new JunkItem { Path = f, SizeBytes = new FileInfo(f).Length }); } catch { }
                }
            }
            catch { }
        }
        return results;
    }

    public static (int Deleted, long FreedBytes, List<string> Errors) Delete(IEnumerable<JunkItem> items)
    {
        int deleted = 0; long freed = 0; var errors = new List<string>();
        foreach (var it in items)
        {
            try { if (File.Exists(it.Path)) { File.Delete(it.Path); deleted++; freed += it.SizeBytes; } } catch (Exception ex) { errors.Add($"{it.Path}: {ex.Message}"); }
        }
        return (deleted, freed, errors);
    }
}