# Live API Setup: Gemini and WhatsApp Cloud API

Smart Port runs safely without live credentials. When credentials are configured, the server-side connectors can call Gemini and Meta WhatsApp Cloud API. Do **not** commit `.env` files, API keys, access tokens, real driver numbers or private credentials.

## Gemini live connector

Get a Gemini API key from Google AI Studio, then configure the server environment:

```bash
export GEMINI_API_KEY (set this to your server-side key)
export export Gemini__Enabled=true
export export Gemini__Mode=Hybrid
export export Gemini__Model=gemini-2.5-flash
```

Fallback names are also supported:

```bash
export GEMINI_ENABLED=true
export GEMINI_MODE=Hybrid
export GEMINI_MODEL=gemini-2.5-flash
```

Open `/gemini-agent` and click **Run Live Gemini Test**. If the key is missing, disabled, timed out or rejected, Smart Port keeps the deterministic local fallback active and records the safe source label in audit history.

## WhatsApp Cloud API live-test connector

Create/configure a Meta App with WhatsApp API access, then set:

```bash
export export SMARTPORT_WHATSAPP_ENABLED=true
export export SMARTPORT_WHATSAPP_MODE=LiveTest
export SMARTPORT_WHATSAPP_ACCESS_TOKEN (set this server-side only)
export SMARTPORT_WHATSAPP_PHONE_NUMBER_ID="..."
export SMARTPORT_WHATSAPP_BUSINESS_ACCOUNT_ID="..."
export SMARTPORT_WHATSAPP_VERIFY_TOKEN="..."
export SMARTPORT_WHATSAPP_GRAPH_VERSION="v22.0"
export SMARTPORT_PUBLIC_BASE_URL="https://smartport.culltron.app"
export SMARTPORT_WHATSAPP_TEST_RECIPIENT_NUMBER="..." # optional, never commit
```

Webhook callback URL:

```text
https://smartport.culltron.app/webhooks/whatsapp
```

Verify challenge test:

```bash
curl "https://smartport.culltron.app/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=YOUR_VERIFY_TOKEN&hub.challenge=12345"
```

Expected response:

```text
12345
```

Outbound endpoint used by Smart Port when enabled and safe:

```text
POST https://graph.facebook.com/{SMARTPORT_WHATSAPP_GRAPH_VERSION}/{SMARTPORT_WHATSAPP_PHONE_NUMBER_ID}/messages
```

Payload shape:

```json
{
  "messaging_product": "whatsapp",
  "to": "<approved-recipient>",
  "type": "text",
  "text": { "body": "<safe operational message>" }
}
```

Safety rules:

- Demo mode never calls Meta; it stores simulated notifications.
- LiveTest sends only to `SMARTPORT_WHATSAPP_TEST_RECIPIENT_NUMBER` or manually added approved/consented test drivers.
- Live mode still requires approved/consented recipients and operational approval.
- Meta message IDs are stored when returned; failures are recorded safely without losing the operational ticket/history.

## Future live data sources

Pilot/live data connectors can ingest IPMS/TOS, berth schedules, gate OCR/RFID, weighbridge data, fleet GPS/telematics, driver mobile app events, WhatsApp Cloud API inbound messages, ERP/fleet systems, weather/disruption feeds and emissions factors.


## Demo and judge codes

For hackathon judging, enable visible demo codes with `SMARTPORT_SHOW_DEMO_CREDENTIALS=true`. Codes: Port Admin `smartport2026`, Fleet Owner `smartport2026`, Driver `smartport2026`, Judge `smartport2026`. Do not commit real Gemini keys, WhatsApp tokens, `.env` files or private phone numbers.

## Final API Verification Readiness

Use these environment variables for a Gemini + WhatsApp LiveTest-ready run. Keep values in the deployment environment only; do not commit `.env` files, API keys, access tokens, phone numbers, or bearer tokens.

