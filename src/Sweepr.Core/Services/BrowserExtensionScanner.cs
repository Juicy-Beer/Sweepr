using System.Runtime.Versioning;
using System.Text.Json;
using Sweepr.Core.Models;

namespace Sweepr.Core.Services;

// scans browser extension folders for chrome, edge and firefox.
[SupportedOSPlatform("windows")]
public static class BrowserExtensionScanner
{
    public static List<BrowserExtension> ScanAll()
    {
        var results = new List<BrowserExtension>();
        results.AddRange(ScanChromium("Chrome", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data")));
        results.AddRange(ScanChromium("Edge", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "User Data")));
        results.AddRange(ScanFirefox());
        return results.OrderBy(e => e.Browser).ThenBy(e => e.Name).ToList();
    }

    private static IEnumerable<BrowserExtension> ScanChromium(string browser, string userDataRoot)
    {
        var results = new List<BrowserExtension>();
        if (!Directory.Exists(userDataRoot)) return results;
        foreach (var profileDir in Directory.EnumerateDirectories(userDataRoot))
        {
            var extRoot = Path.Combine(profileDir, "Extensions");
            if (!Directory.Exists(extRoot)) continue;
            foreach (var idDir in Directory.EnumerateDirectories(extRoot))
            {
                foreach (var verDir in Directory.EnumerateDirectories(idDir))
                {
                    var manifest = Path.Combine(verDir, "manifest.json");
                    if (!File.Exists(manifest)) continue;
                    try
                    {
                        using var doc = JsonDocument.Parse(File.ReadAllText(manifest));
                        var root = doc.RootElement;
                        var name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                        var version = root.TryGetProperty("version", out var v) ? v.GetString() ?? "" : "";
                        if (name.StartsWith("__MSG_")) name = idDir; // localized name placeholder
                        results.Add(new BrowserExtension
                        {
                            Browser = browser,
                            Id = Path.GetFileName(idDir),
                            Name = string.IsNullOrWhiteSpace(name) ? Path.GetFileName(idDir) : name,
                            Version = version,
                            Path = verDir
                        });
                    }
                    catch { }
                }
            }
        }
        return results;
    }

    private static IEnumerable<BrowserExtension> ScanFirefox()
    {
        var results = new List<BrowserExtension>();
        var profilesRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla", "Firefox", "Profiles");
        if (!Directory.Exists(profilesRoot)) return results;
        foreach (var profile in Directory.EnumerateDirectories(profilesRoot))
        {
            var extJson = Path.Combine(profile, "extensions.json");
            if (!File.Exists(extJson)) continue;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(extJson));
                if (!doc.RootElement.TryGetProperty("addons", out var addons) || addons.ValueKind != JsonValueKind.Array) continue;
                foreach (var addon in addons.EnumerateArray())
                {
                    var id = addon.TryGetProperty("id", out var i) ? i.GetString() ?? "" : "";
                    var name = addon.TryGetProperty("defaultLocale", out var dl) && dl.TryGetProperty("name", out var dn) ? dn.GetString() ?? "" : (addon.TryGetProperty("name", out var n) ? n.GetString() ?? "" : id);
                    var version = addon.TryGetProperty("version", out var v) ? v.GetString() ?? "" : "";
                    var path = addon.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    results.Add(new BrowserExtension
                    {
                        Browser = "Firefox",
                        Id = id,
                        Name = string.IsNullOrWhiteSpace(name) ? id : name,
                        Version = version,
                        Path = path
                    });
                }
            }
            catch { }
        }
        return results;
    }

    // best-effort removal: delete the extension folder. browsers may lock files.
    public static (bool Ok, string Error) Remove(BrowserExtension ext)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(ext.Path) && Directory.Exists(ext.Path))
                Directory.Delete(ext.Path, recursive: true);
            return (true, "");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}