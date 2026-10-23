package vn.limityt.tv.rules

import java.time.DayOfWeek
import java.time.Instant
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.LocalTime
import java.time.ZoneId
import java.time.temporal.ChronoUnit

/** Tên ngày trong cấu hình: "Mon".."Sun". */
object DayNames {
    private val names = listOf("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")

    fun parse(name: String): DayOfWeek {
        val i = names.indexOfFirst { it.equals(name.trim(), ignoreCase = true) }
        require(i >= 0) { "Tên ngày không hợp lệ: '$name' (dùng Mon..Sun)" }
        return DayOfWeek.of(i + 1)
    }
}

object TimeOfDay {
    private val pattern = Regex("""^(\d{2}):(\d{2})$""")

    fun parse(s: String): LocalTime {
        val m = requireNotNull(pattern.matchEntire(s)) { "Giờ '$s' sai định dạng HH:mm." }
        return LocalTime.of(m.groupValues[1].toInt(), m.groupValues[2].toInt())
    }
}

/**
 * Các phép tính lịch theo giờ địa phương: ngày nghỉ, đầu tuần, khung giờ cấm.
 * Port của LocalCalendar.cs — phải cho cùng kết quả trên spec/rules-test-vectors.json.
 */
class LocalCalendar(private val config: LimitConfig) {
    private data class Window(val days: Set<DayOfWeek>, val from: LocalTime, val to: LocalTime, val schoolOnly: Boolean)

    val zone: ZoneId = ZoneId.of(config.timeZone)
    private val weekStart = DayNames.parse(config.weekStart)
    private val windows = config.blockedWindows.map { w ->
        Window(w.days.map(DayNames::parse).toSet(), TimeOfDay.parse(w.from), TimeOfDay.parse(w.to), w.schoolOnly)
    }
    private val holidays = config.holidays.map { LocalDate.parse(it.from) to LocalDate.parse(it.to) }
    private val overrides = config.holidayOverrides.mapKeys { LocalDate.parse(it.key) }

    fun toLocal(instant: Instant): LocalDateTime = LocalDateTime.ofInstant(instant, zone)

    fun toUtc(local: LocalDateTime): Instant = local.atZone(zone).toInstant()

    fun localDate(instant: Instant): LocalDate = toLocal(instant).toLocalDate()

    fun isHoliday(date: LocalDate): Boolean =
        overrides[date] ?: holidays.any { (from, to) -> !date.isBefore(from) && !date.isAfter(to) }

    fun weekStartOf(date: LocalDate): LocalDate =
        date.minusDays(((date.dayOfWeek.value - weekStart.value + 7) % 7).toLong())

    fun dailyQuotaMinutes(date: LocalDate): Int =
        config.holidayDailyQuotaMinutes?.takeIf { isHoliday(date) } ?: config.dailyQuotaMinutes

    /** Nếu [t] nằm trong khung giờ cấm, trả về thời điểm hết cấm (tính cả các khung nối tiếp nhau). */
    fun activeWindowEnd(t: LocalDateTime): LocalDateTime? {
        var result: LocalDateTime? = null
        var probe = t
        repeat(32) {
            var end: LocalDateTime? = null
            for ((start, e) in instancesStartingOn(probe.toLocalDate().minusDays(1), 2)) {
                if (!start.isAfter(probe) && probe.isBefore(e) && (end == null || e.isAfter(end))) end = e
            }
            val found = end ?: return result
            result = found
            probe = found
        }
        return result
    }

    /** Thời điểm bắt đầu khung giờ cấm tiếp theo sau [t] (tìm trong 8 ngày tới). */
    fun nextWindowStart(t: LocalDateTime): LocalDateTime? =
        instancesStartingOn(t.toLocalDate(), 9).map { it.first }.filter { it.isAfter(t) }.minOrNull()

    private fun instancesStartingOn(firstDay: LocalDate, dayCount: Int): Sequence<Pair<LocalDateTime, LocalDateTime>> = sequence {
        for (i in 0 until dayCount) {
            val day = firstDay.plusDays(i.toLong())
            val midnight = day.atStartOfDay()
            for (w in windows) {
                if (day.dayOfWeek !in w.days) continue
                if (w.schoolOnly && isHoliday(day)) continue
                val start = midnight.plusSeconds(w.from.toSecondOfDay().toLong())
                val end = if (w.to.isAfter(w.from)) midnight.plusSeconds(w.to.toSecondOfDay().toLong())
                else midnight.plusDays(1).plusSeconds(w.to.toSecondOfDay().toLong())
                yield(start to end)
            }
        }
    }

    /** Tách một khoảng dài [ms] kết thúc lúc [end] theo ngày địa phương. */
    fun splitByLocalDate(end: Instant, ms: Long): List<Pair<LocalDate, Long>> {
        val result = mutableListOf<Pair<LocalDate, Long>>()
        val localEnd = toLocal(end)
        var cursor = localEnd.minus(ms, ChronoUnit.MILLIS)
        while (cursor.isBefore(localEnd)) {
            val nextMidnight = cursor.toLocalDate().plusDays(1).atStartOfDay()
            val segmentEnd = if (nextMidnight.isBefore(localEnd)) nextMidnight else localEnd
            result += cursor.toLocalDate() to ChronoUnit.MILLIS.between(cursor, segmentEnd)
            cursor = segmentEnd
        }
        return result
    }
}
