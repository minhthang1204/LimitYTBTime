package vn.limityt.tv.ui

import android.app.Activity
import android.content.Intent
import android.os.Bundle
import android.os.SystemClock
import android.text.InputFilter
import android.text.InputType
import android.view.KeyEvent
import android.view.inputmethod.EditorInfo
import android.widget.EditText
import android.widget.TextView
import vn.limityt.tv.LimitApp
import vn.limityt.tv.guard.PinGuard
import java.time.Instant
import java.time.ZoneId
import java.time.format.DateTimeFormatter

class PinActivity : Activity() {
    private lateinit var input: EditText
    private lateinit var status: TextView

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        val page = Ui.page(this)
        page.addView(Ui.title(this, "Phụ huynh"))
        page.addView(Ui.text(this, "Nhập PIN 6 số"))
        input = EditText(this).apply {
            inputType = InputType.TYPE_CLASS_NUMBER or InputType.TYPE_NUMBER_VARIATION_PASSWORD
            filters = arrayOf(InputFilter.LengthFilter(6))
            textSize = 30f
            setTextColor(0xFFFFFFFF.toInt())
            imeOptions = EditorInfo.IME_ACTION_DONE
            setOnEditorActionListener { _, actionId, event ->
                if (actionId == EditorInfo.IME_ACTION_DONE || event?.keyCode == KeyEvent.KEYCODE_ENTER) submit()
                true
            }
        }
        page.addView(input)
        status = Ui.text(this, "", 20f, 0xFFFF8A80.toInt())
        page.addView(status)
        page.addView(Ui.button(this, "Xác nhận") { submit() })
        page.addView(Ui.button(this, "Huỷ") { finish() })
        input.requestFocus()
        showLockout()
    }

    private fun submit() {
        val pin = input.text.toString()
        input.setText("")
        when (val r = LimitApp.guardian(this).pin.verify(pin, System.currentTimeMillis())) {
            PinGuard.Result.Ok -> {
                grantedAt = SystemClock.elapsedRealtime()
                startActivity(Intent(this, AdminActivity::class.java))
                finish()
            }
            is PinGuard.Result.Wrong -> status.text = "Sai PIN. Còn ${r.attemptsLeft} lần thử."
            is PinGuard.Result.LockedOut -> showLockout()
            PinGuard.Result.NotSet -> status.text = "TV chưa được ghép cặp với máy tính."
        }
    }

    private fun showLockout() {
        val until = LimitApp.guardian(this).pin.lockedUntil(System.currentTimeMillis()) ?: return
        val time = DateTimeFormatter.ofPattern("HH:mm").format(Instant.ofEpochMilli(until).atZone(ZoneId.systemDefault()))
        status.text = "Nhập sai quá nhiều lần. Thử lại sau $time."
    }

    companion object {
        private var grantedAt = 0L

        /** AdminActivity chỉ mở được ngay sau khi nhập đúng PIN (trong vòng 10 giây). */
        fun consumeGrant(): Boolean {
            val ok = grantedAt != 0L && SystemClock.elapsedRealtime() - grantedAt < 10_000
            grantedAt = 0L
            return ok
        }
    }
}
