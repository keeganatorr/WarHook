#!/usr/bin/env bash
# Serve the published web build for local browser testing.
set -euo pipefail

cd "$(dirname "$0")"
PORT="${1:-8000}"

if [ ! -f dist/web/wwwroot/index.html ]; then
    echo "No published web build found; run ./build-web.sh first."
    exit 1
fi

echo "Open http://127.0.0.1:$PORT/ in Chromium (do not use file://)."
python3 -m http.server "$PORT" --directory dist/web/wwwroot
