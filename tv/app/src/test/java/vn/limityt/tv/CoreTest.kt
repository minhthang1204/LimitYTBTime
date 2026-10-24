package vn.limityt.tv

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test
import vn.limityt.tv.guard.ClockGuard
import vn.limityt.tv.guard.KeyValue
import vn.limityt.tv.guard.PinGuard
import vn.limityt.tv.guard.Screen
import vn.limityt.tv.guard.ScreenRules
import vn.limityt.tv.rules.Adjustment
import vn.limityt.tv.rules.BlockReason
import vn.limityt.tv.rules.ConfigJson
import vn.limityt.tv.rules.Decision
import vn.limityt.tv.rules.Defaults
import vn.limityt.tv.rules.DurationText
import vn.limityt.tv.rules.LocalCalendar
import vn.limityt.tv.rules.TvConfig
import vn.limityt.tv.rules.UsageEntry
import vn.limityt.tv.rules.UsageLedger
import vn.limityt.tv.rules.WarningTracker
import java.time.LocalDate
import java.time.OffsetDateTime

class LedgerTest {
    private val d = LocalDate.of(2026, 10, 26)

    @Test
    fun mergeIsOrderIndependentAndIdempotent() {
        val a = UsageLedger().apply { add("pc", d, 1000); merge(Adjustment("x", d, 5, null, "")) }
        val b = UsageLedger().apply { add("tv", d, 2000); add("pc", d, 500) }

        val ab = UsageLedger().apply { a.entries.forEach(::merge); b.entries.forEach(::merge); a.allAdjustments.forEach(::merge) }
        val ba = UsageLedger().apply { b.entries.forEach(::merge); a.entries.forEach(::merge); a.allAdjustments.forEach(::merge) }
        ab.entries.forEach(ab::merge)

        assertEquals(ab.entries.toSet(), ba.entries.toSet())
        assertEquals(1000, ab.deviceMs("pc", d))
        assertEquals(3000 - 5, ab.netUsedMs(d))
        assertFalse(ab.merge(UsageEntry("pc", d, 999)))
        assertFalse(ab.merge(Adjustment("x", d, 5, null, "")))
    }
}

class CalendarTest {
    private val calendar = LocalCalendar(Defaults.create())

    @Test
    fun splitAcrossMidnight() {
        val end = OffsetDateTime.parse("2026-10-27T00:00:03+07:00").toInstant()
        val parts = calendar.splitByLocalDate(end, 10_000)
        assertEquals(listOf(LocalDate.of(2026, 10, 26) to 7_000L, LocalDate.of(2026, 10, 27) to 3_000L), parts)
    }

    @Test
    fun durationText() {
        assertEquals("1 giờ 35 phút", DurationText.format(95 * 60_000L))
        assertEquals("30 phút", DurationText.format(29 * 60_000L + 1, roundUp = true))
        assertEquals("2 giờ", DurationText.format(120 * 60_000L))
    }
}

class WarningTest {
    private fun allowed(minutes: Double) = Decision(true, BlockReason.None, (minutes * 60_000).toLong(), null)

    @Test
    fun warnsWhenCrossingThresholdAndOnStartBelowMax() {
        val w = WarningTracker()
        val t = listOf(15, 5)
        assertNull(w.next(allowed(20.0), true, t))
        assertEquals(15, w.next(allowed(14.9), true, t))
        assertNull(w.next(allowed(14.0), true, t))
        assertEquals(5, w.next(allowed(4.5), true, t))

        val w2 = WarningTracker()
        assertEquals(10, w2.next(allowed(10.0), true, t))   // bắt đầu xem khi chỉ còn 10 phút (case A7)
    }
}

class ScreenRulesTest {
    private val rules = ScreenRules(
        Defaults.create().copy(tv = TvConfig(neverBlockPackages = listOf("com.xiaomi.hdmi"), cleanerPackages = listOf("com.x.clean"))),
        "vn.limityt.tv",
    )

    private fun screen(pkg: String, label: String? = null, vararg texts: String): Screen =
        rules.resolve(rules.classifyPackage(pkg, label)) { texts.asSequence() }

