# Culltron Smart Port Flow — Judge Demo Script

Use this script for a concise competition recording. Keep claims precise: the system is a working deployed demo/prototype using synthetic/demo data, Gemini Agent Mode, local fallback, and human-approved recommendations.

## Judge demo path

Landing → Dashboard → AI Command Centre → SmartPort Copilot Chat → Truck Tracking → Scenario Simulator → Emissions → Recommendations / Reports

## 2–3 minute video script

### 0:00–0:20 — Problem

“Ports face congestion, idling, emissions, vessel delays, yard pressure, truck queues, and disruption events. When these signals sit in disconnected tools, teams struggle to identify the biggest risk and choose the next safest action.”

### 0:20–0:45 — Landing and Dashboard

Open https://smartport.culltron.app/ and move from the landing page to the dashboard. Explain that Culltron Smart Port Flow is an enterprise-style AI command centre for cleaner, smarter port logistics. Highlight that all operational data shown in the demo is synthetic/demo data.

### 0:45–1:15 — AI Command Centre

Open **AI Command Centre**. Say: “This page answers what is happening across the port right now. It brings together berth, gate, yard, truck, incident, recommendation, and emissions pressure into one command-centre view.”

Show monitoring lanes, top risk, confidence, recommendation cards, and action guidance.

### 1:15–1:50 — SmartPort Copilot Chat and Gemini Agent Mode

Open **SmartPort Copilot Chat** and use these exact prompts:

1. `what is the biggest operational risk right now?`
2. `generate an operator action plan`
3. `what happens if load-shedding starts at 16:00`
4. `prepare a 2-minute demo summary`

Say: “When configured, Gemini Agent Mode uses Gemini 2.5 Flash to enhance the operator-facing answer. Calls are user-triggered only, there are no automatic Gemini calls on page load, and the local offline-safe fallback still works if Gemini is unavailable.”

### 1:50–2:20 — Truck Tracking and Scenario Simulator

Open **Truck Tracking & ETA Intelligence**. Show active trucks, delayed trucks, ETA risk, checkpoint/geofence labels, hold-outside-port candidates, priority releases, idling minutes, and CO₂ exposure.

Then open **Scenario Simulator** and run a congestion, vessel delay, or load-shedding-style scenario. Explain that simulation supports what-if planning before human approval.

### 2:20–2:45 — Emissions and Recommendations / Reports

Open **Emissions** to show indicative clean-logistics impact through idling, fuel, and CO₂ estimates. Open **Recommendations / Reports** or **Agent Intelligence Desk** to show human-reviewable decision support and report generation.

### 2:45–3:00 — Close

“Culltron Smart Port Flow demonstrates a pilot-ready architecture: observe, analyze, recommend, simulate, human approves, and audit trail. It is not connected to live port systems in this demo; production use would require live data integration, security review, stakeholder workflow mapping, and operational validation.”

## Exact prompt list

- `hello`
- `what can you do`
- `what is the biggest operational risk right now?`
- `which trucks should be held outside the port`
- `track delayed trucks`
- `truck ETA`
- `gate bottleneck`
- `berth pressure`
- `yard congestion`
- `vessel delay`
- `how much CO2 can we save`
- `reduce idling`
- `load shedding`
- `what happens if load-shedding starts at 16:00`
- `run scenario`
- `generate an operator action plan`
- `prepare shift brief`
- `prepare a 2-minute demo summary`

## Fallback if live deployment or Gemini is unavailable

1. If the live deployment is slow, run locally:

   ```bash
   docker compose up --build
   ```

2. Open `http://localhost:8080`.
3. If Gemini is not configured in a self-hosted/local environment, use the local offline-safe fallback and explain that fallback behavior is intentional.
4. Use prompt chips rather than typing long prompts during recording.
5. If a scenario result takes too long, continue to Truck Tracking and Emissions; all data is synthetic/demo data so the story remains coherent.

## Required truth statement

“All operational data in this demo is synthetic/demo data. Culltron Smart Port Flow is a working deployed demo/prototype and pilot-ready architecture, not a live production deployment inside an actual port. It does not claim live Transnet, IPMS, Navayuga, TOS, gate, GPS/telematics, customer, energy, or emissions-factor integration. Gemini Agent Mode receives only sanitized operational summaries and is triggered by user action. Recommendations require human review and are not automatically executed. Savings and impact estimates are demo outputs until validated in a controlled pilot.”

## Fleet & Driver Queue Companion Demo Flow

1. Control room detects congestion.
2. Gemini/fallback AI creates queue recommendation.
3. Fleet owner sees trucks and instructions at `/fleet`.
4. Driver receives simulated WhatsApp/in-app update.
5. Driver opens mobile web page or Android companion.
6. Driver sees queue number, ETA, gate, and instruction.
7. Driver acknowledges.
8. Dashboard shows reduced idling/emissions impact.
