using System.Runtime.Versioning;
using Microsoft.Win32;
using Sweepr.Core.Models;

namespace Sweepr.Core.Services;

[SupportedOSPlatform("windows")]
public static class RegistryExporter
{
    public static List<RegKeyEntry> Snapshot(string fullPath)
    {
        if (!RegistryPath.TryParse(fullPath, out var hive, out var segments) || segments.Count == 0)
            throw new ArgumentException($"Not a valid registry key path: {fullPath}", nameof(fullPath));

        var subPath = string.Join('\\', segments);

        using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var key = root.OpenSubKey(subPath)
            ?? throw new KeyNotFoundException($"Registry key not found: {fullPath}");

        var entries = new List<RegKeyEntry>();
        Collect(key, $"{RegistryPath.HiveName(hive)}\\{subPath}", entries);
        return entries;
    }

    public static (int Keys, int Values) ExportToFile(IEnumerable<string> keyPaths, string filePath)
    {
        var entries = new List<RegKeyEntry>();
        foreach (var path in keyPaths) entries.AddRange(Snapshot(path));

        var folder = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

        RegFileWriter.WriteFile(filePath, entries);
        return (entries.Count, entries.Sum(e => e.Values.Count));
    }

    private static void Collect(RegistryKey key, string path, List<RegKeyEntry> into)
    {
        var values = new List<RegValueEntry>();
        foreach (var name in key.GetValueNames())
            values.Add(ReadValue(key, name, path));

        into.Add(new RegKeyEntry(path, values));

        foreach (var childName in key.GetSubKeyNames())
        {
            using var child = key.OpenSubKey(childName);
            if (child is null) continue;
            Collect(child, $"{path}\\{childName}", into);
        }
    }

    private static RegValueEntry ReadValue(RegistryKey key, string name, string path)
    {
        var kind = key.GetValueKind(name);
        var data = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);

        return (kind, data) switch
        {
            (RegistryValueKind.String, string text) => new RegStringValue(name, text),
            (RegistryValueKind.ExpandString, string text) => new RegExpandStringValue(name, text),
            (RegistryValueKind.MultiString, string[] items) => new RegMultiStringValue(name, items),
            (RegistryValueKind.DWord, int number) => new RegDWordValue(name, unchecked((uint)number)),
            (RegistryValueKind.QWord, long number) => new RegQWordValue(name, unchecked((ulong)number)),
            (RegistryValueKind.Binary, byte[] bytes) => new RegBinaryValue(name, bytes),
            (RegistryValueKind.None, byte[] bytes) => new RegNoneValue(name, bytes),
            _ => throw new NotSupportedException($"Cannot back up value \"{name}\" of type {kind} in {path}.")
        };
    }
}
