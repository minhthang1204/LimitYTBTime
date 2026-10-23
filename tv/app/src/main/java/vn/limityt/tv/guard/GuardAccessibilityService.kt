package vn.limityt.tv.guard

import android.accessibilityservice.AccessibilityService
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.graphics.PixelFormat
import android.graphics.drawable.GradientDrawable
import android.os.Handler
import android.os.Looper
import android.os.PowerManager
import android.os.SystemClock
import android.provider.Settings
import android.view.Gravity
import android.view.WindowManager
import android.view.accessibility.AccessibilityEvent
import android.view.accessibility.AccessibilityNodeInfo
import android.widget.TextView
import vn.limityt.tv.LimitApp
import vn.limityt.tv.data.Guardian
import vn.limityt.tv.rules.Decision
import vn.limityt.tv.rules.WarningTracker
import vn.limityt.tv.ui.BlockActivity
import java.io.FileDescriptor
import java.io.PrintWriter

/**
 * Đo giờ xem và chặn trên TV (docs/02-kien-truc.md §7.1, §7.2).
 * Mọi thứ chạy trên main thread: sự kiện Accessibility + một tick mỗi 5 giây.
 */
class GuardAccessibilityService : AccessibilityService() {
    private val handler = Handler(Looper.getMainLooper())
    private lateinit var guardian: Guardian
    private lateinit var power: PowerManager
    private lateinit var overlay: WarningOverlay
    private val warnings = WarningTracker()
    private val labels = HashMap<String, String?>()

    private var currentPkg: String? = null
    private var currentScreen: Screen = Screen.Normal
    private var watching = false
    private var lastTickElapsed = 0L
    private var lastFlushElapsed = 0L
    private var lastKickElapsed = 0L
    private var lastContentScanElapsed = 0L
    private var generation = -1

    private val tick = object : Runnable {
        override fun run() {
            try {
                onTick()
            } finally {
                handler.removeCallbacks(this)
                handler.postDelayed(this, TICK_MS)
            }
        }
    }

    private val screenReceiver = object : BroadcastReceiver() {
        override fun onReceive(context: Context, intent: Intent) {
            if (intent.action == Intent.ACTION_SCREEN_OFF) guardian.flush()
            // Màn hình bật lại: kiểm tra ngay app đang mở (YouTube có thể đã mở sẵn trong lúc bị chặn).
            else handler.post(tick)
        }
    }

    override fun onServiceConnected() {
        guardian = LimitApp.guardian(this)
        power = getSystemService(PowerManager::class.java)
        overlay = WarningOverlay(this)
        registerReceiver(screenReceiver, IntentFilter().apply {
            addAction(Intent.ACTION_SCREEN_OFF)
            addAction(Intent.ACTION_SCREEN_ON)
        })
        lastTickElapsed = SystemClock.elapsedRealtime()
        lastFlushElapsed = lastTickElapsed
        handler.post(tick)
    }

    override fun onAccessibilityEvent(event: AccessibilityEvent) {
        if (!::guardian.isInitialized) return
        val eventPkg = event.packageName?.toString() ?: return
        val contentChanged = event.eventType == AccessibilityEvent.TYPE_WINDOW_CONTENT_CHANGED

        // Đường nhanh: nội dung trong app đang mở thay đổi (ví dụ thanh tiến trình video) mà app đó
        // phân loại được chỉ theo package → không cần đọc lại màn hình.
        if (contentChanged && eventPkg == currentPkg && !needsContent(eventPkg)) return
        if (contentChanged && SystemClock.elapsedRealtime() - lastContentScanElapsed < CONTENT_SCAN_MS) return

        // Sự kiện có thể đến từ cửa sổ phụ (bàn phím, toast): lấy package của cửa sổ đang active.
        val root = rootInActiveWindow
        try {
            inspect(root?.packageName?.toString() ?: eventPkg, root)
        } finally {
            recycle(root)
        }
    }

