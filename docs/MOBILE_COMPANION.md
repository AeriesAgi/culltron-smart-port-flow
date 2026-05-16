# Smart Port Driver Companion — Android Source

The Android Driver Companion source is included under `mobile/SmartPortDriverCompanion`. It is a native Kotlin project intended for queue status, driver confirmations, demo location check-ins, notification history, and backend Copilot calls.

## Backend URL

Default deployment URL:

```text
https://smartport.culltron.app
```

For local Android emulator testing, use a reachable ASP.NET backend URL such as:

```text
https://10.0.2.2:5001
```

For a physical device, use a LAN, tunnel, or deployed HTTPS URL that the device can reach.

## Security model

- The Android app stores no Gemini API key.
- The Android app stores no WhatsApp token.
- AI and WhatsApp logic remain on the backend.
- The app calls Smart Port backend APIs and renders the returned state.

## Expected capabilities

- Fetch truck status from `/api/mobile/truck/status/{reference}`.
- Show queue number, ETA, assigned gate, staging zone, current status, and latest instruction.
- Show notification history from `/api/mobile/notifications/{reference}`.
- Post confirmations and commands through backend endpoints.
- Post demo location check-ins.
- Call `/api/mobile/copilot/driver` for scoped driver assistance.
- Handle connectivity and validation errors without exposing secrets.

## Build APK

Open `mobile/SmartPortDriverCompanion` in Android Studio and run a debug build. If building from a CLI environment with the Android SDK installed, use the project Gradle wrapper or Gradle installation to assemble a debug APK, then place the resulting file at:

```text
src/SmartPort.Web/wwwroot/downloads/SmartPortDriverCompanion.apk
```

The web app does not require the Android SDK to build or deploy.

## Final enterprise hackathon pass — Gemini, mobile driver loop, and connector readiness

Smart Port is designed so the only remaining production step is adding approved live environment variables/API keys. Demo data is synthetic and the product does not claim live Transnet/IPMS/TOS/customer/production WhatsApp operation.

### Judge demo codes and path
When the app runs in Development or `SMARTPORT_SHOW_DEMO_CREDENTIALS=true`, `/demo-access` shows quick-fill role cards:
- Port Admin: `culltron-admin-2026` → `/dashboard`
- Fleet Owner: `culltron-fleet-2026` → `/fleet`
- Driver: `culltron-driver-2026` → `/driver/demo`
- Judge: `culltron-judge-2026` → `/demo-tour`

Recommended judging path: `/demo-access` → Judge quick-fill → `/demo-tour` → `/gemini-agent` → `/agent-governance` → `/ops-ingest` → `/execution/plans` → `/fleet/trucks/SPQ-2026-0042` → `/driver/demo` → `/fleet/download-app` → `/fleet/notifications` → `/enterprise-readiness` → `/health/integrations`.

### Gemini award centerpiece and quota-safe model strategy
Gemini is openly used as the on-demand AI agent layer. It activates only when a user submits an explicit Gemini/AI/Copilot/Ops Ingest/driver command action, not on normal page loads, health checks, dashboards, enterprise readiness, Android app launch, mobile login/status refresh, seed data, smoke tests, or background timers.

Default server-side configuration:
- `GEMINI_API_KEY`
- `Gemini__Enabled=false` by default unless explicitly enabled
- `Gemini__Mode=Hybrid`
- `Gemini__PremiumModel=gemini-2.5-flash`
- `Gemini__RoutineModel=gemini-3.1-flash-lite`
- `Gemini__PrimaryModel=gemini-2.5-flash`
- `Gemini__FallbackModels=gemini-3.1-flash-lite,gemini-2.5-flash-lite,gemini-2.0-flash-lite,gemini-2.0-flash`
- `Gemini__ExperimentalFallbackModels=` and `Gemini__AllowExperimentalModels=false`
- `Gemini__MaxCallsPerSession=20`
- `Gemini__ManualTestCooldownSeconds=60`
- `Gemini__QuotaCooldownMinutes=30`
- `Gemini__AutoRunOnAgentPage=false`
- `Gemini__AutoRunOnDemoTour=false`
- `Gemini__AutoRunCooldownMinutes=30`

Task categories:
- Premium judge/high-value reasoning uses `gemini-2.5-flash` first for Executive Judge Summary, Risk & Governance Review, high-impact Disruption Recovery Plan, and final/polished operations briefs. Because observed free-tier availability is low (5 RPM / 20 RPD), calls are kept sparse.
- Routine operational AI uses `gemini-3.1-flash-lite` first for driver instruction phrasing, routine fleet briefs, mobile Driver Copilot, Ops Ingest summarization, delay explanations, queue-action explanations, and notification suggestions.
- Secondary Gemini fallback uses `gemini-2.5-flash-lite`, then configured fallback text models.
- Optional Gemma text fallbacks are disabled unless explicitly configured and proven compatible with the same backend API path.
- Local deterministic fallback is always available and produces polished driver instructions, fleet plans, executive summaries, governance/risk reviews, disruption recovery, emissions/idling impact, and Copilot responses.

The backend skips unsupported/model-not-found responses, marks quota-limited models for cooldown, avoids retry loops, records safe diagnostics (counts, action type, route/source, model, latency, quota/fallback state), and never logs or displays API keys, bearer tokens, prompts containing secrets, WhatsApp tokens, phone numbers, or provider credentials.

### Driver Companion APK, web companion, and mobile API
The primary driver channels are the Android Driver Companion, the web companion, and token-secured mobile APIs. The Android app lives in `mobile/SmartPortDriverCompanion`, uses Java/Kotlin 17, and calls:
- `POST /api/mobile/auth/demo-login`
- `GET /api/mobile/truck/status/{reference}`
- `GET /api/mobile/notifications/{reference}`
- `POST /api/mobile/driver/confirm-status`
- `POST /api/mobile/driver/location-checkin`
- `POST /api/mobile/copilot/driver`

The app has editable backend URL setup, quick-fill `culltron-driver-2026`, quick-fill `SPQ-2026-0042`, truck status, driver action buttons, notifications, Driver Copilot, and WhatsApp connector-readiness explanation. All Gemini and WhatsApp calls happen on the backend; the device stores no provider secrets.

GitHub Actions builds and uploads the debug APK artifact. If a local Android SDK is available, build with `cd mobile/SmartPortDriverCompanion && gradle assembleDebug` (or `./gradlew assembleDebug` when a wrapper is present) and optionally copy the debug APK to `src/SmartPort.Web/wwwroot/downloads/SmartPortDriverCompanion.apk`.

### WhatsApp connector-ready position
WhatsApp Cloud API is implemented as a connector-ready sandbox/live-test integration with webhook verification, inbound parser, gated outbound sender, masked contacts, status labels, and safe failure when credentials/approval are missing. Production use requires WhatsApp Business setup, opt-in/templates, and billing. Smart Port does not depend on WhatsApp for the judge demo.

### Pilot/live data path
Future live pilots can connect approved IPMS/TOS, gate/OCR/RFID, weighbridge, fleet GPS/telematics, berth/yard, ERP, weather/disruption, emissions, driver app, and WhatsApp Cloud API feeds after NDA/data-sharing, field mapping, credential provisioning, security review, and supervised KPI validation.
