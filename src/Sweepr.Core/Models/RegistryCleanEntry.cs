namespace Sweepr.Core.Models;

// one invalid registry entry found by the cleaner.
public sealed class RegistryCleanEntry
{
    public string Category { get; init; } = "";
    public string Hive { get; init; } = "";
    public string KeyPath { get; init; } = "";
    public string ValueName { get; init; } = "";
    public string Reason { get; init; } = "";
}