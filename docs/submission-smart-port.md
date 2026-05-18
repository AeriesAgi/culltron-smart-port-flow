# Smart Port submission package

Culltron Smart Port Flow is an agentic port operations copilot for congestion, queue execution, fleet coordination, driver app check-ins, emissions-aware operations and auditable decision support.

## Judge story

Control room detects queue pressure or disruption, generates an execution plan, approves/dispatches it to fleet owners and drivers, receives Driver Companion check-ins, updates the fleet tracker, records audit history and shows idling/CO₂ impact.

## Core demo routes

- `/` public enterprise landing.
- `/dashboard` control-room command centre.
- `/execution` and `/execution/plans/{id}` execution workflow.
- `/fleet` fleet owner dashboard.
- `/fleet/tracker` app-based fleet tracker.
- `/driver-app` true app-first driver shell.
- `/gemini-agent` Gemini Operations Agent.
- `/emissions` emissions/idling impact.
- `/enterprise-readiness` pilot readiness.

## Honest limitations

Data is synthetic/demo. Port/TOS/IPMS/ERP/GIS/telematics integrations are connector-ready pilot architecture, not live production integration. WhatsApp requires approved Meta credentials and consented recipients. Driver GPS is tap-to-check-in only, not background surveillance.

## Demo access and environment

Safe fallback access code when no env code is configured: `smartport2026`.

Environment overrides: `SMARTPORT_SHOW_DEMO_CREDENTIALS`, `SMARTPORT_DEMO_ACCESS_CODE`, `SMARTPORT_ADMIN_DEMO_CODE`, `SMARTPORT_PORT_ADMIN_DEMO_CODE`, `SMARTPORT_FLEET_DEMO_CODE`, `SMARTPORT_DRIVER_DEMO_CODE`, and `SMARTPORT_JUDGE_DEMO_CODE`.

Role landings: Judge `/demo-tour`, Control Room `/dashboard`, Fleet Owner `/fleet`, Driver `/driver-app`.
