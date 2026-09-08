#!/usr/bin/env bash
# Publish WarHook as an itch.io HTML5 game.
# Output: dist/WarHook-web.zip (index.html is at the ZIP root).
set -euo pipefail

cd "$(dirname "$0")"

rm -rf dist/web dist/WarHook-web.zip
dotnet publish WarHookWeb/WarHookWeb.csproj -c Release -o dist/web

# Blazor's deployable static site is the publish wwwroot directory. itch.io
# requires index.html at the root of the uploaded ZIP.
(cd dist/web/wwwroot && zip -qr "../../WarHook-web.zip" .)

echo "==> Done: dist/WarHook-web.zip"
unzip -l dist/WarHook-web.zip | sed -n '1,12p'
