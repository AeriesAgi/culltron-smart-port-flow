# Deployment Smoke Test

Run:

```bash
chmod +x scripts/smoke-test.sh
./scripts/smoke-test.sh http://localhost:8080
```

Manual pages: `/`, `/platform`, `/demo-access`, `/demo-tour`, `/dashboard`, `/gemini-agent`, `/agent-governance`, `/enterprise-readiness`, `/execution/plans`, `/fleet`, `/fleet/trucks/SPQ-2026-0042`, `/driver/demo`, `/fleet/notifications`, `/fleet/download-app`, `/health`, `/health/readiness`, `/health/integrations`.

API checks: mobile demo login, token-protected truck status, invalid token rejection and WhatsApp webhook verify token success/failure.


## Live connector setup

See `docs/LIVE_API_SETUP.md` for exact Gemini and WhatsApp Cloud API environment variables, webhook callback setup, curl verify test, and safety rules. Future pilot/live connectors can ingest IPMS/TOS, gate systems, fleet GPS, weighbridge, ERP, WhatsApp Cloud API, driver app, berth schedules, weather/disruption feeds and emissions systems.
