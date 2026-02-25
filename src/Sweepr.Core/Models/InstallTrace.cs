namespace Sweepr.Core.Models;

// a recorded install: files and registry keys captured before/after an install.
public sealed class InstallTrace
{
    public string Name { get; init; } = "";
    public DateTime StartedAt { get; init; }
    public DateTime CompletedAt { get; init; }
    public List<string> Files { get; init; } = new();
    public List<string> RegistryKeys { get; init; } = new();
}