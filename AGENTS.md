# WarHook — agent context

Notes for any agent working in this directory. Loaded automatically at
the start of every session.

## What this is

WarHook is a cross-platform port/extension of an old Windows-only MonoGame 3.0
Pyoro clone (`MonogameTest/` legacy source; game code in `PyoroGL/Game1.cs` and
`PyoroGL/GameB.cs`; UFO gameplay in `PyoroGL/UfoGame.cs`). It targets .NET 8 with MonoGame 3.8 DesktopGL, runs
natively on Linux and under Wine on Windows builds. Legacy artifacts survive
in the namespace `MonogameTest` and assembly name `WarHook` — do not rename.

## Build, test, run

```bash
cd PyoroGL && dotnet run        # dev run (native Linux)
./build.sh win-x64              # release publish + dist/WarHook-win-x64.zip
./build.sh linux-x64            # release publish + dist/WarHook-linux-x64.zip
./build.sh                      # current OS (linux-x64 or osx-x64)
./build-web.sh                   # itch.io HTML5 ZIP -> dist/WarHook-web.zip
./serve-web.sh                   # local HTTP test server (not file://)
```

Requires .NET 8 SDK and the global MGCB tool: `dotnet tool install -g dotnet-mgcb --version 3.8.4.1`.
There is no test suite; verify by running the game.

## Layout

- `PyoroGL/` — the game (csproj is the single source of truth for assets/publish)
- `WarHookWeb/` — KNI BlazorGL web host; shares the game source files
- `PyoroGL/Content/` — desktop MGCB content; a pre-Build target runs `mgcb` from inside that directory
- `PyoroGL/Assets/` — raw PNGs/OGGs loaded at runtime via `Texture2D.FromStream`
- `dist/<rid>/` — publish output; the matching `dist/WarHook-<rid>.zip` is the
  ready-to-distribute archive (it contains the entire folder, never just the exe)
- `PyoroGL/README-dist.txt` — ships as `README.txt` in the publish output

## Conventions

- Don't add copy/prune steps to `build.sh`/`build.bat`; declare assets and
  native-library layout in `PyoroGL/PyoroGL.csproj` instead.
- New runtime asset files must be added to the `<None Update="Assets/...">`
  list in the csproj or they won't be copied/published.

## Working notes

Keep this section current. When you learn something durable about this
project — a build step, a non-obvious gotcha, where something lives, a
decision and the reason for it — append a short bullet here, and delete
entries that have stopped being true.

Record only what would still be useful to someone starting fresh next
week. Not task chatter, not a changelog, not anything already obvious
from reading the code or `git log`.

- Native libraries (`SDL2.dll`, `openal.dll`) are NOT embedded in the
  Windows single-file bundle (`IncludeNativeLibrariesForSelfExtract=false`
  for `win-*` RIDs only) because MonoGame resolves SDL/OpenAL by filename;
  they must sit beside `WarHook.exe`. Linux builds keep them extracted.
- Wine gotcha: Wine 11's default X11 EGL backend fails to create the GL
  context on NVIDIA. `PyoroGL/WineCompatibility.cs` (called from `Main`)
  detects Wine, sets `UseEGL=N` in `HKCU/Software/Wine/AppDefaults/WarHook.exe/X11 Driver`
  (per-executable, prefix graphics untouched), and restarts the process once.
  The restart exists because the Wine driver may already be loaded before
  managed `Main` runs.
- Windows cross-build from Linux: `./build.sh win-x64` works; NuGet fetches
  the runtime pack automatically. `build.bat` is the equivalent on Windows.
  Both produce identical layouts thanks to the csproj-declared assets.
- Web build: `WarHookWeb` uses KNI 4.3.9001 BlazorGL/WebGL; upload
  `dist/WarHook-web.zip` with its `index.html` at the ZIP root. Web scores use
  localStorage, and web loose assets are fetched through `TitleContainer`.
- KNI WebGL Reach rejects oversized textures and compressed non-power-of-two
  textures. Web-only resized copies of the 2172px title/explosion sheets and
  uncompressed KNI-compatible SpriteFont XNBs live under `WarHookWeb/wwwroot`;
  do not substitute the desktop compressed XNBs.
