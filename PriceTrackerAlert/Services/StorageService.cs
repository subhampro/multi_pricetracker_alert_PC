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
            Id          INTEGER PRIMARY KEY AUTOINCREMENT,
            Symbol      TEXT    NOT NULL,
            TargetPrice REAL    NOT NULL,
            Condition   INTEGER NOT NULL,
            IsActive    INTEGER NOT NULL DEFAULT 1,
            IsTriggered INTEGER NOT NULL DEFAULT 0,
            SoundFile   TEXT    NOT NULL DEFAULT 'default_mp3',
            Note        TEXT    NOT NULL DEFAULT '',
            Source      INTEGER NOT NULL DEFAULT 0
        )");

        // Migrate existing DBs that don't have the Source column yet
        try { conn.Execute("ALTER TABLE Alerts ADD COLUMN Source INTEGER NOT NULL DEFAULT 0"); }
        catch { /* column already exists */ }
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
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = "SELECT Id,Symbol,TargetPrice,Condition,IsActive,IsTriggered,SoundFile,Note,Source FROM Alerts ORDER BY Id";
        using var r = cmd.ExecuteReader();
        var list = new List<Alert>();
        while (r.Read()) list.Add(MapAlert(r));
        return list;
    }

    public int AddAlert(Alert a)
    {
        using var conn = Open();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Alerts (Symbol,TargetPrice,Condition,IsActive,IsTriggered,SoundFile,Note,Source) VALUES ($s,$t,$c,$ia,$it,$sf,$n,$src); SELECT last_insert_rowid();";
        Bind(cmd, a);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void UpdateAlert(Alert a)
    {
        using var conn = Open();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = "UPDATE Alerts SET Symbol=$s,TargetPrice=$t,Condition=$c,IsActive=$ia,IsTriggered=$it,SoundFile=$sf,Note=$n,Source=$src WHERE Id=$id";
        Bind(cmd, a);
        cmd.Parameters.AddWithValue("$id", a.Id);
        cmd.ExecuteNonQuery();
    }

    public void DeleteAlert(int id)
    {
        using var conn = Open();
        using var cmd  = conn.CreateCommand();
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

    private static void Bind(SqliteCommand cmd, Alert a)
    {
        cmd.Parameters.AddWithValue("$s",   a.Symbol);
        cmd.Parameters.AddWithValue("$t",   a.TargetPrice);
        cmd.Parameters.AddWithValue("$c",   (int)a.Condition);
        cmd.Parameters.AddWithValue("$ia",  a.IsActive   ? 1 : 0);
        cmd.Parameters.AddWithValue("$it",  a.IsTriggered ? 1 : 0);
        cmd.Parameters.AddWithValue("$sf",  a.SoundFile);
        cmd.Parameters.AddWithValue("$n",   a.Note);
        cmd.Parameters.AddWithValue("$src", (int)a.Source);
    }

    private static Alert MapAlert(SqliteDataReader r) => new()
    {
        Id          = r.GetInt32(0),
        Symbol      = r.GetString(1),
        TargetPrice = r.GetDouble(2),
        Condition   = (AlertCondition)r.GetInt32(3),
        IsActive    = r.GetInt32(4) == 1,
        IsTriggered = r.GetInt32(5) == 1,
        SoundFile   = r.GetString(6),
        Note        = r.GetString(7),
        Source      = (PriceSource)r.GetInt32(8)
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
