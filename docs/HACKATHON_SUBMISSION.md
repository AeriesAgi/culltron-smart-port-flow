# Hackathon Submission

## 1-line pitch

Culltron Smart Port Flow is an enterprise AI operations agent that turns port congestion signals into human-approved execution plans, fleet/driver actions, audit trails and emissions impact.

## 30-second pitch

Smart Port is not just a chatbot. It is a closed operational loop for smart ports and freight logistics: control room signals flow into a Gemini-enhanced agent, the agent compares deterministic fallback against AI reasoning, a human approves an execution plan, fleet owners and drivers receive simulated mobile/WhatsApp actions, and every step is audited with idling and CO₂ impact.

## Long description

The demo uses synthetic port, truck, gate, staging, berth and notification signals to show how an enterprise AI operations agent can coordinate congestion response. Gemini enhances explanations when configured; deterministic fallback always works. The product is connector-ready for IPMS/TOS/gate/fleet/GPS/WhatsApp pilots but makes no unsupported live-integration claims.

## Tags/categories

Enterprise AI, AI Agent Olympics, Gemini, logistics, smart ports, fleet operations, agent governance, mobile API, WhatsApp-ready, sustainability.

## Judging strengths

- Full closed loop from public website to Identity login, Gemini agent, execution plan, fleet/driver action, audit and emissions impact.
- Human-approved workflow with visible governance controls.
- Works without Gemini API key through deterministic fallback.
- Honest pilot-ready and connector-ready production story.
- Android/mobile and WhatsApp paths are represented without storing secrets.

## AI Agent Olympics angle

The agent reads operational state, detects bottlenecks, generates a baseline plan, asks Gemini for reasoning when configured, compares output, gates actions through human approval, saves audit history and recommends fleet/driver actions.

## Enterprise AI angle

Identity roles, authorization policies, health/readiness checks, environment-based secrets, audit history, safety checks and production-hardening backlog make it credible for enterprise review.

## Gemini use

Gemini can generate operations briefs, fleet plans, driver instructions, disruption recovery, emissions impact, executive summaries, risk/governance reviews and pilot readiness briefs.

## Governance use

The `/agent-governance` page demonstrates allowed actions, blocked actions, approval matrix, secret exfiltration blocking, prompt-injection risk checks and audit entries.

## Business value

Reduce idling, improve ETA reliability, coordinate gate/staging actions, lower CO₂ impact and create a governed pilot path for port/freight operators.

## Originality

Smart Port combines agentic reasoning with real operational workflow: fleet instructions, driver companion, simulated WhatsApp, audit and emissions impact.

## Limitations

Synthetic demo data only; no live Transnet/IPMS/customer/WhatsApp/GPS integration is claimed. Production requires approved credentials, pilot governance and hardening.


## Live connector setup

See `docs/LIVE_API_SETUP.md` for exact Gemini and WhatsApp Cloud API environment variables, webhook callback setup, curl verify test, and safety rules. Future pilot/live connectors can ingest IPMS/TOS, gate systems, fleet GPS, weighbridge, ERP, WhatsApp Cloud API, driver app, berth schedules, weather/disruption feeds and emissions systems.


## Final judge route

Use `/demo-access` with Judge code `culltron-judge-2026`, then follow Demo Tour through Gemini, governance, ops ingest, execution plans, truck action, driver companion, notifications, enterprise readiness and health integrations.
