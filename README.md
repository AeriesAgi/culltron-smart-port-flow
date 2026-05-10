# Culltron Smart Port Flow

**Culltron Smart Port Flow is an enterprise-style AI command centre for cleaner, smarter port logistics, combining operational dashboards, scenario simulation, emissions analysis, recommendations, and Gemini-enhanced agentic decision support.**

> Working deployed demo/prototype · ASP.NET Core MVC / .NET 8 · PostgreSQL via Docker Compose · synthetic/demo data · optional Gemini Agent Mode · local offline-safe fallback · human-approved recommendations.

Culltron Smart Port Flow demonstrates how port, terminal, fleet, sustainability, and municipal stakeholders could observe operational pressure, analyze bottlenecks, simulate disruptions, review emissions/idling impact, and prepare human-approved recommendations from one enterprise command-centre experience.

This repository is intentionally honest about scope: the current system is a deployed demo/prototype and pilot-ready architecture using synthetic/demo data. It does **not** claim live port/IPMS/Navayuga/Transnet integration, real customer data, signed pilots, guaranteed savings, or production deployment inside an actual port.

## Live Demo

- **Live demo:** https://smartport.culltron.app/
- **GitHub repo:** https://github.com/AeriesAgi/culltron-smart-port-flow
- **Main judge demo path:** Landing → Dashboard → AI Command Centre → SmartPort Copilot Chat → Truck Tracking → Scenario Simulator → Emissions → Recommendations / Reports

Additional hackathon assets, where available:

- **Hugging Face Space:** https://huggingface.co/spaces/atrx93/culltron-smart-port-flow
- **Demo video:** https://youtu.be/-R4SJraREP4
- **Pitch deck:** https://canva.link/6s7n1perwny0lbs

## What’s New: Gemini Agent Mode

Culltron Smart Port Flow now includes optional **Gemini Agent Mode** for richer operations summaries, agentic planning language, report drafting, recommendation explanations, and Copilot-style decision support.

- **Gemini 2.5 Flash support** via the `gemini-2.5-flash` model setting.
- **Gemini Agent Mode** for enterprise-style operational answers and structured reports.
- **Hybrid Mode** so Gemini can enhance operator-facing responses while deterministic local intelligence remains available.
- **Local Offline-Safe fallback** when Gemini is disabled, unavailable, or not configured.
- **Button/user-triggered responses only**; Gemini is not called automatically on page load.
- **No automatic Gemini calls on page load**, during seed generation, or in background loops.
- **Synthetic/demo operational context** only.
- **Human-approved recommendations** only.
- **No automatic execution** of operational actions.

Gemini is an optional enhancer, not a replacement for governance. The app must continue to run safely without a Gemini API key.

## Core Features

- **AI Command Centre** for system-level operational status, risk scoring, monitoring lanes, confidence indicators, and action guidance.
- **SmartPort Copilot Chat** for scoped natural-language questions about the demo operational state.
- **Gemini-enhanced operational answers** when Gemini is configured and the user intentionally triggers a response.
- **Executive/demo summaries** for judges, enterprise AI reviewers, and pilot/investor conversations.
- **Operator action plans** that translate synthetic/demo risk into clear next-step recommendations.
- **Scenario analysis** for congestion spikes, vessel delays, berth pressure, crane availability, yard backlog, peak gate pressure, and load-shedding-style disruption.
- **Truck tracking / ETA** with active trucks, delayed trucks, hold-outside-port candidates, priority-release candidates, geofence/checkpoint labels, and ETA risk.
- **Berth/gate/yard pressure** views for bottleneck monitoring.
- **Emissions / idling insights** with indicative fuel, CO₂, and avoidable-idling estimates.
- **Recommendation review** with explainable decision support and audit-friendly framing.
- **Reports / Agent Intelligence Desk** for executive operations briefs, scenario reports, operator action plans, emissions reports, incident response notes, pilot readiness summaries, and daily operations reports.
- **Pilot readiness and stakeholder value** pages for port authority, terminal operator, fleet operator, sustainability officer, municipality/province, and operations manager personas.

