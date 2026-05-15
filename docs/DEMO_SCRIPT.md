# Submission Demo Walkthrough — Culltron Smart Port Flow

This walkthrough presents the product as an enterprise AI operations prototype. Use synthetic data honestly and avoid claiming live port-system connectivity unless a pilot connector is explicitly configured.

## 1. Public product entry

- Open `/`.
- Point out the closed operational loop: control-room signal → execution plan → fleet owner → driver → WhatsApp/Android update → impact/audit.
- Click **Demo Access** rather than opening internal pages directly.

## 2. Demo access

- Open `/demo-access`.
- Select the role that matches the audience: Port Admin Demo, Fleet Owner Demo, or Driver Demo.
- Enter the configured `SMARTPORT_DEMO_ACCESS_CODE`.

## 3. Fleet dashboard

- Open `/fleet`.
- Explain queue pressure, staged releases, high-risk trucks, notification readiness, and idling/CO₂ estimates.
- Generate or open an execution plan.

## 4. Execution plan

- Open `/execution/plans` or a plan detail page.
- Show truck-level action cards, reasons, call-forward timing, and audit entries.

## 5. Truck operations console

- Open `/fleet/trucks/SPQ-2026-0042`.
- Send a simulated WhatsApp and an in-app alert.
- Use Move to Staging, Release to Gate, Reschedule, or Mark Exception.
- Ask Copilot what the fleet owner should tell the driver next.

## 6. Driver companion

- Open `/driver` or `/truck/status/SPQ-2026-0042`.
- Run driver actions: Check ETA, I am holding, arrived at staging, proceeding to gate, arrived at gate, completed, break, delayed, location check-in, issue, and Copilot.
- Confirm the status is reflected back in the fleet surfaces.

## 7. Notifications and settings

- Open `/fleet/notifications` and show the communication timeline.
- Open `/fleet/settings` and show Gemini and WhatsApp readiness cards.
- Emphasize that secrets are not displayed and LiveTest sends require approved driver consent.

## 8. Android companion

- Open `/fleet/download-app`.
- Explain that the Android companion source is ready and can be built from Android Studio. The app calls backend APIs and stores no Gemini or WhatsApp keys.

## 9. Closing message

Culltron Smart Port Flow is demo-ready with synthetic data and pilot-integration readiness. It is designed to remain honest, human-approved, and auditable while showing a credible path to real operational integration.
