using Microsoft.Win32;
using Sweepr.Core.Services;
using Xunit;

namespace Sweepr.Tests;

public class RegistryPathTests
{
    [Theory]
    [InlineData(@"HKEY_CURRENT_USER\Software\Foo", RegistryHive.CurrentUser, @"Software\Foo")]
    [InlineData(@"hkcu\Software\Foo", RegistryHive.CurrentUser, @"Software\Foo")]
    [InlineData(@"HKLM\SOFTWARE\WOW6432Node\Foo\", RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Foo")]
    [InlineData(@"""HKEY_CLASSES_ROOT\.txt""", RegistryHive.ClassesRoot, ".txt")]
    [InlineData("HKEY_USERS", RegistryHive.Users, "")]
    public void TryParse_SplitsHiveAndSegments(string path, RegistryHive expectedHive, string expectedTail)
    {
        Assert.True(RegistryPath.TryParse(path, out var hive, out var segments));
        Assert.Equal(expectedHive, hive);
        Assert.Equal(expectedTail, string.Join('\\', segments));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(@"Software\Foo")]
    [InlineData(@"HKEY_BOGUS\Foo")]
    [InlineData(@"HKEY_CURRENT_USER\\Software")]
    [InlineData(@"\HKEY_CURRENT_USER")]
    public void TryParse_RejectsInvalidPaths(string? path) =>
        Assert.False(RegistryPath.TryParse(path, out _, out _));

    [Theory]
    [InlineData(RegistryHive.CurrentUser, "HKEY_CURRENT_USER")]
    [InlineData(RegistryHive.LocalMachine, "HKEY_LOCAL_MACHINE")]
    [InlineData(RegistryHive.ClassesRoot, "HKEY_CLASSES_ROOT")]
    public void HiveName_ReturnsCanonicalName(RegistryHive hive, string expected) =>
        Assert.Equal(expected, RegistryPath.HiveName(hive));
}
