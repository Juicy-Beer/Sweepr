using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
using Sweepr.Core.Models;

namespace Sweepr.Core.Services;

// lists installed microsoft store (uwp) apps via powershell get-appxpackage.
[SupportedOSPlatform("windows")]
public static class WindowsAppScanner
{
    public static List<WindowsApp> GetInstalledApps(bool allUsers = true)
    {
        var results = new List<WindowsApp>();
        try
        {
            var args = allUsers ? "-NoProfile -Command \"Get-AppxPackage -AllUsers | Select-Object Name, PackageFullName, Publisher, Version, InstallLocation, IsFramework, IsBundle, Architecture | ConvertTo-Json -Compress\"" :
                                  "-NoProfile -Command \"Get-AppxPackage | Select-Object Name, PackageFullName, Publisher, Version, InstallLocation, IsFramework, IsBundle, Architecture | ConvertTo-Json -Compress\"";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi);
            if (process is null) return results;
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(120000);
            if (string.IsNullOrWhiteSpace(output)) return results;

            // powershell may return a single object or an array.
            using var doc = JsonDocument.Parse(output.Trim());
            var items = doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.EnumerateArray() : new[] { doc.RootElement }.AsEnumerable();

            foreach (var el in items)
            {
                var name = GetString(el, "Name");
                if (string.IsNullOrWhiteSpace(name)) continue;
                results.Add(new WindowsApp
                {
                    Name = name,
                    PackageFullName = GetString(el, "PackageFullName"),
                    Publisher = GetString(el, "Publisher"),
                    Version = GetString(el, "Version"),
                    InstallLocation = GetString(el, "InstallLocation"),
                    IsFramework = GetBool(el, "IsFramework"),
                    IsBundle = GetBool(el, "IsBundle"),
                    Architecture = GetString(el, "Architecture")
                });
            }
        }
        catch
        {
            // ignore; return whatever we have.
        }
        return results.OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static string GetString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    private static bool GetBool(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && (v.ValueKind == JsonValueKind.True || (v.ValueKind == JsonValueKind.String && bool.TryParse(v.GetString(), out var b) && b));
}