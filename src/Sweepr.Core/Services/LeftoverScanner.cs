using System.Runtime.Versioning;
using Microsoft.Win32;
using Sweepr.Core.Models;

namespace Sweepr.Core.Services;

public enum ScanMode
{
    // exact name matches only.
    Safe,
    // folder/key names that contain the program name.
    Moderate,
    // also matches on the first word of the name, review carefully.
    Advanced
}

public enum LeftoverKind { Folder, RegistryKey }

public sealed record Leftover(LeftoverKind Kind, string Path);

// looks for folders and registry keys a program left behind, never deletes anything.
[SupportedOSPlatform("windows")]
public static class LeftoverScanner
{
    // shared registry containers that must never be reported as leftovers.
    private static readonly HashSet<string> IgnoredRegistryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft", "Classes", "Policies", "WOW6432Node", "Windows", "Clients",
        "RegisteredApplications", "Wow6432Node", "Intel", "NVIDIA Corporation", "AMD"
    };

    public static List<Leftover> Scan(InstalledProgram program, ScanMode mode)
    {
        var results = new List<Leftover>();

        // never scan around microsoft components.
        if (program.Publisher.Contains("Microsoft", StringComparison.OrdinalIgnoreCase)) return results;

        var cleanName = NameCleaner.Clean(program.Name);
        var key = NameCleaner.Normalize(cleanName);
        if (key.Length < 3) return results;

        var firstWord = NameCleaner.Normalize(cleanName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "");
        var publisher = NameCleaner.Normalize(program.Publisher);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. the recorded install folder, if it still exists.
        if (program.InstallLocation.Length > 0 &&
            Directory.Exists(program.InstallLocation) &&
            !ProtectedPaths.IsProtectedFolder(program.InstallLocation) &&
            seen.Add(program.InstallLocation))
        {
            results.Add(new Leftover(LeftoverKind.Folder, program.InstallLocation));
        }

        // 2. folders in the usual places.
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs"),
            Environment.GetFolderPath(Environment.SpecialFolder.Programs),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
        }
        .Where(r => !string.IsNullOrWhiteSpace(r) && Directory.Exists(r))
        .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots)
        {
            foreach (var dir in SafeDirectories(root))
            {
                var dirName = NameCleaner.Normalize(Path.GetFileName(dir));
                bool isPublisherFolder = publisher.Length > 0 && dirName == publisher && publisher != key;

                if (!isPublisherFolder && Matches(dirName, key, firstWord, mode))
                {
                    if (!ProtectedPaths.IsProtectedFolder(dir) && seen.Add(dir))
                        results.Add(new Leftover(LeftoverKind.Folder, dir));
                }
                else if (isPublisherFolder)
                {
                    // look one level down: <publisher>\<program>
                    foreach (var sub in SafeDirectories(dir))
                    {
                        var subName = NameCleaner.Normalize(Path.GetFileName(sub));
                        if (Matches(subName, key, firstWord, mode) && !ProtectedPaths.IsProtectedFolder(sub) && seen.Add(sub))
                            results.Add(new Leftover(LeftoverKind.Folder, sub));
                    }
                }
            }
        }

        // 3. registry keys under software (review only).
        ScanRegistry(Registry.CurrentUser, "HKEY_CURRENT_USER", key, firstWord, publisher, mode, results, seen);
        ScanRegistry(Registry.LocalMachine, "HKEY_LOCAL_MACHINE", key, firstWord, publisher, mode, results, seen);

        return results;
    }

    private static void ScanRegistry(RegistryKey hive, string hiveName, string key, string firstWord,
        string publisher, ScanMode mode, List<Leftover> results, HashSet<string> seen)
    {
        foreach (var softwarePath in new[] { "Software", @"Software\WOW6432Node" })
        {
            try
            {
                using var software = hive.OpenSubKey(softwarePath);
                if (software is null) continue;

                foreach (var name in software.GetSubKeyNames())
                {
                    if (IgnoredRegistryNames.Contains(name)) continue;

                    var norm = NameCleaner.Normalize(name);
                    bool isPublisherKey = publisher.Length > 0 && norm == publisher && publisher != key;

                    if (!isPublisherKey && Matches(norm, key, firstWord, mode))
                    {
                        var path = $@"{hiveName}\{softwarePath}\{name}";
                        if (seen.Add(path)) results.Add(new Leftover(LeftoverKind.RegistryKey, path));
                    }
                    else if (isPublisherKey)
                    {
                        using var pubKey = software.OpenSubKey(name);
                        if (pubKey is null) continue;
                        foreach (var sub in pubKey.GetSubKeyNames())
                        {
                            if (!Matches(NameCleaner.Normalize(sub), key, firstWord, mode)) continue;
                            var path = $@"{hiveName}\{softwarePath}\{name}\{sub}";
                            if (seen.Add(path)) results.Add(new Leftover(LeftoverKind.RegistryKey, path));
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                // skip keys we cannot read.
            }
        }
    }

    private static bool Matches(string candidate, string key, string firstWord, ScanMode mode)
    {
        if (candidate.Length == 0) return false;
        if (candidate == key) return true;
        if (mode == ScanMode.Safe) return false;
        if (key.Length >= 4 && candidate.Contains(key, StringComparison.Ordinal)) return true;
        if (mode == ScanMode.Advanced && firstWord.Length >= 5 && candidate.Contains(firstWord, StringComparison.Ordinal)) return true;
        return false;
    }

    private static IEnumerable<string> SafeDirectories(string path)
    {
        try { return Directory.EnumerateDirectories(path).ToList(); }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) { return Array.Empty<string>(); }
    }
}
