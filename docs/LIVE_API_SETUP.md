# Live API Setup: Gemini and WhatsApp Cloud API

Smart Port runs safely without live credentials. When credentials are configured, the server-side connectors can call Gemini and Meta WhatsApp Cloud API. Do **not** commit `.env` files, API keys, access tokens, real driver numbers or private credentials.

## Gemini live connector

Get a Gemini API key from Google AI Studio, then configure the server environment:

```bash
export GEMINI_API_KEY="..."
export Gemini__Enabled=true
export Gemini__Mode=Hybrid
export Gemini__Model=gemini-2.5-flash
```

Fallback names are also supported:

```bash
export GEMINI_ENABLED=true
export GEMINI_MODE=Hybrid
export GEMINI_MODEL=gemini-2.5-flash
```

Open `/gemini-agent` and click **Run Live Gemini Test**. If the key is missing, disabled, timed out or rejected, Smart Port keeps the deterministic local fallback active and records the safe source label in audit history.

## WhatsApp Cloud API live-test connector

Create/configure a Meta App with WhatsApp API access, then set:

```bash
export SMARTPORT_WHATSAPP_ENABLED=true
export SMARTPORT_WHATSAPP_MODE=LiveTest
export SMARTPORT_WHATSAPP_ACCESS_TOKEN="..."
export SMARTPORT_WHATSAPP_PHONE_NUMBER_ID="..."
export SMARTPORT_WHATSAPP_BUSINESS_ACCOUNT_ID="..."
export SMARTPORT_WHATSAPP_VERIFY_TOKEN="..."
export SMARTPORT_WHATSAPP_GRAPH_VERSION="v22.0"
export SMARTPORT_PUBLIC_BASE_URL="https://smartport.culltron.app"
export SMARTPORT_WHATSAPP_TEST_RECIPIENT_NUMBER="..." # optional, never commit
```

Webhook callback URL:

```text
https://smartport.culltron.app/webhooks/whatsapp
```

Verify challenge test:

```bash
curl "https://smartport.culltron.app/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=YOUR_VERIFY_TOKEN&hub.challenge=12345"
```

Expected response:

```text
12345
```

Outbound endpoint used by Smart Port when enabled and safe:

```text
POST https://graph.facebook.com/{SMARTPORT_WHATSAPP_GRAPH_VERSION}/{SMARTPORT_WHATSAPP_PHONE_NUMBER_ID}/messages
```

Payload shape:

```json
{
  "messaging_product": "whatsapp",
  "to": "<approved-recipient>",
  "type": "text",
  "text": { "body": "<safe operational message>" }
}
```

Safety rules:

- Demo mode never calls Meta; it stores simulated notifications.
- LiveTest sends only to `SMARTPORT_WHATSAPP_TEST_RECIPIENT_NUMBER` or manually added approved/consented test drivers.
- Live mode still requires approved/consented recipients and operational approval.
- Meta message IDs are stored when returned; failures are recorded safely without losing the operational ticket/history.

## Future live data sources

Pilot/live data connectors can ingest IPMS/TOS, berth schedules, gate OCR/RFID, weighbridge data, fleet GPS/telematics, driver mobile app events, WhatsApp Cloud API inbound messages, ERP/fleet systems, weather/disruption feeds and emissions factors.
