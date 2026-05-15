# Gemini Environment Setup

Smart Port Copilot is backend-driven. The browser and Android companion never store Gemini keys.

## Local environment

```bash
export GEMINI_API_KEY=<real key outside repo>
export Gemini__Enabled=true
export Gemini__Mode=Hybrid
export Gemini__Model=gemini-2.5-flash
```

## Docker Compose / deployment

Set the same variables in the host or deployment secret manager. Do not commit `.env` files containing secrets.

## Verify Gemini is active

1. Open `/demo-access` and enter a demo credential.
2. Open `/fleet/settings`.
3. Confirm **Gemini API key configured** is `Yes`.
4. Run **Test Gemini Copilot**.
5. Open `/Copilot` or call `/api/mobile/copilot/driver` and verify the response source indicates Gemini or hybrid Gemini mode.

## Verify fallback mode

Unset `GEMINI_API_KEY` or set `Gemini__Enabled=false`, restart the app, then run the same test. The workflow should continue with deterministic Smart Port fallback responses.