## Architecture

Culltron Smart Port Flow is built as a conventional enterprise .NET web application with a clear separation between UI, services, persistence, and optional AI enhancement.

- **ASP.NET Core MVC / .NET 8** web application with Razor views.
- **Services layer** for operational intelligence, Copilot responses, scenario simulation, truck tracking, emissions, reports, and recommendations.
- **PostgreSQL** persistence through Entity Framework Core.
- **Docker Compose** for local/demo orchestration of the web app and database.
- **Synthetic seeded data** for demo-safe port operations, truck flow, recommendations, incidents, and metrics.
- **Gemini optional service** for configured, user-triggered Gemini Agent Mode responses.
- **Local fallback service** for deterministic/offline-safe responses when Gemini is unavailable or disabled.
- **Human approval / audit-friendly decision support**: recommendations are reviewed by people and are not automatically executed.

Simplified flow:

```text
Razor UI / MVC Controllers
        ↓
Application + Infrastructure Services
        ↓
Synthetic PostgreSQL operational data
        ↓
Deterministic intelligence + optional Gemini enhancement
        ↓
Human-reviewed recommendations, reports, and scenario outputs
```

## AI Safety / Grounding

- Gemini receives only sanitized operational summaries, such as queue pressure, berth/yard status, active incident summaries, idling/emissions estimates, deterministic recommendation summaries, and the selected report or Copilot prompt.
- No secrets, credentials, API keys, connection strings, private deployment values, or sensitive internal configuration values are sent to Gemini.
- No live integration claims are made.
- No real customer data is used.
- No automatic execution of operational actions occurs.
- Recommendations require human review and approval.
- Local fallback remains available when Gemini is unavailable, disabled, or not configured.
- Gemini responses should use careful wording such as “demo,” “prototype,” “pilot-ready architecture,” “designed to integrate,” and “would require live data integration for production use.”

## Environment Variables

Use placeholders only in documentation and examples:

```bash
GEMINI_API_KEY=your_key_here
Gemini__Enabled=true
Gemini__Mode=Hybrid
Gemini__Model=gemini-2.5-flash
```

Security rules:

- Never commit `.env`.
- Never commit API keys.
- Use server environment variables, deployment secrets, or a local `.env` excluded from Git.
- Do not put real keys in `docker-compose.yml`, `Dockerfile`, `appsettings.json`, `appsettings.Development.json`, README examples, screenshots, logs, or source code.

## Local Run

From the repository root:

```bash
dotnet restore
dotnet build
docker compose up --build
```

Default local URL:

```text
http://localhost:8080
```

To reset the synthetic/demo database volume for a clean demo run:

```bash
docker compose down -v
docker compose up --build
```

## Deployment Notes

For a live server, provide Gemini settings through environment variables, deployment secrets, or an uncommitted `docker-compose.override.yml` plus local `.env` file.

- Do not put keys in `docker-compose.yml`.
- Do not put keys in `appsettings.json`.
- Do not put keys in README.
- Do not put keys in committed scripts, screenshots, logs, or issue comments.
- The public repo is safe only because secrets live outside Git.
- Keep Gemini calls user-triggered and observable.
- Keep the local fallback path available for demo resilience.

Example container environment check:

```bash
docker compose exec web sh -lc 'test -n "$GEMINI_API_KEY" && echo "GEMINI_API_KEY present" || echo "GEMINI_API_KEY missing"; printenv | grep -E "^Gemini__"'
```

## Hackathon / Competition Framing

- **For Enterprise AI:** “Enterprise AI Command Centre for Cleaner Port Logistics”
- **For AI Agent / Agent Builder:** “Agentic Operations Planner for Smart Ports”

Recommended narrative:

```text
observe → analyze → recommend → simulate → human approves → audit trail
```

