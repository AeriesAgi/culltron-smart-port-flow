# Sunday Hackathon Demo Script — Culltron Smart Port Flow

## Local run
```bash
docker compose up -d db
dotnet run --project src/SmartPort.Web/SmartPort.Web.csproj
```

## Demo flow
1. Open `/` and explain the high-tech landing page: Smart Port closes the loop from AI recommendations to driver execution, ETA updates, WhatsApp location check-ins, audit and CO2/idling impact.
2. Open `/dashboard` for the control-room overview.
3. Open `/fleet` and show the live truck queue, high-risk trucks, execution plan generator and impact metrics.
4. Open `/execution` and `/execution/plans`; show truck-level actions, reasons, ETA/call-forward, idling avoided, CO2 avoided and confidence.
5. Open a truck detail page such as `/fleet/trucks/SPQ-2026-0042` and explain it is the fleet-owner operations console.
6. Show driver contact/WhatsApp safety: seeded demo contacts are blocked for LiveTest; only active, manually added, Test Approved and WhatsApp Consent Confirmed numbers can receive LiveTest.
7. Click **Send Simulated WhatsApp** to prove demo mode works without paid providers.
8. If Meta credentials and an approved tester exist, click **Send LiveTest WhatsApp** and then **Request LiveTest Location**.
9. Open `/driver` then `/driver/demo`; use buttons for HOW LONG, HOLDING, ARRIVED_STAGING, ARRIVED_GATE or LOCATION_SHARED. The timeline, ETA, audit and notification history update.
10. If WhatsApp LiveTest is configured, have the driver reply `STATUS`, `HOW LONG`, `BREAK 15`, `READY`, `ARRIVED_GATE`, or share a WhatsApp location check-in. The webhook updates backend state and sends a reply.
11. Open `/fleet/notifications` and show notification/audit history.
12. Open `/fleet/download-app` and show the Android companion source/build/APK status.
13. Open `/fleet/data-sources` and explain integration readiness honestly: current demo uses synthetic data; pilot connectors can integrate IPMS/TOS/gate/berth/yard/fleet feeds later.
14. Open `/fleet/settings`; show Gemini and WhatsApp readiness without exposing secrets. Run **Test Gemini Copilot** if `GEMINI_API_KEY` is configured; otherwise show fallback.
15. Ask Copilot a driver or fleet question, for example “How long until I am called forward?” or “Which trucks should move first?”

## Key proof statement
Smart Port does not only recommend actions. It executes them across fleet owners, drivers, WhatsApp LiveTest, Android/mobile APIs and auditable operational workflows.
