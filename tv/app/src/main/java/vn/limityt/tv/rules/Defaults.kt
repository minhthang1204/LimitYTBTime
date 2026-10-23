package vn.limityt.tv.rules

/** Cấu hình mặc định — giống Defaults.cs bên PC. Dùng khi ghép cặp mà không kèm cấu hình. */
object Defaults {
    val allDays = listOf("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")
    val weekdays = listOf("Mon", "Tue", "Wed", "Thu", "Fri")

    fun create(): LimitConfig = LimitConfig(
        blockedWindows = listOf(
            BlockedWindow(days = allDays, from = "22:00", to = "06:00"),
            BlockedWindow(days = weekdays, from = "07:00", to = "11:00", schoolOnly = true),
            BlockedWindow(days = weekdays, from = "13:00", to = "17:00", schoolOnly = true),
        ),
        offlineCapPercent = mapOf("pc" to 50, "tv" to 100),
        services = listOf(
            LimitedService(
                id = "youtube",
                domains = listOf(
                    "youtube.com", "www.youtube.com", "m.youtube.com", "music.youtube.com", "youtu.be",
                    "youtube-nocookie.com", "www.youtube-nocookie.com", "youtubei.googleapis.com", "i.ytimg.com",
                ),
                tvPackages = listOf(
                    "com.google.android.youtube.tv", "com.google.android.youtube.tvkids",
                    "com.google.android.youtube.tvmusic", "com.liskovsoft.smarttubetv",
                    "com.liskovsoft.smarttubetv.beta", "com.teamsmart.videomanager.tv",
                ),
            ),
            LimitedService(
                id = "tiktok",
                domains = listOf(
                    "tiktok.com", "www.tiktok.com", "m.tiktok.com", "vt.tiktok.com", "vm.tiktok.com",
                    "tiktokv.com", "www.tiktokv.com",
                ),
                tvPackages = listOf("com.ss.android.ugc.trill", "com.zhiliaoapp.musically"),
            ),
        ),
        tv = TvConfig(
            guardedSettingsPackages = listOf("com.android.tv.settings", "com.xiaomi.mitv.settings"),
            installerPackages = listOf("com.android.packageinstaller", "com.google.android.packageinstaller"),
        ),
    )
}
