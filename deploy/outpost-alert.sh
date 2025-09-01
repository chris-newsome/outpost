#!/usr/bin/env bash
set -euo pipefail

# Usage: outpost-alert.sh <unit_name> <status>
# Sends a simple JSON payload to a Slack webhook or generic webhook.

WEBHOOK_FILE="/etc/outpost-alert.env"
if [[ -f "$WEBHOOK_FILE" ]]; then
  # shellcheck disable=SC1090
  source "$WEBHOOK_FILE"
fi

UNIT=${1:-unknown}
STATUS=${2:-failed}
HOST=$(hostname)
TIME=$(date -Is)

PAYLOAD=$(cat <<EOF
{
  "text": "Outpost alert: unit=$UNIT status=$STATUS host=$HOST time=$TIME"
}
EOF
)

if [[ -n "${SLACK_WEBHOOK_URL:-}" ]]; then
  curl -fsS -X POST -H 'Content-type: application/json' --data "$PAYLOAD" "$SLACK_WEBHOOK_URL" || true
fi

if [[ -n "${GENERIC_WEBHOOK_URL:-}" ]]; then
  curl -fsS -X POST -H 'Content-type: application/json' --data "$PAYLOAD" "$GENERIC_WEBHOOK_URL" || true
fi

echo "[outpost-alert] sent for $UNIT -> $STATUS" >&2

