using System.Runtime.Versioning;

namespace Sweepr.Core.Services;

// scans common junk/temp locations and reports file sizes.
[SupportedOSPlatform("windows")]
public static class JunkFilesCleaner
{
    public sealed class JunkItem { public string Path { get; init; } = ""; public long SizeBytes { get; init; } }

    public static List<JunkItem> Scan(bool ignoreFilesAccessedLast24Hours = false, List<string>? patterns = null)
    {
        var results = new List<JunkItem>();
        var roots = new[]
        {
            Path.GetTempPath(),
            Environment.GetFolderPath(Environment.SpecialFolder.InternetCache),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")
        };
        var cutoff = DateTime.Now.AddHours(-24);
        foreach (var root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(root)) continue;
            try
            {
                foreach (var f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var fi = new FileInfo(f);
                        if (ignoreFilesAccessedLast24Hours && fi.LastAccessTime > cutoff) continue;
                        if (patterns is not null && patterns.Count > 0)
                        {
                            var name = Path.GetFileName(f);
                            bool match = false;
                            foreach (var pat in patterns)
                            {
                                if (pat.Contains('*') || pat.Contains('?'))
                                {
                                    // simple wildcard match
                                    var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pat).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                                    if (System.Text.RegularExpressions.Regex.IsMatch(name, regex, System.Text.RegularExpressions.RegexOptions.IgnoreCase)) { match = true; break; }
                                }
                                else if (name.Equals(pat, StringComparison.OrdinalIgnoreCase)) { match = true; break; }
                            }
                            if (!match) continue;
                        }
                        results.Add(new JunkItem { Path = f, SizeBytes = fi.Length });
                    }
                    catch { }
                }
            }
            catch { }
        }
        return results;
    }

    public static (int Deleted, long FreedBytes, List<string> Errors) Delete(IEnumerable<JunkItem> items, bool toRecycleBin = false)
    {
        int deleted = 0; long freed = 0; var errors = new List<string>();
        foreach (var it in items)
        {
            try
            {
                if (File.Exists(it.Path))
                {
                    if (toRecycleBin && RecycleBin.Delete(it.Path)) { deleted++; freed += it.SizeBytes; }
                    else if (!toRecycleBin) { File.Delete(it.Path); deleted++; freed += it.SizeBytes; }
                    else { errors.Add($"{it.Path}: recycle bin failed"); }
                }
            }
            catch (Exception ex) { errors.Add($"{it.Path}: {ex.Message}"); }
        }
        return (deleted, freed, errors);
    }
}