package vn.limityt.tv.ui

import android.app.Activity
import android.content.Intent
import vn.limityt.tv.LimitApp
import vn.limityt.tv.heal.AccessibilityToggle
import vn.limityt.tv.heal.SelfHealJob

/** Biểu tượng LimitYT trên launcher: xem còn bao nhiêu giờ, và lối vào menu phụ huynh. */
class MainActivity : Activity() {
    override fun onResume() {
        super.onResume()
        SelfHealJob.heal(this)
        val g = LimitApp.guardian(this)
        val page = Ui.page(this)
        page.addView(Ui.title(this, "LimitYT"))
        g.statusLines().forEach { page.addView(Ui.text(this, it)) }
        when {
            g.protectionDisabled -> page.addView(Ui.text(this, "Bảo vệ đang TẮT", 22f, 0xFFFF8A80.toInt()))
            !AccessibilityToggle.isEnabled(this) ->
                page.addView(Ui.text(this, "Dịch vụ Trợ năng của LimitYT chưa bật — chạy lại install-tv.ps1", 22f, 0xFFFF8A80.toInt()))
        }
        val close = Ui.button(this, "Đóng") { finish() }
        page.addView(close)
        page.addView(Ui.button(this, "Phụ huynh (PIN)") { startActivity(Intent(this, PinActivity::class.java)) })
        close.requestFocus()
    }
}
