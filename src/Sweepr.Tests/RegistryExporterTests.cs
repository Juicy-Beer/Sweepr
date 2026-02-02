using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32;
using Sweepr.Core.Models;
using Sweepr.Core.Services;
using Xunit;

namespace Sweepr.Tests;

[SupportedOSPlatform("windows")]
public class RegistryExporterTests : IDisposable
{
    private readonly string _relative = $@"Software\SweeprTests-{Guid.NewGuid():N}";

    private string FullPath => $@"HKEY_CURRENT_USER\{_relative}";

    public RegistryExporterTests()
    {
        using var key = Registry.CurrentUser.CreateSubKey(_relative);
        key.SetValue("", "default text");
        key.SetValue("Text", @"C:\Temp\Foo", RegistryValueKind.String);
        key.SetValue("Expand", @"%SystemRoot%\Temp", RegistryValueKind.ExpandString);
        key.SetValue("Multi", new[] { "one", "two" }, RegistryValueKind.MultiString);
        key.SetValue("Number", -1, RegistryValueKind.DWord);
        key.SetValue("Big", 1234567890123L, RegistryValueKind.QWord);
        key.SetValue("Bytes", new byte[] { 1, 2, 255 }, RegistryValueKind.Binary);

        using var child = key.CreateSubKey("Child");
        child.SetValue("Inner", "x");
    }

    public void Dispose() =>
        Registry.CurrentUser.DeleteSubKeyTree(_relative, throwOnMissingSubKey: false);

    [Fact]
    public void Snapshot_ReturnsKeyAndSubkeys()
    {
        var entries = RegistryExporter.Snapshot(FullPath);

        Assert.Equal(new[] { FullPath, FullPath + @"\Child" }, entries.Select(e => e.Path));
    }

    [Fact]
    public void Snapshot_ReadsEveryValueType()
    {
        var root = RegistryExporter.Snapshot(FullPath)[0];
        RegValueEntry Get(string name) => root.Values.Single(v => v.Name == name);

        Assert.Equal("default text", Assert.IsType<RegStringValue>(Get("")).Data);
        Assert.Equal(@"C:\Temp\Foo", Assert.IsType<RegStringValue>(Get("Text")).Data);
        Assert.Equal(@"%SystemRoot%\Temp", Assert.IsType<RegExpandStringValue>(Get("Expand")).Data);
        Assert.Equal(new[] { "one", "two" }, Assert.IsType<RegMultiStringValue>(Get("Multi")).Data);
        Assert.Equal(0xFFFFFFFFu, Assert.IsType<RegDWordValue>(Get("Number")).Data);
        Assert.Equal(1234567890123UL, Assert.IsType<RegQWordValue>(Get("Big")).Data);
        Assert.Equal(new byte[] { 1, 2, 255 }, Assert.IsType<RegBinaryValue>(Get("Bytes")).Data);
    }

    [Fact]
    public void Snapshot_MissingKey_Throws() =>
        Assert.Throws<KeyNotFoundException>(() => RegistryExporter.Snapshot(FullPath + @"\DoesNotExist"));

    [Fact]
    public void Snapshot_WholeHive_Throws() =>
        Assert.Throws<ArgumentException>(() => RegistryExporter.Snapshot("HKEY_CURRENT_USER"));

    [Fact]
    public void ExportToFile_WritesRegFileAndReportsCounts()
    {
        var file = Path.Combine(Path.GetTempPath(), $"sweepr-{Guid.NewGuid():N}.reg");
        try
        {
            var (keys, values) = RegistryExporter.ExportToFile(new[] { FullPath }, file);

            Assert.Equal(2, keys);
            Assert.Equal(8, values);

            var text = File.ReadAllText(file, Encoding.Unicode);
            Assert.StartsWith(RegFileWriter.Header, text);
            Assert.Contains($"[{FullPath}]", text);
            Assert.Contains("@=\"default text\"", text);
            Assert.Contains("\"Number\"=dword:ffffffff", text);
            Assert.Contains("\"Expand\"=hex(2):", text);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void ExportedFile_ImportsBackWithRegExe()
    {
        var file = Path.Combine(Path.GetTempPath(), $"sweepr-{Guid.NewGuid():N}.reg");
        try
        {
            RegistryExporter.ExportToFile(new[] { FullPath }, file);
            Registry.CurrentUser.DeleteSubKeyTree(_relative);
            Assert.Null(Registry.CurrentUser.OpenSubKey(_relative));

            var psi = new ProcessStartInfo("reg.exe", $"import \"{file}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi)!;
            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, output);

            using var restored = Registry.CurrentUser.OpenSubKey(_relative);
            Assert.NotNull(restored);
            Assert.Equal("default text", (string)restored!.GetValue("")!);
            Assert.Equal(@"C:\Temp\Foo", (string)restored.GetValue("Text")!);
            Assert.Equal(@"%SystemRoot%\Temp",
                (string)restored.GetValue("Expand", null, RegistryValueOptions.DoNotExpandEnvironmentNames)!);
            Assert.Equal(new[] { "one", "two" }, (string[])restored.GetValue("Multi")!);
            Assert.Equal(-1, (int)restored.GetValue("Number")!);
            Assert.Equal(1234567890123L, (long)restored.GetValue("Big")!);
            Assert.Equal(new byte[] { 1, 2, 255 }, (byte[])restored.GetValue("Bytes")!);

            using var child = restored.OpenSubKey("Child");
            Assert.NotNull(child);
            Assert.Equal("x", (string)child!.GetValue("Inner")!);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
