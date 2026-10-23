package vn.limityt.tv.data

import android.content.ContentValues
import android.content.Context
import android.database.sqlite.SQLiteDatabase
import android.database.sqlite.SQLiteOpenHelper
import vn.limityt.tv.guard.KeyValue
import vn.limityt.tv.rules.Adjustment
import vn.limityt.tv.rules.UsageLedger
import java.time.LocalDate

/** SQLite cùng lược đồ với PC (docs/02-kien-truc.md §4.3). */
class UsageStore(context: Context) : SQLiteOpenHelper(context, "limityt.db", null, SCHEMA_VERSION), KeyValue {

    override fun onCreate(db: SQLiteDatabase) {
        db.execSQL("CREATE TABLE usage (device_id TEXT NOT NULL, date TEXT NOT NULL, ms INTEGER NOT NULL, PRIMARY KEY (device_id, date))")
        db.execSQL("CREATE TABLE adjustment (id TEXT PRIMARY KEY, date TEXT NOT NULL, ms INTEGER NOT NULL, reason TEXT, created_at TEXT NOT NULL)")
        db.execSQL("CREATE TABLE peer (device_id TEXT PRIMARY KEY, last_sync_utc TEXT, last_address TEXT)")
        db.execSQL("CREATE TABLE kv (key TEXT PRIMARY KEY, value TEXT)")
    }

    override fun onUpgrade(db: SQLiteDatabase, oldVersion: Int, newVersion: Int) {
        // Chưa có migration: schema 1 là bản đầu tiên.
    }

    fun loadLedger(since: LocalDate): UsageLedger {
        val ledger = UsageLedger()
        val db = readableDatabase
        db.rawQuery("SELECT device_id, date, ms FROM usage WHERE date >= ?", arrayOf(since.toString())).use { c ->
            while (c.moveToNext()) ledger.add(c.getString(0), LocalDate.parse(c.getString(1)), c.getLong(2))
        }
        db.rawQuery("SELECT id, date, ms, reason, created_at FROM adjustment WHERE date >= ?", arrayOf(since.toString())).use { c ->
            while (c.moveToNext())
                ledger.merge(Adjustment(c.getString(0), LocalDate.parse(c.getString(1)), c.getLong(2), c.getString(3), c.getString(4)))
        }
        return ledger
    }

    fun saveUsage(device: String, date: LocalDate, ms: Long) {
        writableDatabase.insertWithOnConflict("usage", null, ContentValues().apply {
            put("device_id", device)
            put("date", date.toString())
            put("ms", ms)
        }, SQLiteDatabase.CONFLICT_REPLACE)
    }

    fun saveAdjustment(a: Adjustment) {
        writableDatabase.insertWithOnConflict("adjustment", null, ContentValues().apply {
            put("id", a.id)
            put("date", a.date.toString())
            put("ms", a.ms)
            put("reason", a.reason)
            put("created_at", a.createdAtUtc)
        }, SQLiteDatabase.CONFLICT_IGNORE)
    }

    fun pruneBefore(date: LocalDate) {
        writableDatabase.delete("usage", "date < ?", arrayOf(date.toString()))
        writableDatabase.delete("adjustment", "date < ?", arrayOf(date.toString()))
    }

    override fun get(key: String): String? =
        readableDatabase.rawQuery("SELECT value FROM kv WHERE key = ?", arrayOf(key)).use { c ->
            if (c.moveToFirst() && !c.isNull(0)) c.getString(0) else null
        }

    override fun put(key: String, value: String?) {
        if (value == null) writableDatabase.delete("kv", "key = ?", arrayOf(key))
        else writableDatabase.insertWithOnConflict("kv", null, ContentValues().apply {
            put("key", key)
            put("value", value)
        }, SQLiteDatabase.CONFLICT_REPLACE)
    }

    companion object {
        const val SCHEMA_VERSION = 1
    }
}
