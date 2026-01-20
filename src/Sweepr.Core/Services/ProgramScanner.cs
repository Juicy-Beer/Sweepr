using System.Globalization;
using System.Runtime.Versioning;
using Microsoft.Win32;
using Sweepr.Core.Models;

namespace Sweepr.Core.Services;

// reads installed programs from the registry (hklm + hkcu, 32-bit + 64-bit)
[SupportedOSPlatform("windows")]
public static class ProgramScanner
{
    private const string UninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    private static readonly (RegistryHive Hive, RegistryView View, string Arch, string HiveName)[] Sources =
    {
        (RegistryHive.LocalMachine, RegistryView.Registry64, "64-bit", "HKEY_LOCAL_MACHINE"),
        (RegistryHive.LocalMachine, RegistryView.Registry32, "32-bit", "HKEY_LOCAL_MACHINE"),
        (RegistryHive.CurrentUser,  RegistryView.Registry64, "64-bit", "HKEY_CURRENT_USER"),
        (RegistryHive.CurrentUser,  RegistryView.Registry32, "32-bit", "HKEY_CURRENT_USER"),
    };

    public static List<InstalledProgram> GetInstalledPrograms()
    {
        var found = new List<InstalledProgram>();

        foreach (var (hive, view, arch, hiveName) in Sources)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var uninstall = baseKey.OpenSubKey(UninstallPath);
                if (uninstall is null) continue;

                foreach (var subName in uninstall.GetSubKeyNames())
                {
                    using var key = uninstall.OpenSubKey(subName);
                    if (key is null) continue;

                    var program = Read(key, $@"{hiveName}\{UninstallPath}\{subName}", arch);
                    if (program is not null) found.Add(program);
                }
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                // skip hives we cannot read.
            }
        }

        return found
            .GroupBy(p => (p.Name.ToLowerInvariant(), p.Version, p.Publisher.ToLowerInvariant()))
            .Select(g => g.First())
            .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static InstalledProgram? Read(RegistryKey key, string keyPath, string arch)
    {
        var name = Str(key, "DisplayName").Trim();
        if (name.Length == 0) return null;

        // hide system components, updates and hotfixes.
        if (key.GetValue("SystemComponent") is int sc && sc == 1) return null;
        if (Str(key, "ParentKeyName").Length > 0) return null;

        var releaseType = Str(key, "ReleaseType");
        if (releaseType is "Update" or "Hotfix" or "Security Update" or "Service Pack") return null;
        if (name.StartsWith("KB", StringComparison.OrdinalIgnoreCase) && name.Length <= 12 && name.Skip(2).All(char.IsDigit)) return null;

        // programs without any uninstall command cannot be removed from here.
        var uninstall = Str(key, "UninstallString");
        if (uninstall.Length == 0) uninstall = Str(key, "QuietUninstallString");

        long sizeKb = key.GetValue("EstimatedSize") switch
        {
            int i => (uint)i,
            long l => l,
            string s when long.TryParse(s, out var v) => v,
            _ => 0
        };

        return new InstalledProgram
        {
            Name = name,
            Version = Str(key, "DisplayVersion"),
            Publisher = Str(key, "Publisher"),
            InstallLocation = Str(key, "InstallLocation").Trim().Trim('"'),
            UninstallString = uninstall,
            InstallDate = FormatDate(Str(key, "InstallDate")),
            Website = Str(key, "URLInfoAbout").Length > 0 ? Str(key, "URLInfoAbout") : Str(key, "HelpLink"),
            Comments = Str(key, "Comments"),
            Architecture = arch,
            SizeBytes = sizeKb * 1024,
            RegistryKeyPath = keyPath,
            DisplayIcon = Str(key, "DisplayIcon")
        };
    }

    private static string Str(RegistryKey key, string name) => key.GetValue(name) as string ?? "";

    private static string FormatDate(string raw) =>
        DateTime.TryParseExact(raw, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : "";
}
