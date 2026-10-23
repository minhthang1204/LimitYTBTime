package vn.limityt.tv.data

import android.content.Context
import vn.limityt.tv.guard.ClockGuard
import vn.limityt.tv.guard.PinGuard
import vn.limityt.tv.guard.ScreenRules
import vn.limityt.tv.rules.Adjustment
import vn.limityt.tv.rules.BlockReason
import vn.limityt.tv.rules.ConfigJson
import vn.limityt.tv.rules.Decision
import vn.limityt.tv.rules.Defaults
import vn.limityt.tv.rules.Devices
import vn.limityt.tv.rules.DurationText
import vn.limityt.tv.rules.LimitConfig
import vn.limityt.tv.rules.RulesEngine
import vn.limityt.tv.rules.UsageLedger
import java.time.Instant
import java.time.LocalDate
import java.time.format.DateTimeFormatter
import java.util.UUID

/**
 * Trạng thái dùng chung trong tiến trình: cấu hình, sổ giờ, PIN, các mốc mở khoá.
 * Mọi lời gọi đều trên main thread (service và activity cùng tiến trình).
 */
class Guardian(context: Context) {
    val store = UsageStore(context.applicationContext)
    val pin = PinGuard(store)
    private val clockGuard = ClockGuard()
    private val selfPackage = context.packageName

    var config: LimitConfig? = null
        private set
    var rules: RulesEngine? = null
        private set
    var screenRules: ScreenRules = ScreenRules(Defaults.create(), selfPackage)
        private set
    /** Tăng mỗi khi cấu hình đổi, để service biết mà làm mới bộ đệm. */
    var configGeneration = 0
        private set

    private var ledger = UsageLedger()
    private val dirtyDates = HashSet<LocalDate>()
    private var clockAnchor: ClockGuard.Anchor? = null

    // Các cờ dưới đây được đọc ở mỗi sự kiện Accessibility nên giữ trong RAM, chỉ ghi xuống kv khi đổi.

    var unlockedUntil: Instant? = store.get(KEY_UNLOCKED_UNTIL)?.let(Instant::parse)
        private set(value) {
            field = value
            store.put(KEY_UNLOCKED_UNTIL, value?.toString())
        }

    /** Phụ huynh đã nhập PIN: được vào Settings, cài app... tới thời điểm này. */
    var adminUntil: Instant? = store.get(KEY_ADMIN_UNTIL)?.let(Instant::parse)
        private set(value) {
            field = value
            store.put(KEY_ADMIN_UNTIL, value?.toString())
        }

    var clockTampered: Boolean = store.get(KEY_CLOCK_TAMPERED) == "1"
        private set(value) {
            field = value
            store.put(KEY_CLOCK_TAMPERED, if (value) "1" else null)
        }

    /** Phụ huynh tắt bảo vệ để gỡ app: SelfHeal không bật lại Accessibility. */
    var protectionDisabled: Boolean = store.get(KEY_PROTECTION_DISABLED) == "1"
        set(value) {
            field = value
            store.put(KEY_PROTECTION_DISABLED, if (value) "1" else null)
        }

    init {
        store.get(KEY_CONFIG)?.let { text -> runCatching { ConfigJson.parse(text) }.onSuccess { applyConfig(it) } }
        clockAnchor = store.get(KEY_CLOCK_ANCHOR)?.split(',')?.takeIf { it.size == 2 }
            ?.let { ClockGuard.Anchor(it[0].toLong(), it[1].toLong()) }
        reloadLedger()
    }

    val paired: Boolean get() = config != null && pin.isSet

    fun isAdminUnlocked(now: Instant = Instant.now()) = adminUntil?.isAfter(now) == true

    fun evaluate(now: Instant = Instant.now()): Decision {
        val r = rules
        if (r == null || !pin.isSet) return Decision.block(BlockReason.NotPaired, null)
        if (clockTampered && unlockedUntil?.isAfter(now) != true) return Decision.block(BlockReason.ClockTampered, null)
        // Giai đoạn 2 chưa có đồng bộ: offlineCapPercent của TV là 100% nên chưa cần OfflineState.
        return r.evaluate(now, ledger, Devices.TV, null, unlockedUntil)
    }

    /** Cộng [ms] giờ xem kết thúc lúc [now]; tách theo ngày nếu vắt qua 00:00. */
    fun addWatched(ms: Long, now: Instant = Instant.now()) {
        val r = rules ?: return
        for ((date, part) in r.calendar.splitByLocalDate(now, ms)) {
            ledger.add(Devices.TV, date, part)
            dirtyDates += date
        }
    }

