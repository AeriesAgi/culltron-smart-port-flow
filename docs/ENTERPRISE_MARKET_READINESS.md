# Enterprise Market Readiness

Smart Port is demo-ready for judging and connector-ready for pilots. It is not represented as a live production integration.

Statuses:

- Demo Ready: role login, Gemini/fallback console, execution loop, fleet/driver pages, mobile API, governance and audit views.
- Configured: Identity policies, environment-based secrets, health endpoints, Docker/PostgreSQL deployment shape.
- Connector Ready: IPMS/TOS/gate/fleet/GPS/WhatsApp data-source boundaries.
- Needs Pilot Credentials: live WhatsApp, live Gemini, live port/fleet/GPS feeds.
- Production Hardening Required: SSO, secrets vault, observability, DR, rate limiting, pen test and runbooks.

Pilot pathway: NDA → systems identification → field mapping → sandbox connector → supervised pilot → KPI validation → production hardening.


## Live connector setup

See `docs/LIVE_API_SETUP.md` for exact Gemini and WhatsApp Cloud API environment variables, webhook callback setup, curl verify test, and safety rules. Future pilot/live connectors can ingest IPMS/TOS, gate systems, fleet GPS, weighbridge, ERP, WhatsApp Cloud API, driver app, berth schedules, weather/disruption feeds and emissions systems.
