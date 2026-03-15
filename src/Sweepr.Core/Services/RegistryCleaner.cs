using System.Runtime.Versioning;
using Microsoft.Win32;
using Sweepr.Core.Models;

namespace Sweepr.Core.Services;

public enum RegistryCleanMode { Light, Moderate, Deep, Custom }

[SupportedOSPlatform("windows")]
public static class RegistryCleaner
{
    public static readonly string[] AllCategories = new[] { "Uninstall entries", "Startup entries", "App Paths", "File extensions", "COM / ActiveX", "Shared DLLs", "Fonts", "MUI cache", "Services", "Active Setup" };

    public static IEnumerable<string> CategoriesFor(RegistryCleanMode mode) => mode switch
    {
        RegistryCleanMode.Light => new[] { "Uninstall entries", "Startup entries", "App Paths" },
        RegistryCleanMode.Moderate => new[] { "Uninstall entries", "Startup entries", "App Paths", "File extensions", "COM / ActiveX", "Shared DLLs" },
        _ => AllCategories
    };

    public static List<RegistryCleanEntry> Scan(RegistryCleanMode mode, IEnumerable<string>? categories = null)
    {
        var cats = categories ?? CategoriesFor(mode);
        var catSet = new HashSet<string>(cats, StringComparer.OrdinalIgnoreCase);
        var results = new List<RegistryCleanEntry>();
        if (catSet.Contains("Uninstall entries")) ScanUninstall(results);
        if (catSet.Contains("Startup entries")) ScanStartup(results);
        if (catSet.Contains("App Paths")) ScanAppPaths(results);
        if (catSet.Contains("File extensions")) ScanFileExtensions(results);
        if (catSet.Contains("COM / ActiveX")) ScanCom(results);
        if (catSet.Contains("Shared DLLs")) ScanSharedDlls(results);
        if (catSet.Contains("Fonts")) ScanFonts(results);
        if (catSet.Contains("MUI cache")) ScanMuiCache(results);
        if (catSet.Contains("Services")) ScanServices(results);
        if (catSet.Contains("Active Setup")) ScanActiveSetup(results);
        return results;
    }

    private static void Add(List<RegistryCleanEntry> r, string c, string h, string kp, string vn, string reason) => r.Add(new RegistryCleanEntry { Category = c, Hive = h, KeyPath = kp, ValueName = vn, Reason = reason });

