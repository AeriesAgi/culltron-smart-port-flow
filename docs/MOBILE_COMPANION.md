# Driver Companion App and Mobile API

Smart Port’s primary driver communication and tracking channel is the Driver Companion App / web companion / mobile API. The Android source is in `mobile/SmartPortDriverCompanion` and the central download/setup page is `/fleet/download-app`.

## Tracking behavior

- The app sends driver status, instructions acknowledgements, explicit check-ins and optional one-shot current location to Smart Port.
- Location is requested only when the driver taps **Share current location / Check in**.
- If GPS/browser geolocation is unavailable or denied, a manual/demo location label still records a successful check-in.
- No background tracking and no silent location collection are implemented.
- No Gemini API key, WhatsApp token, bearer token, phone number or provider secret is stored on device; all AI and connector calls remain backend-side.

## Mobile API proof path

- `POST /api/mobile/auth/demo-login`
- `GET /api/mobile/truck/status/{reference}`
- `GET /api/mobile/notifications/{reference}`
- `POST /api/mobile/driver/confirm-status`
- `POST /api/mobile/driver/location-checkin`
- `POST /api/mobile/copilot/driver`

Demo code: `smartport2026`
Demo reference: `SPQ-2026-0042`

## Fleet and control-room tracking

Fleet owners use `/fleet/tracker` and truck detail pages to see current status, gate/staging zone, queue position, ETA/call-forward time, delay risk, last driver action, latest app check-in, location label/coordinates when available, notification state and audit history. Port Admin sees a higher-level tracker summary on the Dashboard.

WhatsApp remains an optional connector-ready integration only: “Optional connector-ready WhatsApp Cloud API integration for future pilots/live-test messaging.” The judge flow does not depend on WhatsApp production approval.
