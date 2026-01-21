namespace Sweepr.Core.Services;

// folders sweepr must never delete, no matter what the scan finds.
public static class ProtectedPaths
{
    public static bool IsProtectedFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return true;

        string full;
        try { full = Normalize(Path.GetFullPath(path.Trim().Trim('"'))); }
        catch { return true; }

        // drive roots such as "c:".
        var root = Path.GetPathRoot(full);
        if (root is null || full.Length <= root.TrimEnd(Path.DirectorySeparatorChar).Length) return true;

        // exact top-level folders.
        foreach (var folder in ExactProtected())
        {
            if (string.Equals(full, folder, StringComparison.OrdinalIgnoreCase)) return true;
        }

        // everything inside the windows folder.
        var windows = Normalize(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
        if (windows.Length > 0 && full.StartsWith(windows + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static IEnumerable<string> ExactProtected()
    {
        var folders = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.Programs),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"),
        };

        return folders.Where(f => !string.IsNullOrWhiteSpace(f)).Select(Normalize);
    }

    private static string Normalize(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
