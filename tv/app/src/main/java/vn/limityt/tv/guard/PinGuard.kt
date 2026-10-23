package vn.limityt.tv.guard

import java.security.MessageDigest
import java.security.SecureRandom
import java.util.Base64
import javax.crypto.SecretKeyFactory
import javax.crypto.spec.PBEKeySpec

/** Kho key-value đơn giản (bảng kv trong SQLite; bản in-memory khi test). */
interface KeyValue {
    fun get(key: String): String?
    fun put(key: String, value: String?)
}

/**
 * PIN phụ huynh 6 số, lưu dạng PBKDF2 (docs/02-kien-truc.md §7.1, case C6).
 * Sai 5 lần liên tiếp → khoá 15 phút, mỗi lần khoá sau gấp đôi lần trước.
 * Định dạng hash: "pbkdf2-sha256$<vòng lặp>$<salt base64>$<hash base64>" — PC dùng cùng định dạng ở Giai đoạn 3.
 */
class PinGuard(private val kv: KeyValue) {
    sealed interface Result {
        data object Ok : Result
        data class Wrong(val attemptsLeft: Int) : Result
        data class LockedOut(val untilMs: Long) : Result
        data object NotSet : Result
    }

    val isSet: Boolean get() = kv.get(KEY_HASH) != null

    fun set(pin: String) {
        require(isValid(pin)) { "PIN phải gồm 6 chữ số" }
        kv.put(KEY_HASH, hash(pin))
        reset()
    }

    /** Dùng khi PC gửi sang hash sẵn (Giai đoạn 3). */
    fun setHash(hash: String) {
        require(hash.startsWith("$SCHEME$")) { "Định dạng hash PIN không hợp lệ" }
        kv.put(KEY_HASH, hash)
        reset()
    }

    fun lockedUntil(nowMs: Long): Long? = kv.get(KEY_LOCKED_UNTIL)?.toLongOrNull()?.takeIf { it > nowMs }

    fun verify(pin: String, nowMs: Long): Result {
        val stored = kv.get(KEY_HASH) ?: return Result.NotSet
        lockedUntil(nowMs)?.let { return Result.LockedOut(it) }

        if (matches(pin, stored)) {
            reset()
            return Result.Ok
        }

        val failures = (kv.get(KEY_FAILURES)?.toIntOrNull() ?: 0) + 1
        if (failures < MAX_ATTEMPTS) {
            kv.put(KEY_FAILURES, failures.toString())
            return Result.Wrong(MAX_ATTEMPTS - failures)
        }
        val lockouts = kv.get(KEY_LOCKOUTS)?.toIntOrNull() ?: 0
        val until = nowMs + LOCKOUT_MS * (1L shl lockouts.coerceAtMost(10))
        kv.put(KEY_FAILURES, "0")
        kv.put(KEY_LOCKOUTS, (lockouts + 1).toString())
        kv.put(KEY_LOCKED_UNTIL, until.toString())
        return Result.LockedOut(until)
    }

    private fun reset() {
        kv.put(KEY_FAILURES, "0")
        kv.put(KEY_LOCKOUTS, "0")
        kv.put(KEY_LOCKED_UNTIL, null)
    }

    companion object {
        const val MAX_ATTEMPTS = 5
        const val LOCKOUT_MS = 15 * 60_000L
        private const val SCHEME = "pbkdf2-sha256"
        private const val ITERATIONS = 60_000
        private const val KEY_HASH = "pin.hash"
        private const val KEY_FAILURES = "pin.failures"
        private const val KEY_LOCKOUTS = "pin.lockouts"
        private const val KEY_LOCKED_UNTIL = "pin.lockedUntil"

        fun isValid(pin: String) = pin.length == 6 && pin.all { it in '0'..'9' }

        fun hash(pin: String, salt: ByteArray = ByteArray(16).also { SecureRandom().nextBytes(it) }, iterations: Int = ITERATIONS): String {
            val enc = Base64.getEncoder()
            return "$SCHEME$$iterations$${enc.encodeToString(salt)}$${enc.encodeToString(derive(pin, salt, iterations))}"
        }

        fun matches(pin: String, stored: String): Boolean {
            val parts = stored.split('$')
            if (parts.size != 4 || parts[0] != SCHEME) return false
            val iterations = parts[1].toIntOrNull() ?: return false
            val dec = Base64.getDecoder()
            val expected = dec.decode(parts[3])
            return MessageDigest.isEqual(expected, derive(pin, dec.decode(parts[2]), iterations))
        }

        private fun derive(pin: String, salt: ByteArray, iterations: Int): ByteArray =
            SecretKeyFactory.getInstance("PBKDF2WithHmacSHA256")
                .generateSecret(PBEKeySpec(pin.toCharArray(), salt, iterations, 256)).encoded
    }
}

/**
 * Phát hiện giờ hệ thống bị chỉnh (docs/02-kien-truc.md §7.3): so độ trôi của giờ thực với đồng hồ
 * đơn điệu `elapsedRealtime`. Khi bật "giờ tự động", giờ chỉ đổi do đồng bộ mạng → không coi là chỉnh.
 */
class ClockGuard(private val maxDriftMs: Long = 2 * 60_000L) {
    data class Anchor(val elapsedMs: Long, val wallMs: Long)

    /** @return true nếu giờ bị chỉnh tay kể từ [previous]. */
    fun tampered(previous: Anchor?, now: Anchor, autoTime: Boolean): Boolean {
        if (previous == null || now.elapsedMs < previous.elapsedMs) return false   // lần đầu hoặc vừa khởi động lại
        val drift = (now.wallMs - previous.wallMs) - (now.elapsedMs - previous.elapsedMs)
        return Math.abs(drift) > maxDriftMs && !autoTime
    }
}
