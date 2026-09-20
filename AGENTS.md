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
- Held/falling people use separate four-frame
  `Assets/ufo-abducted-soldier.png` and `Assets/ufo-abducted-engineer.png`
  sheets, alpha-trimmed per frame and animated independently from the walking atlas.
- UFO gameplay has no score or high-score submission; its persistent currency
  is the round-end crew reward. Siege mode is removed from the visible UFO
  menus; older mode values in saves migrate to the remaining UFO mode.
- UFO playfield is 384×216 (16:9); desktop and web presentation fit a 16:9 viewport,
  with point-sampled scaling. The ship is 44×20 and its centre moves within
  Y=34..159 (GroundY - 38 at the low bound) using Up/Down, W/S or the controller. Both previous ship coordinates
  feed missile collision sweeps; firing and tractor origins follow altitude. X/Space (pad A) fires downward; Z/Shift (pad Y)
  holds the tractor cone. Captured people stay in the people list, keeping
  their reserved bean slot and slowly drifting toward the cone centre. Release
  or cone separation drops the person. Rockets are never affected by suction.
  People fall and resume their paused run-in from their shifted X. The two-layer
  skyline spans the full 384px viewport as background scenery for altitude
  reference, with no building collisions.
- UFO spawns reuse the original 60 Hz bean RNG, spawn arithmetic, and
  `speedloop`; score is not shown or awarded in UFO play. ResetUfo starts bigspeed at 0x600 and caps
  max_time at 0x78 (0.25–0.33s opening spawn intervals, subject to the
  base 16-slot pool plus four slots per prestige). UFO spawn and missile speed use uncapped 64-bit counters;
  spawn intervals bottom at one 60 Hz tick. Missile velocity uses a separate
  missileDifficultySpeed, starting at 0x180 (1.5x original speed). Both speeds
  advance on the same smallspeed rollover. Original bean replay keeps its 0x7F0 cap.
  UpdateUfoDifficulty advances fractional 60 Hz difficulty ticks at
  1 + 0.25 * roundAbductions; both soldier and engineer deliveries count.
  It does not advance spawns, movement, or the separate survival/reward clock.
  The gameplay HUD's right-aligned DIFF level advances in 0x80 missile-speed
  bands from level 1 and displays one decimal place.
  The original replay test explicitly restores the old starting constants.
  Each event has a fixed 3-second run-in to its assigned X;
  ordinary beans fire once; special beans become non-firing engineers only
  after Engineer Tools rank 1 is owned, otherwise they become ordinary soldiers.
  Spawn timing, positions and RNG order are unchanged by the unlock. The
  16-slot base pool counts approaching soldiers, missiles, and engineers;
  departing soldiers do not occupy a second slot. Rockets aim at the ship's
  launch-time position and keep that heading, with the original speed ramp.
- Ship impacts and friendly bullets sweep relative movement.
  The cone narrows toward the UFO; horizontal pull is slower than ship movement,
  so moving too far away still drops people. Bullets sweep predicted missile
  motion before ship impacts and consume themselves on the first target.
- Incremental progression lives in `UfoProgression.cs`; the pannable 35-node
  tech web is `UfoUpgradeMap.cs`. Soldier deliveries earn currency at round end:
  floor(soldiers × soldier value × multiplier × 100) / 100. The timer counts
  active play only;
  base multiplier is 1 + seconds / 120. Death and pause End Round show the
  timed tally in UfoRoundResults.cs before the map. Bank immediately on round
  end; the tally only animates a snapshot. Require released confirm controls
  after the tally completes before continuing, then guard input again on the map.
  Permanent ranks apply in ResetUfo, including uncapped Fleet Abduction beam
  capacity, hull,
  repair, cone width, and multiplier growth/start bonuses. Held people remain
  in the spawn pool and drop independently; releasing the beam drops all.
  Tractor Drive keeps the stable `tractor` save ID; its ten ranks each add
  20% lift and pull speed, with costs following the normal quadratic curve.
  Pickup feedback uses white `+value` soldier popups and blue `+repair` popups
  for engineers; these do not affect the legacy score field.
  Shield Array is a five-rank hull-branch defense: its blue ring absorbs one
  rocket, then regenerates after two seconds; later ranks shorten that delay.
  Rocket Intercept (`auto-fire`, after Point Defence 1) auto-fires on the
  normal weapon cooldown, leading the closest rocket with the existing laser
  bullets; manual fire remains downward when no rocket is available.
  The gameplay HUD renders multiplier progress as a color-cycling 1x-wide bar
  with the numeric multiplier beside it; each integer band resets the fill.
- Options volume sliders use ten percentage steps mapped across -20..0 dB;
  both sound effects and music start at 50% and are converted to linear gains
  only at the audio API boundary.
- Progression uses explicit JSON and atomic desktop replacement at
  `Warhook/ufo-save-{1..3}.json`, or synchronous browser localStorage keys
  `warhook.ufo.save.{1..3}.v1`. UfoSaveMenu.cs handles New Game / Continue
  selection and defaults overwrite confirmation to Cancel. With no slot saves,
  LoadSaveSlots imports the legacy ufo-progression.json /
  warhook.ufo.progression.v1 into slot 1, retaining the original as backup.
  Continue restores currency, ranks, mode and music between rounds, not an
  in-flight snapshot. Clear roundId/state when switching saves to prevent
  cross-slot payout retries. Failed writes do not commit purchases/rewards;
  round IDs prevent duplicate payouts. `--shots` uses in-memory progression
  and runs checks in `UfoSmokeChecks.cs`, including temporary-file save/reload.
