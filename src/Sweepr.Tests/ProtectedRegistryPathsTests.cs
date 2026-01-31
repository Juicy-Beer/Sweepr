using Sweepr.Core.Services;
using Xunit;

namespace Sweepr.Tests;

public class ProtectedRegistryPathsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("HKEY_LOCAL_MACHINE")]
    [InlineData(@"HKEY_LOCAL_MACHINE\SOFTWARE")]
    [InlineData(@"HKEY_CURRENT_USER\Software")]
    [InlineData(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Foo")]
    [InlineData(@"HKEY_LOCAL_MACHINE\SAM\SAM")]
    [InlineData(@"HKEY_CLASSES_ROOT\.txt")]
    [InlineData(@"HKEY_USERS\.DEFAULT\Software\Foo")]
    [InlineData(@"HKEY_CURRENT_USER\Environment")]
    [InlineData(@"HKEY_CURRENT_USER\Software\Microsoft")]
    [InlineData(@"HKEY_CURRENT_USER\Software\Microsoft\" )]
    [InlineData(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Foo")]
    [InlineData(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node")]
    [InlineData(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows")]
    [InlineData(@"HKEY_CURRENT_USER\Software\Classes\.foo")]
    [InlineData(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Foo")]
    [InlineData(@"HKEY_LOCAL_MACHINE\SOFTWARE\Clients\Mail")]
    [InlineData(@"HKEY_LOCAL_MACHINE\SOFTWARE\RegisteredApplications")]
    [InlineData(@"HKEY_CURRENT_USER\Software\NVIDIA Corporation\Global")]
    [InlineData(@"HKEY_LOCAL_MACHINE\SOFTWARE\Intel\Foo")]
    [InlineData(@"HKEY_CURRENT_USER\Software\AMD\Foo")]
    [InlineData(@"hkey_local_machine\software\MICROSOFT\Foo")]
    [InlineData(@"HKLM\SOFTWARE\Policies\Foo")]
    [InlineData(@"HKEY_CURRENT_USER\\Software\Foo")]
    [InlineData(@"HKEY_BOGUS\Software\Foo")]
    public void CriticalOrInvalidKeys_AreProtected(string? path) =>
        Assert.True(ProtectedRegistryPaths.IsProtectedKey(path));

    [Theory]
    [InlineData(@"HKEY_CURRENT_USER\Software\Audacity")]
    [InlineData(@"HKEY_LOCAL_MACHINE\SOFTWARE\Audacity")]
    [InlineData(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Audacity")]
    [InlineData(@"HKEY_LOCAL_MACHINE\SOFTWARE\Adobe\Acrobat Reader")]
    [InlineData(@"HKEY_CURRENT_USER\Software\7-Zip\FM")]
    [InlineData(@"hkcu\software\SomeApp")]
    [InlineData(@"HKLM\SOFTWARE\SomeApp")]
    public void ApplicationKeys_AreNotProtected(string path) =>
        Assert.False(ProtectedRegistryPaths.IsProtectedKey(path));
}
