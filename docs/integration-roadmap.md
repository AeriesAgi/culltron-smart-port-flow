# Integration Roadmap

## Current demo state
The hackathon demo uses synthetic operational data for trucks, gates, berth readiness, staging, notifications and impact. It does not claim live IPMS/TOS/port integration.

## Gemini setup
Configure on the server only:
```bash
GEMINI_API_KEY=...
Gemini__Enabled=true
Gemini__Mode=Hybrid
Gemini__Model=gemini-2.5-flash
```
Gemini is optional. The deterministic fallback remains available when Gemini is missing, disabled, rate-limited or unavailable.

## WhatsApp LiveTest setup
```bash
SMARTPORT_WHATSAPP_MODE=LiveTest
SMARTPORT_WHATSAPP_ENABLED=true
SMARTPORT_WHATSAPP_ACCESS_TOKEN=...
SMARTPORT_WHATSAPP_PHONE_NUMBER_ID=...
SMARTPORT_WHATSAPP_BUSINESS_ACCOUNT_ID=...
SMARTPORT_WHATSAPP_VERIFY_TOKEN=...
SMARTPORT_WHATSAPP_GRAPH_VERSION=v20.0
SMARTPORT_PUBLIC_BASE_URL=https://smartport.culltron.app
```
Set Meta callback URL to `{SMARTPORT_PUBLIC_BASE_URL}/webhooks/whatsapp` and the verify token to `SMARTPORT_WHATSAPP_VERIFY_TOKEN`.

## Safety rules
- Demo mode is safe and uses no external WhatsApp calls.
- LiveTest sends only to active manually added drivers with Test Approved and WhatsApp Consent Confirmed.
- Seed demo contacts and invalid numbers are blocked.
- No SMS integration is included.
- No hardcoded secrets or exposed tokens.
- Android stores no Gemini or WhatsApp secrets.

## Pilot connector path
Future pilot connectors can map approved feeds from IPMS/PCS/TOS, gate appointment/OCR/RFID systems, berth and yard systems, fleet TMS/ERP, WhatsApp Cloud API and later telematics/GPS if contractually approved.
