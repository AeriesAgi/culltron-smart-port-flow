# Smart Port Driver Companion Android App

Native Kotlin companion client for **Smart Port Fleet & Driver Queue Companion**.

## Open and build
1. Open `mobile/SmartPortDriverCompanion` in Android Studio.
2. Let Gradle sync.
3. Run on an emulator/device or choose **Build > Build APK(s)**.

The repository environment may not include Android SDK/build tools; APK generation should be run in Android Studio or an Android-enabled CI runner.

## Backend URL
Default backend: `https://smartport.culltron.app`. The first screen lets you override it for local development, for example `https://10.0.2.2:5001` when using an emulator.

## Demo references
- `SPQ-2026-0042`
- `SPQ-2026-0043`
- `SPQ-2026-0044`

## API usage
The app calls the Smart Port backend only:
- `GET /api/mobile/truck/status/{reference}`
- `GET /api/mobile/notifications/{reference}` through the status payload/history screen
- `POST /api/mobile/driver/acknowledge`
- `POST /api/mobile/device/register` placeholder

It does not duplicate backend queue logic and stores no Gemini, WhatsApp, or Firebase secrets.

## Notifications
Hackathon mode uses in-app history pulled from Smart Port and simulated WhatsApp/Android push records. Future Firebase Cloud Messaging can be wired by replacing the placeholder device token flow with a real token and enabling backend push configuration.

## Play Store path
Add signed release config, production privacy copy, future authentication, Firebase token registration, and CI-based APK/AAB signing before Play Store release.
