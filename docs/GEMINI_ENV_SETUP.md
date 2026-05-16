# Gemini Environment Setup

Smart Port supports live Gemini calls from the server when configured and falls back deterministically when not configured.

```bash
export GEMINI_API_KEY="..."
export Gemini__Enabled=true
export Gemini__Mode=Hybrid
export Gemini__Model=gemini-2.5-flash
```

Fallback variable names are also supported:

```bash
export GEMINI_ENABLED=true
export GEMINI_MODE=Hybrid
export GEMINI_MODEL=gemini-2.5-flash
```

Open `/gemini-agent` and use **Run Live Gemini Test**. Secrets are never rendered in the frontend. Logs should only contain safe metadata such as status/latency/source.


## Demo and judge codes

For hackathon judging, enable visible demo codes with `SMARTPORT_SHOW_DEMO_CREDENTIALS=true`. Codes: Port Admin `culltron-admin-2026`, Fleet Owner `culltron-fleet-2026`, Driver `culltron-driver-2026`, Judge `culltron-judge-2026`. Do not commit real Gemini keys, WhatsApp tokens, `.env` files or private phone numbers.
