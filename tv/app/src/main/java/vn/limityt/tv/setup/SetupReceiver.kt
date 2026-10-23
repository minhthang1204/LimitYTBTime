package vn.limityt.tv.setup

import android.app.Activity
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.util.Base64
import vn.limityt.tv.LimitApp
import vn.limityt.tv.guard.PinGuard
import vn.limityt.tv.heal.AccessibilityToggle

/**
 * Ghép cặp / cập nhật cấu hình qua ADB (tv/install/install-tv.ps1):
 *
 *   adb shell am broadcast -n vn.limityt.tv/.setup.SetupReceiver -a vn.limityt.tv.SETUP \
 *       --es pin 123456 [--es config <JSON base64>]
 *
 * Receiver yêu cầu quyền android.permission.DUMP: chỉ `adb shell` (và hệ thống) có quyền này,
 * app thường không xin được, nên không app nào khác trên TV gửi được lệnh ghép cặp.
 */
class SetupReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        val result = runCatching {
            when (intent.action) {
                ACTION_SETUP -> setup(context, intent)
                ACTION_STATUS -> LimitApp.guardian(context).let { g ->
                    "paired=${g.paired} accessibility=${AccessibilityToggle.isEnabled(context)}\n" + g.statusLines().joinToString("\n")
                }
                else -> error("Lệnh không hợp lệ: ${intent.action}")
            }
        }
        if (result.isSuccess) {
            resultCode = Activity.RESULT_OK
            resultData = result.getOrThrow()
        } else {
            resultCode = Activity.RESULT_CANCELED
            resultData = "ERROR: " + result.exceptionOrNull()?.message
        }
    }

    private fun setup(context: Context, intent: Intent): String {
        val pin = intent.getStringExtra("pin")
        require(pin == null || PinGuard.isValid(pin)) { "PIN phải gồm 6 chữ số" }
        val config = intent.getStringExtra("config")?.let { String(Base64.decode(it, Base64.DEFAULT), Charsets.UTF_8) }

        val guardian = LimitApp.guardian(context)
        require(pin != null || guardian.pin.isSet) { "Lần ghép cặp đầu tiên cần --es pin" }
        guardian.applySetup(config, pin)

        val accessibility = AccessibilityToggle.ensureEnabled(context)
        return "OK paired=${guardian.paired} accessibility=$accessibility writeSecureSettings=${AccessibilityToggle.canWrite(context)}"
    }

    companion object {
        const val ACTION_SETUP = "vn.limityt.tv.SETUP"
        const val ACTION_STATUS = "vn.limityt.tv.STATUS"
    }
}
