#!/usr/bin/env bash
set -euo pipefail
BASE_URL="${1:-${BASE_URL:-http://localhost:8080}}"
DRIVER_DEMO_CODE="${SMARTPORT_DRIVER_DEMO_CODE:-culltron-driver-2026}"
VERIFY_TOKEN="${SMARTPORT_WHATSAPP_VERIFY_TOKEN:-}"
REFERENCE="${SMARTPORT_DEMO_REFERENCE:-SPQ-2026-0042}"

echo "==> Mobile demo login"
login_body=$(curl -fsS -X POST "$BASE_URL/api/mobile/auth/demo-login" -H 'Content-Type: application/json' -d "{\"role\":\"Driver Demo\",\"accessCode\":\"$DRIVER_DEMO_CODE\"}")
token=$(printf '%s' "$login_body" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')
if [[ -z "$token" ]]; then echo "mobile demo login did not return a token" >&2; exit 1; fi
echo "token issued: ${token:0:8}… (masked)"

echo "==> Mobile truck status with token"
curl -fsS "$BASE_URL/api/mobile/truck/status/$REFERENCE" -H "X-SmartPort-Mobile-Token: $token" >/dev/null

echo "==> Mobile notifications with token"
curl -fsS "$BASE_URL/api/mobile/notifications/$REFERENCE" -H "X-SmartPort-Mobile-Token: $token" >/dev/null

echo "==> Mobile invalid token rejection"
if curl -fsS "$BASE_URL/api/mobile/truck/status/$REFERENCE" -H "X-SmartPort-Mobile-Token: invalid" >/dev/null; then
  echo "invalid mobile token unexpectedly succeeded" >&2; exit 1
fi

echo "==> WhatsApp invalid verify token rejection"
if curl -fsS "$BASE_URL/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=invalid-api-check&hub.challenge=smartport" >/dev/null; then
  echo "invalid WhatsApp verify token unexpectedly succeeded" >&2; exit 1
fi

if [[ -n "$VERIFY_TOKEN" ]]; then
  echo "==> WhatsApp valid verify token"
  challenge=$(curl -fsS "$BASE_URL/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=$VERIFY_TOKEN&hub.challenge=smartport")
  [[ "$challenge" == "smartport" ]] || { echo "WhatsApp challenge mismatch" >&2; exit 1; }
else
  echo "==> WhatsApp valid verify token skipped (SMARTPORT_WHATSAPP_VERIFY_TOKEN not set)"
fi

echo "==> Gemini readiness"
curl -fsS "$BASE_URL/health/integrations" | sed -n 's/.*"gemini":{\([^}]*\)}.*/gemini: {\1}/p' || true

echo "API check passed. Gemini UI button is available at /gemini-agent; fallback remains active when GEMINI_API_KEY is unset."
