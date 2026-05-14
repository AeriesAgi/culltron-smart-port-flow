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


## Enterprise Execution Platform Update

Smart Port now models an execution loop: Control Room/AI Simulator → Execution Plan Generator → Fleet Owner Operations Console → Driver Web Portal/Android Companion → WhatsApp Demo or LiveTest notifications → Driver Status Assistant → audit-ready ETA, queue, status, timeline and emissions impact updates. The deterministic state machine and queue optimizer work without Gemini; Gemini/Copilot remains an optional explanation layer. Demo Mode is free and safe, no SMS is used, no paid provider is required, and LiveTest WhatsApp is gated by environment variables plus manually approved/consented tester numbers. WhatsApp location check-ins are event-based driver shares, not continuous GPS tracking.

Supported driver commands include STATUS, ETA, HOW LONG, WHAT NOW, WHERE MUST I GO, HELP, READY, BREAK 15, LUNCH 30, DELAYED 20, HOLDING, ARRIVED_STAGING, PROCEEDING_GATE, ARRIVED_GATE, COMPLETED, ISSUE and LOCATION_SHARED. Fleet and mobile APIs expose current status, allowed next actions, timeline, last location check-in and impact values.

90-second demo: control room detects congestion; execution plan recommends truck actions; fleet owner sees affected trucks; driver receives simulated or LiveTest WhatsApp instruction; driver asks HOW LONG or WHAT NOW; driver checks in or confirms staging/gate; Smart Port updates ETA/status/timeline/dashboard; Android shows the same state and Copilot answer; idling/CO2 impact updates; audit trail records the decision.
