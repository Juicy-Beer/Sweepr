namespace Sweepr.Core.Models;

// one microsoft store / uwp app installed on the machine.
public sealed class WindowsApp
{
    public string Name { get; init; } = "";
    public string PackageFullName { get; init; } = "";
    public string Publisher { get; init; } = "";
    public string Version { get; init; } = "";
    public string InstallLocation { get; init; } = "";
    public bool IsFramework { get; init; }
    public bool IsBundle { get; init; }
    public string Architecture { get; init; } = "";
}