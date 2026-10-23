package vn.limityt.tv.heal

import android.app.job.JobInfo
import android.app.job.JobParameters
import android.app.job.JobScheduler
import android.app.job.JobService
import android.content.BroadcastReceiver
import android.content.ComponentName
import android.content.Context
import android.content.Intent
import vn.limityt.tv.LimitApp

/** Mỗi 15 phút kiểm tra Accessibility còn bật không; nếu không thì bật lại (docs/02-kien-truc.md §7.4). */
class SelfHealJob : JobService() {
    override fun onStartJob(params: JobParameters): Boolean {
        heal(this)
        return false
    }

    override fun onStopJob(params: JobParameters) = true

    companion object {
        private const val JOB_ID = 4760

        fun schedule(context: Context) {
            val scheduler = context.getSystemService(JobScheduler::class.java)
            if (scheduler.getPendingJob(JOB_ID) != null) return
            scheduler.schedule(
                JobInfo.Builder(JOB_ID, ComponentName(context, SelfHealJob::class.java))
                    .setPeriodic(15 * 60_000L)
                    .setPersisted(true)
                    .build()
            )
        }

        fun heal(context: Context) {
            if (LimitApp.guardian(context).protectionDisabled) return
            AccessibilityToggle.ensureEnabled(context)
        }
    }
}

/** Sau khi TV khởi động (hoặc app vừa được cài đè): bật lại Accessibility và lịch SelfHeal. */
class BootReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        SelfHealJob.schedule(context)
        SelfHealJob.heal(context)
    }
}
