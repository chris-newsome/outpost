#!/usr/bin/env bash
set -euo pipefail

API_ENV="/etc/outpost-api-docker.env"
WEB_ENV="/etc/outpost-web-docker.env"

API_PORT=8080
WEB_PORT=8081

if [[ -f "$API_ENV" ]]; then
  # shellcheck disable=SC1090
  source "$API_ENV"
fi
if [[ -f "$WEB_ENV" ]]; then
  # shellcheck disable=SC1090
  source "$WEB_ENV"
fi

API_PORT=${API_PORT:-8080}
WEB_PORT=${WEB_PORT:-8081}

ok_api=true
ok_web=true

if ! curl -fsS --max-time 5 "http://127.0.0.1:${API_PORT}/health" >/dev/null; then
  ok_api=false
fi

if ! curl -fsS --max-time 5 "http://127.0.0.1:${WEB_PORT}/" >/dev/null; then
  ok_web=false
fi

if [[ "$ok_api" != true ]]; then
  echo "[outpost-healthcheck] API unhealthy on :${API_PORT}, restarting outpost-api-docker" >&2
  systemctl restart outpost-api-docker || true
fi

if [[ "$ok_web" != true ]]; then
  echo "[outpost-healthcheck] Web unhealthy on :${WEB_PORT}, restarting outpost-web-docker" >&2
  systemctl restart outpost-web-docker || true
fi

if [[ "$ok_api" == true && "$ok_web" == true ]]; then
  echo "[outpost-healthcheck] OK"
fi

