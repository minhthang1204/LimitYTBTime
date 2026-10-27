package vn.limityt.tv.sync

import kotlinx.serialization.Serializable

@Serializable
data class SyncPacket(
    val deviceId: String,
    val sentAtUtc: String,
    val configVersion: Int = 0,
    val configJson: String? = null,
    val usage: List<UsageItem> = emptyList(),
    val adjustments: List<AdjustmentItem> = emptyList()
)

@Serializable
data class UsageItem(
    val device: String,
    val date: String,
    val ms: Long
)

@Serializable
data class AdjustmentItem(
    val id: String,
    val date: String,
    val ms: Long,
    val reason: String? = null,
    val createdAtUtc: String
)
