using System.Runtime.Versioning;
using Microsoft.Win32;
using Sweepr.Core.Models;

namespace Sweepr.Core.Services;

// deletes folders the user approved, registry deletion comes in phase 2 (with backups)
public static class LeftoverCleaner
{
    // deletes files captured by an install trace.
    public static (int Deleted, List<string> Errors) DeleteFiles(IEnumerable<string> paths)
    {
        int deleted = 0;
        var errors = new List<string>();
        foreach (var p in paths)
        {
            try
            {
                if (File.Exists(p)) { File.Delete(p); deleted++; }
                else if (Directory.Exists(p)) { Directory.Delete(p, recursive: true); deleted++; }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                errors.Add($"{p}: {ex.Message}");
            }
        }
        return (deleted, errors);
    }

    public static (int Deleted, List<string> Errors) DeleteFolders(IEnumerable<Leftover> items)
    {
        int deleted = 0;
        var errors = new List<string>();

        foreach (var item in items.Where(i => i.Kind == LeftoverKind.Folder))
        {
            if (ProtectedPaths.IsProtectedFolder(item.Path))
            {
                errors.Add($"skipped protected folder: {item.Path}");
                continue;
            }

            try
            {
                if (Directory.Exists(item.Path))
                {
                    Directory.Delete(item.Path, recursive: true);
                    deleted++;
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                errors.Add($"{item.Path}: {ex.Message}");
            }
        }

        return (deleted, errors);
    }

    // deletes registry leftovers after writing a .reg backup. returns (deleted, errors, backup files).
    [SupportedOSPlatform("windows")]
    public static (int Deleted, List<string> Errors, List<string> Backups) DeleteRegistryKeys(IEnumerable<Leftover> items, string? backupFolder)
    {
        int deleted = 0;
        var errors = new List<string>();
        var backups = new List<string>();

        if (string.IsNullOrWhiteSpace(backupFolder)) backupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sweepr", "Backups");
        Directory.CreateDirectory(backupFolder);

        foreach (var item in items.Where(i => i.Kind == LeftoverKind.RegistryKey))
        {
            var path = item.Path;
            if (ProtectedRegistryPaths.IsProtectedKey(path))
            {
                errors.Add($"skipped protected key: {path}");
                continue;
            }

            if (!RegistryPath.TryParse(path, out var hive, out var segments) || segments.Count == 0)
            {
                errors.Add($"invalid key path: {path}");
                continue;
            }

            // back up first.
            try
            {
                var entries = RegistryExporter.Snapshot(path);
                var backupFile = Path.Combine(backupFolder, $"sweepr-backup-{DateTime.Now:yyyyMMdd-HHmmss}-{backups.Count + 1}.reg");
                RegFileWriter.WriteFile(backupFile, entries);
                backups.Add(backupFile);
            }
            catch (KeyNotFoundException)
            {
                // key already gone; nothing to back up or delete.
                deleted++;
                continue;
            }
            catch (Exception ex)
            {
                errors.Add($"backup failed for {path}: {ex.Message}");
                continue;
            }

            // delete the key.
            try
            {
                var parentSegments = segments.Take(segments.Count - 1).ToList();
                var keyName = segments[^1];
                using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var parent = parentSegments.Count == 0 ? root : root.OpenSubKey(string.Join('\\', parentSegments), writable: true);
                if (parent is null)
                {
                    errors.Add($"could not open parent for {path}");
                    continue;
                }
                parent.DeleteSubKeyTree(keyName, throwOnMissingSubKey: false);
                deleted++;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or ArgumentException)
            {
                errors.Add($"{path}: {ex.Message}");
            }
        }

        return (deleted, errors, backups);
    }
}
