package vn.limityt.tv.ui

import android.app.Activity
import android.content.Context
import android.graphics.Typeface
import android.view.Gravity
import android.view.ViewGroup
import android.widget.Button
import android.widget.LinearLayout
import android.widget.ScrollView
import android.widget.TextView

/** Giao diện dựng bằng code, điều khiển được bằng remote (D-pad). */
object Ui {
    fun page(activity: Activity): LinearLayout {
        val density = activity.resources.displayMetrics.density
        val column = LinearLayout(activity).apply {
            orientation = LinearLayout.VERTICAL
            gravity = Gravity.CENTER_HORIZONTAL
            val pad = (48 * density).toInt()
            setPadding(pad, pad, pad, pad)
        }
        activity.setContentView(ScrollView(activity).apply {
            setBackgroundColor(0xFF101418.toInt())
            isFillViewport = true
            addView(column, ViewGroup.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT))
        })
        return column
    }

    fun title(context: Context, text: String) = TextView(context).apply {
        this.text = text
        textSize = 34f
        setTypeface(typeface, Typeface.BOLD)
        setTextColor(0xFFFFFFFF.toInt())
        gravity = Gravity.CENTER
        setPadding(0, 0, 0, 24)
    }

    fun text(context: Context, text: String, size: Float = 22f, color: Int = 0xFFD0D6DC.toInt()) = TextView(context).apply {
        this.text = text
        textSize = size
        setTextColor(color)
        gravity = Gravity.CENTER
        setPadding(0, 6, 0, 6)
    }

    fun button(context: Context, label: String, onClick: () -> Unit) = Button(context).apply {
        text = label
        textSize = 20f
        isAllCaps = false
        setOnClickListener { onClick() }
        layoutParams = LinearLayout.LayoutParams(
            (520 * context.resources.displayMetrics.density).toInt(), ViewGroup.LayoutParams.WRAP_CONTENT,
        ).apply { topMargin = 12 }
    }
}
