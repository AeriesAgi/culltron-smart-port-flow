# Smart Port Driver Companion Android App

Native Kotlin companion client for **Culltron Smart Port Flow**. The Android app delegates queue intelligence, Copilot, WhatsApp coordination and state transitions to the ASP.NET backend.

## Build options

### Automated GitHub Actions build

Run `.github/workflows/build-android-apk.yml`. The workflow sets up JDK and Android SDK, builds the debug APK, and uploads the APK artifact.

To publish the APK through the Smart Port web app, copy the artifact or local build output to:

```text
src/SmartPort.Web/wwwroot/downloads/SmartPortDriverCompanion.apk
```

### Android Studio or local Gradle

1. Open `mobile/SmartPortDriverCompanion` in Android Studio or an Android SDK-enabled shell.
2. Let Gradle sync the Kotlin/Android project.
3. Keep the default backend URL or set a local/dev URL on the first screen.
4. Build the debug APK.
5. Copy `app/build/outputs/apk/debug/app-debug.apk` to the web downloads path above before deployment if a direct web download is required.

The container used for the web app may not include Android SDK/build tools; the web build does not depend on Android tooling.

## Backend URL

Default backend: `https://smartport.culltron.app`.

Local development examples:
- Android emulator to local ASP.NET HTTPS: `https://10.0.2.2:5001`
- Physical device: use a reachable LAN HTTPS URL or deployed/forwarded HTTPS URL.

## Demo references

- `SPQ-2026-0042`
- `SPQ-2026-0043`
- `SPQ-2026-0044`
- `SPQ-2026-0045`
- `SPQ-2026-0046`
- `SPQ-2026-0047`

## API usage

The app calls the Smart Port backend only:
- `GET /api/mobile/truck/status/{reference}`
- `GET /api/mobile/notifications/{reference}` via status/notification history
- `POST /api/mobile/driver/acknowledge`
- `POST /api/mobile/driver/confirm-status`
- `POST /api/mobile/driver/location-checkin`
- `POST /api/mobile/copilot/driver`
- `POST /api/mobile/device/register`

The app can fetch queue number, ETA, gate/staging/current status, latest instruction, notification history and allowed actions; post driver confirmations and demo location check-ins; and call backend Copilot.

## Secrets and AI rules

The Android app stores no Gemini key, WhatsApp token, phone-number ID, verify token, API key or backend AI logic. Gemini and WhatsApp integrations run server-side only. Demo location is a driver-triggered check-in, not continuous GPS tracking.
