package app.culltron.smartportdriver

import android.Manifest
import android.app.Activity
import android.content.pm.PackageManager
import android.location.Location
import android.location.LocationManager
import android.os.Bundle
import android.graphics.Color
import android.graphics.Typeface
import android.text.InputType
import android.widget.*
import org.json.JSONObject
import java.net.HttpURLConnection
import java.net.URL
import java.net.URLEncoder
import kotlin.concurrent.thread

class MainActivity : Activity() {
    private var backend = "http://10.0.2.2:8080"
    private lateinit var root: LinearLayout
    private var currentReference = "SPQ-2026-0042"
    private var demoCode = "culltron-driver-2026"
    private var mobileToken = ""
    private var manualLocationLabel = "Driver Companion check-in"

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        val prefs = getSharedPreferences("smartport-driver", MODE_PRIVATE)
        backend = prefs.getString("backend", backend) ?: backend
        currentReference = prefs.getString("reference", currentReference) ?: currentReference
        demoCode = prefs.getString("demoCode", demoCode) ?: demoCode
        mobileToken = prefs.getString("mobileToken", "") ?: ""
        showLogin()
    }

    private fun persist() = getSharedPreferences("smartport-driver", MODE_PRIVATE).edit()
        .putString("backend", backend).putString("reference", currentReference).putString("demoCode", demoCode).putString("mobileToken", mobileToken).apply()

    private fun baseLayout(): LinearLayout {
        val scroll = ScrollView(this)
        root = LinearLayout(this).apply { orientation = LinearLayout.VERTICAL; setPadding(28, 42, 28, 28); setBackgroundColor(Color.rgb(5, 16, 31)) }
        scroll.addView(root); setContentView(scroll); return root
    }

    private fun showLogin() {
        baseLayout()
        titleText("Smart Port Driver Companion")
        label("Real Android companion for the Smart Port backend. The APK calls mobile APIs; it stores no Gemini keys, WhatsApp tokens, or provider secrets.", 15)
        val backendInput = input(backend, "Backend URL, e.g. Codespaces/DigitalOcean HTTPS URL")
        val refInput = input(currentReference, "Truck/job reference")
        val codeInput = input(demoCode, "Demo driver code")
        codeInput.inputType = InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_VISIBLE_PASSWORD
        button("Quick-fill Android emulator localhost") { backendInput.setText("http://10.0.2.2:8080") }
        button("Quick-fill demo driver") { refInput.setText("SPQ-2026-0042"); codeInput.setText("culltron-driver-2026") }
        button("Login to backend") {
            backend = backendInput.text.toString().trim().trimEnd('/')
            currentReference = refInput.text.toString().trim().ifBlank { "SPQ-2026-0042" }
            demoCode = codeInput.text.toString().trim()
            persist(); demoLogin()
        }
        button("Refresh status with saved token") {
            backend = backendInput.text.toString().trim().trimEnd('/'); currentReference = refInput.text.toString().trim(); persist(); fetchStatus()
        }
        whatsAppInfoCard()
    }

    private fun demoLogin() = thread {
        try {
            val body = JSONObject().put("role", "Driver Demo").put("accessCode", demoCode).toString()
            val json = JSONObject(postJsonForText("$backend/api/mobile/auth/demo-login", body, includeToken = false))
            mobileToken = json.getString("token")
            currentReference = json.optString("demoReference", currentReference)
            persist()
            runOnUiThread { toast("Demo access granted"); fetchStatus() }
        } catch (ex: Exception) { runOnUiThread { toast("Backend login failed: ${ex.message}") } }
    }

    private fun fetchStatus() = thread {
        try {
            val ref = URLEncoder.encode(currentReference, "UTF-8")
            val json = getJson("$backend/api/mobile/truck/status/$ref")
            runOnUiThread { showStatus(json) }
        } catch (ex: Exception) { runOnUiThread { toast(if (ex.message?.contains("401") == true) "Invalid/expired demo token. Login again." else "Unable to load status: ${ex.message}") } }
    }

    private fun showStatus(json: JSONObject) {
        baseLayout(); currentReference = json.getString("bookingReference"); persist()
        titleText(json.optString("truckRegistration", currentReference))
        label("${json.optString("driverName", "Demo Driver")} · ${json.optString("fleetOperatorName", "Fleet Operator")}", 16)
        val lastCheckIn = json.optJSONObject("lastLocationCheckIn")
        val checkInText = if (lastCheckIn == null) "No check-in yet" else listOf(lastCheckIn.optString("locationLabel"), lastCheckIn.optString("timestamp"), lastCheckIn.optString("source")).joinToString(" · ")
        card("Truck Status", "Reference: $currentReference\nStatus: ${json.optString("currentStatus")}\nQueue #: ${json.optInt("queueNumber")}\nGate: ${json.optString("assignedGate")}\nStaging: ${json.optString("berthYardStagingZone")}\nCall-forward/ETA: ${json.optString("etaCallForwardTime")}\nLast check-in: $checkInText\nLast updated: ${json.optString("lastUpdated", "backend timestamp")}")
        val instruction = json.optJSONObject("currentInstruction") ?: JSONObject()
        card("Current Instruction", "${instruction.optString("instruction", "Refresh for latest instruction")}\nReason: ${instruction.optString("reason", "Smart Port operational state")}")
        card("Impact", "Delay risk: ${json.optString("delayRisk")}\nIdling avoided: ${json.optInt("estimatedIdlingMinutesAvoided")} min\nCO₂ avoided: ${json.optDouble("estimatedCo2KgAvoided")} kg\nAI/source: ${json.optString("aiSource", "Backend Gemini/local fallback")}")
        button("Ready") { confirmStatus("DriverReady") }
        button("Holding") { confirmStatus("Holding") }
        button("Delayed 20") { confirmStatus("Delayed20") }
        button("Arrived at Staging") { confirmStatus("ArrivedAtStaging") }
        button("Proceeding to Gate") { confirmStatus("ProceedingToGate") }
        button("Arrived at Gate") { confirmStatus("ArrivedAtGate") }
        button("Completed") { confirmStatus("Completed") }
        button("Report Issue") { confirmStatus("ReportIssue") }
        button("Confirm Instruction") { confirmStatus("ConfirmInstruction") }
        val locationInput = input(manualLocationLabel, "Manual location label if GPS is unavailable")
        button("Share current location / Check in") { manualLocationLabel = locationInput.text.toString().ifBlank { "Driver Companion check-in" }; shareCurrentLocationOrManualCheckIn() }
        button("Refresh Status") { fetchStatus() }
        val questionInput = input("What should I do now?", "Driver Copilot question")
        button("Ask Driver Copilot") { askCopilot(questionInput.text.toString()) }
        LinearLayout(this).apply { orientation = LinearLayout.HORIZONTAL; root.addView(this); listOf("Why am I delayed?", "Where do I go?", "What is my ETA?").forEach { q -> addView(actionButton(q) { askCopilot(q) }) } }
        button("Notifications") { fetchNotifications() }
        button("Backend setup") { showLogin() }
        whatsAppInfoCard()
    }

    private fun confirmStatus(action: String) = thread {
        val event = when (action) { "Holding" -> "DriverConfirmedHolding"; "ArrivedAtStaging" -> "DriverArrivedAtStaging"; "ProceedingToGate" -> "DriverProceedingToGate"; "ArrivedAtGate" -> "DriverArrivedAtGate"; "Completed" -> "DriverCompletedJob"; "Delayed20" -> "DriverDelayed"; "ReportIssue" -> "DriverIssueReported"; "ConfirmInstruction" -> "DriverAcknowledgedInstruction"; else -> action }
        val body = JSONObject().put("reference", currentReference).put("eventType", event).put("sourceLabel", "Android").toString()
        try { postJsonForText("$backend/api/mobile/driver/confirm-status", body); runOnUiThread { toast("Action sent: $action"); fetchStatus() } } catch (ex: Exception) { runOnUiThread { toast("Action failed: ${ex.message}") } }
    }

    private fun shareCurrentLocationOrManualCheckIn() {
        if (checkSelfPermission(Manifest.permission.ACCESS_FINE_LOCATION) != PackageManager.PERMISSION_GRANTED && checkSelfPermission(Manifest.permission.ACCESS_COARSE_LOCATION) != PackageManager.PERMISSION_GRANTED) {
            requestPermissions(arrayOf(Manifest.permission.ACCESS_FINE_LOCATION, Manifest.permission.ACCESS_COARSE_LOCATION), 42)
            toast("Location permission requested; sending manual check-in label now.")
            locationCheckIn(null)
            return
        }
        val manager = getSystemService(LOCATION_SERVICE) as LocationManager
        val providers = listOf(LocationManager.GPS_PROVIDER, LocationManager.NETWORK_PROVIDER)
        val location = providers.firstNotNullOfOrNull { provider -> try { manager.getLastKnownLocation(provider) } catch (_: Exception) { null } }
        locationCheckIn(location)
    }

    private fun locationCheckIn(location: Location?) = thread {
        val body = JSONObject()
            .put("reference", currentReference)
            .put("eventType", "DriverReady")
            .put("sourceLabel", "Android Driver Companion")
            .put("status", "CheckIn")
            .put("action", "Share current location / Check in")
            .put("locationLabel", manualLocationLabel.ifBlank { if (location == null) "Manual Android check-in" else "Android current-location check-in" })
            .apply { if (location != null) { put("latitude", location.latitude); put("longitude", location.longitude); put("accuracy", location.accuracy.toDouble()) } }
            .toString()
        try { postJsonForText("$backend/api/mobile/driver/location-checkin", body); runOnUiThread { toast("Check-in shared successfully"); fetchStatus() } } catch (ex: Exception) { runOnUiThread { toast("Location failed: ${ex.message}") } }
    }

    private fun askCopilot(question: String) = thread {
        val body = JSONObject().put("reference", currentReference).put("userRole", "Driver").put("question", question).toString()
        try { val json = JSONObject(postJsonForText("$backend/api/mobile/copilot/driver", body)); runOnUiThread { card("Driver Copilot · ${json.optString("source", "backend fallback")}", "${json.optString("answer")}\n\nSuggested action: ${json.optString("suggestedAction")}") } } catch (ex: Exception) { runOnUiThread { toast("Copilot unavailable: ${ex.message}") } }
    }

    private fun fetchNotifications() = thread {
        try { val arrText = getText("$backend/api/mobile/notifications/${URLEncoder.encode(currentReference, "UTF-8")}"); runOnUiThread { showNotifications(arrText) } } catch (ex: Exception) { runOnUiThread { toast("Notifications unavailable: ${ex.message}") } }
    }

    private fun showNotifications(text: String) {
        baseLayout(); titleText("Notifications")
        val arr = org.json.JSONArray(text)
        if (arr.length() == 0) label("No notifications yet.", 16)
        for (i in 0 until arr.length()) { val n = arr.getJSONObject(i); card("${n.optString("channel")} · ${n.optString("status")}", "${n.optString("message")}\n${n.optString("timestampUtc")}\nSource: ${n.optString("source")}\nExternal id: ${n.optString("externalMessageId")}\nContact: ${n.optString("maskedContact")}") }
        button("Back to status") { fetchStatus() }
    }

    private fun whatsAppInfoCard() = card("WhatsApp connector-ready", "WhatsApp Cloud API is optional sandbox/live-test connector readiness. Production use requires WhatsApp Business setup, opt-in/templates, and billing. This driver app works through Smart Port web/mobile APIs without WhatsApp production approval.")

    private fun getJson(url: String) = JSONObject(getText(url))
    private fun getText(url: String): String = (URL(url).openConnection() as HttpURLConnection).run { if (mobileToken.isNotBlank()) setRequestProperty("X-SmartPort-Mobile-Token", mobileToken); connectTimeout = 8000; readTimeout = 12000; if (responseCode == 401) throw Exception("401 Demo access required"); if (responseCode >= 400) throw Exception("HTTP $responseCode"); inputStream.bufferedReader().readText() }
    private fun postJsonForText(url: String, body: String, includeToken: Boolean = true): String = (URL(url).openConnection() as HttpURLConnection).run { requestMethod = "POST"; setRequestProperty("Content-Type", "application/json"); if (includeToken && mobileToken.isNotBlank()) setRequestProperty("X-SmartPort-Mobile-Token", mobileToken); connectTimeout = 8000; readTimeout = 15000; doOutput = true; outputStream.use { it.write(body.toByteArray()) }; if (responseCode == 401) throw Exception("401 Demo access required"); if (responseCode >= 400) throw Exception("HTTP $responseCode"); inputStream.bufferedReader().readText() }
    private fun titleText(text: String) { val t = TextView(this); t.text = text; t.textSize = 28f; t.typeface = Typeface.DEFAULT_BOLD; t.setTextColor(Color.WHITE); t.setPadding(0, 10, 0, 16); root.addView(t) }
    private fun label(text: String, size: Int) { root.addView(TextView(this).apply { this.text = text; textSize = size.toFloat(); setTextColor(Color.rgb(219, 234, 254)); setPadding(0, 8, 0, 10) }) }
    private fun input(text: String, hint: String) = EditText(this).apply { setText(text); setHint(hint); setTextColor(Color.WHITE); setHintTextColor(Color.GRAY); setSingleLine(false); root.addView(this) }
    private fun button(text: String, action: () -> Unit) { root.addView(actionButton(text, action)) }
    private fun actionButton(text: String, action: () -> Unit) = Button(this).apply { this.text = text; setOnClickListener { action() } }
    private fun card(title: String, body: String) { root.addView(TextView(this).apply { text = "$title\n$body"; textSize = 16f; setTextColor(Color.rgb(165, 243, 252)); setPadding(22, 22, 22, 22); background = android.graphics.drawable.GradientDrawable().apply { setColor(Color.rgb(11, 31, 52)); cornerRadius = 24f; setStroke(1, Color.rgb(6, 182, 212)) } }) }
    private fun toast(message: String) = Toast.makeText(this, message, Toast.LENGTH_LONG).show()
}
