package vn.limityt.tv.rules

import java.time.Duration
import java.time.Instant

enum class BlockReason {
    None,
    BlockedWindow,
    DailyQuota,
    WeeklyQuota,
    OfflineCap,

    // Chỉ có trên TV (docs/02-kien-truc.md §7.3, §7.4).
    NotPaired,
    ClockTampered,
}

/**
 * @param leftMs thời gian còn được xem liên tục (khi allowed).
 * @param nextChange khi bị chặn: thời điểm hết chặn, null nếu không xác định.
 */
data class Decision(
    val allowed: Boolean,
    val reason: BlockReason,
    val leftMs: Long,
    val nextChange: Instant?,
    val unlocked: Boolean = false,
) {
    fun describe(): String = when (reason) {
        BlockReason.BlockedWindow -> "Đang trong giờ cấm"
        BlockReason.DailyQuota -> "Đã hết giờ xem hôm nay"
        BlockReason.WeeklyQuota -> "Đã hết giờ xem tuần này"
        BlockReason.OfflineCap -> "Chưa đồng bộ được với máy tính, tạm hết giờ"
        BlockReason.NotPaired -> "Cần kết nối lại với máy tính"
        BlockReason.ClockTampered -> "Giờ trên TV vừa bị thay đổi, cần phụ huynh nhập PIN"
        BlockReason.None -> if (unlocked) "Phụ huynh đang mở khoá tạm thời" else "Được phép xem"
    }

    companion object {
        fun block(reason: BlockReason, until: Instant?) = Decision(false, reason, 0, until)
    }
}

/** Trạng thái khi không đồng bộ được với thiết bị kia (docs/01-phan-tich.md §8). */
data class OfflineState(val budgetMs: Long, val selfWeekUsedAtStartMs: Long)

/** Quy tắc chung cho PC và TV (docs/02-kien-truc.md §5). Port của RulesEngine.cs. */
class RulesEngine(val config: LimitConfig) {
    val calendar = LocalCalendar(config)

    fun evaluate(
        now: Instant,
        ledger: UsageLedger,
        selfDevice: String,
        offline: OfflineState? = null,
        unlockedUntil: Instant? = null,
    ): Decision {
        if (unlockedUntil != null && unlockedUntil.isAfter(now))
            return Decision(true, BlockReason.None, Duration.between(now, unlockedUntil).toMillis(), null, unlocked = true)

        val local = calendar.toLocal(now)
        calendar.activeWindowEnd(local)?.let { return Decision.block(BlockReason.BlockedWindow, calendar.toUtc(it)) }

        val today = local.toLocalDate()
        val weekStart = calendar.weekStartOf(today)

        val weeklyLeft = config.weeklyQuotaMinutes * MINUTE_MS - ledger.netUsedMs(weekStart, today)
        if (weeklyLeft <= 0)
            return Decision.block(BlockReason.WeeklyQuota, calendar.toUtc(weekStart.plusDays(7).atStartOfDay()))

        val dailyLeft = calendar.dailyQuotaMinutes(today) * MINUTE_MS - ledger.netUsedMs(today)
        if (dailyLeft <= 0)
            return Decision.block(BlockReason.DailyQuota, calendar.toUtc(today.plusDays(1).atStartOfDay()))

        var left = minOf(weeklyLeft, dailyLeft)

        if (offline != null) {
            val selfUsedSinceOffline = ledger.deviceMs(selfDevice, weekStart, today) - offline.selfWeekUsedAtStartMs
            val offlineLeft = offline.budgetMs - selfUsedSinceOffline
            if (offlineLeft <= 0) return Decision.block(BlockReason.OfflineCap, null)
            left = minOf(left, offlineLeft)
        }

        calendar.nextWindowStart(local)?.let { left = minOf(left, Duration.between(local, it).toMillis()) }

        return Decision(true, BlockReason.None, left, null)
    }

    companion object {
        const val MINUTE_MS = 60_000L
    }
}

/**
 * Quyết định khi nào hiện cảnh báo "còn N phút" (F9, case A7):
 * khi thời gian còn lại đi qua một ngưỡng, hoặc khi bắt đầu xem mà đã ở dưới ngưỡng lớn nhất.
 */
class WarningTracker {
    private var lastLeftMinutes: Int? = null
    private var wasWatching = false

    /** @return số phút cần báo, hoặc null nếu không cần báo. */
    fun next(decision: Decision, watching: Boolean, thresholds: Collection<Int>): Int? {
        if (!decision.allowed || thresholds.isEmpty()) {
            lastLeftMinutes = null
            wasWatching = watching
            return null
        }

        val left = Math.ceil(decision.leftMs / 60_000.0).toInt()
        var warn: Int? = null

        val previous = lastLeftMinutes
        if (watching && !wasWatching && left <= thresholds.max()) warn = left
        else if (previous != null) warn = thresholds.filter { previous > it && left <= it }.minOrNull()

        lastLeftMinutes = left
        wasWatching = watching
        return warn
    }
}

object DurationText {
    /** 95 phút → "1 giờ 35 phút". [roundUp] để "còn 29,2 phút" hiện thành "còn 30 phút". */
    fun format(ms: Long, roundUp: Boolean = false): String {
        val minutes = if (roundUp) Math.ceil(ms / 60_000.0).toLong() else ms / 60_000
        val h = minutes / 60
        val m = minutes % 60
        return when {
            h == 0L -> "$m phút"
            m == 0L -> "$h giờ"
            else -> "$h giờ $m phút"
        }
    }
}
