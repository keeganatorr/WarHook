#!/usr/bin/env bash
# Package PyoroGL for release: builds linux-x64 + win-x64 dists, strips debug
# files, adds a README, and zips each into dist/<name>-<rid>.zip.
#
# Usage:
#   ./release.sh            # uses default name and version
#   ./release.sh MyGame 1.0 # custom zip name prefix and version suffix
set -euo pipefail

cd "$(dirname "$0")"

NAME="${1:-WarHook}"
VER="${2:-1.0}"

# 1. Build both platform dists.
./build.sh linux-x64
./build.sh win-x64

# 2. Clean each dist: remove PDBs and stray screenshots.
for RID in linux-x64 win-x64; do
    rm -f "dist/$RID/WarHook.pdb"
    rm -rf "dist/$RID/screenshots"
done

# 3. Write a README into each dist.
for RID in linux-x64 win-x64; do
    cat > "dist/$RID/README.txt" <<EOF
$NAME $VER ($RID)

Run:
  linux-x64:  chmod +x WarHook && ./WarHook
  win-x64:    run WarHook.exe

Files:
  WarHook    the game (single-file, self-contained — no install needed)
  Assets/         runtime art + audio (menu/gameplay/gameover music, sprites)
  Content/        compiled spritefont data

Controls:
  Arrow keys / A-D   drive the tank
  X / Space          fire the tractor beam
  Esc                pause
  F1                 debug menu
  Tab                screenshot

  Music and sound volume are adjustable in OPTIONS (arrow keys to change).
  On game over: R opens the retry picker — choose Game A/B and music, then
  press Enter or X to restart.
EOF
done

# 4. Zip each dist as <name>-<rid>.zip (folder inside the zip).
rm -f "dist/$NAME-linux-x64.zip" "dist/$NAME-win-x64.zip"
(cd dist && zip -qr "$NAME-linux-x64.zip" linux-x64)
(cd dist && zip -qr "$NAME-win-x64.zip" win-x64)

echo "==> Release packaged:"
ls -la "dist/$NAME-linux-x64.zip" "dist/$NAME-win-x64.zip"
