# Driver Companion App

The Driver Companion is app-first and separate from the normal web dashboard.

## Web/PWA shell

- Route: `/driver-app` or `/app/driver`.
- No public navbar/footer.
- Screens: Home, Assigned Job, Actions, Location Check-in, Copilot, Notifications/Audit and Profile/Safety.
- Location is sent only when the driver taps Check In.

## Android app

- Project: `mobile/SmartPortDriverCompanion`.
- Native Kotlin screens call backend mobile APIs directly; no Gemini/WhatsApp/API secrets live on device.
- Build command: `cd mobile/SmartPortDriverCompanion && ./gradlew assembleDebug`.
- CI artifact: `.github/workflows/build-android-apk.yml` uploads `SmartPortDriverCompanion-debug.apk`.

## Primary APIs

- `POST /api/mobile/auth/demo-login`
- `GET /api/mobile/driver/status/{reference}`
- `POST /api/mobile/driver/confirm-status`
- `POST /api/mobile/driver/location-checkin`
- `POST /api/mobile/driver/report-incident`
- `POST /api/mobile/copilot/driver`
