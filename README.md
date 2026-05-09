# Culltron Smart Port Flow

> **Premium Smart Port Agentic Operations Copilot demonstrator** by Culltron.
> ASP.NET Core 8 MVC · PostgreSQL · Entity Framework Core · Docker Compose · synthetic demo data · deterministic local intelligence.

Culltron Smart Port Flow is a cohesive smart-port operations command system that demonstrates how port teams can monitor congestion, truck ETA risk, berth pressure, yard density, load-shedding disruption, incidents, recommendations and indicative emissions/idling impact from one enterprise command-centre UI.

The product is intentionally **demo-safe**: it uses synthetic operational data, local deterministic rules/scoring, explainable response cards and scoped Copilot intent routing. It does **not** require OpenAI, Gemini, Claude, Azure OpenAI, Anthropic, external map APIs, cloud secrets or internet-dependent AI.

---

## Product summary

Culltron Smart Port Flow is designed for hackathon judging, executive demos and product discovery. It is not presented as a live integration to any real port system. Production integrations would connect to TOS/IPMS, gate systems, GPS/telematics, berth planning, yard systems and energy data.

### Platform modules

- Landing and public product pages.
- Dashboard command centre.
- **AI Command Centre** for system-level intelligence, risk scoring, monitoring lanes and action plans.
- **SmartPort Copilot Chat** for conversational, scoped smart-port questions.
- Truck Tracking & ETA Intelligence.
- Scenario Simulator.
- Emissions / Idling impact.
- Incidents, disruptions and load-shedding response.
- Flow Recommendations and decision/audit reports.
- Vessels, berths, gates, containers, dispatch, fleet, documents and analytics.

---

## AI Command Centre vs SmartPort Copilot Chat

### AI Command Centre

The AI Command Centre answers: **“What is happening across the port right now?”**

It is a system-level intelligence wall, not a chat page. It shows:

- live synthetic operational snapshot;
- gate, berth, yard, truck, emissions, energy and incident monitoring lanes;
- top risk, severity, affected area, operational reason and expected consequence;
- confidence indicators;
- immediate, next-30-minute, next-2-hour and escalation actions;
- deterministic shift brief;
- links to Truck Tracking, Simulator, Emissions, Recommendations and Copilot.

### SmartPort Copilot Chat

The Copilot Chat answers: **“Ask the system a question.”**

It is the natural-language interface for scoped questions about:

- greetings and capabilities;
- biggest risk / operational risk;
- truck queues, ETA tracking, hold-outside-port candidates and priority releases;
- gate bottlenecks;
- berth pressure, yard congestion and vessel delay;
- emissions, CO₂ and idling;
- load-shedding / energy disruption;
- scenario simulation;
- recommended action plans;
- audit / decision history;
- judge demo summary.

Small talk is displayed as compact assistant bubbles. Operational answers use structured response cards. Out-of-scope, unsafe or secret-seeking prompts are refused and redirected to supported smart-port topics.

---

## Local deterministic AI explanation

The system uses a shared local intelligence layer (`ISmartPortIntelligenceService` / `SmartPortIntelligenceService`) plus the deterministic Copilot routing service. The snapshot combines existing demo services and seeded data where available:

- vessels and berths;
- gates and queues;
- dispatch/fleet trips;
- truck ETA intelligence;
- disruptions and incidents;
- flow recommendations;
- idling/emissions estimates;
- scenario/recommendation links.

If operational data is unavailable, deterministic synthetic fallback values are clearly treated as demo data. There is no random black-box generation and no external paid AI inference path.

---

## Copilot governance and scope control

The Copilot is intentionally scoped to smart-port operations. It supports robust phrase matching for prompts such as:

- `hello`
- `what can you do`
- `what is the biggest risk right now`
- `trucks on queue`
- `which trucks should be held outside the port`
- `track delayed trucks`
- `how much CO2 can we save`
- `what happens if load-shedding starts at 16:00`
- `generate an operator action plan`
- `prepare a 2-minute demo summary`

It refuses unrelated medical/legal/financial advice, politics, celebrity/news/general knowledge, offensive prompts, prompt-injection attempts, secret/config requests and database dump requests.

