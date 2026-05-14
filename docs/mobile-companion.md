# Smart Port Android Driver Companion

The Android companion in `mobile/SmartPortDriverCompanion` is a Kotlin client for Smart Port backend APIs. It reads queue status, notification history, and writes driver acknowledgements through the web backend.

## Demo mode
No Firebase, WhatsApp, billing, or secrets are required. The app pulls notification history from Smart Port and displays simulated WhatsApp/in-app/Android push records.

## Backend APIs
- `GET /api/mobile/truck/status/{reference}`
- `POST /api/mobile/truck/check`
- `GET /api/mobile/notifications/{reference}`
- `POST /api/mobile/driver/acknowledge`
- `GET /api/mobile/driver/demo`
- `POST /api/mobile/device/register`
- `POST /api/mobile/device/unregister`

## Future Firebase setup
Use environment variables only: `SMARTPORT_PUSH_ENABLED`, `SMARTPORT_PUSH_PROVIDER=Firebase`, `SMARTPORT_FIREBASE_PROJECT_ID`, `SMARTPORT_FIREBASE_SERVICE_ACCOUNT_PATH`, or `SMARTPORT_FIREBASE_CREDENTIALS_JSON`. Never hardcode Firebase credentials in Android or server source.


## Enterprise Execution Platform Update

Smart Port now models an execution loop: Control Room/AI Simulator → Execution Plan Generator → Fleet Owner Operations Console → Driver Web Portal/Android Companion → WhatsApp Demo or LiveTest notifications → Driver Status Assistant → audit-ready ETA, queue, status, timeline and emissions impact updates. The deterministic state machine and queue optimizer work without Gemini; Gemini/Copilot remains an optional explanation layer. Demo Mode is free and safe, no SMS is used, no paid provider is required, and LiveTest WhatsApp is gated by environment variables plus manually approved/consented tester numbers. WhatsApp location check-ins are event-based driver shares, not continuous GPS tracking.

Supported driver commands include STATUS, ETA, HOW LONG, WHAT NOW, WHERE MUST I GO, HELP, READY, BREAK 15, LUNCH 30, DELAYED 20, HOLDING, ARRIVED_STAGING, PROCEEDING_GATE, ARRIVED_GATE, COMPLETED, ISSUE and LOCATION_SHARED. Fleet and mobile APIs expose current status, allowed next actions, timeline, last location check-in and impact values.

90-second demo: control room detects congestion; execution plan recommends truck actions; fleet owner sees affected trucks; driver receives simulated or LiveTest WhatsApp instruction; driver asks HOW LONG or WHAT NOW; driver checks in or confirms staging/gate; Smart Port updates ETA/status/timeline/dashboard; Android shows the same state and Copilot answer; idling/CO2 impact updates; audit trail records the decision.
