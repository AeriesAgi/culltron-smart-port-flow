# WhatsApp Cloud API LiveTest Guide

Culltron Smart Port Flow supports **Demo**, **ConnectorReady**, and **LiveTest** WhatsApp modes. Demo mode never calls Meta. LiveTest calls Meta WhatsApp Cloud API only when all safety conditions are met.

## Environment variables

```bash
SMARTPORT_WHATSAPP_MODE=LiveTest
SMARTPORT_WHATSAPP_ENABLED=true
SMARTPORT_WHATSAPP_ACCESS_TOKEN=<Meta temporary or permanent token>
SMARTPORT_WHATSAPP_PHONE_NUMBER_ID=<Meta phone number ID>
SMARTPORT_WHATSAPP_BUSINESS_ACCOUNT_ID=<Meta business account ID>
SMARTPORT_WHATSAPP_VERIFY_TOKEN=<your webhook verify token>
SMARTPORT_WHATSAPP_GRAPH_VERSION=v20.0
SMARTPORT_PUBLIC_BASE_URL=https://smartport.culltron.app
```

Do not commit these values. Do not put Meta tokens in Android, JavaScript, logs, screenshots, or public UI.

## Meta callback URL

Set the Meta webhook callback URL to:

```text
{SMARTPORT_PUBLIC_BASE_URL}/webhooks/whatsapp
```

Example for the public deployment domain:

```text
https://smartport.culltron.app/webhooks/whatsapp
```

The verify token in Meta must exactly match `SMARTPORT_WHATSAPP_VERIFY_TOKEN`.

## Add an approved test driver

1. Open `/demo-access` and enter a Fleet Owner or Port Admin demo credential.
2. Open `/fleet/drivers`.
3. Add or edit a driver with a real international WhatsApp number.
4. Mark the driver as active.
5. Confirm WhatsApp consent.
6. Mark the driver Test Approved.
7. Assign that driver to a truck/job.
8. Keep demo seed records blocked for LiveTest.

## Send a LiveTest message

1. Open `/fleet/trucks/{reference}` for the truck assigned to the approved test driver.
2. Confirm `/fleet/settings` shows LiveTest allowed.
3. Select **Send LiveTest WhatsApp** or **Request Location**.
4. Check `/fleet/notifications` for masked recipient, status, event type, source, and Meta message ID when Meta returns one.

## Inbound commands to test

The approved driver can reply with:

- `STATUS`
- `ETA`
- `HOW LONG`
- `WHAT NOW`
- `READY`
- `BREAK 15`
- `LUNCH 30`
- `DELAYED 20`
- `ARRIVED_STAGING`
- `ARRIVED_GATE`
- `COMPLETED`
- `ISSUE`

Interactive replies and shared WhatsApp locations are also accepted. Location check-ins update ETA/status context, timeline and notification history.

## Expected UI updates

- `/fleet/notifications` records inbound and outbound events.
- `/driver/status/{reference}` reflects updated truck state and timeline.
- `/fleet/trucks/{reference}` reflects operational status, latest notification and allowed actions.
- `/api/mobile/truck/status/{reference}` returns the same backend state for Android.

Unapproved senders are safely ignored and audited without changing truck state.
