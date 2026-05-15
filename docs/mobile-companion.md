# Smart Port Driver Companion — Android Source

The Android Driver Companion source is included under `mobile/SmartPortDriverCompanion`. It is a native Kotlin project intended for queue status, driver confirmations, demo location check-ins, notification history, and backend Copilot calls.

## Backend URL

Default deployment URL:

```text
https://smartport.culltron.app
```

For local Android emulator testing, use a reachable ASP.NET backend URL such as:

```text
https://10.0.2.2:5001
```

For a physical device, use a LAN, tunnel, or deployed HTTPS URL that the device can reach.

## Security model

- The Android app stores no Gemini API key.
- The Android app stores no WhatsApp token.
- AI and WhatsApp logic remain on the backend.
- The app calls Smart Port backend APIs and renders the returned state.

## Expected capabilities

- Fetch truck status from `/api/mobile/truck/status/{reference}`.
- Show queue number, ETA, assigned gate, staging zone, current status, and latest instruction.
- Show notification history from `/api/mobile/notifications/{reference}`.
- Post confirmations and commands through backend endpoints.
- Post demo location check-ins.
- Call `/api/mobile/copilot/driver` for scoped driver assistance.
- Handle connectivity and validation errors without exposing secrets.

## Build APK

Open `mobile/SmartPortDriverCompanion` in Android Studio and run a debug build. If building from a CLI environment with the Android SDK installed, use the project Gradle wrapper or Gradle installation to assemble a debug APK, then place the resulting file at:

```text
src/SmartPort.Web/wwwroot/downloads/SmartPortDriverCompanion.apk
```

The web app does not require the Android SDK to build or deploy.
