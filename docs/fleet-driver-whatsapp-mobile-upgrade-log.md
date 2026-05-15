# Fleet / Driver / WhatsApp / Mobile Upgrade Log

## Final hackathon pass
- Public landing page refreshed as a high-tech enterprise AI Smart Port website with honest demo-mode and integration-readiness language.
- Android companion nav now points to `/fleet/download-app`; `/mobile/download` is also available.
- Android download/status page documents default backend, local development URL, demo references, build steps, APK status and no-secret rules.
- Fleet settings now exposes Gemini and WhatsApp readiness without showing tokens or API keys.
- WhatsApp LiveTest path uses Meta Cloud API only when `SMARTPORT_WHATSAPP_MODE=LiveTest`, `SMARTPORT_WHATSAPP_ENABLED=true`, token and phone-number ID are configured, and the driver is approved/consented/non-seed.
- WhatsApp webhook supports verification, inbound text, interactive replies and location messages. Approved driver commands update queue state and generate replies; unapproved senders are audited and ignored.
- Driver web portal action buttons now post to backend command handling and update shared state.
- Fleet truck detail is an operations console with simulated WhatsApp, LiveTest WhatsApp, location request, in-app alert, staging/gate/reschedule/exception and Copilot actions.
- Mobile APIs and Copilot endpoints have documented curl smoke tests.

## LiveTest commands
`STATUS`, `ETA`, `HOW LONG`, `WHAT NOW`, `WHERE MUST I GO`, `HELP`, `READY`, `BREAK 15`, `LUNCH 30`, `DELAYED 20`, `HOLDING`, `ARRIVED_STAGING`, `PROCEEDING_GATE`, `ARRIVED_GATE`, `COMPLETED`, `ISSUE`, `LOCATION_SHARED`.
