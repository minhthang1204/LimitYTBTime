package vn.limityt.tv.heal

import android.Manifest
import android.content.ComponentName
import android.content.Context
import android.content.pm.PackageManager
import android.provider.Settings
import vn.limityt.tv.guard.GuardAccessibilityService

/**
 * Bật/tắt GuardAccessibilityService bằng quyền WRITE_SECURE_SETTINGS
 * (cấp một lần qua ADB lúc cài: `pm grant <pkg> android.permission.WRITE_SECURE_SETTINGS`).
 */
object AccessibilityToggle {
    fun component(context: Context) = ComponentName(context, GuardAccessibilityService::class.java)

    fun canWrite(context: Context) =
        context.checkSelfPermission(Manifest.permission.WRITE_SECURE_SETTINGS) == PackageManager.PERMISSION_GRANTED

    fun isEnabled(context: Context): Boolean {
        val enabled = Settings.Secure.getString(context.contentResolver, Settings.Secure.ENABLED_ACCESSIBILITY_SERVICES) ?: return false
        return parse(enabled).any { it == component(context) }
    }

    /** @return true nếu service đang bật (sẵn hoặc vừa bật lại). */
    fun ensureEnabled(context: Context): Boolean {
        if (isEnabled(context)) return true
        if (!canWrite(context)) return false
        val resolver = context.contentResolver
        val current = parse(Settings.Secure.getString(resolver, Settings.Secure.ENABLED_ACCESSIBILITY_SERVICES))
        val updated = (current + component(context)).distinct().joinToString(":") { it.flattenToString() }
        Settings.Secure.putString(resolver, Settings.Secure.ENABLED_ACCESSIBILITY_SERVICES, updated)
        Settings.Secure.putInt(resolver, Settings.Secure.ACCESSIBILITY_ENABLED, 1)
        return isEnabled(context)
    }

    fun disable(context: Context): Boolean {
        if (!canWrite(context)) return false
        val resolver = context.contentResolver
        val current = parse(Settings.Secure.getString(resolver, Settings.Secure.ENABLED_ACCESSIBILITY_SERVICES))
        val updated = current.filter { it != component(context) }
        Settings.Secure.putString(resolver, Settings.Secure.ENABLED_ACCESSIBILITY_SERVICES, updated.joinToString(":") { it.flattenToString() })
        return true
    }

    private fun parse(value: String?): List<ComponentName> =
        value.orEmpty().split(':').filter { it.isNotBlank() }.mapNotNull(ComponentName::unflattenFromString)
}
