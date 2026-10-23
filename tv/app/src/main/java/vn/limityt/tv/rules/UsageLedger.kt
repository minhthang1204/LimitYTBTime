package vn.limityt.tv.rules

import java.time.LocalDate

object Devices {
    const val PC = "pc"
    const val TV = "tv"
}

data class UsageEntry(val device: String, val date: LocalDate, val ms: Long)

/** Điều chỉnh của phụ huynh. ms dương = cộng thêm thời gian được xem; âm = trừ (phạt). */
data class Adjustment(val id: String, val date: LocalDate, val ms: Long, val reason: String?, val createdAtUtc: String)

/**
 * Sổ ghi giờ đã dùng (docs/02-kien-truc.md §4.2). Mỗi thiết bị chỉ ghi dòng của chính nó;
 * gộp hai bản = lấy max theo (thiết bị, ngày) và hợp các adjustment theo id, nên không bao giờ xung đột.
 */
class UsageLedger {
    private val usage = HashMap<Pair<String, LocalDate>, Long>()
    private val adjustments = LinkedHashMap<String, Adjustment>()

    val entries: List<UsageEntry> get() = usage.map { (k, v) -> UsageEntry(k.first, k.second, v) }
    val allAdjustments: Collection<Adjustment> get() = adjustments.values

    fun add(device: String, date: LocalDate, ms: Long) {
        if (ms <= 0) return
        usage[device to date] = deviceMs(device, date) + ms
    }

    /** @return true nếu sổ thay đổi. */
    fun merge(entry: UsageEntry): Boolean {
        val key = entry.device to entry.date
        val current = usage[key]
        if (current != null && current >= entry.ms) return false
        usage[key] = entry.ms
        return true
    }

    /** @return true nếu là adjustment mới. */
    fun merge(adjustment: Adjustment): Boolean {
        if (adjustment.id in adjustments) return false
        adjustments[adjustment.id] = adjustment
        return true
    }

    fun deviceMs(device: String, date: LocalDate): Long = usage[device to date] ?: 0

    fun deviceMs(device: String, from: LocalDate, to: LocalDate): Long =
        usage.entries.filter { it.key.first == device && it.key.second.inRange(from, to) }.sumOf { it.value }

    fun totalMs(from: LocalDate, to: LocalDate): Long =
        usage.entries.filter { it.key.second.inRange(from, to) }.sumOf { it.value }

    fun adjustmentMs(from: LocalDate, to: LocalDate): Long =
        adjustments.values.filter { it.date.inRange(from, to) }.sumOf { it.ms }

    /** Giờ đã dùng sau khi trừ adjustment. Có thể âm (khi phụ huynh cộng thêm giờ) — không kẹp ở đây. */
    fun netUsedMs(from: LocalDate, to: LocalDate): Long = totalMs(from, to) - adjustmentMs(from, to)

    fun netUsedMs(date: LocalDate): Long = netUsedMs(date, date)

    private fun LocalDate.inRange(from: LocalDate, to: LocalDate) = !isBefore(from) && !isAfter(to)
}