    override fun onInterrupt() {}

    override fun onDestroy() {
        handler.removeCallbacksAndMessages(null)
        if (::guardian.isInitialized) {
            runCatching { unregisterReceiver(screenReceiver) }
            overlay.hide()
            guardian.flush()
        }
        super.onDestroy()
    }

    private fun onTick() {
        val nowElapsed = SystemClock.elapsedRealtime()
        val delta = (nowElapsed - lastTickElapsed).coerceIn(0, MAX_DELTA_MS)   // TV ngủ thì không bị cộng giờ
        lastTickElapsed = nowElapsed
        val interactive = power.isInteractive

        if (interactive) {
            guardian.checkClock(nowElapsed, System.currentTimeMillis(), autoTimeEnabled())
            // Chỉ dựa vào sự kiện thì có thể bỏ sót: kiểm tra lại app foreground mỗi tick.
            val root = rootInActiveWindow
            try {
                val pkg = root?.packageName?.toString()
                if (pkg != null && (pkg != currentPkg || needsContent(pkg))) inspect(pkg, root, enforce = false)
            } finally {
                recycle(root)
            }
        }

        val nowWatching = interactive && currentScreen is Screen.Limited
        if (nowWatching) guardian.addWatched(delta)

        val decision = guardian.evaluate()
        if (nowWatching && !decision.allowed) kick(decision)
        val screen = currentScreen
        if (interactive && screen is Screen.NeedsPin && needsPin()) kick(screen.message, null)

        warnings.next(decision, nowWatching, guardian.config?.warnAtMinutesLeft.orEmpty())?.let {
            overlay.show("Còn $it phút xem YouTube")
        }

        if ((watching && !nowWatching) || nowElapsed - lastFlushElapsed >= FLUSH_MS) {
            guardian.flush()
            lastFlushElapsed = nowElapsed
        }
        watching = nowWatching
    }

    private fun inspect(pkg: String, root: AccessibilityNodeInfo?, enforce: Boolean = true) {
        if (generation != guardian.configGeneration) {
            generation = guardian.configGeneration
            labels.clear()
        }
        val rules = guardian.screenRules
        val kind = rules.classifyPackage(pkg, label(pkg))
        if (kind.needsContent) lastContentScanElapsed = SystemClock.elapsedRealtime()
        val screen = rules.resolve(kind) { collectTexts(root) }
        currentPkg = pkg
        currentScreen = screen

        if (!enforce) return   // tick tự xử lý ngay sau đó
        when (screen) {
            is Screen.Limited -> guardian.evaluate().let { if (!it.allowed) kick(it) }
            is Screen.NeedsPin -> if (needsPin()) kick(screen.message, null)
            Screen.NeverBlock, Screen.Normal -> {}
        }
    }

    private fun needsContent(pkg: String) =
        guardian.screenRules.classifyPackage(pkg, label(pkg)).needsContent

    /** Chưa ghép cặp thì không có PIN để nhập: không khoá Settings (YouTube vẫn bị chặn, xem case C1). */
    private fun needsPin() = guardian.pin.isSet && !guardian.isAdminUnlocked()

    private fun kick(decision: Decision) {
        val until = decision.nextChange?.let { guardian.rules?.calendar?.toLocal(it) }
        kick(decision.describe(), until?.let { BlockActivity.formatUntil(it) })
    }

    private fun kick(message: String, untilText: String?) {
        val now = SystemClock.elapsedRealtime()
        if (now - lastKickElapsed < KICK_DEBOUNCE_MS) return
        lastKickElapsed = now
        guardian.flush()
        performGlobalAction(GLOBAL_ACTION_HOME)
        startActivity(BlockActivity.intent(this, message, untilText))
        currentScreen = Screen.Normal
    }

    private fun label(pkg: String): String? = labels.getOrPut(pkg) {
        runCatching { packageManager.getApplicationInfo(pkg, 0).loadLabel(packageManager).toString() }.getOrNull()
    }

