# Culltron Smart Port Flow — Demo Script

This script is written for hackathon judges, enterprise AI reviewers, and potential pilot/investor readers. Keep the wording honest: Culltron Smart Port Flow is a working deployed demo/prototype and pilot-ready architecture using synthetic/demo data, Gemini Agent Mode, and human-approved recommendations.

## Main judge demo path

Landing → Dashboard → AI Command Centre → SmartPort Copilot Chat → Truck Tracking → Scenario Simulator → Emissions → Recommendations / Reports

## 2-minute judge walkthrough

### 0:00–0:20 — Problem

“Ports face congestion, idling, emissions, vessel delays, yard pressure, truck queues, and disruptions. The challenge is that teams often observe these signals in disconnected tools, which makes it hard to prioritize the next safest operational action.”

### 0:20–0:40 — Solution

“Culltron Smart Port Flow is an enterprise-style Smart Port command centre. It combines operational dashboards, truck ETA views, scenario simulation, emissions/idling insights, recommendations, and AI-assisted decision support in one demo-safe system.”

Open the live demo at https://smartport.culltron.app/ and move from Landing to Dashboard.

### 0:40–1:05 — AI Command Centre

Open **AI Command Centre**.

Say: “This page answers what is happening across the port right now. It summarizes synthetic/demo operational pressure, highlights risk, shows monitoring lanes, and turns the current state into human-reviewable recommendations.”

### 1:05–1:30 — SmartPort Copilot Chat with Gemini Agent Mode

Open **SmartPort Copilot Chat**. Use these exact prompts:

- `what is the biggest operational risk right now?`
- `generate an operator action plan`

Say: “When configured, Gemini Agent Mode uses Gemini 2.5 Flash to enhance the operator-facing response. If Gemini is unavailable or not configured in a self-hosted/local environment, the local offline-safe fallback still provides demo-safe decision support. Calls are user-triggered only, not automatic page-load calls.”

### 1:30–1:45 — Scenario Simulator

Open **Scenario Simulator** and show a congestion, vessel delay, or load-shedding-style what-if.

Say: “The simulator lets operators compare potential disruption scenarios before approving action.”

### 1:45–1:55 — Emissions and clean logistics impact

Open **Emissions**.

Say: “The demo estimates avoidable idling, diesel, and CO₂ impact so teams can reason about cleaner logistics. These values are indicative demo outputs until validated in a real pilot.”

### 1:55–2:00 — Close

“Culltron Smart Port Flow is a working deployed demo/prototype with pilot-ready architecture: observe, analyze, recommend, simulate, human approves, and audit trail. It uses synthetic/demo data, Gemini Agent Mode, local fallback, and human-approved recommendations only.”

## 3-minute extended route

### Landing and Dashboard

- Show the live demo URL: https://smartport.culltron.app/.
- Explain that the repository is public at https://github.com/AeriesAgi/culltron-smart-port-flow.
- Highlight that all operational data is synthetic/demo data.

### AI Command Centre

- Show top risk, confidence, monitoring lanes, action guidance, and report links.
- Explain that the page is not a live port control system; it is an AI-assisted demo command layer.

### SmartPort Copilot Chat

Suggested prompts:

- `hello`
- `what can you do`
- `what is the biggest operational risk right now?`
- `which trucks should be held outside the port`
- `generate an operator action plan`
- `what happens if load-shedding starts at 16:00`
- `prepare a 2-minute demo summary`

Emphasize that Gemini-enhanced responses are available when configured, user-triggered, grounded in sanitized synthetic/demo context, and backed by a local fallback.

### Truck Tracking and ETA

- Show active trucks, delayed trucks, checkpoint/geofence labels, ETA to gate, delay risk, hold-outside-port candidates, priority releases, idling minutes, and CO₂ exposure.
- Explain that this is not connected to live GPS/telematics in the demo.

### Scenario Simulator

- Run a congestion, vessel delay, crane availability, berth pressure, yard backlog, or load-shedding-style scenario.
- Show how outputs help compare possible action plans before human approval.

### Emissions

- Show indicative idling, fuel, and CO₂ estimates.
- State that impacts are estimates for demo framing and require validation in a controlled pilot.

### Recommendations / Reports / Agent Intelligence Desk

- Show recommendations as decision support, not automated execution.
- Show report options such as executive operations brief, scenario analysis report, operator action plan, emissions reduction report, incident response report, pilot readiness report, and daily port operations report.

## Exact truth statement

“All operational data in this demo is synthetic/demo data. The system is a working deployed demo/prototype and pilot-ready architecture, not a live production deployment inside an actual port. It does not claim live Transnet, IPMS, Navayuga, TOS, gate, GPS/telematics, energy, customer, or emissions-factor integration. Gemini Agent Mode receives only sanitized operational summaries and is triggered by user action. Recommendations require human review and are not automatically executed. Savings and impact estimates are demo outputs until validated in a controlled pilot.”

## Fallback if Gemini or the live demo is unavailable

1. If Gemini is unavailable or not configured in a self-hosted/local environment, use the local offline-safe fallback and explain that the fallback is part of the safety design.
2. If the live deployment is slow, run locally:

   ```bash
   docker compose up --build
   ```

3. Open `http://localhost:8080`.
4. Use prompt chips or short prompts rather than typing long prompts during recording.
5. If a scenario result takes too long, continue to Truck Tracking and Emissions; the synthetic/demo data keeps the story coherent.
