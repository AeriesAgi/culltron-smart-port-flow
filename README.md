# Culltron Smart Port Flow

Culltron Smart Port Flow is an enterprise AI operations agent for smart ports and freight logistics. It turns synthetic/demo port signals into Gemini-assisted execution plans, fleet instructions, Driver Companion App/web/mobile API check-ins, fleet/control-room tracking, audit trails and emissions/idling impact insights.

**Live demo URL:** replace with deployed URL when available. Local default is `http://localhost:8080` when Docker is available.

## Judge path

1. Open the live URL or local URL.
2. Click **Demo Access**.
3. Choose **Judge Demo**.
4. Use the provided access code or seeded Identity account if enabled.
5. Open **Demo Tour**.
6. Open **Gemini Operations Agent** and generate a brief.
7. Generate an execution plan, open truck `SPQ-2026-0042`, use the Driver Companion App/driver app shell to send a status/check-in/location update, then review fleet tracker, truck timeline, audit trail, queue/ETA/idling/CO₂ impact.
8. Open **Agent Governance** and **Enterprise Readiness**.

## Demo credentials

Seeded users use password `SmartPort@2026!`:

| Role | Email | Landing |
| --- | --- | --- |
| Port Admin | `admin@smartport.culltron.app` | `/dashboard` |
| Fleet Owner | `fleet.owner@smartport.culltron.app` | `/fleet` |
| Driver | `driver@smartport.culltron.app` | `/driver-app` |
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

WhatsApp is optional connector-ready only. LiveTest/Live mode can call Meta WhatsApp Cloud API when server-side credentials are configured, but the system does not depend on WhatsApp production approval. LiveTest sends only to an approved test recipient or consented test driver. Do not claim production WhatsApp integration unless credentials, approval and live operations are configured.

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

The Android workflow is `.github/workflows/build-android-apk.yml`; it uses JDK 17, Android SDK setup, Gradle 8.14.4 and uploads the artifact `SmartPortDriverCompanion-debug.apk` from `mobile/SmartPortDriverCompanion/artifacts/SmartPortDriverCompanion-debug.apk`.

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

The Driver Companion App is the primary driver communication and tracking channel. The Android app, driver app shell at `/driver-app`, and mobile API send driver status, explicit check-ins and optional one-shot location labels/coordinates to Smart Port. Fleet owners and Port Admins track drivers through app-based updates in `/fleet/tracker` and dashboard tracker summaries. No secrets live on the device; all Gemini and connector calls remain backend-side. Location/check-in is user-triggered only, not background tracking. WhatsApp remains optional connector-ready proof for future pilots/live-test messaging.

## Final enterprise hackathon pass — Gemini, mobile driver loop, and connector readiness

Smart Port is designed so the only remaining production step is adding approved live environment variables/API keys. Demo data is synthetic and the product does not claim live Transnet/IPMS/TOS/customer/production WhatsApp operation.

### Judge demo codes and path
When the app runs in Development or `SMARTPORT_SHOW_DEMO_CREDENTIALS=true`, `/demo-access` shows quick-fill role cards:
- Port Admin: `culltron-admin-2026` → `/dashboard`
- Fleet Owner: `culltron-fleet-2026` → `/fleet`
- Driver: `culltron-driver-2026` → `/driver-app`
- Judge: `culltron-judge-2026` → `/demo-tour`

Recommended judging path: `/demo-access` → Judge quick-fill → `/demo-tour` → `/gemini-agent` → `/agent-governance` → `/ops-ingest` → `/execution/plans` → `/fleet/trucks/SPQ-2026-0042` → `/driver-app` → `/fleet/download-app` → `/fleet/notifications` → `/enterprise-readiness` → `/health/integrations`.

### Gemini award centerpiece and quota-safe model strategy
Gemini is openly used as the on-demand AI agent layer. It activates only when a user submits an explicit Gemini/AI/Copilot/Ops Ingest/driver command action, not on normal page loads, health checks, dashboards, enterprise readiness, Android app launch, mobile login/status refresh, seed data, smoke tests, or background timers.

Default server-side configuration:
- `GEMINI_API_KEY`
- `Gemini__Enabled=true` by default; without `GEMINI_API_KEY` Smart Port uses deterministic fallback, and operators can set `Gemini__Enabled=false` to disable calls
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
- Deterministic fallback is always available and produces polished driver instructions, fleet plans, executive summaries, governance/risk reviews, disruption recovery, emissions/idling impact, and Copilot responses.