---

## Truck Tracking & ETA Intelligence

The Truck Tracking page is a showpiece logistics console with no external GPS or map API dependency. It displays:

- active trucks;
- delayed trucks;
- hold-outside-port candidates;
- priority-release candidates;
- estimated queue idling;
- indicative CO₂ exposure;
- fleet ID, organisation, route/corridor, checkpoint/geofence, ETA, queue status, idling minutes, CO₂ estimate, delay risk, status badge and recommended action;
- checkpoint timeline from depot/corridor/staging/gate/terminal handoff;
- operator focus lists and Copilot prompt links.

---

## Scenario Simulator

The simulator models what-if operating conditions such as truck arrival spikes, vessel delay, berth pressure, crane availability, yard backlog, peak gate congestion and load-shedding. Outputs include indicative risk scores, waiting time changes, idling minutes, diesel/fuel cost and CO₂ impact.

---

## Emissions / idling assumptions

Emissions figures are indicative demo estimates based on idling minutes and fixed diesel/CO₂ assumptions in the local services. They are useful for comparing scenarios and explaining avoidable idling, but they are not certified emissions reductions.

---

## Architecture overview

```text
Culltron Smart Port Flow
├── SmartPort.sln
├── Dockerfile
├── docker-compose.yml
├── README.md
├── docs/
└── src/
    ├── SmartPort.Domain/          # Entities and enums
    ├── SmartPort.Application/     # DTOs and service contracts
    ├── SmartPort.Infrastructure/  # EF Core, seed data, deterministic intelligence
    ├── SmartPort.Shared/          # Roles, policies and shared constants
    └── SmartPort.Web/             # MVC controllers, Razor views, CSS/JS
```

| Layer | Technology |
|---|---|
| Web | ASP.NET Core 8 MVC / Razor Views |
| Data | PostgreSQL 16 |
| ORM/Auth | Entity Framework Core 8 / ASP.NET Core Identity |
| UI | Custom CSS/JavaScript, Chart.js, Feather icons |
| Intelligence | Local deterministic scoring, routing and heuristics |
| Packaging | Dockerfile and Docker Compose |

---

## Local run instructions

```bash
dotnet restore SmartPort.sln
dotnet build SmartPort.sln
docker compose up --build
```

Default application URL:

```text
http://localhost:8080
```

Reset seeded demo data:

```bash
docker compose down -v
docker compose up --build
```

---

## DigitalOcean deployment command

From the repository root on the droplet:

```bash
git pull && docker compose up -d --build
```

If a full reset is intentionally required for a demo environment:

```bash
docker compose down -v && docker compose up -d --build
```

---

## Demo route

Landing → Dashboard → AI Command Centre → SmartPort Copilot Chat → Truck Tracking → Scenario Simulator → Emissions → Recommendations

Recommended Copilot prompt list:

- `hello`
- `what can you do`
- `what is the biggest risk right now`
- `trucks on queue`
- `which trucks should be held outside the port`
- `track delayed trucks`
- `how much CO2 can we save`
- `what happens if load-shedding starts at 16:00`
- `generate an operator action plan`
- `prepare a 2-minute demo summary`

---

## Limitations

- Synthetic/demo data only.
- No claim of live TOS/IPMS, GPS, telematics, gate OCR/RFID or energy-system integration.
- No external paid AI API required in demo mode.
- Emissions and diesel calculations are indicative estimates based on assumptions.
- The deterministic engine is explainable and repeatable, not a trained predictive model.
- Production deployment would require security review, integrations, observability, real identity/tenant controls and data-governance work.

---

## Next phase roadmap

1. Real integration adapters for TOS/IPMS, gate systems, telematics/GPS and energy schedules.
2. Tenant-aware workflow, approval and audit controls.
3. Advanced simulation/digital twin calibration against historical operations data.
4. Optional production-grade ML forecasting after real data governance is in place.
5. Operational alerting, SLA tracking and dispatcher communications.
6. Certified sustainability methodology if emissions reporting becomes a regulated feature.
