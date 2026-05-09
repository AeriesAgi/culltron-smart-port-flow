# Culltron Smart Port Flow — Judge Demo Script

## 2–3 minute video script

### 0:00–0:20 — Problem

“Ports lose time and money when berth pressure, truck queues, yard density, load-shedding and emissions are handled in separate tools. Culltron Smart Port Flow demonstrates an AI-assisted command layer that turns synthetic port state into explainable operator actions.”

### 0:20–0:45 — Landing and Dashboard

Open the landing page, then log in and open the Dashboard. Highlight that the UI is a dark navy / teal / cyan enterprise command centre with synthetic demo data. Show the KPI cards and explain that the platform is demo-safe and locally deterministic.

### 0:45–1:15 — AI Command Centre

Open **AI Command Centre**. Say: “This is not chat. This answers what is happening across the port right now.” Show monitoring lanes, the top risk panel, confidence, deterministic shift brief and recommended action plan.

### 1:15–1:45 — SmartPort Copilot Chat

Open **SmartPort Copilot Chat** and use these exact prompts:

1. `hello`
2. `what can you do`
3. `what is the biggest risk right now`
4. `which trucks should be held outside the port`
5. `what happens if load-shedding starts at 16:00`
6. `prepare a 2-minute demo summary`

Point out that chat sends via AJAX, the URL remains `/Copilot`, responses are scoped, and no paid external AI API is called.

### 1:45–2:15 — Truck Tracking and Simulator

Open **Truck Tracking & ETA Intelligence**. Show active trucks, checkpoints/geofences, ETA to gate, delay risk, hold-outside-port candidates, priority releases, idling minutes and CO₂ exposure. Then open the Scenario Simulator and run a load-shedding or congestion scenario.

### 2:15–2:45 — Emissions and Recommendations

Open Emissions to show indicative idling/CO₂ estimates. Open Recommendations / audit to show explainable decision support and action history.

### 2:45–3:00 — Close

“Culltron Smart Port Flow is a smart port operations demonstrator: synthetic data, deterministic local intelligence, explainable recommendations, no external paid AI API and a realistic roadmap to production integrations.”

---

## Judge demo path

Landing → Dashboard → AI Command Centre → SmartPort Copilot Chat → Truck Tracking → Scenario Simulator → Emissions → Recommendations

---

## Exact prompt list

- `hello`
- `what can you do`
- `what is the biggest risk right now`
- `trucks on queue`
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

---

## Fallback if live deployment is slow

1. Use the local Docker build if the DigitalOcean deployment is still starting:

   ```bash
   docker compose up --build
   ```

2. If the browser is slow, narrate over screenshots or reload the same route after the database seed completes.
3. Use the prompt chips rather than typing long prompts during recording.
4. If a scenario result takes too long, continue to Truck Tracking and Emissions; all data is synthetic and deterministic so the story remains coherent.

---

## Required truth statement

“All operational data in this demo is synthetic/demo data. The intelligence layer is a deterministic local rules/scoring engine. No OpenAI, Gemini, Claude, Azure OpenAI, Anthropic or paid external AI API is required for demo mode. Production integrations would connect to real TOS/IPMS, gate, GPS/telematics and energy systems after governance and security review.”
