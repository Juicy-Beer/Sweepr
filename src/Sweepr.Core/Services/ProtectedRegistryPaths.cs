using Microsoft.Win32;

namespace Sweepr.Core.Services;

public static class ProtectedRegistryPaths
{
    private static readonly HashSet<string> BlockedSoftwareChildren = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft", "Classes", "Policies", "Windows", "Clients", "RegisteredApplications",
        "Intel", "NVIDIA Corporation", "AMD", "WOW6432Node"
    };

    public static bool IsProtectedKey(string? fullPath)
    {
        if (!RegistryPath.TryParse(fullPath, out var hive, out var segments)) return true;
        if (hive is not (RegistryHive.CurrentUser or RegistryHive.LocalMachine)) return true;
        if (segments.Count < 2) return true;
        if (!segments[0].Equals("Software", StringComparison.OrdinalIgnoreCase)) return true;

        var nameIndex = 1;
        if (segments[1].Equals("WOW6432Node", StringComparison.OrdinalIgnoreCase))
        {
            if (segments.Count < 3) return true;
            nameIndex = 2;
        }

        return BlockedSoftwareChildren.Contains(segments[nameIndex]);
    }
}
