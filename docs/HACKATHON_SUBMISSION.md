# Hackathon Submission

## 1-line pitch

Culltron Smart Port Flow is an enterprise AI operations agent that turns port congestion signals into human-approved execution plans, fleet/driver actions, audit trails and emissions impact.

## 30-second pitch

Smart Port is not just a chatbot. It is a closed operational loop for smart ports and freight logistics: control room signals flow into a Gemini-enhanced agent, the agent compares deterministic fallback against AI reasoning, a human approves an execution plan, fleet owners and drivers receive simulated mobile/WhatsApp actions, and every step is audited with idling and CO₂ impact.

## Long description

The demo uses synthetic port, truck, gate, staging, berth and notification signals to show how an enterprise AI operations agent can coordinate congestion response. Gemini enhances explanations when configured; deterministic fallback always works. The product is connector-ready for IPMS/TOS/gate/fleet/GPS/WhatsApp pilots but makes no unsupported live-integration claims.

## Tags/categories

Enterprise AI, AI Agent Olympics, Gemini, logistics, smart ports, fleet operations, agent governance, mobile API, WhatsApp-ready, sustainability.

## Judging strengths

- Full closed loop from public website to Identity login, Gemini agent, execution plan, fleet/driver action, audit and emissions impact.
- Human-approved workflow with visible governance controls.
- Works without Gemini API key through deterministic fallback.
- Honest pilot-ready and connector-ready production story.
- Android/mobile and WhatsApp paths are represented without storing secrets.

## AI Agent Olympics angle

The agent reads operational state, detects bottlenecks, generates a baseline plan, asks Gemini for reasoning when configured, compares output, gates actions through human approval, saves audit history and recommends fleet/driver actions.

## Enterprise AI angle

Identity roles, authorization policies, health/readiness checks, environment-based secrets, audit history, safety checks and production-hardening backlog make it credible for enterprise review.

## Gemini use

Gemini can generate operations briefs, fleet plans, driver instructions, disruption recovery, emissions impact, executive summaries, risk/governance reviews and pilot readiness briefs.

## Governance use

The `/agent-governance` page demonstrates allowed actions, blocked actions, approval matrix, secret exfiltration blocking, prompt-injection risk checks and audit entries.

## Business value

Reduce idling, improve ETA reliability, coordinate gate/staging actions, lower CO₂ impact and create a governed pilot path for port/freight operators.

## Originality

Smart Port combines agentic reasoning with real operational workflow: fleet instructions, driver companion, simulated WhatsApp, audit and emissions impact.

## Limitations

Synthetic demo data only; no live Transnet/IPMS/customer/WhatsApp/GPS integration is claimed. Production requires approved credentials, pilot governance and hardening.


## Live connector setup

See `docs/LIVE_API_SETUP.md` for exact Gemini and WhatsApp Cloud API environment variables, webhook callback setup, curl verify test, and safety rules. Future pilot/live connectors can ingest IPMS/TOS, gate systems, fleet GPS, weighbridge, ERP, WhatsApp Cloud API, driver app, berth schedules, weather/disruption feeds and emissions systems.


## Final judge route

Use `/demo-access` with Judge code `culltron-judge-2026`, then follow Demo Tour through Gemini, governance, ops ingest, execution plans, truck action, driver companion, notifications, enterprise readiness and health integrations.

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