    @Test
    fun youtubeAppsAreLimited() {
        assertEquals(Screen.Limited("youtube"), screen("com.google.android.youtube.tv"))
        assertEquals(Screen.Limited("youtube"), screen("com.liskovsoft.smarttubetv.beta"))
        assertEquals(Screen.Limited("tiktok"), screen("com.zhiliaoapp.musically"))
        // App lạ cài thêm (case C11)
        assertEquals(Screen.Limited("youtube"), screen("org.schabi.newpipe", "NewPipe"))
        assertEquals(Screen.Limited("youtube"), screen("com.example.player", "MyTube"))
        assertEquals(Screen.Limited("tiktok"), screen("com.example.short", "Tik Tok Lite"))
    }

    @Test
    fun hdmiLauncherAndSelfAreNeverBlocked() {
        assertEquals(Screen.NeverBlock, screen("com.xiaomi.hdmi", null, "YouTube"))
        assertEquals(Screen.NeverBlock, screen("com.google.android.tvlauncher", null, "YouTube", "Accessibility"))
        assertEquals(Screen.NeverBlock, screen("vn.limityt.tv"))
    }

    @Test
    fun settingsNeedPinOnlyForGuardedScreens() {
        assertTrue(screen("com.android.tv.settings", null, "Device Preferences", "Accessibility") is Screen.NeedsPin)
        assertTrue(screen("com.android.tv.settings", null, "Ứng dụng", "LimitYT", "Buộc dừng") is Screen.NeedsPin)
        assertTrue(screen("com.android.tv.settings", null, "Ngày & giờ") is Screen.NeedsPin)
        assertEquals(Screen.Normal, screen("com.android.tv.settings", null, "Âm thanh", "Hình ảnh", "Network & Internet"))
        // So khớp cả từ: "Update" không khớp "Date", "Rebuild" không khớp "Build".
        assertEquals(Screen.Normal, screen("com.android.tv.settings", null, "System update", "Rebuilding index"))
    }

    @Test
    fun adbAuthorizationDialogNeedsPinAndIsDismissedWithBack() {
        val dialog = screen("com.android.systemui", null, "Allow USB debugging?", "The computer's RSA key fingerprint is: AB:CD")
        assertEquals(Screen.NeedsPin("Cần PIN của phụ huynh để cho phép kết nối gỡ lỗi (ADB)", back = true), dialog)
        assertTrue(screen("com.android.systemui", null, "Cho phép gỡ lỗi USB?") is Screen.NeedsPin)
        // Các cửa sổ khác của System UI (âm lượng, thông báo) không bao giờ bị chặn.
        assertEquals(Screen.NeverBlock, screen("com.android.systemui", null, "Âm lượng", "YouTube"))
        // Bật ADB trong Settings cũng cần PIN.
        assertTrue(screen("com.android.tv.settings", null, "Network debugging") is Screen.NeedsPin)
    }

    @Test
    fun installersAndCleanersNeedPin() {
        assertTrue(screen("com.google.android.packageinstaller") is Screen.NeedsPin)
        assertTrue(screen("com.x.clean") is Screen.NeedsPin)
        assertTrue(screen("com.some.vendor", "Dọn dẹp TV") is Screen.NeedsPin)
    }

    @Test
    fun browserIsLimitedOnlyOnServiceDomains() {
        assertEquals(Screen.Limited("youtube"), screen("com.android.chrome", null, "https://m.youtube.com/watch?v=1"))
        assertEquals(Screen.Limited("tiktok"), screen("org.mozilla.tv.firefox", null, "tiktok.com/@abc"))
        assertEquals(Screen.Normal, screen("com.android.chrome", null, "google.com/search?q=youtube"))
        assertEquals(Screen.Normal, screen("com.android.chrome", null, "notyoutube.com"))
        assertEquals(Screen.Limited("youtube"), screen("com.vendor.browser", null, "youtu.be/abc"))
    }
}

class PinGuardTest {
    private class MemoryKv : KeyValue {
        val map = HashMap<String, String>()
        override fun get(key: String) = map[key]
        override fun put(key: String, value: String?) {
            if (value == null) map.remove(key) else map[key] = value
        }
    }

    @Test
    fun hashRoundTrip() {
        val h = PinGuard.hash("123456", iterations = 1000)
        assertTrue(PinGuard.matches("123456", h))
        assertFalse(PinGuard.matches("123457", h))
        assertFalse(PinGuard.matches("123456", "garbage"))
    }

