package vn.limityt.tv.guard

import vn.limityt.tv.rules.LimitConfig

/** Loại màn hình đang ở foreground — quyết định GuardAccessibilityService làm gì (docs/02-kien-truc.md §7.1). */
sealed interface Screen {
    /** HDMI/camera, launcher, chính app LimitYT: không bao giờ chặn, không đếm giờ. */
    data object NeverBlock : Screen

    /** Đang xem dịch vụ bị giới hạn (app YouTube, hoặc trình duyệt đang mở youtube.com/tiktok.com). */
    data class Limited(val serviceId: String) : Screen

    /** Màn hình cần PIN: trình cài APK, trình dọn dẹp, trang Settings nhạy cảm. */
    data class NeedsPin(val message: String) : Screen

    data object Normal : Screen
}

/**
 * Luật nhận diện màn hình. Không phụ thuộc Android để test được trên JVM.
 * Danh sách có sẵn ở đây luôn được hợp với danh sách trong cấu hình từ PC.
 * Các package của Xiaomi cần xác nhận ở Giai đoạn 0 (docs/03-ke-hoach.md).
 */
class ScreenRules(config: LimitConfig, private val selfPackage: String) {
    private val neverBlock = BUILTIN_NEVER_BLOCK + config.tv.neverBlockPackages + selfPackage
    private val servicePackages: Map<String, String> =
        config.services.flatMap { s -> s.tvPackages.map { it to s.id } }.toMap()
    private val serviceDomains: List<Pair<String, String>> =
        config.services.flatMap { s -> s.domains.map { it.lowercase() to s.id } }.sortedByDescending { it.first.length }
    private val installers = BUILTIN_INSTALLERS + config.tv.installerPackages
    private val cleaners = BUILTIN_CLEANERS + config.tv.cleanerPackages
    private val guardedSettings = BUILTIN_GUARDED_SETTINGS + config.tv.guardedSettingsPackages
    private val browsers = BUILTIN_BROWSERS + config.tv.browserPackages
    private val guardedKeywords = keywordRegex(BUILTIN_KEYWORDS + config.tv.guardedKeywords)

    /** Phân loại chỉ theo package và tên app — rẻ, gọi được ở mỗi sự kiện. */
    fun classifyPackage(pkg: String, appLabel: String?): Kind {
        if (pkg in neverBlock) return Kind.NeverBlock
        servicePackages[pkg]?.let { return Kind.LimitedApp(it) }
        if (pkg in installers) return Kind.Installer
        if (pkg in cleaners || appLabel != null && CLEANER_LABEL.containsMatchIn(appLabel)) return Kind.Cleaner
        if (pkg in guardedSettings) return Kind.Settings
        if (pkg in browsers || pkg.contains("browser", ignoreCase = true)) return Kind.Browser
        // Case C11: app lạ cài thêm có tên kiểu "...Tube", "...Tok" → coi như YouTube/TikTok.
        if (SUSPICIOUS_PACKAGE.containsMatchIn(pkg) || appLabel != null && SUSPICIOUS_LABEL.containsMatchIn(appLabel))
            return Kind.LimitedApp(if (pkg.contains("tok", true) || appLabel?.contains("tok", true) == true) "tiktok" else "youtube")
        return Kind.Normal
    }

    /** Màn hình Settings có nhắc tới mục cần bảo vệ không (Accessibility, Ngày giờ, LimitYT...). */
    fun settingsNeedPin(texts: Sequence<String>): Boolean = texts.any { guardedKeywords.containsMatchIn(it) }

    /** Trình duyệt đang mở trang của dịch vụ bị giới hạn không (dò thanh địa chỉ / nội dung trang). */
    fun browserService(texts: Sequence<String>): String? {
        for (text in texts) {
            val lower = text.lowercase()
            for ((domain, id) in serviceDomains) if (containsHost(lower, domain)) return id
        }
        return null
    }

    fun resolve(kind: Kind, texts: () -> Sequence<String>): Screen = when (kind) {
        Kind.NeverBlock -> Screen.NeverBlock
        is Kind.LimitedApp -> Screen.Limited(kind.serviceId)
        Kind.Installer -> Screen.NeedsPin("Cần PIN của phụ huynh để cài ứng dụng")
        Kind.Cleaner -> Screen.NeedsPin("Cần PIN của phụ huynh để mở trình dọn dẹp")
        Kind.Settings -> if (settingsNeedPin(texts())) Screen.NeedsPin("Cần PIN của phụ huynh để mở mục cài đặt này") else Screen.Normal
        Kind.Browser -> browserService(texts())?.let { Screen.Limited(it) } ?: Screen.Normal
        Kind.Normal -> Screen.Normal
    }