    private static void ScanUninstall(List<RegistryCleanEntry> results)
    {
        foreach (var (hive, hiveName) in new[] { (RegistryHive.LocalMachine, "HKLM"), (RegistryHive.CurrentUser, "HKCU") })
        {
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                    using var uninst = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                    if (uninst is null) continue;
                    foreach (var sub in uninst.GetSubKeyNames())
                    {
                        using var key = uninst.OpenSubKey(sub);
                        if (key is null) continue;
                        var name = key.GetValue("DisplayName") as string;
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        var installLoc = key.GetValue("InstallLocation") as string ?? "";
                        var uninstStr = key.GetValue("UninstallString") as string ?? "";
                        var quiet = key.GetValue("QuietUninstallString") as string ?? "";
                        if (string.IsNullOrWhiteSpace(uninstStr) && string.IsNullOrWhiteSpace(quiet))
                            Add(results, "Uninstall entries", hiveName, $@"{hiveName}\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{sub}", "DisplayName", "no uninstall command");
                        else if (!string.IsNullOrWhiteSpace(installLoc) && !Directory.Exists(installLoc.Trim().Trim('"')))
                            Add(results, "Uninstall entries", hiveName, $@"{hiveName}\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{sub}", "InstallLocation", "install folder missing");
                    }
                }
                catch { }
            }
        }
    }

    private static string ExtractExe(string data)
    {
        data = data.Trim().Trim('"');
        if (data.StartsWith("\""))
        {
            var end = data.IndexOf('"', 1);
            if (end > 0) return data.Substring(1, end - 1);
        }
        var idx = data.IndexOf(' ');
        var candidate = idx > 0 ? data.Substring(0, idx) : data;
        if (!Path.HasExtension(candidate)) candidate += ".exe";
        return candidate;
    }
    private static void ScanStartup(List<RegistryCleanEntry> results)
    {
        var paths = new[] { @"Software\Microsoft\Windows\CurrentVersion\Run", @"Software\Microsoft\Windows\CurrentVersion\RunOnce" };
        foreach (var (hive, hiveName) in new[] { (Registry.CurrentUser, "HKCU"), (Registry.LocalMachine, "HKLM") })
        {
            foreach (var p in paths)
            {
                try
                {
                    using var key = hive.OpenSubKey(p);
                    if (key is null) continue;
                    foreach (var name in key.GetValueNames())
                    {
                        var data = key.GetValue(name) as string;
                        if (string.IsNullOrWhiteSpace(data)) continue;
                        var exe = ExtractExe(data);
                        if (!string.IsNullOrWhiteSpace(exe) && !File.Exists(exe))
                            Add(results, "Startup entries", hiveName, $@"{hiveName}\{p}", name, "target missing");
                    }
                }
                catch { }
            }
        }
    }

    private static void ScanAppPaths(List<RegistryCleanEntry> results)
    {
        foreach (var (hive, hiveName) in new[] { (Registry.LocalMachine, "HKLM"), (Registry.CurrentUser, "HKCU") })
        {
            try
            {
                using var key = hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths");
                if (key is null) continue;
                foreach (var sub in key.GetSubKeyNames())
                {
                    using var k = key.OpenSubKey(sub);
                    if (k is null) continue;
                    var def = k.GetValue("") as string;
                    if (!string.IsNullOrWhiteSpace(def))
                    {
                        var exe = ExtractExe(def);
                        if (!string.IsNullOrWhiteSpace(exe) && !File.Exists(exe))
                            Add(results, "App Paths", hiveName, $@"{hiveName}\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{sub}", "(Default)", "target missing");
                    }
                }
            }
            catch { }
        }
    }

    private static void ScanFileExtensions(List<RegistryCleanEntry> results)
    {
        try
        {
            using var classes = Registry.ClassesRoot;
            foreach (var ext in classes.GetSubKeyNames().Where(n => n.StartsWith(".")))
            {
                using var key = classes.OpenSubKey(ext);
                if (key is null) continue;
                var progId = key.GetValue("") as string;
                if (string.IsNullOrWhiteSpace(progId)) continue;
                if (classes.OpenSubKey(progId) is null)
                    Add(results, "File extensions", "HKCR", $@"HKEY_CLASSES_ROOT\{ext}", "(Default)", "progid missing");
            }
        }
        catch { }
    }

    private static void ScanCom(List<RegistryCleanEntry> results)
    {
        try
        {
            using var clsid = Registry.ClassesRoot.OpenSubKey("CLSID");
            if (clsid is null) return;
            foreach (var guid in clsid.GetSubKeyNames())
            {
                using var key = clsid.OpenSubKey(guid);
                if (key is null) continue;
                using var inproc = key.OpenSubKey("InprocServer32");
                if (inproc is null) continue;
                var dll = inproc.GetValue("") as string;
                if (!string.IsNullOrWhiteSpace(dll) && !dll.StartsWith("%") && !File.Exists(dll))
                    Add(results, "COM / ActiveX", "HKCR", $@"HKEY_CLASSES_ROOT\CLSID\{guid}\InprocServer32", "(Default)", "dll missing");
            }
        }
        catch { }
    }

    private static void ScanSharedDlls(List<RegistryCleanEntry> results)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\SharedDLLs");
            if (key is null) return;
            foreach (var name in key.GetValueNames())
            {
                if (!File.Exists(name))
                    Add(results, "Shared DLLs", "HKLM", @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\SharedDLLs", name, "file missing");
            }
        }
        catch { }
    }

    private static void ScanFonts(List<RegistryCleanEntry> results)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts");
            if (key is null) return;
            foreach (var name in key.GetValueNames())
            {
                var data = key.GetValue(name) as string;
                if (string.IsNullOrWhiteSpace(data)) continue;
                var fontPath = Path.IsPathRooted(data) ? data : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), data);
                if (!File.Exists(fontPath))
                    Add(results, "Fonts", "HKLM", @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts", name, "font file missing");
            }
        }
        catch { }
    }

    private static void ScanMuiCache(List<RegistryCleanEntry> results)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache");
            if (key is null) return;
            foreach (var name in key.GetValueNames())
            {
                if (name.EndsWith(".FriendlyAppName", StringComparison.OrdinalIgnoreCase)) continue;
                if (name.Contains('\\') && !File.Exists(name))
                    Add(results, "MUI cache", "HKCU", @"HKCU\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache", name, "file missing");
            }
        }
        catch { }
    }

    private static void ScanServices(List<RegistryCleanEntry> results)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
            if (key is null) return;
            foreach (var name in key.GetSubKeyNames())
            {
                using var svc = key.OpenSubKey(name);
                if (svc is null) continue;
                var imagePath = svc.GetValue("ImagePath") as string;
                if (string.IsNullOrWhiteSpace(imagePath)) continue;
                var exe = ExtractExe(imagePath);
                if (!string.IsNullOrWhiteSpace(exe) && !File.Exists(exe) && !exe.Contains("system32", StringComparison.OrdinalIgnoreCase))
                    Add(results, "Services", "HKLM", $@"HKLM\SYSTEM\CurrentControlSet\Services\{name}", "ImagePath", "image missing");
            }
        }
        catch { }
    }

    private static void ScanActiveSetup(List<RegistryCleanEntry> results)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Active Setup\Installed Components");
            if (key is null) return;
            foreach (var sub in key.GetSubKeyNames())
            {
                using var k = key.OpenSubKey(sub);
                if (k is null) continue;
                var stub = k.GetValue("StubPath") as string;
                if (string.IsNullOrWhiteSpace(stub)) continue;
                var exe = ExtractExe(stub);
                if (!string.IsNullOrWhiteSpace(exe) && !File.Exists(exe))
                    Add(results, "Active Setup", "HKLM", $@"HKLM\SOFTWARE\Microsoft\Active Setup\Installed Components\{sub}", "StubPath", "target missing");
            }
        }
        catch { }
    }
}
