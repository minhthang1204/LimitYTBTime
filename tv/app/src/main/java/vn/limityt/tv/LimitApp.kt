package vn.limityt.tv

import android.app.Application
import android.content.Context
import vn.limityt.tv.data.Guardian
import vn.limityt.tv.heal.SelfHealJob

class LimitApp : Application() {
    val guardian: Guardian by lazy { Guardian(this) }

    override fun onCreate() {
        super.onCreate()
        SelfHealJob.schedule(this)
    }

    companion object {
        fun guardian(context: Context): Guardian = (context.applicationContext as LimitApp).guardian
    }
}