- Original-game online leaderboards (unused by the UFO variant) are plain Supabase REST calls (no SDK, shared
  `PyoroGL/OnlineScores.cs`). Config: `PyoroGL/supabase.json` is git-ignored
  and embedded into both binaries at compile time (desktop csproj + web csproj
  EmbeddedResource) so no plaintext key file ships; created by
  `setup-leaderboards.sh`, which also applies `supabase/schema.sql` via the
  Management API and refuses service_role keys. Missing config = feature
  disabled. The anon key is public by design; the RPC validates submissions.
- Leaderboard ownership uses the persistent `PlayerId` stored in the local
  high-score save (browser localStorage or desktop JSON); rerun
  `setup-leaderboards.sh` after applying the `player_id` schema change.
- UFO variant: `PyoroGL/UfoGame.cs` owns the moving UFO, tractor cone,
  ground people, missiles, and engineer repairs; it is linked by the web
  csproj. `--shots` exercises the mechanics in the running game and captures
  screenshots. Atlas regions are alpha-trimmed at load time from a 4x4 grid.
- UFO scores use `Warhook/ufo-highscores.json` / `warhook.ufo.highscores` and
  do not initialize the original online leaderboard. Abduct/Siege retain the
  save model's A/B slots, with distinct rules from the original game.
- UFO playfield is 288×216; the ship is 44×20 and its centre moves within
  Y=26..139 (GroundY - 58 at the low bound) using Up/Down, W/S or the controller. Both previous ship coordinates
  feed missile collision sweeps; firing and tractor origins follow altitude. X/Space (pad A) fires downward; Z/Shift (pad Y)
  holds the tractor cone. Captured people stay in the people list, keeping
  their reserved bean slot and slowly drifting toward the cone centre. Release
  or cone separation drops the person. Rockets are never affected by suction.
  People fall and resume their paused run-in from their shifted X. The two-layer
  skyline is background scenery for altitude reference, with no building collisions.
- UFO spawns reuse the original 60 Hz bean RNG, timer, score thresholds, and
  `speedloop`. Each event has a fixed 3-second run-in to its assigned X;
  ordinary beans fire once, special beans become non-firing engineers. The
  16-slot pool counts approaching soldiers, missiles, and engineers;
  departing soldiers do not occupy a second slot. Rockets aim at the ship's
  launch-time position and keep that heading, with the original speed ramp.
- Ship impacts and friendly bullets sweep relative movement.
  The cone narrows toward the UFO; horizontal pull is slower than ship movement,
  so moving too far away still drops people. Bullets sweep predicted missile
  motion before ship impacts and consume themselves on the first target.
- Soldier deliveries fill a crew target (3, then 5, 7, ... additional soldiers).
  Reaching it freezes simulation and opens three repeatable upgrade choices:
  25% base fire rate, 25% base tractor lift/pull, or 20% base flight speed.
  `GameMenus` consumes choice input before normal play/pause controls; confirm
  must be released after opening to prevent held X selecting automatically.
  Only the chosen stat improves. Rapid Fire preserves reload fraction.
  Base firing interval is 0.8 seconds; tractor lift/pull speeds are 30/15 px/s.
- Bullets instantly kill soldiers and engineers or destroy missiles for 50
  points; only the held payload is protected. Delivered engineers heal 25 of
  100 UFO health and do not advance the soldier target. Missiles deal 25 with
  a one-second hit grace period. Progress and upgrades reset per round.
- KNI WebAudio can throw `NullReferenceException` in `SourceNodeStop` when
  restarting a paused sound. `BeamAudio.Update` must leave menu voices 4/5
  unpaused while gameplay is suspended; upgrade navigation restarts voice 4.
  An uncaught error in `TickDotNet` stops the browser animation loop, appearing
  as a frozen upgrade panel. Verify this path in a browser with audio enabled.
- Web letter controls use `KeyboardEvent.code` via `wwwroot/keyboard-controls.js`
  and `GameKeyboard.cs`, replacing KNI's layout-dependent A/D/W/S/X/Z values.
  Keep high-score initials on the original typed-key path. Space fires and
  either Shift activates the tractor on desktop/web. Held gameplay controls
  persist until keyup (repeat is idempotent); blur/hidden-tab events clear them. Run `node --test tests/web-keyboard-controls.test.cjs`.
