using Microsoft.Win32;

namespace Sweepr.Core.Services;

public static class RegistryPath
{
    private static readonly Dictionary<string, RegistryHive> Hives = new(StringComparer.OrdinalIgnoreCase)
    {
        ["HKEY_CURRENT_USER"] = RegistryHive.CurrentUser,
        ["HKCU"] = RegistryHive.CurrentUser,
        ["HKEY_LOCAL_MACHINE"] = RegistryHive.LocalMachine,
        ["HKLM"] = RegistryHive.LocalMachine,
        ["HKEY_CLASSES_ROOT"] = RegistryHive.ClassesRoot,
        ["HKCR"] = RegistryHive.ClassesRoot,
        ["HKEY_USERS"] = RegistryHive.Users,
        ["HKU"] = RegistryHive.Users,
        ["HKEY_CURRENT_CONFIG"] = RegistryHive.CurrentConfig,
        ["HKCC"] = RegistryHive.CurrentConfig
    };

    public static bool TryParse(string? path, out RegistryHive hive, out List<string> segments)
    {
        hive = default;
        segments = new List<string>();

        if (string.IsNullOrWhiteSpace(path)) return false;

        var parts = path.Trim().Trim('"').TrimEnd('\\').Split('\\');
        if (parts.Any(p => p.Length == 0)) return false;
        if (!Hives.TryGetValue(parts[0], out hive)) return false;

        segments = parts.Skip(1).ToList();
        return true;
    }

    public static string HiveName(RegistryHive hive) => hive switch
    {
        RegistryHive.CurrentUser => "HKEY_CURRENT_USER",
        RegistryHive.LocalMachine => "HKEY_LOCAL_MACHINE",
        RegistryHive.ClassesRoot => "HKEY_CLASSES_ROOT",
        RegistryHive.Users => "HKEY_USERS",
        RegistryHive.CurrentConfig => "HKEY_CURRENT_CONFIG",
        _ => throw new ArgumentOutOfRangeException(nameof(hive))
    };
}
