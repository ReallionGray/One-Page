#!/usr/bin/env bash
set -euo pipefail

# Development runner for OnePage API + UI
# Usage: ./scripts/run-dev.sh

ROOT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
cd "$ROOT_DIR"

API_URL=http://localhost:5001
UI_URL=http://localhost:5002

mkdir -p ./logs

echo "Starting OnePage API at $API_URL..."
(dotnet run --project src/OnePage.Api --urls $API_URL --no-build > ./logs/api.log 2>&1) &
API_PID=$!

# wait for API port
echo -n "Waiting for API to listen on $API_URL"
for i in {1..60}; do
  if lsof -iTCP -sTCP:LISTEN -P -n | grep -q ':5001'; then
    echo " -> up"
    break
  fi
  echo -n "."
  sleep 0.5
done

if ! lsof -iTCP -sTCP:LISTEN -P -n | grep -q ':5001'; then
  echo "API failed to start; see ./logs/api.log"
  exit 1
fi

echo "Starting OnePage UI at $UI_URL..."
(dotnet run --project src/OnePage.Ui --urls $UI_URL --no-build > ./logs/ui.log 2>&1) &
UI_PID=$!

# wait for UI port
echo -n "Waiting for UI to listen on $UI_URL"
for i in {1..60}; do
  if lsof -iTCP -sTCP:LISTEN -P -n | grep -q ':5002'; then
    echo " -> up"
    break
  fi
  echo -n "."
  sleep 0.5
done

if ! lsof -iTCP -sTCP:LISTEN -P -n | grep -q ':5002'; then
  echo "UI failed to start; see ./logs/ui.log"
  echo "API logs: ./logs/api.log"
  exit 1
fi

# Try to open the UI in the default browser (macOS/linux)
if command -v open >/dev/null 2>&1; then
  open "$UI_URL" || true
elif command -v xdg-open >/dev/null 2>&1; then
  xdg-open "$UI_URL" || true
fi

echo "Started: API PID=$API_PID, UI PID=$UI_PID"
echo "Logs: ./logs/api.log ./logs/ui.log"
echo "To stop: kill $API_PID $UI_PID"

wait
