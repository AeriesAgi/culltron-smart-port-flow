# Gemini Agent Award Notes

## How Gemini is used

The Gemini Operations Agent is exposed at `/gemini-agent`. It uses structured Smart Port context: truck queue count, gate pressure, berth readiness, yard/incident placeholders, WhatsApp/driver confirmation state, execution plans, audit history and idling/emissions estimates.

## Why this is agentic

The agent loop is: inputs → deterministic bottleneck detection → Gemini reasoning/explanation when configured → recommendations → human approval → saved audit/demo history → fleet/driver channel action if approved. Gemini is used for operational reasoning and narrative synthesis, not uncontrolled automation.

## Fallback mode

If `GEMINI_API_KEY` or `Gemini__Enabled=true` is not configured, the page remains functional and labels `Local Fallback Active`. This resilience is intentional for demos and field pilots.

## Model/env vars

- `GEMINI_API_KEY`
- `Gemini__Enabled=true`
- `Gemini__Mode=Hybrid`
- `Gemini__Model=gemini-2.5-flash`

## Screenshots checklist

- `/demo-tour`
- `/dashboard` with Gemini status
- `/gemini-agent` readiness and workflow trace
- Generated brief history row
- `/execution/plans/{id}` truck actions
- `/fleet/trucks/SPQ-2026-0042`
- `/driver/demo`
- `/fleet/notifications`
- `/enterprise-readiness`
