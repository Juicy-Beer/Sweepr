using Sweepr.Core.Services;
using Xunit;

namespace Sweepr.Tests;

public class ProtectedPathsTests
{
    private static string Folder(Environment.SpecialFolder f) => Environment.GetFolderPath(f);

    [Fact]
    public void WindowsFolder_IsProtected() =>
        Assert.True(ProtectedPaths.IsProtectedFolder(Folder(Environment.SpecialFolder.Windows)));

    [Fact]
    public void SystemFolder_IsProtected() =>
        Assert.True(ProtectedPaths.IsProtectedFolder(Folder(Environment.SpecialFolder.System)));

    [Fact]
    public void ProgramFilesRoot_IsProtected() =>
        Assert.True(ProtectedPaths.IsProtectedFolder(Folder(Environment.SpecialFolder.ProgramFiles)));

    [Fact]
    public void UserProfile_IsProtected() =>
        Assert.True(ProtectedPaths.IsProtectedFolder(Folder(Environment.SpecialFolder.UserProfile)));

    [Fact]
    public void DriveRoot_IsProtected() =>
        Assert.True(ProtectedPaths.IsProtectedFolder(@"C:\"));

    [Fact]
    public void EmptyPath_IsProtected() =>
        Assert.True(ProtectedPaths.IsProtectedFolder(""));

    [Fact]
    public void AppFolderInsideProgramFiles_IsNotProtected() =>
        Assert.False(ProtectedPaths.IsProtectedFolder(Path.Combine(Folder(Environment.SpecialFolder.ProgramFiles), "SomeApp")));
}
