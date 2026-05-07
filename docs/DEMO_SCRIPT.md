# SmartPort — Competition & Investor Demo Script

## Setup
- Open browser at `http://localhost:8080` (Docker) or `https://localhost:5001` (local)
- Have a second tab ready at `/auth/login`
- Recommended login: `ops.manager@smartport.co.za` / `SmartPort@2025!`

---

## Opening Statement (30 seconds)

> "SmartPort is an intelligent port operations platform built specifically for South Africa's maritime and logistics industry. It gives terminal operators, port authorities, and logistics stakeholders a single unified operational picture — from vessel arrivals and berth assignments, all the way through to container dwell, gate queues, document compliance, and incident management — in real time."

---

## Step 1 — Public Website (`/`)

**Talking points:**
- Professional product landing page — credible to investors and buyers
- Clear module overview: vessel, container, gate, document, AI, analytics
- Pricing page shows commercial viability
- Demo access page with live credentials

---

## Step 2 — Login (`/auth/login`)

- Point out the credentials table — 5 distinct roles
- Log in as **Port Operations Manager**: `ops.manager@smartport.co.za`

> "Role-based access means a logistics partner sees different views from a terminal supervisor or an executive. Every route and action is governed by policy."

---

## Step 3 — Operations Dashboard (`/dashboard`)

**Key talking points:**

**KPI Banner:**
- 3 vessels in port, 82% berth utilisation, 2,847 TEU today, 5 open incidents
- Trucks in queue with real-time wait estimate

**Vessels In Port panel:**
- MSC SARACEN in cargo operations — note the +90 min delay flag
- CMA CGM CALLISTO berthed
- EVER GREET at anchor — 3h+ wait, AI has raised a berth reallocation recommendation

**Throughput Chart:**
- 14-day trend with realistic seasonal variation

**Berth Tiles:**
- Visual berth occupancy — immediate situational awareness

**Yard Blocks:**
- Block A at 84% — near capacity warning
- Block R reefer block showing reefer badge

**AI Recommendations panel:**
- 3 pending recommendations — berth reallocation, gate queue, DG document chase

---

## Step 4 — Vessel Detail (`/vessels/1`)

- Full MSC SARACEN record: IMO, flag, shipping line, agent, technical particulars
- Berth assignment: cargo ops in progress, 2,600 / 2,800 TEU discharged
- Documents panel: **DG Declaration missing** — overdue flag, red highlight
- Related incident: crane failure

> "The platform links vessels, documents, incidents, and berth assignments — everything about this vessel call is in one place."

---

## Step 5 — AI Recommendations (`/incidents/recommendations`)

- Show 4 pending recommendations
- **EVER GREET Berth Reallocation**: detailed rationale, suggested action, estimated 4h time saving
- **Gate Queue Mitigation**: queue at 12 trucks, recommendation to open Gate 4
- **DG Declaration Chase**: MSC SARACEN departure at risk, step-by-step action plan
- Demonstrate **Accept & Action** workflow

> "This isn't just dashboards — it's an AI layer that watches the data and tells operators what to do next, with an estimated impact for each suggestion."

---

## Step 6 — Incident Management (`/incidents/1`)

- Crane failure incident: Critical severity, In Progress status
- Audit trail showing acknowledgment
- Live resolution workflow: root cause, resolution notes, corrective action

---

## Step 7 — Container Tracker (`/containers/track`)

- Search any container number (e.g. MAEU...)
- Show: status, yard location, customs status, dwell time, reefer/hazmat flags

---

## Step 8 — Yard Overview (`/containers/yard`)

- Block-level occupancy visualisation
- Block A near capacity — near-capacity badge
- Hazmat block clearly labelled
- Drill into Block A — see individual containers

---

## Step 9 — Analytics (`/analytics`)

- Default 30-day view
- KPIs: total TEU, average turnaround, crane productivity, berth utilisation
- Throughput chart and turnaround chart side by side
- Berth efficiency table — utilisation per berth
- Adjust date range to show flexibility

---

## Step 10 — Admin (login as admin)

- Switch to: `admin@smartport.co.za`
- User management table — 5 users, roles, last login
- Settings page: alert thresholds, integration roadmap
- Roles page: clear permission matrix

---

## Closing

> "SmartPort runs on ASP.NET Core 8, PostgreSQL, and Docker. The codebase is structured for maintainability and enterprise deployment. Integration pathways are already documented for AIS vessel feeds, SARS customs, SAWS weather, and Navis N4 TOS. We are seeking pilot partners for deployment at a South African terminal. The platform is ready to run today."

---

## Differentiators to Emphasise

1. **South African context** — SARS, SAMSA, DAFF workflows built in
2. **AI layer** — not just dashboards, actionable recommendations with impact estimates
3. **Full document workflow** — departure-critical document tracking
4. **Role-based** — right information to the right stakeholder
5. **Production-ready stack** — .NET 8, PostgreSQL, Docker, not a prototype
6. **Realistic data** — based on Durban Container Terminal operational patterns