    @Test
    fun lockoutAfterFiveFailuresThenDoubles() {
        val pin = PinGuard(MemoryKv()).apply { set("246810") }
        var now = 1_000_000L
        repeat(4) { assertEquals(PinGuard.Result.Wrong(4 - it), pin.verify("000000", now)) }
        val first = pin.verify("000000", now) as PinGuard.Result.LockedOut
        assertEquals(now + 15 * 60_000, first.untilMs)
        // Đang khoá thì PIN đúng cũng bị từ chối
        assertTrue(pin.verify("246810", now + 60_000) is PinGuard.Result.LockedOut)

        now = first.untilMs + 1
        repeat(4) { pin.verify("000000", now) }
        val second = pin.verify("000000", now) as PinGuard.Result.LockedOut
        assertEquals(now + 30 * 60_000, second.untilMs)

        assertEquals(PinGuard.Result.Ok, pin.verify("246810", second.untilMs + 1))
        assertEquals(PinGuard.Result.Wrong(4), pin.verify("000000", second.untilMs + 2))
    }

    @Test
    fun validation() {
        assertTrue(PinGuard.isValid("012345"))
        assertFalse(PinGuard.isValid("12345"))
        assertFalse(PinGuard.isValid("12345a"))
    }
}

class ClockGuardTest {
    private val g = ClockGuard()

    @Test
    fun detectsManualChangeOnly() {
        val a = ClockGuard.Anchor(100_000, 1_000_000_000)
        assertFalse(g.tampered(null, a, autoTime = false))
        assertFalse(g.tampered(a, ClockGuard.Anchor(105_000, 1_000_005_000), autoTime = false))
        // Chỉnh lùi 1 giờ
        assertTrue(g.tampered(a, ClockGuard.Anchor(105_000, 1_000_005_000 - 3_600_000), autoTime = false))
        // Giờ tự động (đồng bộ mạng) → không tính là chỉnh
        assertFalse(g.tampered(a, ClockGuard.Anchor(105_000, 1_000_005_000 - 3_600_000), autoTime = true))
        // Vừa khởi động lại: elapsed nhỏ hơn mốc cũ
        assertFalse(g.tampered(a, ClockGuard.Anchor(1_000, 5_000_000_000), autoTime = false))
    }
}

class ConfigJsonTest {
    @Test
    fun readsConfigWrittenByPc() {
        // Định dạng do System.Text.Json (JsonSerializerDefaults.Web) bên PC ghi ra.
        val json = """
            {
              "version": 3,
              "updatedAtUtc": "0001-01-01T00:00:00+00:00",
              "timeZone": "Asia/Ho_Chi_Minh",
              "weekStart": "Mon",
              "weeklyQuotaMinutes": 300,
              "dailyQuotaMinutes": 60,
              "holidayDailyQuotaMinutes": null,
              "blockedWindows": [ { "days": ["Mon"], "from": "22:00", "to": "06:00", "schoolOnly": false } ],
              "holidays": [ { "from": "2027-06-01", "to": "2027-08-31", "note": "Nghỉ hè" } ],
              "holidayOverrides": { "2026-10-26": true },
              "services": [ { "id": "youtube", "domains": ["youtube.com"], "titlePatterns": ["(?<=\\S\\s)[-–—|]\\sYouTube"],
                              "tvPackages": ["com.google.android.youtube.tv"], "pcExecutables": ["FreeTube.exe"] } ],
              "tv": { "enabled": true, "host": "192.168.1.50", "port": 5555, "guardedSettingsPackages": [],
                      "neverBlockPackages": [], "installerPackages": [], "cleanerPackages": [] },
              "adminPinHash": null,
              "someFutureField": 1,
            }
        """.trimIndent()
        val c = ConfigJson.parse(json)
        assertEquals(3, c.version)
        assertEquals(300, c.weeklyQuotaMinutes)
        assertEquals(true, c.holidayOverrides["2026-10-26"])
        assertEquals("192.168.1.50", c.tv.host)
        assertEquals(c, ConfigJson.parse(ConfigJson.stringify(c)))
        assertTrue(LocalCalendar(c).isHoliday(LocalDate.of(2027, 7, 1)))
    }
}
