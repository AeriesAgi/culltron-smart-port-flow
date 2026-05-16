# Culltron Smart Port Flow

Culltron Smart Port Flow is an enterprise AI operations agent for smart ports and freight logistics. It turns synthetic/demo port signals into execution plans, fleet instructions, driver updates, simulated WhatsApp/mobile actions, audit trails and emissions/idling impact insights.

**Live demo URL:** replace with deployed URL when available. Local default is `http://localhost:8080` when Docker is available.

## Judge path

1. Open the live URL or local URL.
2. Click **Demo Access**.
3. Choose **Judge Demo**.
4. Use the provided access code or seeded Identity account if enabled.
5. Open **Demo Tour**.
6. Open **Gemini Operations Agent** and generate a brief.
7. Generate an execution plan, open truck `SPQ-2026-0042`, send a simulated WhatsApp/driver action, then review audit/notification history.
8. Open **Agent Governance** and **Enterprise Readiness**.

## Demo credentials

Seeded users use password `SmartPort@2026!`:

| Role | Email | Landing |
| --- | --- | --- |
| Port Admin | `admin@smartport.culltron.app` | `/dashboard` |
| Fleet Owner | `fleet.owner@smartport.culltron.app` | `/fleet` |
| Driver | `driver@smartport.culltron.app` | `/driver/demo` |
| Judge Demo | `judge@smartport.culltron.app` | `/demo-tour` |

Production deployments should not expose role cards/access codes unless `SMARTPORT_SHOW_DEMO_CREDENTIALS=true` is intentionally set.

## Gemini setup

Gemini is optional. Without configuration, the demo runs deterministic fallback safely.

```bash
export GEMINI_API_KEY (set this to your server-side key)
export Gemini__Enabled=true
export Gemini__Model=gemini-2.5-flash
# fallback names also supported: GEMINI_ENABLED, GEMINI_MODE, GEMINI_MODEL
```

The Gemini console shows key configured yes/no, enabled yes/no, model, mode, fallback status, latest generation and latency. Use **Run Live Gemini Test** on `/gemini-agent` to verify the server-side connector. All AI actions remain human-approved.

## WhatsApp setup

WhatsApp is simulated by default. LiveTest/Live mode can call Meta WhatsApp Cloud API when server-side credentials are configured. LiveTest sends only to an approved test recipient or consented test driver. Do not claim production WhatsApp integration unless credentials, approval and live operations are configured.

Required WhatsApp environment variables are documented in `docs/LIVE_API_SETUP.md`. Relevant endpoints:

- `GET /webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=...&hub.challenge=...`
- `POST /webhooks/whatsapp`
- Fleet simulated sends from `/fleet/trucks/SPQ-2026-0042`

## Mobile and Android

Mobile API demo flow:

- `POST /api/mobile/auth/demo-login`
- Header: `X-SmartPort-Mobile-Token`
- `GET /api/mobile/truck/status/SPQ-2026-0042`
- `GET /api/mobile/notifications/SPQ-2026-0042`
- `POST /api/mobile/driver/confirm-status`
- `POST /api/mobile/driver/location-checkin`
- `POST /api/mobile/copilot/driver`
- `POST /api/mobile/copilot/fleet`

The Android workflow is `.github/workflows/build-android-apk.yml`; it uses JDK 17, Android SDK setup, Gradle 8.14.4 and uploads `mobile/SmartPortDriverCompanion/app/build/outputs/apk/debug/*.apk`.

## Docker setup

```bash
docker compose up -d --build
docker compose ps
docker compose logs --tail=150 web
```

## Local .NET commands

```bash
dotnet restore SmartPort.sln
dotnet build SmartPort.sln
dotnet publish src/SmartPort.Web/SmartPort.Web.csproj -c Release
```

## Architecture

- ASP.NET Core MVC web app with Identity role login.
- PostgreSQL via Entity Framework Core.
- Fleet/driver queue services using synthetic demo operational data.
- Gemini-enhanced agent service with deterministic fallback.
- Mobile API with memory-only demo tokens.
- WhatsApp webhook and simulated notification history.
- Governance, health and enterprise readiness pages.

## Market/pilot readiness

Smart Port is pilot-ready and connector-ready for approved integrations with IPMS/TOS/gate/fleet/GPS/WhatsApp systems. A real pilot requires NDA/data sharing, field mapping, sandbox connector, supervised operations, KPI validation and production hardening.

## Limitations and honest claims

- No live Transnet, IPMS, TOS, customer, GPS or production WhatsApp integration is claimed.
- Demo data is synthetic.
- Gemini is only used when configured; deterministic fallback always works.
- External sends and operational changes require human approval.
- Integration requires approved pilot credentials.


## Live connector setup

See `docs/LIVE_API_SETUP.md` for exact Gemini and WhatsApp Cloud API environment variables, webhook callback setup, curl verify test, and safety rules. Future pilot/live connectors can ingest IPMS/TOS, gate systems, fleet GPS, weighbridge, ERP, WhatsApp Cloud API, driver app, berth schedules, weather/disruption feeds and emissions systems.


## Final judge demo codes

When running locally or with `SMARTPORT_SHOW_DEMO_CREDENTIALS=true`, `/demo-access` shows quick-fill cards for: Port Admin `culltron-admin-2026`, Fleet Owner `culltron-fleet-2026`, Driver `culltron-driver-2026`, and Judge `culltron-judge-2026`. Judge Demo is the recommended hackathon path.

## Driver/mobile companion story

Primary driver channels are WhatsApp sandbox/live-test, the web driver companion at `/driver/demo`, and the mobile API. The optional Android source lives at `mobile/SmartPortDriverCompanion`; APKs are built through GitHub Actions or Android Studio and should contain no Gemini or WhatsApp secrets.

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
