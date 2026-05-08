# Culltron Smart Port Flow

> **Enterprise Smart Port Agentic Operations Copilot** for competitions, hackathons, and premium logistics SaaS demonstrations.  
> ASP.NET Core 8 MVC · PostgreSQL · Entity Framework Core · Docker Compose · deterministic local AI/rules engine.

Culltron Smart Port Flow is a competition-ready Smart Port command center that demonstrates how terminal operators can predict congestion, protect berth productivity, reduce gate queues, manage truck flow, respond to load-shedding, and quantify idling/emissions impact without relying on paid external AI APIs or cloud secrets.

---

## Competition positioning

This project is designed to look and feel like a polished enterprise AI logistics platform rather than a toy CRUD demo:

- **Futuristic command-center UI** with dark navy, teal and cyan styling, glass panels, glowing KPI cards, charts, badges and animated status signals.
- **Agentic operations copilot** that turns demo port state into explainable recommendations and action plans.
- **Local deterministic intelligence** for reliable judging-room demos with no external paid AI dependency.
- **Smart Port decision layers** spanning vessels, berths, gates, trucks, yard pressure, disruptions, load-shedding, emissions and audit history.
- **Rich seed/demo data** so the platform looks alive immediately after first launch.

---

## Architecture overview

The solution follows a clean ASP.NET Core architecture:

```text
Culltron Smart Port Flow
├── SmartPort.sln
├── Dockerfile
├── docker-compose.yml
├── README.md
├── docs/
│   ├── ARCHITECTURE.md
│   ├── DEMO_SCRIPT.md
│   ├── ROADMAP.md
│   └── demo-script.md
├── scripts/
│   └── init.sql
└── src/
    ├── SmartPort.Domain/          # Entities and enums
    ├── SmartPort.Application/     # DTOs and service contracts
    ├── SmartPort.Infrastructure/  # EF Core, deterministic intelligence, seed data
    ├── SmartPort.Shared/          # Roles, policies and shared constants
    └── SmartPort.Web/             # ASP.NET Core MVC controllers, Razor views, CSS/JS
```

### Runtime stack

| Layer | Technology |
|---|---|
| Web | ASP.NET Core 8 MVC / Razor Views |
| Data | PostgreSQL 16 |
| ORM/Auth | Entity Framework Core 8 / ASP.NET Core Identity |
| UI | Custom CSS, JavaScript, Chart.js, Feather icons |
| Intelligence | Local deterministic scoring, rules and heuristics |
| Packaging | Dockerfile and Docker Compose |

---

## Core features

### SmartPort AI Agent / Copilot Command Center

The copilot analyzes synthetic demo port state and generates explainable answers/action plans covering:

- congestion intelligence
- berth intelligence
- gate queue intelligence
- truck-flow intelligence
- vessel and yard pressure intelligence
- load-shedding / energy disruption intelligence
- emissions and truck-idling impact
- priority operator action plans

Each recommendation/answer is intended to surface the operational reason, suggested action, expected impact, confidence/risk context and decision-audit friendly data points.


### Local SmartPort Copilot Chat

The `/copilot` page is a flagship premium chat-style operator assistant. It does **not** call external AI APIs. Instead, it uses local deterministic intent routing over seeded/demo operational context. Operators can ask about biggest risk, truck idling, berth attention, gate bottlenecks, load-shedding, CO₂ savings, action plans and demo narration. Each response includes:

- short summary
- affected area and urgency/severity
- operational reasoning
- recommended action
- expected impact
- confidence score
- emissions/idling impact
- load-shedding/energy impact
- buttons to related pages such as Dashboard, Simulator, Emissions, Recommendations and Audit reports

### Scenario simulation

Run what-if simulations for:

- truck arrival spikes
- vessel ETA slips
- berth occupancy increases
- crane availability reductions
- container backlog growth
- load-shedding stages
- peak-hour gate congestion

The simulator outputs congestion, berth, gate, energy and yard pressure scores, estimated waiting time changes, idling minutes, diesel/fuel cost and CO₂ impact.

### Decision audit and recommendations

Operators can review recommendation history, pending actions, accepted/dismissed decisions, risk level, confidence, route/vehicle context and timestamps.

### Emissions / energy / disruption layer

The emissions view estimates avoidable idling, diesel use, fuel cost and CO₂ impact, while disruption views model load-shedding, road congestion, gate delays and operational impacts.

### Enterprise operations modules

- Landing page and public product pages
- Main dashboard
- Vessels and berth management
- Container and yard operations
- Gates and truck queues
- Dispatch and fleet flow intelligence
- Incidents and alerts
- Documents and compliance
- Analytics and reports
- Admin/users/roles

