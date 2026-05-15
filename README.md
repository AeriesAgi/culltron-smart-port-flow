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
export GEMINI_API_KEY="..."
export Gemini__Enabled=true
export Gemini__Model=gemini-2.5-flash
```

The Gemini console shows key configured yes/no, enabled yes/no, model, mode, fallback status, latest generation and latency. All AI actions remain human-approved.

## WhatsApp setup

WhatsApp is simulated by default. Live test mode requires approved Meta credentials, approved test recipients and verify token configuration. Do not claim production WhatsApp integration unless credentials and approval exist.

Relevant endpoints:

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
