namespace Sweepr.Core.Models;

// one browser extension found on disk.
public sealed class BrowserExtension
{
    public string Browser { get; init; } = "";
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Version { get; init; } = "";
    public string Path { get; init; } = "";
}