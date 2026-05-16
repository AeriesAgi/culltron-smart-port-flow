# Security and Agent Governance

- ASP.NET Identity roles protect operational areas.
- Demo cookies are secondary to Identity and are cleared on sign-out.
- Agent actions require human approval before external sends.
- Secrets are not included in prompts or UI output.
- Demo data is synthetic; no customer data is required.
- Prompt-injection style requests are treated as untrusted instructions.
- WhatsApp live sends require configured credentials and approved test recipients.
- `/agent-governance` demonstrates blocked secrets, approval bypass blocking, bulk-send gating and audit history.


## Live connector setup

See `docs/LIVE_API_SETUP.md` for exact Gemini and WhatsApp Cloud API environment variables, webhook callback setup, curl verify test, and safety rules. Future pilot/live connectors can ingest IPMS/TOS, gate systems, fleet GPS, weighbridge, ERP, WhatsApp Cloud API, driver app, berth schedules, weather/disruption feeds and emissions systems.
