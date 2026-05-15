# Culltron Smart Port Flow — Gemini Agent Enterprise Demo

**Live demo URL:** `https://<your-smart-port-demo-url>`

Smart Port is an ASP.NET Core 8 MVC + PostgreSQL/Docker prototype for an enterprise AI logistics operations platform. It demonstrates a complete, human-approved loop: public website → role-based demo login → Port Admin command centre → Gemini Operations Agent → execution plan → fleet owner action → driver Android/mobile status → WhatsApp/demo notification → driver confirmation/location/check-in → updated queue state → audit trail → emissions/idling impact report.

## Demo access

Use `/demo-access` for role-based Identity sign-in. Seeded users are created automatically when the database is initialised:

- Port Admin Demo: `admin@smartport.culltron.app`
- Fleet Owner Demo: `fleet.owner@smartport.culltron.app`
- Driver Demo: `driver@smartport.culltron.app`
- Judge Demo: `judge@smartport.culltron.app`

Password for seeded demo users: `SmartPort@2026!`. Do not expose credentials in production unless `SMARTPORT_SHOW_DEMO_CREDENTIALS=true`.

## Exact judge path

1. `/` public story
2. `/demo-access` → Judge Demo
3. `/demo-tour`
4. `/dashboard`
5. `/gemini-agent` → generate operations brief
6. `/execution/plans` → generate/open plan
7. `/fleet/trucks/SPQ-2026-0042` → request location/send demo WhatsApp/move to staging
8. `/driver/demo` → confirm driver action
9. `/fleet/notifications`
10. `/reports` or `/enterprise-readiness`

## Gemini setup

Set Gemini configuration through environment variables or configuration:

```bash
GEMINI_API_KEY=<key>
Gemini__Enabled=true
Gemini__Mode=Hybrid
Gemini__Model=gemini-2.5-flash
```

If Gemini is unavailable, the platform remains demo-ready using local deterministic fallback and clearly labels fallback mode.

## WhatsApp setup

Webhook verification uses `SMARTPORT_WHATSAPP_VERIFY_TOKEN`. Live test outbound messaging requires `SMARTPORT_WHATSAPP_MODE=LiveTest`, approved sender numbers, `SMARTPORT_WHATSAPP_ACCESS_TOKEN`, and `SMARTPORT_WHATSAPP_PHONE_NUMBER_ID`. Demo mode uses simulated notifications only.

## Android APK workflow

The driver companion lives in `mobile/SmartPortDriverCompanion`. GitHub Actions builds a debug APK with Java/Kotlin target 17 and uploads `app/build/outputs/apk/debug/*.apk`.

```bash
cd mobile/SmartPortDriverCompanion
gradle assembleDebug
ls -lah app/build/outputs/apk/debug/
```

## Architecture summary

- ASP.NET Core 8 MVC with Identity roles and policies
- PostgreSQL via Docker Compose
- Gemini/hybrid/local fallback agent narrative services
- In-memory demo fleet/driver queue state and notification history
- Mobile API protected by `X-SmartPort-Mobile-Token`
- WhatsApp webhook verification and simulated/live-test-safe notification sender
- Public website plus protected command/fleet/driver/report surfaces

## Honest integration boundary

No live Transnet, IPMS, terminal, customer, WhatsApp production, or GPS production integration is claimed. Current data is synthetic demo data. Integration surfaces are connector-ready for approved pilot credentials and human-approved action workflows.