```bash
export GEMINI_API_KEY (set this to your server-side key)
export Gemini__Enabled=true
export Gemini__Mode=Hybrid
export Gemini__Model=gemini-2.5-flash

export SMARTPORT_WHATSAPP_ENABLED=true
export SMARTPORT_WHATSAPP_MODE=LiveTest
export SMARTPORT_WHATSAPP_ACCESS_TOKEN (set this server-side only)
export SMARTPORT_WHATSAPP_PHONE_NUMBER_ID="..."
export SMARTPORT_WHATSAPP_BUSINESS_ACCOUNT_ID="..."
export SMARTPORT_WHATSAPP_VERIFY_TOKEN="..."
export SMARTPORT_WHATSAPP_GRAPH_VERSION=v20.0
export SMARTPORT_PUBLIC_BASE_URL=https://your-forwarded-or-deployed-host
export SMARTPORT_WHATSAPP_TEST_RECIPIENT_NUMBER="..."
```

Verification commands:

```bash
./scripts/api-check.sh http://localhost:8080
./scripts/link-audit.sh http://localhost:8080
```

The API check confirms mobile demo login, token-protected truck status, token-protected notifications, invalid mobile-token rejection, invalid WhatsApp verify-token rejection, valid WhatsApp verify-token challenge when `SMARTPORT_WHATSAPP_VERIFY_TOKEN` is set, and Gemini readiness status through `/health/integrations`.

Smoke and link-audit scripts never require a real Meta send. LiveTest sending should only target `SMARTPORT_WHATSAPP_TEST_RECIPIENT_NUMBER`, and UI/logs must continue to mask live recipient details.

## Final enterprise hackathon pass — Gemini, mobile driver loop, and connector readiness

Smart Port is designed so the only remaining production step is adding approved live environment variables/API keys. Demo data is synthetic and the product does not claim live Transnet/IPMS/TOS/customer/production WhatsApp operation.

### Judge demo codes and path
When the app runs in Development or `SMARTPORT_SHOW_DEMO_CREDENTIALS=true`, `/demo-access` shows quick-fill role cards:
- Port Admin: `smartport2026` → `/dashboard`
- Fleet Owner: `smartport2026` → `/fleet`
- Driver: `smartport2026` → `/driver-app`
- Judge: `smartport2026` → `/demo-tour`

Recommended judging path: `/demo-access` → Judge quick-fill → `/demo-tour` → `/gemini-agent` → `/agent-governance` → `/ops-ingest` → `/execution/plans` → `/fleet/trucks/SPQ-2026-0042` → `/driver-app` → `/fleet/download-app` → `/fleet/notifications` → `/enterprise-readiness` → `/health/integrations`.

### Gemini award centerpiece and quota-safe model strategy
Gemini is openly used as the on-demand AI agent layer. It activates only when a user submits an explicit Gemini/AI/Copilot/Ops Ingest/driver command action, not on normal page loads, health checks, dashboards, enterprise readiness, Android app launch, mobile login/status refresh, seed data, smoke tests, or background timers.

Default server-side configuration:
- `GEMINI_API_KEY`
- `Gemini__Enabled=true` by default; without `GEMINI_API_KEY` deterministic fallback is used
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

The app has editable backend URL setup, quick-fill `smartport2026`, quick-fill `SPQ-2026-0042`, truck status, driver action buttons, notifications, Driver Copilot, and WhatsApp connector-readiness explanation. All Gemini and WhatsApp calls happen on the backend; the device stores no provider secrets.

GitHub Actions builds and uploads the debug APK artifact. If a local Android SDK is available, build with `cd mobile/SmartPortDriverCompanion && gradle assembleDebug` (or `./gradlew assembleDebug` when a wrapper is present) and optionally copy the debug APK to `src/SmartPort.Web/wwwroot/downloads/SmartPortDriverCompanion-debug.apk`.

### WhatsApp connector-ready position
WhatsApp Cloud API is implemented as a connector-ready sandbox/live-test integration with webhook verification, inbound parser, gated outbound sender, masked contacts, status labels, and safe failure when credentials/approval are missing. Production use requires WhatsApp Business setup, opt-in/templates, and billing. Smart Port does not depend on WhatsApp for the judge demo.

### Pilot/live data path
Future live pilots can connect approved IPMS/TOS, gate/OCR/RFID, weighbridge, fleet GPS/telematics, berth/yard, ERP, weather/disruption, emissions, driver app, and WhatsApp Cloud API feeds after NDA/data-sharing, field mapping, credential provisioning, security review, and supervised KPI validation.
