# Integration Roadmap

Culltron Smart Port Flow currently runs on synthetic operational data. The demo is intentionally honest: it does not claim live IPMS, terminal operating system, gate, berth, yard, fleet, WhatsApp, GPS, or telematics integration unless those systems are configured in an approved pilot.

## Current demo mode

- Synthetic truck queue and appointment data.
- Synthetic berth, yard, gate and congestion context.
- Simulated in-app and WhatsApp notifications in Demo Mode.
- Gemini-enhanced explanations only when server-side credentials are configured.
- Deterministic fallback remains available without external AI credentials.

## Pilot integration candidates

1. IPMS or port management signals for berth, vessel, and operational events.
2. Terminal operating systems for container, yard, and gate context.
3. Gate appointment systems for time slots and release windows.
4. Fleet owner systems for driver/job assignment and vehicle references.
5. Meta WhatsApp Cloud API for approved LiveTest messaging.
6. GPS/telematics feeds where consent, governance, and data-sharing agreements exist.
7. Emissions and energy data sources for validated sustainability reporting.

## Governance requirements

- Partner-approved access and security review.
- Data-processing agreement and credential management.
- Human approval for operational actions.
- Audit trail for recommendations, notifications, and driver acknowledgements.
- Pilot validation before production use.