    sealed interface Kind {
        data object NeverBlock : Kind
        data class LimitedApp(val serviceId: String) : Kind
        data object Installer : Kind
        data object Cleaner : Kind
        data object Settings : Kind
        data object Browser : Kind
        data object Normal : Kind

        /** Cần đọc nội dung màn hình mới phân loại được. */
        val needsContent: Boolean get() = this == Settings || this == Browser
    }

    companion object {
        // Launcher, Live TV / nguồn vào HDMI. Package của Xiaomi lấy chính xác ở Giai đoạn 0.
        val BUILTIN_NEVER_BLOCK = setOf(
            "com.android.tv", "com.google.android.tv", "com.android.systemui",
            "com.google.android.tvlauncher", "com.google.android.apps.tv.launcherx", "com.android.tv.launcher",
            "com.mitv.tvhome", "com.mitv.tvhome.atv", "com.xiaomi.mitv.tvplayer", "com.mitv.livetv",
        )
        val BUILTIN_INSTALLERS = setOf("com.android.packageinstaller", "com.google.android.packageinstaller")
        val BUILTIN_CLEANERS = setOf("com.xiaomi.mitv.tvmanager", "com.mitv.tvmanager", "com.miui.cleanmaster")
        val BUILTIN_GUARDED_SETTINGS = setOf("com.android.tv.settings", "com.xiaomi.mitv.settings", "com.android.settings")
        val BUILTIN_BROWSERS = setOf(
            "com.android.chrome", "org.mozilla.tv.firefox", "org.mozilla.firefox", "com.opera.browser",
            "com.cloudmosa.puffinTV", "com.cloudmosa.puffinFree", "com.tcl.browser", "com.xiaomi.mitv.browser",
            "com.android.browser", "com.coccoc.trinhduyet", "com.sec.android.app.sbrowser", "com.microsoft.emmx",
        )

        /** Từ khoá trên màn hình Settings cần PIN. So khớp theo cả từ, không phân biệt hoa thường. */
        val BUILTIN_KEYWORDS = listOf(
            "LimitYT", "YouTube", "TikTok", "SmartTube",
            "Accessibility", "Trợ năng", "Hỗ trợ tiếp cận",
            "Date & time", "Date and time", "Ngày & giờ", "Ngày và giờ", "Ngày giờ",
            "Developer options", "Tùy chọn nhà phát triển", "Tuỳ chọn nhà phát triển", "Tùy chọn cho nhà phát triển",
            "Build", "Bản dựng",
            "Unknown sources", "Install unknown apps", "Nguồn không xác định", "Ứng dụng không rõ nguồn gốc",
            "Force stop", "Buộc dừng", "Device admin", "Quản trị thiết bị",
            "Factory reset", "Khôi phục cài đặt gốc", "Đặt lại về trạng thái ban đầu",
        )

        private val CLEANER_LABEL = Regex("""clean|boost|dọn dẹp|tăng tốc|tv manager|quản lý tv|trình quản lý tv""", RegexOption.IGNORE_CASE)
        private val SUSPICIOUS_PACKAGE = Regex("""youtube|tiktok|smarttube|newpipe|vanced|revanced|musically""", RegexOption.IGNORE_CASE)
        private val SUSPICIOUS_LABEL = Regex("""tube\b|tube[\s\d]|tik\s?tok""", RegexOption.IGNORE_CASE)

        fun keywordRegex(words: Collection<String>): Regex =
            Regex(words.distinct().joinToString("|", """(?<![\p{L}\p{N}])(?:""", """)(?![\p{L}\p{N}])""") { Regex.escape(it) },
                setOf(RegexOption.IGNORE_CASE))

        /** "m.youtube.com/watch" chứa host "youtube.com"; "notyoutube.com" thì không. */
        fun containsHost(text: String, domain: String): Boolean {
            var i = text.indexOf(domain)
            while (i >= 0) {
                val before = if (i == 0) ' ' else text[i - 1]
                val afterIndex = i + domain.length
                val after = if (afterIndex >= text.length) ' ' else text[afterIndex]
                if (!before.isLetterOrDigit() && before != '-' && !after.isLetterOrDigit() && after != '-') return true
                i = text.indexOf(domain, i + 1)
            }
            return false
        }
    }
}
