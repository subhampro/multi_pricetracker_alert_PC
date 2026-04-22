using Microsoft.Data.Sqlite;
using PriceTrackerAlert.Models;
using System.IO;
using System.Text.Json;

namespace PriceTrackerAlert.Services;

public class StorageService
{
    private readonly string _dbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PriceTrackerAlert", "alerts.db");

    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PriceTrackerAlert", "settings.json");

    public StorageService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        InitDb();
    }

    private void InitDb()
    {
        using var conn = Open();
        conn.Execute(@"CREATE TABLE IF NOT EXISTS Alerts (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Symbol TEXT NOT NULL,
            TargetPrice REAL NOT NULL,
            Condition INTEGER NOT NULL,
            IsActive INTEGER NOT NULL DEFAULT 1,
            IsTriggered INTEGER NOT NULL DEFAULT 0,
            SoundFile TEXT NOT NULL DEFAULT 'default',
            Note TEXT NOT NULL DEFAULT ''
        )");
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }

    public List<Alert> GetAlerts()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Alerts ORDER BY Id";
        using var reader = cmd.ExecuteReader();
        var list = new List<Alert>();
        while (reader.Read())
            list.Add(MapAlert(reader));
        return list;
    }

    public int AddAlert(Alert a)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Alerts (Symbol,TargetPrice,Condition,IsActive,IsTriggered,SoundFile,Note) VALUES ($s,$t,$c,$ia,$it,$sf,$n); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$s", a.Symbol);
        cmd.Parameters.AddWithValue("$t", a.TargetPrice);
        cmd.Parameters.AddWithValue("$c", (int)a.Condition);
        cmd.Parameters.AddWithValue("$ia", a.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("$it", a.IsTriggered ? 1 : 0);
        cmd.Parameters.AddWithValue("$sf", a.SoundFile);
        cmd.Parameters.AddWithValue("$n", a.Note);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void UpdateAlert(Alert a)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Alerts SET Symbol=$s,TargetPrice=$t,Condition=$c,IsActive=$ia,IsTriggered=$it,SoundFile=$sf,Note=$n WHERE Id=$id";
        cmd.Parameters.AddWithValue("$s", a.Symbol);
        cmd.Parameters.AddWithValue("$t", a.TargetPrice);
        cmd.Parameters.AddWithValue("$c", (int)a.Condition);
        cmd.Parameters.AddWithValue("$ia", a.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("$it", a.IsTriggered ? 1 : 0);
        cmd.Parameters.AddWithValue("$sf", a.SoundFile);
        cmd.Parameters.AddWithValue("$n", a.Note);
        cmd.Parameters.AddWithValue("$id", a.Id);
        cmd.ExecuteNonQuery();
    }

    public void DeleteAlert(int id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Alerts WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsPath)) return new AppSettings();
        try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath)) ?? new AppSettings(); }
        catch { return new AppSettings(); }
    }

    public void SaveSettings(AppSettings s) =>
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));

    private static Alert MapAlert(SqliteDataReader r) => new()
    {
        Id = r.GetInt32(0),
        Symbol = r.GetString(1),
        TargetPrice = r.GetDouble(2),
        Condition = (AlertCondition)r.GetInt32(3),
        IsActive = r.GetInt32(4) == 1,
        IsTriggered = r.GetInt32(5) == 1,
        SoundFile = r.GetString(6),
        Note = r.GetString(7)
    };
}

internal static class SqliteConnectionExtensions
{
    public static void Execute(this SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
