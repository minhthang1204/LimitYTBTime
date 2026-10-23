package vn.limityt.tv.ui

import android.app.Activity
import android.os.Bundle
import android.widget.Toast
import vn.limityt.tv.LimitApp
import vn.limityt.tv.heal.AccessibilityToggle

/** Menu của phụ huynh, chỉ mở được ngay sau khi nhập đúng PIN. Rời màn hình là đóng luôn. */
class AdminActivity : Activity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (!PinActivity.consumeGrant()) return finish()
        render()
    }

    override fun onStop() {
        super.onStop()
        finish()
    }

    private fun render() {
        val g = LimitApp.guardian(this)
        val page = Ui.page(this)
        page.addView(Ui.title(this, "Phụ huynh"))
        g.statusLines().forEach { page.addView(Ui.text(this, it, 20f)) }

        fun action(label: String, done: String, block: () -> Unit) = page.addView(Ui.button(this, label) {
            block()
            Toast.makeText(this, done, Toast.LENGTH_LONG).show()
            finish()
        })

        action("Mở Cài đặt / cài ứng dụng trong 10 phút", "Đã mở khoá Cài đặt 10 phút") { g.openAdmin(10) }
        action("Cho xem thêm 15 phút hôm nay", "Đã cộng 15 phút") { g.addTime(15, "TV +15m") }
        action("Cho xem thêm 30 phút hôm nay", "Đã cộng 30 phút") { g.addTime(30, "TV +30m") }
        action("Bỏ mọi chặn trong 1 giờ (vẫn đếm giờ)", "Đã bỏ chặn 1 giờ") { g.unlockFor(60) }
        if (g.unlockedUntil != null || g.adminUntil != null)
            action("Khoá lại ngay", "Đã khoá lại") { g.lockAll() }
        if (g.clockTampered)
            action("Giờ trên TV đã đúng — bỏ khoá", "Đã bỏ khoá chỉnh giờ") { g.clearClockTamper() }
        if (g.protectionDisabled)
            action("Bật lại bảo vệ", "Đã bật lại bảo vệ") {
                g.protectionDisabled = false
                AccessibilityToggle.ensureEnabled(this)
            }
        else
            action("Tắt bảo vệ (để gỡ cài đặt)", "Đã tắt bảo vệ. Gỡ app bằng uninstall-tv.ps1") {
                g.protectionDisabled = true
                g.openAdmin(30)
                AccessibilityToggle.disable(this)
            }
        page.addView(Ui.button(this, "Đóng") { finish() })
    }
}