    fun flush() {
        for (date in dirtyDates) store.saveUsage(Devices.TV, date, ledger.deviceMs(Devices.TV, date))
        dirtyDates.clear()
        clockAnchor?.let { store.put(KEY_CLOCK_ANCHOR, "${it.elapsedMs},${it.wallMs}") }
    }

    /** @return true nếu phát hiện giờ bị chỉnh tay. */
    fun checkClock(elapsedMs: Long, wallMs: Long, autoTime: Boolean): Boolean {
        val now = ClockGuard.Anchor(elapsedMs, wallMs)
        val tampered = clockGuard.tampered(clockAnchor, now, autoTime)
        clockAnchor = now
        if (tampered && !clockTampered) clockTampered = true
        return tampered
    }

    // --- Thao tác của phụ huynh (sau khi nhập PIN) ---

    fun addTime(minutes: Int, reason: String, now: Instant = Instant.now()) {
        val r = rules ?: return
        val a = Adjustment(UUID.randomUUID().toString(), r.calendar.localDate(now), minutes * RulesEngine.MINUTE_MS, reason, now.toString())
        ledger.merge(a)
        store.saveAdjustment(a)
    }

    fun unlockFor(minutes: Int, now: Instant = Instant.now()) {
        unlockedUntil = now.plusSeconds(minutes * 60L)
    }

    fun openAdmin(minutes: Int, now: Instant = Instant.now()) {
        adminUntil = now.plusSeconds(minutes * 60L)
    }

    fun lockAll() {
        unlockedUntil = null
        adminUntil = null
    }

    fun clearClockTamper() {
        clockTampered = false
        clockAnchor = null
    }

    /**
     * Ghép cặp qua ADB (SetupReceiver): đặt PIN và cấu hình.
     * [configJson] null → giữ cấu hình đang có, hoặc dùng cấu hình mặc định nếu chưa có.
     */
    fun applySetup(configJson: String?, newPin: String?) {
        val parsed = if (configJson.isNullOrBlank()) config ?: Defaults.create() else ConfigJson.parse(configJson)
        RulesEngine(parsed)   // kiểm tra cấu hình hợp lệ trước khi lưu
        store.put(KEY_CONFIG, ConfigJson.stringify(parsed))
        applyConfig(parsed)
        when {
            newPin != null -> pin.set(newPin)
            parsed.adminPinHash != null -> pin.setHash(parsed.adminPinHash)
        }
        clearClockTamper()
        protectionDisabled = false
        reloadLedger()
    }

    private fun applyConfig(c: LimitConfig) {
        config = c
        rules = RulesEngine(c)
        screenRules = ScreenRules(c, selfPackage)
        configGeneration++
    }

    private fun reloadLedger() {
        flush()
        val today = LocalDate.now(rules?.calendar?.zone ?: java.time.ZoneId.systemDefault())
        store.pruneBefore(today.minusDays(KEEP_DAYS))
        ledger = store.loadLedger(today.minusDays(KEEP_DAYS))
    }

    fun statusLines(now: Instant = Instant.now()): List<String> {
        val r = rules ?: return listOf("Chưa ghép cặp với máy tính.")
        val c = r.config
        val today = r.calendar.localDate(now)
        val weekStart = r.calendar.weekStartOf(today)
        val d = evaluate(now)
        val timeFmt = DateTimeFormatter.ofPattern("HH:mm")
        val lines = mutableListOf(
            "Hôm nay: đã xem ${DurationText.format(ledger.netUsedMs(today).coerceAtLeast(0))} / ${DurationText.format(r.calendar.dailyQuotaMinutes(today) * RulesEngine.MINUTE_MS)}" +
                "  (TV ${DurationText.format(ledger.deviceMs(Devices.TV, today))})",
            "Tuần này: đã xem ${DurationText.format(ledger.netUsedMs(weekStart, today).coerceAtLeast(0))} / ${DurationText.format(c.weeklyQuotaMinutes * RulesEngine.MINUTE_MS)}",
        )
        lines += if (d.allowed) "${d.describe()} — còn ${DurationText.format(d.leftMs, roundUp = true)}"
        else d.describe() + (d.nextChange?.let { " — tới ${r.calendar.toLocal(it).format(timeFmt)}" } ?: "")
        if (r.calendar.isHoliday(today)) lines += "Hôm nay là ngày nghỉ (không áp giờ học)."
        return lines
    }

    companion object {
        private const val KEEP_DAYS = 30L
        private const val KEY_CONFIG = "config"
        private const val KEY_UNLOCKED_UNTIL = "rules.unlockedUntil"
        private const val KEY_ADMIN_UNTIL = "admin.until"
        private const val KEY_CLOCK_TAMPERED = "clock.tampered"
        private const val KEY_CLOCK_ANCHOR = "clock.anchor"
        private const val KEY_PROTECTION_DISABLED = "protection.disabled"
    }
}
