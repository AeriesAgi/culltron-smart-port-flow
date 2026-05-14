package app.culltron.smartportdriver

import android.app.*
import android.os.*
import android.graphics.Color
import android.view.*
import android.widget.*
import org.json.JSONObject
import java.net.HttpURLConnection
import java.net.URL
import kotlin.concurrent.thread

class MainActivity : Activity() {
    private var backend = "https://smartport.culltron.app"
    private lateinit var root: LinearLayout
    private var currentReference = "SPQ-2026-0042"

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        showSearch()
        registerDevicePlaceholder()
    }

    private fun baseLayout(): LinearLayout {
        val scroll = ScrollView(this)
        root = LinearLayout(this).apply { orientation = LinearLayout.VERTICAL; setPadding(28, 42, 28, 28); setBackgroundColor(Color.rgb(7,21,38)) }
        scroll.addView(root); setContentView(scroll); return root
    }

    private fun showSearch() {
        baseLayout()
        titleText("Culltron Smart Port")
        label("Driver Queue Companion", 18)
        val backendInput = input(backend, "Backend URL")
        val refInput = input(currentReference, "Booking, truck, or job reference")
        button("Check queue status") { backend = backendInput.text.toString().trimEnd('/'); currentReference = refInput.text.toString(); fetchStatus(currentReference) }
        button("Use demo reference SPQ-2026-0042") { refInput.setText("SPQ-2026-0042"); fetchStatus("SPQ-2026-0042") }
        label("Free demo mode pulls queue status and notification history from Smart Port APIs. No Firebase, Gemini key, or WhatsApp credential is stored in the app.", 14)
    }

    private fun fetchStatus(reference: String) = thread {
        try {
            val json = getJson("$backend/api/mobile/truck/status/$reference")
            runOnUiThread { showStatus(json) }
        } catch (ex: Exception) { runOnUiThread { toast("Unable to load status: ${ex.message}") } }
    }

    private fun showStatus(json: JSONObject) {
        baseLayout(); currentReference = json.getString("bookingReference")
        titleText(json.getString("truckRegistration")); label(json.getString("driverName") + " · " + json.getString("fleetOperatorName"), 16)
        card("Queue #${json.getInt("queueNumber")}", "Status: ${json.getString("currentStatus")}\nGate: ${json.getString("assignedGate")}\nCall-forward: ${json.getString("etaCallForwardTime")}")
        val instruction = json.getJSONObject("currentInstruction")
        card("AI movement instruction", instruction.getString("instruction") + "\n\nWhy: " + instruction.getString("reason"))
        card("Impact", "Delay risk: ${json.getString("delayRisk")}\nIdling avoided: ${json.getInt("estimatedIdlingMinutesAvoided")} min\nCO₂ avoided: ${json.getDouble("estimatedCo2KgAvoided")} kg")
        card("Latest notification", json.optString("latestNotification"))
        LinearLayout(this).apply { orientation = LinearLayout.HORIZONTAL; root.addView(this)
            listOf("Seen", "Holding", "Proceeding").forEach { ack -> addView(actionButton(ack) { acknowledge(ack) }) }
        }
        label("Allowed actions", 18)
        val allowed = json.optJSONArray("allowedNextActions")
        if (allowed != null) for (i in 0 until allowed.length()) { val action = allowed.getString(i); button(action) { confirmStatus(action) } }
        val questionInput = input("HOW LONG", "Ask Smart Port Copilot")
        button("Ask Copilot") { askCopilot(questionInput.text.toString()) }
        button("Share demo WhatsApp location check-in") { locationCheckIn() }
        button("Notification history") { showNotifications(json) }
        button("Back") { showSearch() }
        if (json.getString("currentStatus").contains("Proceed") || json.getString("currentStatus").contains("Hold") || json.getString("currentStatus").contains("Delayed")) toast("Smart Port update: ${instruction.getString("instruction")}")
    }

    private fun acknowledge(ack: String) = thread {
        val body = "{\"reference\":\"$currentReference\",\"acknowledgement\":\"$ack\"}"
        try { postJson("$backend/api/mobile/driver/acknowledge", body); runOnUiThread { toast("Acknowledged: $ack") } } catch (ex: Exception) { runOnUiThread { toast("Ack failed") } }
    }

    private fun confirmStatus(action: String) = thread {
        val event = when(action) { "ConfirmHolding" -> "DriverConfirmedHolding"; "ArrivedAtStaging" -> "DriverArrivedAtStaging"; "ProceedingToGate" -> "DriverProceedingToGate"; "ArrivedAtGate" -> "DriverArrivedAtGate"; "CompleteJob" -> "DriverCompletedJob"; "Break15" -> "DriverBreak"; "Lunch30" -> "DriverLunch"; "Delayed20" -> "DriverDelayed"; "ReportIssue" -> "DriverIssueReported"; else -> "DriverAcknowledgedInstruction" }
        val body = "{\"reference\":\"$currentReference\",\"eventType\":\"$event\",\"sourceLabel\":\"Android\"}"
        try { postJson("$backend/api/mobile/driver/confirm-status", body); runOnUiThread { toast("Action sent: $action"); fetchStatus(currentReference) } } catch (ex: Exception) { runOnUiThread { toast("Action failed") } }
    }

    private fun locationCheckIn() = thread {
        val body = "{\"reference\":\"$currentReference\",\"eventType\":\"WhatsAppLocationShared\",\"sourceLabel\":\"WhatsApp\",\"locationLabel\":\"Demo WhatsApp check-in from Android companion\"}"
        try { postJson("$backend/api/mobile/driver/location-checkin", body); runOnUiThread { toast("Location check-in shared"); fetchStatus(currentReference) } } catch (ex: Exception) { runOnUiThread { toast("Location failed") } }
    }

    private fun askCopilot(question: String) = thread {
        val body = "{\"reference\":\"$currentReference\",\"userRole\":\"Driver\",\"question\":\"$question\"}"
        try { val text = postJsonForText("$backend/api/mobile/copilot/driver", body); runOnUiThread { card("Copilot", text) } } catch (ex: Exception) { runOnUiThread { toast("Copilot unavailable") } }
    }

    private fun showNotifications(json: JSONObject) {
        baseLayout(); titleText("Notifications")
        val arr = json.getJSONArray("notificationHistory")
        for (i in 0 until arr.length()) { val n = arr.getJSONObject(i); card(n.getString("channel") + " · " + n.getString("status"), n.getString("message")) }
        button("Back to status") { showStatus(json) }
    }

    private fun registerDevicePlaceholder() = thread { try { postJson("$backend/api/mobile/device/register", "{\"reference\":\"$currentReference\",\"deviceToken\":\"demo-local-device\",\"platform\":\"Android\",\"appVersion\":\"1.0\"}") } catch (_: Exception) {} }
    private fun getJson(url: String) = JSONObject(URL(url).readText())
    private fun postJson(url: String, body: String) { postJsonForText(url, body) }
    private fun postJsonForText(url: String, body: String): String { return (URL(url).openConnection() as HttpURLConnection).run { requestMethod="POST"; setRequestProperty("Content-Type","application/json"); doOutput=true; outputStream.use{it.write(body.toByteArray())}; inputStream.bufferedReader().readText() } }
    private fun titleText(text: String) = label(text, 28)
    private fun label(text: String, size: Int) { root.addView(TextView(this).apply { setText(text); textSize=size.toFloat(); setTextColor(Color.WHITE); setPadding(0,10,0,10) }) }
    private fun input(text: String, hint: String) = EditText(this).apply { setText(text); setHint(hint); setTextColor(Color.WHITE); setHintTextColor(Color.GRAY); root.addView(this) }
    private fun button(text: String, action: () -> Unit) { root.addView(actionButton(text, action)) }
    private fun actionButton(text: String, action: () -> Unit) = Button(this).apply { setText(text); setOnClickListener { action() } }
    private fun card(title: String, body: String) { root.addView(TextView(this).apply { text="$title\n$body"; textSize=16f; setTextColor(Color.rgb(165,243,252)); setPadding(22,22,22,22); background=android.graphics.drawable.GradientDrawable().apply { setColor(Color.rgb(11,31,52)); cornerRadius=24f; setStroke(1, Color.rgb(6,182,212)) } }) }
    private fun toast(message: String) = Toast.makeText(this, message, Toast.LENGTH_LONG).show()
}
