using System.Text.RegularExpressions;

namespace Sweepr.Core.Services;

// turns "audacity 3.6.1 (x64)" into "audacity" so folder/registry names can be matched
public static class NameCleaner
{
    private static readonly Regex Brackets = new(@"[\(\[][^\)\]]*[\)\]]", RegexOptions.Compiled);
    private static readonly Regex Versions = new(@"\bv?\d+(\.\d+)+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex Noise = new(@"\b(x64|x86|64-bit|32-bit|version)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);

    public static string Clean(string name)
    {
        var s = Brackets.Replace(name, " ");
        s = Versions.Replace(s, " ");
        s = Noise.Replace(s, " ");
        s = Spaces.Replace(s, " ").Trim(' ', '-', '_', '.');
        return s;
    }

    // lower-case letters and digits only, for fuzzy comparison.
    public static string Normalize(string s) =>
        new string(s.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}
