package vn.limityt.tv.ui

import android.app.Activity
import android.content.Context
import android.content.Intent
import android.os.Bundle
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter

/** Màn hình thông báo sau khi bị đẩy về Home. Chuyển sang HDMI/camera vẫn bình thường. */
class BlockActivity : Activity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        render(intent)
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        render(intent)
    }

    private fun render(intent: Intent) {
        val page = Ui.page(this)
        page.addView(Ui.title(this, "⏸  Tạm dừng"))
        page.addView(Ui.text(this, intent.getStringExtra(EXTRA_MESSAGE) ?: "Không được phép mở mục này", 26f, 0xFFFFFFFF.toInt()))
        intent.getStringExtra(EXTRA_UNTIL)?.let { page.addView(Ui.text(this, "Xem lại được lúc $it")) }
        val ok = Ui.button(this, "Về màn hình chính") { finish() }
        page.addView(ok)
        page.addView(Ui.button(this, "Phụ huynh nhập PIN") {
            startActivity(Intent(this, PinActivity::class.java))
            finish()
        })
        ok.requestFocus()
    }

    companion object {
        private const val EXTRA_MESSAGE = "message"
        private const val EXTRA_UNTIL = "until"

        fun intent(context: Context, message: String, untilText: String?): Intent =
            Intent(context, BlockActivity::class.java)
                .addFlags(Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_NO_ANIMATION)
                .putExtra(EXTRA_MESSAGE, message)
                .putExtra(EXTRA_UNTIL, untilText)

        fun formatUntil(t: LocalDateTime): String {
            val time = t.format(DateTimeFormatter.ofPattern("HH:mm"))
            val today = LocalDate.now()
            return when (t.toLocalDate()) {
                today -> time
                today.plusDays(1) -> "$time ngày mai"
                else -> time + " " + t.format(DateTimeFormatter.ofPattern("dd/MM"))
            }
        }
    }
}
