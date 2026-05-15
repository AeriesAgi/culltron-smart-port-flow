# Culltron Smart Port Flow

Culltron Smart Port Flow is an enterprise AI operations prototype for smart-port queue coordination. It demonstrates how a port control-room signal can become an execution plan, fleet-owner action, driver instruction, WhatsApp check-in, Android companion update, audit trail, and transparent idling/CO₂ impact estimate.

The current application uses synthetic operational data by default. It does **not** claim live IPMS, terminal operating system, gate, fleet, WhatsApp, GPS, or telematics integration unless those connectors are configured in a controlled pilot.

## Smart Port Product Modules

- Public product website with access-gated demo CTAs.
- Role-based Demo Access flow using `SMARTPORT_DEMO_ACCESS_CODE`, role-specific codes, and optional credential display.
- Control-room dashboard and operational reports.
- Fleet Owner Console with live queue, truck detail, notifications, settings, data sources, and Android companion page.
- Execution Plan Generator with truck-level actions and audit context.
- Driver Queue Companion for web/mobile status, commands, confirmations, and location check-ins.
- SmartPort Copilot with Gemini enhancement when configured and deterministic fallback when not.
- WhatsApp Demo Mode plus Meta WhatsApp Cloud API LiveTest readiness.
- Android Driver Companion source project under `mobile/SmartPortDriverCompanion`.

## Access boundary

Public pages remain open. Internal operational pages are protected by a lightweight demo access cookie:

- `/dashboard`
- `/fleet/*`
- `/driver/*`
- `/truck/*`
- `/execution/*`
- `/Copilot`
- `/Disruptions`
- `/Recommendations`
- `/Reports`
- `/api/mobile/*`

`/webhooks/whatsapp` remains externally reachable for Meta verification and inbound webhook delivery. It validates the configured verify token for GET verification and safely audits unapproved inbound senders.

Set demo access codes with environment variables:

```bash
export SMARTPORT_DEMO_ACCESS_CODE="use-a-private-general-demo-code"
export SMARTPORT_ADMIN_DEMO_CODE="use-a-private-admin-demo-code"
export SMARTPORT_FLEET_DEMO_CODE="use-a-private-fleet-demo-code"
export SMARTPORT_DRIVER_DEMO_CODE="use-a-private-driver-demo-code"
export SMARTPORT_SHOW_DEMO_CREDENTIALS=false
```

If codes are missing in Development, safe local defaults are available: `culltron-admin-2026`, `culltron-fleet-2026`, `culltron-driver-2026`, and `culltron-demo-2026`. Production only displays credentials when `SMARTPORT_SHOW_DEMO_CREDENTIALS=true`.

## Run locally

### Prerequisites

- .NET SDK compatible with the solution target framework.
- Docker and Docker Compose for PostgreSQL.

### Start PostgreSQL

```bash
docker compose up -d db
```

### Restore, build, and run

```bash
dotnet restore SmartPort.sln
dotnet build SmartPort.sln
dotnet run --project src/SmartPort.Web/SmartPort.Web.csproj
```

Then open the public landing page and use `/demo-access` to enter the internal demo.

## Configuration

### Demo access

| Variable | Purpose |
| --- | --- |
| `SMARTPORT_DEMO_ACCESS_CODE` | General access code for protected demo routes. |
| `SMARTPORT_ADMIN_DEMO_CODE` | Port Admin Demo role code. |
| `SMARTPORT_FLEET_DEMO_CODE` | Fleet Owner Demo role code. |
| `SMARTPORT_DRIVER_DEMO_CODE` | Driver Demo role code. |
| `SMARTPORT_SHOW_DEMO_CREDENTIALS` | `true` to intentionally display configured/public demo credentials. |

### Gemini Copilot

| Variable | Purpose |
| --- | --- |
| `GEMINI_API_KEY` | Server-side Gemini API key. Never expose it in UI or Android. |
| `Gemini__Enabled` | `true` to allow Gemini calls. |
| `Gemini__Mode` | `Hybrid`, `Gemini`, or `Local`. |
| `Gemini__Model` | Gemini model name, for example `gemini-2.5-flash`. |

