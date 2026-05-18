# Video Script — Final Smart Port App-First Tracking Story

**0:00-0:20 — Control room congestion**
Open the Dashboard. Port Admin sees congestion, active fleet pressure, driver tracking summary, delayed drivers, stale/missing check-ins, gate/staging distribution, and queue/ETA/idling/CO₂ impact from synthetic demo data.

**0:20-0:50 — Gemini Operations Agent**
Open Gemini Operations Agent and generate an operations brief on demand. Explain that Gemini is server-side, user-triggered, quota-safe, and backed by deterministic fallback.

**0:50-1:25 — Execution plan to fleet owner**
Open Execution Plans, then Fleet Tracker. Show tracked trucks and highlight SPQ-2026-0042 with registration, driver, status, gate, staging zone, queue position, ETA, delay risk, last driver action, check-in and source.

**1:25-2:00 — Driver Companion App / driver app shell**
Open `/driver/status/SPQ-2026-0042` or `/driver-app`. The driver checks current instruction, submits a status action, then clicks “Share demo location / Check in.” Browser geolocation is requested only after the click; manual/demo location label works if GPS is denied.

**2:00-2:30 — Tracker and operational impact update**
Return to Fleet Tracker and truck detail. Show latest app check-in/location, status/check-in timeline, current ETA/call-forward, delay risk, idling minutes avoided, CO₂ avoided, notifications and audit history.

**2:30-2:55 — Copilot tracking intelligence**
Ask SmartPort Copilot: “Which tracked drivers need attention?” or “Which drivers have stale check-ins?” Show a tracker answer with status, last check-in, ETA/delay risk, action recommendation, and model/fallback source.

**2:55-3:20 — Governance and human control**
Open Agent Governance. Emphasize approvals, auditability, deterministic fallback, no automatic Gemini calls on page load, no secrets on device, and user-triggered location only.

**3:20-3:35 — APK/download path**
Open `/fleet/download-app`. Show APK status, driver app shell, fleet tracker, driver status page, backend URL instructions, demo code `smartport2026`, demo reference `SPQ-2026-0042`, and mobile API proof path.

**3:35-3:45 — Optional WhatsApp connector**
Mention WhatsApp only as: “Optional connector-ready WhatsApp Cloud API integration for future pilots/live-test messaging.” The judge demo does not depend on WhatsApp production approval.

**Final CTA**
Closed loop: control room → Gemini operations agent → execution plan → fleet owner → Driver Companion App/web/mobile API → driver check-in/location → tracker/ETA/idling/CO₂ update → audit/governance.
