using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Sweepr.Core.Services;

// a single registry tweak with apply/revert.
public sealed class RegistryTweak
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Category { get; init; } = "";
    public string KeyPath { get; init; } = "";
    public string ValueName { get; init; } = "";
    public object? ApplyValue { get; init; }
    public object? RevertValue { get; init; }
    public RegistryValueKind Kind { get; init; } = RegistryValueKind.DWord;
}

// applies and reverts registry tweaks.
[SupportedOSPlatform("windows")]
public static class RegistryTweaks
{
    public static IReadOnlyList<RegistryTweak> All { get; } = new List<RegistryTweak>
    {
        new() { Name = "Show file extensions", Description = "Show hidden file extensions in Explorer.", Category = "Explorer", KeyPath = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", ValueName = "HideFileExt", ApplyValue = 0, RevertValue = 1, Kind = RegistryValueKind.DWord },
        new() { Name = "Show hidden files", Description = "Show hidden files and folders in Explorer.", Category = "Explorer", KeyPath = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", ValueName = "Hidden", ApplyValue = 1, RevertValue = 2, Kind = RegistryValueKind.DWord },
        new() { Name = "Disable lock screen", Description = "Disable the lock screen (Windows Pro/Enterprise).", Category = "System", KeyPath = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Personalization", ValueName = "NoLockScreen", ApplyValue = 1, RevertValue = 0, Kind = RegistryValueKind.DWord },
        new() { Name = "Disable tips and suggestions", Description = "Disable Windows tips and suggestions.", Category = "System", KeyPath = @"HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", ValueName = "SubscribedContent-338388Enabled", ApplyValue = 0, RevertValue = 1, Kind = RegistryValueKind.DWord },
        new() { Name = "Disable Cortana", Description = "Disable Cortana.", Category = "System", KeyPath = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search", ValueName = "AllowCortana", ApplyValue = 0, RevertValue = 1, Kind = RegistryValueKind.DWord },
        new() { Name = "Faster menu show delay", Description = "Reduce menu show delay to 0.", Category = "Performance", KeyPath = @"HKCU\Control Panel\Desktop", ValueName = "MenuShowDelay", ApplyValue = "0", RevertValue = "400", Kind = RegistryValueKind.String },
        new() { Name = "Disable startup delay", Description = "Disable startup delay for apps.", Category = "Performance", KeyPath = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize", ValueName = "StartupDelayInMSec", ApplyValue = 0, RevertValue = 1, Kind = RegistryValueKind.DWord },
        new() { Name = "Classic context menu", Description = "Use the classic right-click context menu (Windows 11).", Category = "Explorer", KeyPath = @"HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32", ValueName = "", ApplyValue = "", RevertValue = null, Kind = RegistryValueKind.String },
        new() { Name = "Disable Bing search", Description = "Disable Bing web search in the Start menu.", Category = "Search", KeyPath = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Search", ValueName = "BingSearchEnabled", ApplyValue = 0, RevertValue = 1, Kind = RegistryValueKind.DWord },
        new() { Name = "Disable web search", Description = "Disable web search in Start menu.", Category = "Search", KeyPath = @"HKCU\Software\Policies\Microsoft\Windows\Explorer", ValueName = "DisableSearchBoxSuggestions", ApplyValue = 1, RevertValue = 0, Kind = RegistryValueKind.DWord },
    };

    public static bool Apply(RegistryTweak tweak)
    {
        try
        {
            if (!RegistryPath.TryParse(tweak.KeyPath, out var hive, out var segments)) return false;
            using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = root.CreateSubKey(string.Join('\\', segments));
            if (key is null) return false;
            if (tweak.ApplyValue is null)
            {
                // removing the value reverts (classic context menu case: create empty key)
                return true;
            }
            key.SetValue(tweak.ValueName, tweak.ApplyValue, tweak.Kind);
            return true;
        }
        catch { return false; }
    }

    public static bool Revert(RegistryTweak tweak)
    {
        try
        {
            if (!RegistryPath.TryParse(tweak.KeyPath, out var hive, out var segments)) return false;
            using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = root.OpenSubKey(string.Join('\\', segments), writable: true);
            if (key is null) return false;
            if (tweak.RevertValue is null)
            {
                try { key.DeleteValue(tweak.ValueName, throwOnMissingValue: false); } catch { }
                return true;
            }
            key.SetValue(tweak.ValueName, tweak.RevertValue, tweak.Kind);
            return true;
        }
        catch { return false; }
    }
}