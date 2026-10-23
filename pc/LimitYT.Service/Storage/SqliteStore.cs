using System.Globalization;
using LimitYT.Core.Usage;
using Microsoft.Data.Sqlite;

namespace LimitYT.Service.Storage;

/// <summary>Lưu sổ ghi giờ (docs/02-kien-truc.md §4.3). Chỉ được gọi khi đang giữ lock của LimitController.</summary>
public sealed class SqliteStore : IDisposable
{
    private const string DateFormat = "yyyy-MM-dd";
    private readonly SqliteConnection _conn;

    public SqliteStore(string path)
    {
        _conn = new SqliteConnection($"Data Source={path}");
        _conn.Open();
        Execute("PRAGMA journal_mode=WAL;");
        Execute("""
            CREATE TABLE IF NOT EXISTS usage (
              device_id TEXT NOT NULL, date TEXT NOT NULL, ms INTEGER NOT NULL,
              PRIMARY KEY (device_id, date));
            CREATE TABLE IF NOT EXISTS adjustment (
              id TEXT PRIMARY KEY, date TEXT NOT NULL, ms INTEGER NOT NULL, reason TEXT, created_at TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS kv (key TEXT PRIMARY KEY, value TEXT);
            INSERT OR IGNORE INTO kv (key, value) VALUES ('schema_version', '1');
            """);
    }

    public UsageLedger LoadLedger(DateOnly since)
    {
        var ledger = new UsageLedger();
        var sinceText = since.ToString(DateFormat, CultureInfo.InvariantCulture);

        using (var cmd = Command("SELECT device_id, date, ms FROM usage WHERE date >= $since", ("$since", sinceText)))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                ledger.Merge(new UsageEntry(r.GetString(0), ParseDate(r.GetString(1)), r.GetInt64(2)));

        using (var cmd = Command("SELECT id, date, ms, reason, created_at FROM adjustment WHERE date >= $since", ("$since", sinceText)))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                ledger.Merge(new Adjustment(r.GetString(0), ParseDate(r.GetString(1)), r.GetInt64(2),
                    r.IsDBNull(3) ? null : r.GetString(3), DateTimeOffset.Parse(r.GetString(4), CultureInfo.InvariantCulture)));

        return ledger;
    }

    public void SaveUsage(IEnumerable<UsageEntry> entries)
    {
        using var tx = _conn.BeginTransaction();
        foreach (var e in entries)
        {
            using var cmd = Command("""
                INSERT INTO usage (device_id, date, ms) VALUES ($d, $date, $ms)
                ON CONFLICT (device_id, date) DO UPDATE SET ms = MAX(ms, excluded.ms)
                """, ("$d", e.Device), ("$date", e.Date.ToString(DateFormat, CultureInfo.InvariantCulture)), ("$ms", e.Ms));
            cmd.Transaction = tx;
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public void SaveAdjustment(Adjustment a)
    {
        using var cmd = Command(
            "INSERT OR IGNORE INTO adjustment (id, date, ms, reason, created_at) VALUES ($id, $date, $ms, $reason, $at)",
            ("$id", a.Id), ("$date", a.Date.ToString(DateFormat, CultureInfo.InvariantCulture)), ("$ms", a.Ms),
            ("$reason", (object?)a.Reason ?? DBNull.Value), ("$at", a.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture)));
        cmd.ExecuteNonQuery();
    }

    public string? GetValue(string key)
    {
        using var cmd = Command("SELECT value FROM kv WHERE key = $k", ("$k", key));
        return cmd.ExecuteScalar() as string;
    }

    public void SetValue(string key, string? value)
    {
        using var cmd = value is null
            ? Command("DELETE FROM kv WHERE key = $k", ("$k", key))
            : Command("INSERT INTO kv (key, value) VALUES ($k, $v) ON CONFLICT (key) DO UPDATE SET value = excluded.value",
                ("$k", key), ("$v", value));
        cmd.ExecuteNonQuery();
    }

    public void Dispose() => _conn.Dispose();

    private static DateOnly ParseDate(string s) => DateOnly.ParseExact(s, DateFormat, CultureInfo.InvariantCulture);

    private void Execute(string sql)
    {
        using var cmd = Command(sql);
        cmd.ExecuteNonQuery();
    }

    private SqliteCommand Command(string sql, params (string Name, object Value)[] parameters)
    {
        var cmd = _conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters) cmd.Parameters.AddWithValue(name, value);
        return cmd;
    }
}