---

## Local run instructions

### Prerequisites

- .NET SDK 8.x
- Docker / Docker Compose
- PostgreSQL 16 if running outside Docker

### Restore and build

```bash
dotnet restore SmartPort.sln
dotnet build SmartPort.sln
```

### Run locally with a local PostgreSQL container

```bash
docker run -d \
  --name smartport_db \
  -e POSTGRES_DB=smartport_dev \
  -e POSTGRES_USER=smartport \
  -e POSTGRES_PASSWORD=SmartPort2025! \
  -p 5432:5432 \
  postgres:16-alpine

cd src/SmartPort.Web
dotnet run
```

Browse to the URL printed by `dotnet run`.

---

## Docker run instructions

```bash
docker compose config
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

## Demo credentials

| Role | Email | Password |
|---|---|---|
| Admin | admin@smartport.co.za | SmartPort@2025! |
| Port Operations Manager | ops.manager@smartport.co.za | SmartPort@2025! |
| Terminal Staff | terminal1@smartport.co.za | SmartPort@2025! |
| Logistics Partner | logistics@freightco.co.za | SmartPort@2025! |
| Executive Viewer | executive@transnet.co.za | SmartPort@2025! |

---

## Flagship demo flow

1. **Landing page** — introduce Culltron Smart Port Flow as an enterprise AI logistics SaaS command center.
2. **Login** as `ops.manager@smartport.co.za`.
3. **Dashboard** — show premium KPI cards, vessel/gate/yard health and live operational context.
4. **AI Agent / Copilot Command Center** — ask: “What should the operations manager prioritise right now?”
5. **Copilot Chat** — use prompt chips such as “Biggest risk”, “Reduce idling”, “Gate bottleneck” and “2-minute demo summary”.
6. **Scenario Simulation** — run “Load-Shedding Stage 4 at 16:00” or “Durban High Congestion”.
7. **Emissions / Energy** — show idling, diesel cost, avoidable CO₂ estimates and disruption response.
8. **Flow Recommendations / Audit** — show explainable recommendations, statuses, accept/dismiss actions and decision history.
9. **Close** with roadmap: live integrations, real telemetry, predictive ML and digital-twin optimization.

---

## 2–3 minute video script

**0:00–0:20 — Problem**  
“Ports lose time and money when berth congestion, truck queues, yard pressure, power disruptions and emissions are managed in separate tools. Culltron Smart Port Flow brings those decisions into one command center.”

**0:20–0:45 — Landing and dashboard**  
“Here is the enterprise landing page and main operations dashboard. The system is seeded with live-looking vessels, berths, gates, trucks, incidents and emissions estimates, so judges can see the port state immediately.”

**0:45–1:25 — Copilot Command Center and Chat**
“The AI Agent and Copilot Chat use deterministic local heuristics. No paid AI APIs are required. The chat page feels like an enterprise AI assistant, but every answer is generated locally with explainable reasoning, confidence, expected impact, emissions impact, energy impact and buttons to the relevant operational pages.”

**1:25–1:55 — Simulation**  
“Now we run a load-shedding and peak-hour truck spike scenario. The simulator recalculates gate delay risk, berth risk, energy disruption risk, waiting time, idling minutes, diesel cost and CO₂.”

**1:55–2:25 — Audit and sustainability**  
“Recommendations are auditable. Operators can accept or dismiss actions, and the emissions view quantifies avoidable idling and CO₂ reduction opportunities.”

**2:25–3:00 — Close**  
“This is a demo-ready Smart Port Agentic Operations Copilot: polished UI, rich data, explainable recommendations, scenario planning and a clear path to real integrations.”

---

## Known limitations

- Demo data is synthetic and created for competition presentation.
- The local rules engine is deterministic; it is not a trained predictive model.
- No real AIS, TOS, OCR, GPS, telematics, weather, customs or power utility integrations are connected yet.
- Emissions calculations use configurable assumptions and should be validated against real fleet telemetry before production use.
- Database schema is created with `EnsureCreatedAsync()` for demo simplicity, not production migrations.

---

## Next phase roadmap

1. Integrate real telemetry feeds: AIS, TOS, gate OCR/RFID, truck GPS and berth planning systems.
2. Add forecasting models for ETA drift, gate queues, dwell risk and crane productivity.
3. Add a proper decision-audit workflow with approvals, assignments, SLAs and incident linkage.
4. Add digital-twin optimization for berth plans, yard reshuffles and gate appointment smoothing.
5. Add production migrations, observability, health checks, SSO and hardened deployment profiles.
