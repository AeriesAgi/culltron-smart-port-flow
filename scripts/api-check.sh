#!/usr/bin/env bash
set -euo pipefail
BASE_URL="${1:-${BASE_URL:-http://localhost:8080}}"
DRIVER_DEMO_CODE="${SMARTPORT_DRIVER_DEMO_CODE:-culltron-driver-2026}"
VERIFY_TOKEN="${SMARTPORT_WHATSAPP_VERIFY_TOKEN:-}"
REFERENCE="${SMARTPORT_DEMO_REFERENCE:-SPQ-2026-0042}"

echo "==> Mobile invalid token rejection"
invalid_code=$(curl -sS -o /dev/null -w '%{http_code}' "$BASE_URL/api/mobile/truck/status/$REFERENCE" -H "X-SmartPort-Mobile-Token: invalid" || true)
[[ "$invalid_code" == "401" ]] || { echo "invalid mobile token returned $invalid_code, expected 401" >&2; exit 1; }

echo "==> Mobile demo login"
login_body=$(curl -fsS -X POST "$BASE_URL/api/mobile/auth/demo-login" -H 'Content-Type: application/json' -d "{\"role\":\"Driver Demo\",\"accessCode\":\"$DRIVER_DEMO_CODE\"}")
token=$(printf '%s' "$login_body" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')
[[ -n "$token" ]] || { echo "mobile demo login did not return a token" >&2; exit 1; }
echo "token issued: ${token:0:8}… (masked)"

echo "==> Mobile truck status with token"
curl -fsS "$BASE_URL/api/mobile/truck/status/$REFERENCE" -H "X-SmartPort-Mobile-Token: $token" >/dev/null

echo "==> Mobile notifications with token"
curl -fsS "$BASE_URL/api/mobile/notifications/$REFERENCE" -H "X-SmartPort-Mobile-Token: $token" >/dev/null

echo "==> Mobile driver confirm-status"
curl -fsS -X POST "$BASE_URL/api/mobile/driver/confirm-status" -H 'Content-Type: application/json' -H "X-SmartPort-Mobile-Token: $token" -d "{\"reference\":\"$REFERENCE\",\"eventType\":\"DriverAcknowledgedInstruction\",\"sourceLabel\":\"Android\"}" >/dev/null

echo "==> Mobile location check-in"
curl -fsS -X POST "$BASE_URL/api/mobile/driver/location-checkin" -H 'Content-Type: application/json' -H "X-SmartPort-Mobile-Token: $token" -d "{\"reference\":\"$REFERENCE\",\"eventType\":\"AndroidLocationCheckIn\",\"sourceLabel\":\"Android\",\"locationLabel\":\"API check staging\"}" >/dev/null

echo "==> Mobile Driver Copilot fallback-safe response"
curl -fsS -X POST "$BASE_URL/api/mobile/copilot/driver" -H 'Content-Type: application/json' -H "X-SmartPort-Mobile-Token: $token" -d "{\"reference\":\"$REFERENCE\",\"userRole\":\"Driver\",\"question\":\"What should I do now?\"}" >/dev/null

echo "==> Health integrations (must not call Gemini)"
curl -fsS "$BASE_URL/health/integrations" >/dev/null

echo "==> WhatsApp invalid verify token rejection"
wa_code=$(curl -sS -o /dev/null -w '%{http_code}' "$BASE_URL/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=invalid-api-check&hub.challenge=smartport" || true)
[[ "$wa_code" == "403" || "$wa_code" == "401" || "$wa_code" == "400" ]] || { echo "invalid WhatsApp verify token returned $wa_code, expected rejection" >&2; exit 1; }

if [[ -n "$VERIFY_TOKEN" ]]; then
  echo "==> WhatsApp valid verify token"
  challenge=$(curl -fsS "$BASE_URL/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=$VERIFY_TOKEN&hub.challenge=smartport")
  [[ "$challenge" == "smartport" ]] || { echo "WhatsApp challenge mismatch" >&2; exit 1; }
else
  echo "==> WhatsApp valid verify token skipped (SMARTPORT_WHATSAPP_VERIFY_TOKEN not set)"
fi

echo "API check passed. Gemini UI button is available at /gemini-agent; fallback remains active when GEMINI_API_KEY is unset."