When Gemini is unavailable, the deterministic fallback service continues to answer scoped Smart Port operations questions.

### WhatsApp Cloud API LiveTest

| Variable | Purpose |
| --- | --- |
| `SMARTPORT_WHATSAPP_MODE` | `Demo`, `ConnectorReady`, or `LiveTest`. |
| `SMARTPORT_WHATSAPP_ENABLED` | `true` to allow the connector path. |
| `SMARTPORT_WHATSAPP_ACCESS_TOKEN` | Meta Cloud API token. |
| `SMARTPORT_WHATSAPP_PHONE_NUMBER_ID` | Meta phone number ID. |
| `SMARTPORT_WHATSAPP_BUSINESS_ACCOUNT_ID` | Meta business account ID for readiness display. |
| `SMARTPORT_WHATSAPP_VERIFY_TOKEN` | Meta webhook verification token. |
| `SMARTPORT_WHATSAPP_GRAPH_VERSION` | Graph API version, default `v20.0`. |
| `SMARTPORT_PUBLIC_BASE_URL` | Public deployed base URL used to show the callback URL. |

LiveTest sends are blocked unless the app is in LiveTest mode, the connector is enabled, credentials are configured, and the driver contact is active, test-approved, consent-confirmed, and not a seeded demo contact.

## Demo flow

1. Open `/` and review the product story.
2. Select **Demo Access** and enter the configured code.
3. Open `/fleet` and review queue pressure, high-risk trucks, and execution plan summary.
4. Open `/execution/plans`, generate or inspect a plan, and review truck action cards.
5. Open `/fleet/trucks/SPQ-2026-0042` and send a simulated WhatsApp or in-app alert.
6. Open `/driver` or `/truck/status/SPQ-2026-0042` and run driver actions such as holding, staging, gate arrival, delay, issue, and Copilot.
7. Open `/fleet/notifications` and verify the communication timeline.
8. Open `/fleet/settings` and review Gemini/WhatsApp readiness without exposing secrets.
9. Open `/fleet/download-app` for Android companion source and build instructions.

## Mobile API smoke tests

After demo access is established in a browser, mobile API routes are protected from casual public access. For local service-level testing, run requests with the demo access cookie or temporarily test inside the authorized browser session.

Representative routes:

- `GET /api/mobile/truck/status/SPQ-2026-0042`
- `GET /api/mobile/notifications/SPQ-2026-0042`
- `POST /api/mobile/driver/confirm-status`
- `POST /api/mobile/driver/location-checkin`
- `POST /api/mobile/driver/command`
- `POST /api/mobile/copilot/driver`
- `POST /api/mobile/copilot/fleet`
- `GET /webhooks/whatsapp`
- `POST /webhooks/whatsapp`

## Deployment

Publish the web app with:

```bash
dotnet publish src/SmartPort.Web/SmartPort.Web.csproj -c Release
```

Deployment checklist:

- Set `SMARTPORT_DEMO_ACCESS_CODE` and role-specific demo codes.
- Set database connection string for PostgreSQL.
- Set `SMARTPORT_PUBLIC_BASE_URL` to the deployed HTTPS origin.
- Configure Gemini variables only if Gemini should be enabled.
- Configure WhatsApp variables only for ConnectorReady or LiveTest.
- Do not commit secrets, tokens, phone numbers, or private credentials.
- Demo mode works without Gemini or WhatsApp credentials.

## Android companion

The Android source project is at `mobile/SmartPortDriverCompanion`. It calls backend APIs, stores no provider secrets, and uses the backend for AI and WhatsApp coordination. Build the APK from Android Studio, from Gradle in an Android SDK environment, or via `.github/workflows/build-android-apk.yml`. Copy `app/build/outputs/apk/debug/app-debug.apk` to `src/SmartPort.Web/wwwroot/downloads/SmartPortDriverCompanion.apk` before deployment to expose the web download button.

## Integration roadmap

See `docs/integration-roadmap.md`, `docs/GEMINI_ENV_SETUP.md`, and `docs/WHATSAPP_LIVETEST_GUIDE.md` for the pilot integration plan. The demo is synthetic by design; production integration requires partner-approved access, security review, validation, and operational governance.
