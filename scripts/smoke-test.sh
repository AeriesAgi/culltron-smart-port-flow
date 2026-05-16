#!/usr/bin/env bash
set -euo pipefail
BASE_URL="${1:-${BASE_URL:-http://localhost:8080}}"
DRIVER_DEMO_CODE="${SMARTPORT_DRIVER_DEMO_CODE:-culltron-driver-2026}"
VERIFY_TOKEN="${SMARTPORT_WHATSAPP_VERIFY_TOKEN:-demo-verify-token}"

check(){ local path="$1"; echo "==> $path"; curl -fsS -o /dev/null -w "%{http_code}\n" "$BASE_URL$path"; }

check_optional(){ local path="$1"; if ! check "$path"; then echo "WARN: $path unavailable or requires authenticated browser session"; fi }

check "/"
check "/platform"
check "/features"
check "/demo"
check "/product"
check "/pricing"
check "/about"
check "/contact"
check "/demo-access"
check_optional "/demo-tour"
check_optional "/dashboard"
check_optional "/gemini-agent"
check_optional "/agent-governance"
check_optional "/enterprise-readiness"
check_optional "/execution/plans"
check_optional "/fleet"
check_optional "/fleet/trucks/SPQ-2026-0042"
check_optional "/driver/demo"
check_optional "/fleet/notifications"
check_optional "/fleet/download-app"
check "/health"
check "/health/readiness"
check "/health/integrations"

TOKEN=$(curl -fsS -X POST "$BASE_URL/api/mobile/auth/demo-login" -H 'Content-Type: application/json' -d "{\"role\":\"Driver Demo\",\"accessCode\":\"$DRIVER_DEMO_CODE\"}" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')
if [ -z "$TOKEN" ]; then echo "mobile demo login failed" >&2; exit 1; fi
curl -fsS "$BASE_URL/api/mobile/truck/status/SPQ-2026-0042" -H "X-SmartPort-Mobile-Token: $TOKEN" >/dev/null
curl -fsS "$BASE_URL/api/mobile/notifications/SPQ-2026-0042" -H "X-SmartPort-Mobile-Token: $TOKEN" >/dev/null
if curl -fsS "$BASE_URL/api/mobile/truck/status/SPQ-2026-0042" >/dev/null; then echo "invalid token unexpectedly succeeded" >&2; exit 1; else echo "invalid token rejected"; fi

curl -i -s "$BASE_URL/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=$VERIFY_TOKEN&hub.challenge=smartport" | head -n 1
if curl -fsS "$BASE_URL/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=wrong&hub.challenge=smartport" >/dev/null; then echo "invalid WhatsApp verify token unexpectedly succeeded" >&2; exit 1; else echo "invalid WhatsApp verify token rejected"; fi

SAMPLE_PAYLOAD='{"entry":[{"changes":[{"value":{"contacts":[{"profile":{"name":"Demo Driver"},"wa_id":"27820000000"}],"messages":[{"from":"27820000000","id":"wamid.demo","timestamp":"1760000000","type":"text","text":{"body":"STATUS SPQ-2026-0042"}}]}}]}]}'
curl -fsS -X POST "$BASE_URL/webhooks/whatsapp" -H 'Content-Type: application/json' -d "$SAMPLE_PAYLOAD" >/dev/null
