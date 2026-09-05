#!/usr/bin/env bash
# Build PyoroGL as a single self-contained executable.
#
# Usage:
#   ./build.sh              # build for the current OS (linux-x64 or osx-x64)
#   ./build.sh win-x64      # cross-compile for Windows (requires the target RID's
#                           # runtime pack, which NuGet fetches automatically)
#   ./build.sh linux-x64
#
# Output: dist/<rid>/MonogameTest[.exe] + the Assets/ folder it loads at runtime.
set -euo pipefail

cd "$(dirname "$0")"

RID="${1:-}"
if [ -z "$RID" ]; then
    case "$(uname -s)" in
        Linux*)  RID="linux-x64" ;;
        Darwin*) RID="osx-x64" ;;
        *) echo "Unsupported OS; pass a RID (e.g. ./build.sh win-x64)"; exit 1 ;;
    esac
fi

echo "==> Building $RID (single file, self-contained)"
dotnet publish PyoroGL/PyoroGL.csproj \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "dist/$RID"

# The single-file bundle can't carry the loose Assets/ folder, so copy it next
# to the executable (the game loads PNGs from <exe dir>/Assets at runtime).
echo "==> Copying runtime assets"
rm -rf "dist/$RID/Assets"
cp -r PyoroGL/Assets "dist/$RID/Assets"

# Keep only what the game actually loads at runtime.
pushd "dist/$RID/Assets" > /dev/null
keep="framewide.png newbackdrop.png score.png highscore.png font8x8_atlas.png block_atlas.png tank-pixelart.png mortars-pixelart-cleaned.png explosion-new.png parachute.png mainmenu.png title.png"
for f in *.png *.psd; do
    [ -e "$f" ] || continue
    skip=0
    for k in $keep; do [ "$f" = "$k" ] && skip=1; done
    [ $skip -eq 0 ] && rm -f "$f"
done
rm -f *.psd 2>/dev/null || true
popd > /dev/null

echo "==> Done: dist/$RID/"
ls -la "dist/$RID/"
