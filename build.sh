#!/usr/bin/env bash
# Build PyoroGL as a single self-contained executable.
#
# Usage:
#   ./build.sh              # build for the current OS (linux-x64 or osx-x64)
#   ./build.sh win-x64      # cross-compile for Windows (requires the target RID's
#                           # runtime pack, which NuGet fetches automatically)
#   ./build.sh linux-x64
#
# Output: dist/<rid>/WarHook[.exe], runtime libraries, Assets/ and Content/.
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
    -p:PublishTrimmed=true \
    -p:TrimMode=partial \
    -p:EnableCompressionInSingleFile=true \
    -o "dist/$RID"

# The project declares the complete runtime asset list and native-library layout.
# Use publish's output directly so Windows and Linux ship the same required files.
echo "==> Done: dist/$RID/"
ls -la "dist/$RID/"
