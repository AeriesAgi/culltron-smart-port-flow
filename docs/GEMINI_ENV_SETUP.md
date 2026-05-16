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