- Bullets instantly kill soldiers and engineers or destroy missiles for 50
  points; all held people are protected. Engineer Tools rank 1 unlocks engineers
  (the stable `repair` save ID); they repair 5 hull per owned rank (5..25),
  capped to upgraded hull health; only soldiers earn currency. Missile damage
  is 25 and rocket impacts do not grant invincibility. Base fire interval is .8 seconds;
  tractor lift/pull speeds are 30/15 px/s, with a .04s acquisition handoff
  between units when the beam has capacity.
- KNI WebAudio can throw `NullReferenceException` in `SourceNodeStop` when
  restarting a paused sound. `BeamAudio.Update` leaves menu voices 4/5
  unpaused and stops gameplay sounds on web pause (loops restart on resume).
  End Round from pause must never Stop an already-paused KNI voice.
  An uncaught error in `TickDotNet` stops the browser animation loop, appearing
  as a frozen upgrade panel. Verify this path in a browser with audio enabled.
- Web letter controls use `KeyboardEvent.code` via `wwwroot/keyboard-controls.js`
  and `GameKeyboard.cs`, replacing KNI's layout-dependent A/D/W/S/X/Z values.
  Keep high-score initials on the original typed-key path. Space fires and
  either Shift activates the tractor on desktop/web. Held gameplay controls
  persist until keyup (repeat is idempotent); blur/hidden-tab events clear them. Run `node --test tests/web-keyboard-controls.test.cjs`.

- Tech web catalog coordinates, branch colours, weighted bonuses and ID-based
  multi-prerequisites are in UfoProgression.cs; RequiredCount enables any-N
  prerequisites (Prestige needs three completed capstones). Prestige resets
  crew and ranks, retains its count, zooms out the playfield 6% cumulatively,
  and adds four active enemy/projectile slots per prestige. The old mothership
  rank migrates into the prestige count. Fleet Abduction ranks are uncapped,
  with quadratic costs saturating at the maximum supported price. Core is always
  owned. Old purchased nodes are grandfathered through prerequisite changes.
  Version-3 JSON persists prestige while preserving existing save filenames and
  browser keys; version-1 excess fire3/engine3 ranks are refunded before clamping.
- Tech effects are snapshotted in ResetUfo, including interest on launch-time
  balance. Auto repair integrates only time after five damage-free seconds;
  Point Defence expands rocket interception and blasts nearby missiles, while
  preserving sweep ordering against earlier ship impacts. Exponential growth
  is integrated analytically, with Long Haul applying after 120 active seconds.
  The reward timer and abduction-driven difficulty clock remain separate.
- UfoUpgradeMap uses world coordinates, spatial keyboard selection, free drag,
  discrete .5/1/2 zoom and a clickable overview. Upgrade icons come from the
  8×5, 64px-cell `Assets/upgrade-icons.png` atlas and use point sampling.
  The upgrade screen uses a dedicated 1152×648 render target and a full 576×324
  logical research canvas at 2×; gameplay uses a 384×216 16:9 target, fitted to
  the current window while preserving its aspect ratio.
  The upgrades nebula lives in `Content/upgrade-clouds.fx` and renders into a
  half-size target before being blended behind the starfield and map. Desktop
  builds compile it through `PyoroGL.csproj`; its BlazorGL-compatible XNB is
  checked in at `WarHookWeb/wwwroot/Content/upgrade-clouds.xnb`.
  Map mouse coordinates are promoted into the graph canvas. Keep map geometry
  aligned to that 2× graph transform so Font6 stays crisp.
  --shots checks graph reachability, multi-parent gates, any-three capstones,
  new effects and rank-refund migration in addition to the gameplay checks.
- UfoAltitudeDefense.cs owns the fixed Y=64 danger line and side volleys.
  Flight Clearance (`clearance` save ID, after Thrusters 1) adds 14px per rank,
  max 5; stats snapshot at ResetUfo. Crossing below with the ship centre fixes
  two warning/launch points, waits .85s, then fires six alternating rockets
  .18s apart at current ship positions (fixed heading, 90px/s). A committed
  volley finishes even after retreat; staying low retriggers after 3.5s.
  AltitudeDefense missiles share bullet/hull collisions but are excluded from
  ActiveBeanSpawnCount so the prestige-scaled ordinary spawns remain independent.
- UfoCrashLanding.cs owns the post-destruction visual state. DamageShip starts
  a downward, tilted landing whose slide, fall impulse, and tumble inherit the
  final rocket heading; RoundResults continues that short animation and
  DrawUfoGameplay renders the compact tally over the skyline, crashed UFO,
  animated fire, and rising smoke. RoundResults is deliberately a gameplay
  render path (not a title scene), while gameover still freezes combat state.
- UfoImpactEffects.cs applies a .30s camera shake, stronger directional
  knockback, and rotational tilt impulse on rocket impact. DamageShip receives
  the missile heading so normal and altitude-defense rockets push the ship in their travel direction;
  impact motion resets with each flight and does not alter the combat timer.
