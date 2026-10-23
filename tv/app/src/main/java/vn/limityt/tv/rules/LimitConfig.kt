package vn.limityt.tv.rules

import kotlinx.serialization.ExperimentalSerializationApi
import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json

/** Cấu hình do PC sở hữu (docs/02-kien-truc.md §4.1). Cùng định dạng JSON với LimitConfig bên C#. */
@Serializable
data class LimitConfig(
    val version: Int = 1,
    val updatedAtUtc: String? = null,
    val timeZone: String = "Asia/Ho_Chi_Minh",
    val weekStart: String = "Mon",
    val weeklyQuotaMinutes: Int = 420,
    val dailyQuotaMinutes: Int = 90,
    val holidayDailyQuotaMinutes: Int? = null,
    val blockedWindows: List<BlockedWindow> = emptyList(),
    val holidays: List<HolidayRange> = emptyList(),
    /** Bật/tắt tay theo ngày "yyyy-MM-dd": true = nghỉ, false = đi học. */
    val holidayOverrides: Map<String, Boolean> = emptyMap(),
    val warnAtMinutesLeft: List<Int> = listOf(15, 5),
    val offlineCapPercent: Map<String, Int> = emptyMap(),
    val services: List<LimitedService> = emptyList(),
    val titleExcludePatterns: List<String> = emptyList(),
    val tv: TvConfig = TvConfig(),
    val adminPinHash: String? = null,
)

@Serializable
data class BlockedWindow(
    val days: List<String> = emptyList(),
    val from: String = "00:00",
    val to: String = "00:00",
    /** Khung "giờ học": bị bỏ qua vào ngày nghỉ. */
    val schoolOnly: Boolean = false,
)

@Serializable
data class HolidayRange(
    val from: String = "",
    val to: String = "",
    val note: String? = null,
)

@Serializable
data class LimitedService(
    val id: String = "",
    val domains: List<String> = emptyList(),
    val titlePatterns: List<String> = emptyList(),
    val tvPackages: List<String> = emptyList(),
    val pcExecutables: List<String> = emptyList(),
)

/**
 * Các danh sách ở đây được **hợp** với danh sách có sẵn trong app (xem ScreenRules),
 * nên cấu hình từ PC chỉ thêm được, không bỏ được mục có sẵn.
 */
@Serializable
data class TvConfig(
    val enabled: Boolean = false,
    val host: String? = null,
    val port: Int = 5555,
    val guardedSettingsPackages: List<String> = emptyList(),
    val neverBlockPackages: List<String> = emptyList(),
    val installerPackages: List<String> = emptyList(),
    val cleanerPackages: List<String> = emptyList(),
    val browserPackages: List<String> = emptyList(),
    val guardedKeywords: List<String> = emptyList(),
)

object ConfigJson {
    @OptIn(ExperimentalSerializationApi::class)
    val format = Json {
        ignoreUnknownKeys = true
        isLenient = true
        explicitNulls = false
        encodeDefaults = true
        coerceInputValues = true
        allowTrailingComma = true
        allowComments = true
    }

    fun parse(text: String): LimitConfig = format.decodeFromString(LimitConfig.serializer(), text)

    fun stringify(config: LimitConfig): String = format.encodeToString(LimitConfig.serializer(), config)
}
