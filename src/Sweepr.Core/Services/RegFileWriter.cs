using System.Buffers.Binary;
using System.Text;
using Sweepr.Core.Models;

namespace Sweepr.Core.Services;

public static class RegFileWriter
{
    public const string Header = "Windows Registry Editor Version 5.00";
    private const string NewLine = "\r\n";

    public static string Build(IEnumerable<RegKeyEntry> keys)
    {
        var text = new StringBuilder();
        text.Append(Header).Append(NewLine);

        foreach (var key in keys)
        {
            text.Append(NewLine).Append('[').Append(key.Path).Append(']').Append(NewLine);

            foreach (var value in key.Values.OrderBy(v => v.Name.Length == 0 ? 0 : 1))
                text.Append(FormatValue(value)).Append(NewLine);
        }

        return text.ToString();
    }

    public static void WriteFile(string filePath, IEnumerable<RegKeyEntry> keys)
    {
        var temp = filePath + ".tmp";
        File.WriteAllText(temp, Build(keys), Encoding.Unicode);
        File.Move(temp, filePath, overwrite: true);
    }

    public static string FormatValue(RegValueEntry value)
    {
        var name = value.Name.Length == 0 ? "@" : $"\"{Escape(value.Name)}\"";

        return value switch
        {
            RegStringValue s when s.Data.Any(char.IsControl) => $"{name}=hex(1):{Hex(Utf16(s.Data))}",
            RegStringValue s => $"{name}=\"{Escape(s.Data)}\"",
            RegExpandStringValue e => $"{name}=hex(2):{Hex(Utf16(e.Data))}",
            RegMultiStringValue m => $"{name}=hex(7):{Hex(MultiString(m.Data))}",
            RegDWordValue d => $"{name}=dword:{d.Data:x8}",
            RegQWordValue q => $"{name}=hex(b):{Hex(QWord(q.Data))}",
            RegBinaryValue b => $"{name}=hex:{Hex(b.Data)}",
            RegNoneValue n => $"{name}=hex(0):{Hex(n.Data)}",
            _ => throw new NotSupportedException($"Unsupported registry value type: {value.GetType().Name}")
        };
    }

    private static string Escape(string text) => text.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string Hex(IEnumerable<byte> bytes) => string.Join(",", bytes.Select(b => b.ToString("x2")));

    private static byte[] Utf16(string text) =>
        Encoding.Unicode.GetBytes(text).Concat(new byte[] { 0, 0 }).ToArray();

    private static byte[] MultiString(IEnumerable<string> items) =>
        items.SelectMany(item => Utf16(item)).Concat(new byte[] { 0, 0 }).ToArray();

    private static byte[] QWord(ulong value)
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        return bytes;
    }
}