    private fun autoTimeEnabled() =
        Settings.Global.getInt(contentResolver, Settings.Global.AUTO_TIME, 1) == 1

    /** Đọc chữ trên màn hình (giới hạn số node). Không giữ AccessibilityNodeInfo qua các sự kiện. */
    private fun collectTexts(root: AccessibilityNodeInfo?): Sequence<String> {
        if (root == null) return emptySequence()
        val texts = ArrayList<String>()
        val queue = ArrayDeque<AccessibilityNodeInfo>()
        queue.add(root)
        var visited = 0
        while (queue.isNotEmpty()) {
            val node = queue.removeFirst()
            if (visited++ < MAX_NODES) {
                node.text?.takeIf { it.isNotBlank() }?.let { texts += it.toString() }
                node.contentDescription?.takeIf { it.isNotBlank() }?.let { texts += it.toString() }
                for (i in 0 until node.childCount) node.getChild(i)?.let(queue::add)
            }
            if (node !== root) recycle(node)
        }
        return texts.asSequence()
    }

    @Suppress("DEPRECATION")
    private fun recycle(node: AccessibilityNodeInfo?) {
        node?.recycle()
    }

    override fun dump(fd: FileDescriptor?, writer: PrintWriter, args: Array<out String>?) {
        // adb shell dumpsys activity service vn.limityt.tv/.guard.GuardAccessibilityService
        if (!::guardian.isInitialized) return writer.println("chưa kết nối")
        writer.println("paired=${guardian.paired} foreground=$currentPkg screen=$currentScreen watching=$watching")
        writer.println("adminUnlocked=${guardian.isAdminUnlocked()} unlockedUntil=${guardian.unlockedUntil} clockTampered=${guardian.clockTampered}")
        guardian.statusLines().forEach(writer::println)
    }

    companion object {
        private const val TICK_MS = 5_000L
        private const val MAX_DELTA_MS = 10_000L
        private const val FLUSH_MS = 30_000L
        private const val KICK_DEBOUNCE_MS = 2_000L
        private const val CONTENT_SCAN_MS = 700L
        private const val MAX_NODES = 400
    }
}

/** Cảnh báo nhỏ ở góc màn hình, không chiếm focus và không dừng video (docs/02-kien-truc.md §7.2). */
class WarningOverlay(private val service: AccessibilityService) {
    private val windowManager = service.getSystemService(WindowManager::class.java)
    private val handler = Handler(Looper.getMainLooper())
    private var view: TextView? = null

    fun show(text: String) {
        hide()
        val density = service.resources.displayMetrics.density
        val tv = TextView(service).apply {
            this.text = text
            textSize = 22f
            setTextColor(0xFFFFFFFF.toInt())
            val pad = (16 * density).toInt()
            setPadding(pad * 2, pad, pad * 2, pad)
            background = GradientDrawable().apply {
                setColor(0xE0B3261E.toInt())
                cornerRadius = 12 * density
            }
        }
        val params = WindowManager.LayoutParams(
            WindowManager.LayoutParams.WRAP_CONTENT,
            WindowManager.LayoutParams.WRAP_CONTENT,
            WindowManager.LayoutParams.TYPE_ACCESSIBILITY_OVERLAY,
            WindowManager.LayoutParams.FLAG_NOT_FOCUSABLE or WindowManager.LayoutParams.FLAG_NOT_TOUCHABLE,
            PixelFormat.TRANSLUCENT,
        ).apply {
            gravity = Gravity.TOP or Gravity.END
            x = (32 * density).toInt()
            y = (32 * density).toInt()
        }
        if (runCatching { windowManager.addView(tv, params) }.isSuccess) view = tv
        handler.postDelayed({ hide() }, 10_000)
    }

    fun hide() {
        handler.removeCallbacksAndMessages(null)
        view?.let { runCatching { windowManager.removeView(it) } }
        view = null
    }
}
