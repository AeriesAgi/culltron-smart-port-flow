#!/usr/bin/env bash
set -euo pipefail
BASE_URL="${1:-${BASE_URL:-http://localhost:8080}}"
VERIFY_TOKEN="${SMARTPORT_WHATSAPP_VERIFY_TOKEN:-}"
DRIVER_DEMO_CODE="${SMARTPORT_DRIVER_DEMO_CODE:-culltron-driver-2026}"
REFERENCE="${SMARTPORT_DEMO_REFERENCE:-SPQ-2026-0042}"

failures=0
check_status(){
  local label="$1" url="$2" allowed="$3" code
  shift 3
  code=$(curl -sS -o /tmp/smartport-link-audit.out -w '%{http_code}' "$@" "$url" || true)
  if [[ ",$allowed," == *",$code,"* ]]; then
    echo "PASS $label -> $code"
  else
    echo "FAIL $label -> $code (expected $allowed)" >&2
    failures=$((failures+1))
  fi
}

public_paths=(/ /platform /features /demo /product /pricing /about /contact /demo-access)
protected_paths=(/demo-tour /dashboard /gemini-agent /agent-governance /ops-ingest /execution/plans /fleet /fleet/trucks/$REFERENCE /driver/demo /fleet/notifications /fleet/download-app /enterprise-readiness)
health_paths=(/health /health/readiness /health/integrations)

for path in "${public_paths[@]}"; do check_status "public $path" "$BASE_URL$path" "200"; done
for path in "${protected_paths[@]}"; do check_status "protected $path" "$BASE_URL$path" "200,302"; done
for path in "${health_paths[@]}"; do check_status "health $path" "$BASE_URL$path" "200"; done

check_status "mobile invalid token rejected" "$BASE_URL/api/mobile/truck/status/$REFERENCE" "401"

login_body=$(curl -fsS -X POST "$BASE_URL/api/mobile/auth/demo-login" -H 'Content-Type: application/json' -d "{\"role\":\"Driver Demo\",\"accessCode\":\"$DRIVER_DEMO_CODE\"}" || true)
mobile_token=$(printf '%s' "$login_body" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')
if [[ -n "$mobile_token" ]]; then
  check_status "mobile truck status with demo token" "$BASE_URL/api/mobile/truck/status/$REFERENCE" "200" -H "X-SmartPort-Mobile-Token: $mobile_token"
else
  echo "FAIL mobile demo login did not return token" >&2
  failures=$((failures+1))
fi

check_status "WhatsApp invalid verify token rejected" "$BASE_URL/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=invalid-link-audit&hub.challenge=smartport" "401"
if [[ -n "$VERIFY_TOKEN" ]]; then
  check_status "WhatsApp valid verify token" "$BASE_URL/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=$VERIFY_TOKEN&hub.challenge=smartport" "200"
else
  echo "SKIP WhatsApp valid verify token (SMARTPORT_WHATSAPP_VERIFY_TOKEN not set)"
fi

if { rg -n 'localhost:8080|http://localhost|href="#"|href=""' src/SmartPort.Web/Views src/SmartPort.Web/Controllers -g '*.cshtml' -g '*.cs'; rg -n "href='#|href=''" src/SmartPort.Web/Views src/SmartPort.Web/Controllers -g '*.cshtml' -g '*.cs'; } >/tmp/smartport-link-audit-static.out; then
  echo "FAIL static link audit found unsafe localhost/empty/hash hrefs:" >&2
  cat /tmp/smartport-link-audit-static.out >&2
  failures=$((failures+1))
else
  echo "PASS static link audit found no localhost/empty/hash hrefs in views/controllers"
fi

if (( failures > 0 )); then
  echo "link audit failed with $failures issue(s)" >&2
  exit 1
fi

echo "link audit passed"
