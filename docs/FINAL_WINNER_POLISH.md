# Final winner-grade completion pass

Smart Port is now framed as an app-first operational AI product rather than a messaging demo.

## Product story

- Smart Port operational AI converts synthetic port, berth, gate and fleet signals into human-approved execution plans.
- The Driver Companion App, web companion and mobile API are the primary driver channel.
- Driver check-ins update fleet/control-room tracking with last location, ETA, queue state, status, impact and audit history.
- WhatsApp Cloud API is optional connector-ready support for sandbox/live-test and future production pilots only.

## Gemini architecture

- Gemini is enabled by default when `GEMINI_API_KEY` is present and remains on-demand only.
- Premium reasoning and judge/operator plans use `gemini-2.5-flash` first.
- Routine Copilot, driver, fleet and summarisation tasks use `gemini-3.1-flash-lite` first.
- The fallback chain is configured through `Gemini__FallbackModels`.
- Page loads, dashboards, health checks, mobile login/status refreshes and background flows do not call Gemini.
- If Gemini is missing, disabled, quota-limited or unsupported, deterministic fallback returns a transparent response.

## APK build/install path

- Android project: `mobile/SmartPortDriverCompanion`.
- Local build command: `cd mobile/SmartPortDriverCompanion && ./gradlew assembleDebug`.
- GitHub Actions workflow: `.github/workflows/build-android-apk.yml`.
- CI artifact: `mobile/SmartPortDriverCompanion/artifacts/SmartPortDriverCompanion-debug.apk`.
- Web download path: `src/SmartPort.Web/wwwroot/downloads/SmartPortDriverCompanion.apk`.
- If no APK binary is published, `/fleet/download-app` still presents the build path and offers the web companion as a no-install judge demo.

## Demo path

1. `/demo-access` → Judge quick-fill.
2. `/demo-tour` for guided narrative.
3. `/gemini-agent` → generate an operations brief.
4. `/execution/plans` → inspect execution plans.
5. `/fleet/trucks/SPQ-2026-0042` → request app check-in and ask Copilot.
6. `/driver/demo` → submit GPS/manual check-in and driver action.
7. `/fleet/tracker` → verify location, ETA, status and audit trail updated.
8. `/fleet/download-app` → show APK/mobile API install centre.
9. `/enterprise-readiness` and `/health/integrations` → close with pilot readiness.

## Remaining production caveats

- Demo data is synthetic; live port/fleet/GPS/WhatsApp integrations require approved credentials, data agreements and pilot governance.
- GPS/check-in is user-triggered, not background tracking.
- Android APK publishing requires CI or an Android SDK-enabled local build environment.
