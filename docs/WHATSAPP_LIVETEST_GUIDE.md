# WhatsApp Cloud API LiveTest Guide

Smart Port simulates WhatsApp by default. To test the real Meta Cloud API path, configure server-side environment variables only:

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

Webhook callback:

```text
https://smartport.culltron.app/webhooks/whatsapp
```

Verify:

```bash
curl "https://smartport.culltron.app/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=YOUR_VERIFY_TOKEN&hub.challenge=12345"
```

Expected: `12345`.

Inbound webhook POST parses text, interactive, location and media metadata, then routes known approved senders through Smart Port driver command/location handling. Unknown senders are logged as ignored without changing truck state.

LiveTest sends are gated to approved test recipient/consented test drivers. Demo mode remains fully functional without Meta credentials.
