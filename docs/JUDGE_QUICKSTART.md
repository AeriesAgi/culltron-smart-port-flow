# Judge Quickstart

1. Open the live URL or `http://localhost:8080`.
2. Click **Demo Access**.
3. Choose **Judge Demo**.
4. Open **Demo Tour**.
5. Open **Gemini Operations Agent** and click **Generate Gemini Operations Brief**.
6. Open **Execution Plans** and generate a plan.
7. Open truck `SPQ-2026-0042`, send a simulated WhatsApp or driver action, then review notification/timeline history.
8. Open **Agent Governance** and **Enterprise Readiness**.

Best path: `/demo-access` → Judge Demo → `/demo-tour` → `/gemini-agent` → `/execution/plans` → `/fleet/trucks/SPQ-2026-0042` → `/enterprise-readiness`.


## Live connector setup

See `docs/LIVE_API_SETUP.md` for exact Gemini and WhatsApp Cloud API environment variables, webhook callback setup, curl verify test, and safety rules. Future pilot/live connectors can ingest IPMS/TOS, gate systems, fleet GPS, weighbridge, ERP, WhatsApp Cloud API, driver app, berth schedules, weather/disruption feeds and emissions systems.


## Demo access codes

If codes are visible, use **Judge**: `culltron-judge-2026`. Other role codes are Port Admin `culltron-admin-2026`, Fleet Owner `culltron-fleet-2026`, and Driver `culltron-driver-2026`.

Full low-friction path: `/demo-access` → quick-fill Judge code → `/demo-tour` → `/gemini-agent` → `/agent-governance` → `/ops-ingest` → `/execution/plans` → `/fleet/trucks/SPQ-2026-0042` → simulated WhatsApp → `/driver/demo` → `/fleet/notifications` → `/enterprise-readiness` → `/health/integrations`.
