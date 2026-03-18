using System.Runtime.Versioning;

namespace Sweepr.Core.Services;

// securely wipes a file (overwrite then delete).
[SupportedOSPlatform("windows")]
public static class SecureDelete
{
    public static bool Wipe(string path, int passes = 3)
    {
        try
        {
            if (!File.Exists(path)) return false;
            var info = new FileInfo(path);
            var length = info.Length;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[65536];
                for (int pass = 0; pass < passes; pass++)
                {
                    fs.Position = 0;
                    long remaining = length;
                    var rng = new Random();
                    while (remaining > 0)
                    {
                        int chunk = (int)Math.Min(buffer.Length, remaining);
                        if (pass == passes - 1) Array.Clear(buffer, 0, chunk); else rng.NextBytes(buffer);
                        fs.Write(buffer, 0, chunk);
                        remaining -= chunk;
                    }
                    fs.Flush(true);
                }
            }
            File.Delete(path);
            return true;
        }
        catch { return false; }
    }
}