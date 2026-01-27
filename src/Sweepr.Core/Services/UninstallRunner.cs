using System.Diagnostics;
using Sweepr.Core.Models;

namespace Sweepr.Core.Services;

// runs a program's own uninstall command
public static class UninstallRunner
{
    // starts the registered uninstall command and waits for it to exit
    // note: some uninstallers copy themselves to a temp folder and exit immediately,
    // so the leftover scan can start before the real uninstall finishes.
    public static async Task<int> RunAsync(InstalledProgram program)
    {
        if (string.IsNullOrWhiteSpace(program.UninstallString))
            throw new InvalidOperationException("This program has no uninstall command registered. Try Force Remove.");

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{program.UninstallString}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start the uninstaller.");
        await process.WaitForExitAsync();
        return process.ExitCode;
    }
}
