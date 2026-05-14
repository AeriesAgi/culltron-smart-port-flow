# Fleet & Driver Integration Roadmap

## Current demo-safe integrations
- In-app driver notifications stored in Smart Port notification history.
- Simulated WhatsApp messages with connector-ready status labels.
- Android app polling Smart Port mobile APIs.
- Deterministic AI/fallback explanations when Gemini is unavailable.

## Future connector-ready integrations
- Meta WhatsApp Business Cloud API using `SMARTPORT_WHATSAPP_*` environment variables. Business-initiated production messages may require approved templates.
- Firebase Cloud Messaging using `SMARTPORT_PUSH_*` and Firebase env vars.
- GPS/telematics providers for real truck ETA and geofence state.
- Gate OCR/RFID for check-in automation.
- Appointment systems, fleet ERP, IPMS/PCS, and terminal operating systems.
- Play Store release after auth, privacy review, release signing, and production monitoring.
