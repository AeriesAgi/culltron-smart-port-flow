# Culltron Smart Port Flow — Product Roadmap

Culltron Smart Port Flow is currently a working deployed demo/prototype and pilot-ready architecture using synthetic/demo data. The roadmap below separates what is available in the demo from what would require partner approval, security review, live data integration, and operational validation.

## Phase 1 — Demo Validation (Current)

- [x] ASP.NET Core MVC / .NET 8 web application.
- [x] PostgreSQL via Docker Compose.
- [x] Enterprise-style Smart Port operations dashboard.
- [x] AI Command Centre.
- [x] SmartPort Copilot Chat.
- [x] Gemini Agent Mode using `gemini-2.5-flash`.
- [x] Hybrid mode with local offline-safe fallback.
- [x] Button/user-triggered Gemini responses only.
- [x] Reports / Agent Intelligence Desk.
- [x] Scenario simulator.
- [x] Truck tracking / ETA demo views.
- [x] Emissions and clean logistics impact estimates.
- [x] Recommendations / decision support.
- [x] Pilot readiness and stakeholder value pages.
- [x] Synthetic/demo seeded data only.
- [x] Human-approved recommendations only; no automatic operational execution.

## Phase 2 — Data Mapping and Stakeholder Discovery

- [ ] Confirm pilot stakeholders, decision rights, operating constraints, and success metrics.
- [ ] Map required data sources for gate queues, truck dispatch/telematics, berth/vessel schedules, yard status, incidents, energy disruption, and emissions factors.
- [ ] Define data minimization, privacy, retention, security, and audit requirements.
- [ ] Validate where Gemini-enhanced summaries add value and where governed baseline outputs should remain authoritative.

## Phase 3 — Sandbox Integration

- [ ] Connect approved sample/sandbox feeds rather than live operational feeds.
- [ ] Validate schema mappings, data quality, latency, and failure handling.
- [ ] Keep local fallback available when external feeds or Gemini are unavailable.
- [ ] Confirm that recommendations remain human-reviewed and audit-friendly.

## Phase 4 — Controlled Pilot

- [ ] Run limited-scope pilot workflows with approved users and approved data.
- [ ] Compare pilot outputs against baseline queue, turnaround, idling, and emissions metrics.
- [ ] Review security, compliance, model governance, and operational escalation paths.
- [ ] Measure recommendation adoption and operator feedback.

## Phase 5 — Operational Scale-Up

- [ ] Expand integrations after pilot validation and stakeholder approval.
- [ ] Add enterprise controls such as SSO, advanced RBAC, audit exports, and reporting hardening.
- [ ] Support multi-terminal or multi-port operating models where justified.
- [ ] Move estimated savings and emissions claims to validated, evidence-backed reporting only after real pilot measurement.

## Honest Scope Notes

- The current public repository contains placeholders only and must not contain API keys or private deployment values.
- The demo is not connected to live port/IPMS/Navayuga/Transnet systems.
- Savings, emissions, and impact figures are indicative/demo outputs until validated in a controlled pilot.
