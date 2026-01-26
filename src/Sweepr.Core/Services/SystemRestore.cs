using System.Diagnostics;
using System.Runtime.Versioning;

namespace Sweepr.Core.Services;

// creates a windows system restore point before removals.
[SupportedOSPlatform("windows")]
public static class SystemRestore
{
    public static bool CreateRestorePoint(string description)
    {
        if (string.IsNullOrWhiteSpace(description)) description = "Sweepr restore point";
        // trim to a safe length for the description.
        if (description.Length > 200) description = description.Substring(0, 200);

        try
        {
            // checkpoint-computer needs admin, which sweepr already has.
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -Command \"Checkpoint-Computer -Description '{description.Replace("'", "''")}' -RestorePointType 'MODIFY_SETTINGS'\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi);
            if (process is null) return false;
            process.WaitForExit(60000);
            if (!process.HasExited) { try { process.Kill(); } catch { } return false; }
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}