using System.Runtime.Versioning;
using Microsoft.Data.Sqlite;
using Sweepr.Core.Models;

namespace Sweepr.Core.Services;

// stores install traces in a sqlite database.
[SupportedOSPlatform("windows")]
public static class TraceLogDatabase
{
    private static string DbPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sweepr", "traces.db");

    private static SqliteConnection Open()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
        var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Traces (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            StartedAt TEXT NOT NULL,
            CompletedAt TEXT NOT NULL,
            Files TEXT NOT NULL,
            RegistryKeys TEXT NOT NULL
        );";
        cmd.ExecuteNonQuery();
        return conn;
    }

    public static void Add(InstallTrace trace)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Traces (Name, StartedAt, CompletedAt, Files, RegistryKeys) VALUES ($name, $started, $completed, $files, $keys);";
        cmd.Parameters.AddWithValue("$name", trace.Name);
        cmd.Parameters.AddWithValue("$started", trace.StartedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$completed", trace.CompletedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$files", string.Join("\n", trace.Files));
        cmd.Parameters.AddWithValue("$keys", string.Join("\n", trace.RegistryKeys));
        cmd.ExecuteNonQuery();
    }

    public static List<InstallTrace> GetAll()
    {
        var results = new List<InstallTrace>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Name, StartedAt, CompletedAt, Files, RegistryKeys FROM Traces ORDER BY Id DESC;";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            results.Add(new InstallTrace
            {
                Name = r.GetString(0),
                StartedAt = DateTime.Parse(r.GetString(1)),
                CompletedAt = DateTime.Parse(r.GetString(2)),
                Files = r.GetString(3).Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(),
                RegistryKeys = r.GetString(4).Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList()
            });
        }
        return results;
    }

    public static void Delete(string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Traces WHERE Name = $name;";
        cmd.Parameters.AddWithValue("$name", name);
        cmd.ExecuteNonQuery();
    }
}