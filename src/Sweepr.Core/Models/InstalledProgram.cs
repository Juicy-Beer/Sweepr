namespace Sweepr.Core.Models;

// one entry from the windows "uninstall" registry keys.
public sealed class InstalledProgram
{
    public string Name { get; init; } = "";
    public string Version { get; init; } = "";
    public string Publisher { get; init; } = "";
    public string InstallLocation { get; init; } = "";
    public string UninstallString { get; init; } = "";
    public string InstallDate { get; init; } = "";
    public string Website { get; init; } = "";
    public string Comments { get; init; } = "";
    public string Architecture { get; init; } = "";
    public long SizeBytes { get; init; }
    public string RegistryKeyPath { get; init; } = "";
    public string DisplayIcon { get; init; } = "";
}
