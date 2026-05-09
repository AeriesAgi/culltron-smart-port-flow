# Culltron Smart Port Flow — Demo Script

## Route 1: Hackathon 2–3 minute route

Landing → Dashboard → AI Command Centre → SmartPort Copilot Chat → Truck Tracking → Simulator → Emissions → Recommendations.

### 0:00–0:20 — Problem

“Ports lose time and money when berth pressure, truck queues, yard density, load-shedding and emissions are handled in separate tools. Culltron Smart Port Flow demonstrates an AI-assisted command layer that turns synthetic port state into explainable operator actions.”

### 0:20–0:45 — Landing and Dashboard

Open the landing page, then log in and open the Dashboard. Highlight the dark navy / teal / cyan enterprise command-centre style, synthetic demo data and local deterministic intelligence.

### 0:45–1:15 — AI Command Centre

Open **AI Command Centre**. Say: “This is not chat. This answers what is happening across the port right now.” Show monitoring lanes, top risk, confidence, deterministic shift brief, recommended action plan and business links.

### 1:15–1:45 — SmartPort Copilot Chat

Use exact prompts:

- `hello`
- `what can you do`
- `what is the biggest risk right now`
- `which trucks should be held outside the port`
- `what happens if load-shedding starts at 16:00`

Point out that responses are scoped, deterministic and no paid external AI API is called.

### 1:45–2:15 — Truck Tracking and Simulator

Open **Truck Tracking & ETA Intelligence**. Show active trucks, checkpoints/geofences, ETA to gate, delay risk, hold-outside-port candidates, priority releases, idling minutes and CO₂ exposure. Then open the Scenario Simulator and run a load-shedding or congestion scenario.

### 2:15–2:45 — Emissions and Recommendations

Open Emissions to show indicative idling/CO₂ estimates. Open Recommendations / audit to show explainable decision support and action history.

### 2:45–3:00 — Close

“Culltron Smart Port Flow is a smart-port operations demonstrator: synthetic data, deterministic local intelligence, explainable recommendations, no external paid AI API and a realistic roadmap to production integrations.”

## Route 2: NCIC / business route

Landing → Executive Impact Centre → Clean Logistics Impact → Pilot Readiness → Stakeholder Value → Copilot `prepare grant summary`.

### 0:00–0:30 — Executive Impact Centre

Open `/Impact`. Show estimated idling minutes avoided, CO₂ reduction potential, diesel/fuel cost saving, gate queue reduction, turnaround improvement and assumptions. State that all values are indicative demo estimates, not verified emissions outcomes.

### 0:30–1:00 — Clean Logistics Impact

Open `/CleanLogistics`. Explain the clean logistics problem: congestion, truck idling, diesel waste, berth/gate bottlenecks, load-shedding disruption and limited decision visibility. Explain how local deterministic decision support helps coordinate truck flow.

### 1:00–1:45 — Pilot Readiness

Open `/PilotReadiness`. Show the 30/60/90-day pilot roadmap, required integrations, success metrics, risks and the Integration Readiness wall. Say: “Production integrations would connect later to partner-approved TOS/IPMS, gate, telematics, energy and emissions-factor data.”

### 1:45–2:15 — Stakeholder Value

Open `/Stakeholders`. Pick one or two personas such as Fleet Operator and Port Authority. Show pain point, Culltron capability, expected value and related module links.

### 2:15–3:00 — Copilot grant summary

Open Copilot and use:

- `Prepare a 2-minute grant summary.`

Close with the truth statement below.

## Exact Copilot prompts

- `What is the business impact?`
- `Explain NCIC alignment.`
- `What would a 90-day pilot look like?`
- `What integrations are required?`
- `Generate executive brief.`
- `Prepare a 2-minute grant summary.`
- `What are the pilot success metrics?`
- `What value does this give fleet operators?`
- `What is the biggest risk right now?`
- `Which trucks should be held outside the port?`

## Fallback if live deployment is slow

1. Use the local Docker build if the DigitalOcean deployment is still starting:

   ```bash
   docker compose up --build
   ```

2. If the browser is slow, narrate over screenshots or reload the same route after the database seed completes.
3. Use the prompt chips rather than typing long prompts during recording.
4. If a scenario result takes too long, continue to Truck Tracking and Emissions; all data is synthetic and deterministic so the story remains coherent.

## Required truth statement

“All operational data in this demo is synthetic/demo data. The intelligence layer is a deterministic local rules/scoring engine. No OpenAI, Gemini, Claude, Azure OpenAI, Anthropic or paid external AI API is required for demo mode. Production integrations would connect to partner-approved TOS/IPMS, gate, GPS/telematics, energy and emissions-factor systems after governance and security review. Emissions and fuel figures are indicative and not verified outcomes.”
