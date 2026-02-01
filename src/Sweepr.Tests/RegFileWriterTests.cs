using System.Text;
using Sweepr.Core.Models;
using Sweepr.Core.Services;
using Xunit;

namespace Sweepr.Tests;

public class RegFileWriterTests
{
    private const string Key = @"HKEY_CURRENT_USER\Software\Foo";

    public static TheoryData<RegValueEntry, string> ValueCases => new()
    {
        { new RegStringValue("Path", @"C:\Dir"), "\"Path\"=\"C:\\\\Dir\"" },
        { new RegStringValue("Msg", "say \"hi\""), "\"Msg\"=\"say \\\"hi\\\"\"" },
        { new RegStringValue("", "hello"), "@=\"hello\"" },
        { new RegStringValue(@"a\b", "1"), "\"a\\\\b\"=\"1\"" },
        { new RegStringValue("Text", "a\nb"), "\"Text\"=hex(1):61,00,0a,00,62,00,00,00" },
        { new RegExpandStringValue("Exp", "A"), "\"Exp\"=hex(2):41,00,00,00" },
        { new RegMultiStringValue("M", new[] { "a", "b" }), "\"M\"=hex(7):61,00,00,00,62,00,00,00,00,00" },
        { new RegMultiStringValue("M", Array.Empty<string>()), "\"M\"=hex(7):00,00" },
        { new RegDWordValue("D", 42), "\"D\"=dword:0000002a" },
        { new RegDWordValue("D", uint.MaxValue), "\"D\"=dword:ffffffff" },
        { new RegQWordValue("Q", 0x0102030405060708UL), "\"Q\"=hex(b):08,07,06,05,04,03,02,01" },
        { new RegBinaryValue("B", new byte[] { 1, 2, 255 }), "\"B\"=hex:01,02,ff" },
        { new RegBinaryValue("B", Array.Empty<byte>()), "\"B\"=hex:" },
        { new RegNoneValue("N", new byte[] { 7 }), "\"N\"=hex(0):07" },
        { new RegNoneValue("N", Array.Empty<byte>()), "\"N\"=hex(0):" }
    };

    [Theory]
    [MemberData(nameof(ValueCases))]
    public void FormatValue_MatchesRegeditSyntax(RegValueEntry value, string expected) =>
        Assert.Equal(expected, RegFileWriter.FormatValue(value));

    [Fact]
    public void Build_WritesHeaderAndKeyBlocks()
    {
        var text = RegFileWriter.Build(new[]
        {
            new RegKeyEntry(Key, new RegValueEntry[] { new RegStringValue("A", "1") }),
            new RegKeyEntry(Key + @"\Child", Array.Empty<RegValueEntry>())
        });

        var expected = "Windows Registry Editor Version 5.00\r\n" +
                       "\r\n[HKEY_CURRENT_USER\\Software\\Foo]\r\n\"A\"=\"1\"\r\n" +
                       "\r\n[HKEY_CURRENT_USER\\Software\\Foo\\Child]\r\n";

        Assert.Equal(expected, text);
    }

    [Fact]
    public void Build_WritesDefaultValueFirst()
    {
        var text = RegFileWriter.Build(new[]
        {
            new RegKeyEntry(Key, new RegValueEntry[] { new RegStringValue("A", "1"), new RegStringValue("", "x") })
        });

        Assert.Contains("[HKEY_CURRENT_USER\\Software\\Foo]\r\n@=\"x\"\r\n\"A\"=\"1\"\r\n", text);
    }

    [Fact]
    public void WriteFile_UsesUtf16WithBomAndLeavesNoTempFile()
    {
        var file = Path.Combine(Path.GetTempPath(), $"sweepr-{Guid.NewGuid():N}.reg");
        try
        {
            RegFileWriter.WriteFile(file, new[] { new RegKeyEntry(Key, Array.Empty<RegValueEntry>()) });

            var bytes = File.ReadAllBytes(file);
            Assert.Equal((byte)0xFF, bytes[0]);
            Assert.Equal((byte)0xFE, bytes[1]);
            Assert.StartsWith(RegFileWriter.Header, File.ReadAllText(file, Encoding.Unicode));
            Assert.False(File.Exists(file + ".tmp"));
        }
        finally
        {
            File.Delete(file);
        }
    }
}
