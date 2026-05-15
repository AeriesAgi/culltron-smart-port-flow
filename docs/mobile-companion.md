# Smart Port Android / Mobile Companion

The mobile companion consists of backend mobile APIs plus native Android source under `/mobile/SmartPortDriverCompanion`.

## Backend URL
Default Android backend: `https://smartport.culltron.app`.
Local emulator guidance: `https://10.0.2.2:5001` or a forwarded Codespaces/deployed URL.

## Demo references
`SPQ-2026-0042`, `SPQ-2026-0043`, `SPQ-2026-0044`, `SPQ-2026-0045`, `SPQ-2026-0046`, `SPQ-2026-0047`.

## Mobile APIs to smoke test
```bash
curl http://localhost:5000/api/mobile/truck/status/SPQ-2026-0042
curl http://localhost:5000/api/mobile/notifications/SPQ-2026-0042
curl -X POST http://localhost:5000/api/mobile/driver/confirm-status -H 'Content-Type: application/json' -d '{"reference":"SPQ-2026-0042","eventType":"DriverConfirmedHolding","sourceLabel":"Android"}'
curl -X POST http://localhost:5000/api/mobile/driver/location-checkin -H 'Content-Type: application/json' -d '{"reference":"SPQ-2026-0042","eventType":"WhatsAppLocationShared","sourceLabel":"WhatsApp","latitude":-29.8587,"longitude":31.0218,"locationLabel":"Durban staging area demo"}'
curl -X POST http://localhost:5000/api/mobile/driver/command -H 'Content-Type: application/json' -d '{"reference":"SPQ-2026-0042","commandText":"BREAK 15","actor":"Driver Demo"}'
curl -X POST http://localhost:5000/api/mobile/copilot/driver -H 'Content-Type: application/json' -d '{"reference":"SPQ-2026-0042","question":"How long until I am called forward?"}'
curl -X POST http://localhost:5000/api/mobile/copilot/fleet -H 'Content-Type: application/json' -d '{"question":"Which trucks should move first?"}'
```

## Android build
1. Open `/mobile/SmartPortDriverCompanion` in Android Studio.
2. Let Gradle sync.
3. Set backend URL if not using `https://smartport.culltron.app`.
4. Build a debug APK.
5. Optional demo path: `src/SmartPort.Web/wwwroot/downloads/SmartPortDriverCompanion.apk`.

The app stores no Gemini key, WhatsApp token or API secrets and does not duplicate backend AI logic.
