using System.Diagnostics;
using System.Runtime.Versioning;

namespace Sweepr.Core.Services;

// removes microsoft store (uwp) apps via powershell remove-appxpackage.
[SupportedOSPlatform("windows")]
public static class WindowsAppUninstaller
{
    public static bool Remove(string packageFullName, bool allUsers = true)
    {
        if (string.IsNullOrWhiteSpace(packageFullName)) return false;
        try
        {
            var cmd = allUsers
                ? $"Remove-AppxPackage -Package '{packageFullName.Replace("'", "''")}' -AllUsers"
                : $"Remove-AppxPackage -Package '{packageFullName.Replace("'", "''")}'";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"{cmd}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi);
            if (process is null) return false;
            process.WaitForExit(120000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}