# Smart Port Driver Companion Android App

Native Kotlin companion client for **Culltron Smart Port Flow**. The Android app is real source code and delegates queue intelligence, Copilot, WhatsApp and state transitions to the ASP.NET backend.

## Open and build
1. Open `mobile/SmartPortDriverCompanion` in Android Studio.
2. Let Gradle sync the Kotlin/Android project.
3. Keep the default backend URL or set a local/dev URL on the first screen.
4. Run on an emulator/device or choose **Build > Build APK(s)**.
5. If you produce a demo APK, copy it to `src/SmartPort.Web/wwwroot/downloads/SmartPortDriverCompanion.apk` so `/fleet/download-app` can expose it.

The Codespaces/container environment may not include Android SDK/build tools; APK generation should be run in Android Studio or an Android-enabled CI runner. The web build must not depend on Android tooling.

## Backend URL
Default backend: `https://smartport.culltron.app`.

Local development examples:
- Android emulator to local ASP.NET HTTPS: `https://10.0.2.2:5001`
- Physical device: use a reachable LAN HTTPS URL or deployed/Codespaces forwarded URL.

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
- `POST /api/mobile/device/register` placeholder

The app can fetch queue number, ETA, gate/staging/current status, latest instruction, notification history and allowed actions; post driver confirmations and demo location check-ins; and call backend Copilot.

## Secrets and AI rules
The Android app stores no Gemini key, WhatsApp token, phone-number ID, verify token, API key or backend AI logic. Gemini and WhatsApp integrations run server-side only. Demo location is a driver-triggered check-in, not continuous GPS tracking.

## Play Store path
Add signed release config, production privacy copy, future authentication, Firebase token registration if push is enabled, and CI-based APK/AAB signing before Play Store release.
