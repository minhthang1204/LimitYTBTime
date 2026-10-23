package vn.limityt.tv

import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.booleanOrNull
import kotlinx.serialization.json.int
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import kotlinx.serialization.json.long
import org.junit.Assert.assertEquals
import org.junit.Test
import org.junit.runner.RunWith
import org.junit.runners.Parameterized
import vn.limityt.tv.rules.Adjustment
import vn.limityt.tv.rules.ConfigJson
import vn.limityt.tv.rules.Devices
import vn.limityt.tv.rules.LimitConfig
import vn.limityt.tv.rules.OfflineState
import vn.limityt.tv.rules.RulesEngine
import vn.limityt.tv.rules.UsageLedger
import java.time.Instant
import java.time.LocalDate
import java.time.OffsetDateTime

/** Chạy spec/rules-test-vectors.json — cùng bộ vector với RulesVectorTests.cs bên PC. */
@RunWith(Parameterized::class)
class RulesVectorTest(private val name: String, private val case: JsonObject) {

    @Test
    fun vector() {
        val config = ConfigJson.format.decodeFromJsonElement(
            LimitConfig.serializer(),
            JsonObject(root["baseConfig"]!!.jsonObject + (case["config"]?.jsonObject ?: emptyMap())),
        )

        val ledger = UsageLedger()
        case["usage"]?.jsonArray?.forEach {
            val u = it.jsonObject
            ledger.add(u.str("device"), LocalDate.parse(u.str("date")), u["minutes"]!!.jsonPrimitive.long * 60_000)
        }
        case["adjustments"]?.jsonArray?.forEach {
            val a = it.jsonObject
            ledger.merge(Adjustment(a.str("id"), LocalDate.parse(a.str("date")), a["minutes"]!!.jsonPrimitive.long * 60_000, null, ""))
        }
        val offline = case["offline"]?.jsonObject?.let {
            OfflineState(it["budgetMinutes"]!!.jsonPrimitive.long * 60_000, it["selfWeekUsedAtStartMinutes"]!!.jsonPrimitive.long * 60_000)
        }
        val unlockedUntil = (case["unlockedUntil"] as? JsonPrimitive)?.let { instant(it.content) }
        val self = (case["self"] as? JsonPrimitive)?.content ?: Devices.PC

        val d = RulesEngine(config).evaluate(instant(case.str("now")), ledger, self, offline, unlockedUntil)

        val expect = case["expect"]!!.jsonObject
        assertEquals("$name allowed", expect["allowed"]!!.jsonPrimitive.booleanOrNull, d.allowed)
        expect["leftMinutes"]?.let { assertEquals("$name leftMinutes", it.jsonPrimitive.int.toLong(), d.leftMs / 60_000) }
        expect["reason"]?.let { assertEquals("$name reason", it.jsonPrimitive.content, d.reason.name) }
        if ("nextChange" in expect) {
            val n = expect["nextChange"]!!
            val expected = if (n is JsonPrimitive && n.isString) instant(n.content) else null
            assertEquals("$name nextChange", expected, d.nextChange)
        }
    }

    companion object {
        private val root: JsonObject by lazy {
            val text = RulesVectorTest::class.java.classLoader!!.getResource("rules-test-vectors.json")!!.readText()
            ConfigJson.format.parseToJsonElement(text).jsonObject
        }

        @JvmStatic
        @Parameterized.Parameters(name = "{0}")
        fun cases(): List<Array<Any>> =
            root["cases"]!!.jsonArray.map { arrayOf(it.jsonObject.str("name"), it.jsonObject) }

        private fun JsonObject.str(key: String) = this[key]!!.jsonPrimitive.content
        private fun instant(s: String): Instant = OffsetDateTime.parse(s).toInstant()
    }
}