Culltron Smart Port Flow is best framed as a pilot-ready agentic operations layer: it watches synthetic/demo operational pressure, explains risk, proposes next actions, supports what-if planning, estimates clean-logistics impact, and leaves final decisions with human operators.

## Current Limitations / Honest Scope

- Uses synthetic/demo data.
- Not connected to live port systems yet.
- Not connected to live Transnet, IPMS, Navayuga, TOS, GPS/telematics, gate OCR/RFID, energy schedule, customer, or external emissions-factor systems.
- Production pilot would require live data integration, security review, stakeholder workflow mapping, and operational validation.
- Savings, emissions, turnaround, and clean-logistics impact are estimated/demo outputs until validated in a real pilot.
- Recommendations are decision support, not automated operational execution.

## Pilot Readiness Path

- **Phase 1: Demo validation** — confirm the problem framing, dashboard story, Copilot usefulness, report outputs, and stakeholder interest.
- **Phase 2: Data mapping and stakeholder discovery** — map required data sources, operational workflows, decision rights, security constraints, and success metrics.
- **Phase 3: Sandbox integration** — connect approved sample/sandbox feeds for gate queues, truck dispatch/telematics, berth/vessel schedules, incidents, energy disruption, and emissions factors.
- **Phase 4: Controlled pilot** — run with limited operational scope, human approval, audit logs, baseline comparisons, and measured outcomes.
- **Phase 5: Operational scale-up** — expand to more workflows, users, integrations, reporting, governance, and enterprise controls after pilot validation.

## Demo Script

Two-minute judge walkthrough:

1. **Problem:** congestion, idling, emissions, vessel delays, yard pressure, gate queues, and disruptions are hard to coordinate across disconnected tools.
2. **Solution:** Culltron Smart Port Flow provides an enterprise-style Smart Port command centre for synthetic/demo operations.
3. **Gemini Copilot:** open SmartPort Copilot Chat and ask `what is the biggest operational risk right now?`.
4. **Action plan:** ask `generate an operator action plan`.
5. **Scenario simulator:** show what-if planning for a congestion, vessel delay, or load-shedding-style disruption scenario.
6. **Emissions:** show clean logistics impact through indicative idling, diesel, and CO₂ estimates.
7. **Close:** this is a working deployed demo/prototype with pilot-ready architecture, local fallback, optional Gemini Agent Mode, synthetic/demo data, and human-approved recommendations.

See [`docs/demo-script.md`](docs/demo-script.md) for a longer judge route and fallback plan.

## Troubleshooting

### Gemini not visible or not enhancing responses

Check that server-side configuration is present and enabled:

```bash
GEMINI_API_KEY=your_key_here
Gemini__Enabled=true
Gemini__Mode=Hybrid
Gemini__Model=gemini-2.5-flash
```

Then verify the running container environment without printing any secret value:

```bash
docker compose exec web sh -lc 'test -n "$GEMINI_API_KEY" && echo "GEMINI_API_KEY present" || echo "GEMINI_API_KEY missing"; printenv | grep -E "^Gemini__"'
```

If Gemini is unavailable, disabled, rate-limited, or not configured, the local fallback still works for demo-safe summaries, operator action plans, and reports.

### Local app not reachable

- Confirm Docker is running.
- Run `docker compose up --build` from the repository root.
- Open `http://localhost:8080`.
- If the database state is stale, run `docker compose down -v` and then `docker compose up --build` to recreate seeded synthetic/demo data.

## Security Checklist

- [ ] No API keys committed.
- [ ] `.env` ignored and not committed.
- [ ] Public repo contains only placeholders.
- [ ] Gemini calls are user-triggered.
- [ ] Synthetic/demo data only.
- [ ] Human approval required for recommendations.
- [ ] No automatic operational execution.
- [ ] No live integration claims.
- [ ] Local fallback remains available.

## Repository Notes

This demo supports hackathon judging, enterprise AI review, pilot discovery, grant/funding conversations, and product direction discussions. Production integrations would require partner-approved system access, security review, workflow validation, and operational governance before use with live data.
