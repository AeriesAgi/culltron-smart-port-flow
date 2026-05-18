# Smart Port Driver Companion APK build

## Stable public route

- Public URL: `https://smartport.culltron.app/downloads/SmartPortDriverCompanion-debug.apk`
- Repository publish path: `src/SmartPort.Web/wwwroot/downloads/SmartPortDriverCompanion-debug.apk`
- CI artifact name: `SmartPortDriverCompanion-debug.apk`

If the APK is not present, `/fleet/download-app` shows an **APK artifact pending** message instead of a broken download button.

## Local build

```bash
cd mobile/SmartPortDriverCompanion
./gradlew assembleDebug
mkdir -p ../../src/SmartPort.Web/wwwroot/downloads
cp app/build/outputs/apk/debug/app-debug.apk ../../src/SmartPort.Web/wwwroot/downloads/SmartPortDriverCompanion-debug.apk
```

A local Android SDK is required (`ANDROID_HOME` or `local.properties` with `sdk.dir`).

## GitHub Actions

`.github/workflows/build-android-apk.yml` builds the debug APK, copies it to `artifacts/SmartPortDriverCompanion-debug.apk`, also stages the same stable filename under `src/SmartPort.Web/wwwroot/downloads`, and uploads the artifact.

## App target

The native app defaults to `https://smartport.culltron.app` as the backend API base and starts at its own driver login screen. Web/App-shell entry is `https://smartport.culltron.app/driver-app/login`. It calls Smart Port mobile APIs directly and keeps the app in its own native shell; it is not a website wrapper and stores no Gemini, WhatsApp, database, or provider secrets.
