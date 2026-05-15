#!/usr/bin/env bash
set -euo pipefail
BASE_URL="${BASE_URL:-http://localhost:8080}"
DEMO_CODE="${SMARTPORT_DRIVER_DEMO_CODE:-culltron-driver-2026}"
VERIFY_TOKEN="${SMARTPORT_WHATSAPP_VERIFY_TOKEN:-demo-verify-token}"
check(){ local path="$1"; echo "==> $path"; curl -fsS -o /dev/null -w "%{http_code}\n" "$BASE_URL$path"; }
check "/"
check "/platform" || true
check "/demo-access"
check "/dashboard" || true
check "/fleet" || true
check "/driver" || true
check "/execution/plans" || true
check "/gemini-agent" || check "/Copilot" || true
check "/fleet/settings" || true
check "/fleet/download-app" || true
TOKEN=$(curl -fsS -X POST "$BASE_URL/api/mobile/auth/demo-login" -H 'Content-Type: application/json' -d "{\"role\":\"Driver Demo\",\"accessCode\":\"$DEMO_CODE\"}" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')
if [ -n "$TOKEN" ]; then curl -fsS "$BASE_URL/api/mobile/driver/demo" -H "X-SmartPort-Mobile-Token: $TOKEN" >/dev/null; echo "mobile API ok"; fi
curl -i -s "$BASE_URL/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=$VERIFY_TOKEN&hub.challenge=smartport" | head -n 1