The backend skips unsupported/model-not-found responses, marks quota-limited models for cooldown, avoids retry loops, records safe diagnostics (counts, action type, route/source, model, latency, quota/fallback state), and never logs or displays API keys, bearer tokens, prompts containing secrets, WhatsApp tokens, phone numbers, or provider credentials.

### Driver Companion APK, driver app shell, and mobile API
The primary driver channels are the Android Driver Companion, the driver app shell, and token-secured mobile APIs. The Android app lives in `mobile/SmartPortDriverCompanion`, uses Java/Kotlin 17, and calls:
- `POST /api/mobile/auth/demo-login`
- `GET /api/mobile/truck/status/{reference}`
- `GET /api/mobile/notifications/{reference}`
- `POST /api/mobile/driver/confirm-status`
- `POST /api/mobile/driver/location-checkin`
- `POST /api/mobile/copilot/driver`

The app has editable backend URL setup, quick-fill `culltron-driver-2026`, quick-fill `SPQ-2026-0042`, truck status, driver action buttons, notifications, Driver Copilot, and WhatsApp connector-readiness explanation. All Gemini and WhatsApp calls happen on the backend; the device stores no provider secrets.

GitHub Actions builds and uploads the debug APK artifact named `SmartPortDriverCompanion-debug.apk`. If a local Android SDK is available, build with `cd mobile/SmartPortDriverCompanion && gradle assembleDebug` (or `./gradlew assembleDebug` when a wrapper is present) and optionally copy the debug APK to `src/SmartPort.Web/wwwroot/downloads/SmartPortDriverCompanion.apk`.

### WhatsApp connector-ready position
WhatsApp Cloud API is implemented as a connector-ready sandbox/live-test integration with webhook verification, inbound parser, gated outbound sender, masked contacts, status labels, and safe failure when credentials/approval are missing. Production use requires WhatsApp Business setup, opt-in/templates, and billing. Smart Port does not depend on WhatsApp for the judge demo.

### Pilot/live data path
Future live pilots can connect approved IPMS/TOS, gate/OCR/RFID, weighbridge, fleet GPS/telematics, berth/yard, ERP, weather/disruption, emissions, driver app, and WhatsApp Cloud API feeds after NDA/data-sharing, field mapping, credential provisioning, security review, and supervised KPI validation.


## Final app-first tracking storyline

Control room / Port Admin → Gemini Operations Agent → execution plan → fleet owner → Driver Companion App → driver status + location/check-in updates → fleet tracker + port admin tracker → queue/ETA/idling/CO₂ recalculation → audit/governance trail.

- Driver Companion App is the primary driver communication and tracking channel.
- Fleet owners and Port Admins can track drivers through app-based updates.
- Gemini is embedded as an on-demand enterprise operations agent and can summarize tracked driver risk, stale check-ins and action recommendations.
- WhatsApp is optional connector-ready only; Smart Port does not depend on WhatsApp production approval.
- Synthetic data is used for judging; live connectors can replace it in a supervised pilot.

## Final polish pass: app-first tracking and APK path

The current completion pass positions the Driver Companion App as the primary mobile operations channel. Driver GPS/manual check-ins flow through the driver app shell, Android app or mobile API into `/fleet/tracker`, truck detail timelines, ETA/queue state, impact metrics and audit history. WhatsApp remains optional connector-ready support only.

For the Android APK path, use `cd mobile/SmartPortDriverCompanion && ./gradlew assembleDebug` or run `.github/workflows/build-android-apk.yml`. Publish the resulting APK to `src/SmartPort.Web/wwwroot/downloads/SmartPortDriverCompanion.apk` to enable the direct `/fleet/download-app` download button. See `docs/FINAL_WINNER_POLISH.md` for the recommended judge demo path and production caveats.

## Enterprise AI submission-ready flow

Final demo route: `/` → `/dashboard` → `/execution` → `/execution/plans/{id}` → `/fleet` → `/fleet/tracker` → `/driver-app` → `/gemini-agent` → `/emissions` → `/enterprise-readiness`.

The winning story is one execution loop: control room detects congestion, Gemini/deterministic fallback creates an execution plan, fleet owners coordinate affected trucks, drivers use the Driver Companion App for actions and GPS/manual check-ins, fleet tracker updates, emissions/idling impact is shown and every decision is audit logged.

DigitalOcean redeploy:

```bash
docker compose build web
docker compose up -d db web
```

Set `ASPNETCORE_URLS=http://+:8080`, a production connection string, demo codes if needed and `GEMINI_API_KEY` only when Gemini should run server-side. Do not hardcode secrets.
