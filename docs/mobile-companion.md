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
