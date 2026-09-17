# WarHook: UFO Abduction

A UFO variant of WarHook, built with .NET 8 and MonoGame DesktopGL.
The namespace `MonogameTest` and executable name `WarHook` are retained.

## Play

```bash
cd PyoroGL
dotnet run
```

Fly a compact 44×20 UFO around a 288×216 playfield.
Left/right movement banks the ship with smoothly eased tilt; Up/Down lets
you descend from the upper sky to rooftop height. A layered city skyline
with lit windows and rooftop tanks makes altitude easier to judge. Buildings
are background scenery; the UFO flies in front of them.

Hold **X** or **Space** to shoot small bullets straight down, initially every 0.8 seconds.
One bullet instantly defeats a soldier or engineer, or destroys an incoming
missile, for 50 points. A shot stops at the first target. The held tractor
payload is protected from your bullets.

Hold **Z** or **Shift** to project a narrow tractor cone. It initially lifts one person
at a time, slowly; capacity upgrades let it carry more. You can move and fire while using it. Captured people slowly
drift toward the cone's horizontal centre as they rise. The UFO moves faster
than this pull, so moving outside the cone's range or releasing Z still drops
the payload. People fall back to the ground and resume their route; they can be caught again in midair.
Rockets ignore the tractor beam: dodge them or shoot them down.
Base suction lifts at 30 pixels/second and pulls toward the centre at 15 pixels/second.

Each original bean spawn sends a runner in from the nearer side of the screen.
Soldiers reach that bean's assigned X, launch one missile toward the UFO's
position at that moment, then keep running in the same direction. Rockets
follow a fixed heading, so you can dodge them. Engineers never fire.
Abducting a soldier before it arrives prevents its shot.

The UFO starts with 100 hull health. A missile hit removes 25, with a brief
one-second grace period between hits. Losing all hull health ends the run.
Engineers wear yellow hardhats and orange overalls; delivering one restores
25 hull health (up to 100) and earns 250 points.

Delivering a soldier earns 100 score points and one unit of **CREW** currency.
The survival timer starts at zero and the reward multiplier at **1.00×**,
growing by **0.10× per minute** before upgrades. Pausing freezes the timer.
When the hull is destroyed, or you choose **End Round** from pause, your
collected soldiers × the current multiplier are banked (to two decimal places).
Engineers and shooting enemies do not earn currency.

The end-of-round **Upgrade Map** has 20 connected nodes across four branches.
Start with faster firing, faster abduction, faster movement, or stronger hull.
Buying ranks unlocks deeper nodes for up to five people in one beam, a wider
cone, better engineer repairs, faster multiplier growth, and starting bonuses.
Costs increase with each rank. Purchases apply on the next flight and persist
along with unspent currency between rounds and browser sessions.

Use arrows/WASD or the controller D-pad to select a node, then Enter/X or A
to buy. Mouse clicks select nodes; wheel scrolling or dragging pans the map.
Press **R** (controller Start) or click **Next Flight** to launch; Escape
returns to the main menu. You can also open the map from the main menu.
Web progress is saved in `warhook.ufo.progression.v1` in browser localStorage;
desktop progress uses `Warhook/ufo-progression.json` in your user data folder.
Friendly bullets do not occupy enemy spawn slots.

Both **Abduct** and **Siege** use the original bean spawn arithmetic, random
positions, and 16-slot limit, with separate spawn and missile difficulty: missiles start at 1.5× original
speed while spawn difficulty starts at 6×. Spawn events start every 0.25–0.33 seconds,
subject to the 16-slot pool limit.
The original white/special bean events produce engineers. There are no extra
opening enemies, replacement engineers, or repeat-fire timers. Each runner
has a 3-second run-in, preserving the spacing between scheduled shots;
engineers and soldiers abducted before firing do not produce a rocket.
After reaching their target, they walk offscreen at 24 native pixels per second.

Spawn intervals tighten at the original score thresholds (1,000, 3,000,
5,000, 8,000, 10,000). Rocket speed and spawn frequency use the original speed ramp, but each person
delivered (soldier or engineer) adds 10% to its clock rate for that round:
5 abductions = 1.5×, 10 = 2×. The survival/reward timer still counts real play
time. Pausing freezes both clocks; a new round resets the difficulty bonus.
Approaching soldiers reserve a spawn slot and transfer it to their rocket;
engineers reserve theirs until abducted or offscreen. Soldiers running away
after firing do not consume an additional slot.

## Controls

- Arrow keys or WASD: fly horizontally and descend to rooftop height.
- X or Space: hold to fire bullets.
- Z or Shift: hold the tractor cone; release to drop the payload.
- Escape: pause/resume. The pause menu includes End Round and options.
- 1–5: select gameplay music.
- Up/Down and Enter/X: navigate menus.
- Tab (desktop): save a screenshot beside the executable.
- Controller: D-pad/left stick to move, A for bullets/confirm, Y for tractor, Start to pause.

In the browser, letter controls use physical QWERTY key positions so they
remain consistent on AZERTY, QWERTZ, Dvorak and non-Latin layouts. **Space**
(fire), **Shift** (tractor) and the arrow keys are layout-independent alternatives.
Score initials still follow the letters typed with your keyboard layout.

Local scores are separate for Abduct and Siege and save automatically with
your current initials when the round ends. The upgrade map opens immediately.
Desktop saves use `Warhook/ufo-highscores.json` under the OS local application
data directory. Web saves use `warhook.ufo.highscores` in localStorage.
The original WarHook saves are preserved. This variant does not connect to
the original game's online leaderboard because its scoring rules differ.

## Build and verify

```bash
dotnet build PyoroGL/PyoroGL.csproj
dotnet build WarHookWeb/WarHookWeb.csproj
node --test tests/web-keyboard-controls.test.cjs
./build.sh linux-x64
./build.sh win-x64
./build-web.sh
./serve-web.sh
```

The desktop content build requires the global `dotnet-mgcb` tool version
3.8.4.1. Web uses KNI BlazorGL and shares `UfoGame.cs` with desktop.

For a running-game smoke check, use `dotnet run --project PyoroGL -- --shots`.
This checks movement and eased tilt, manual firing, instant soldier kills and missile destruction,
shootable engineers, cone capture, gradual horizontal centring, dropping,
landing and recapture, repairs, permanent currency, survival rewards, upgrade prerequisites/purchases, persistence, multi-person beams, aimed missiles, rocket suction immunity, pause, game over and reset. It also replays 4,200 ticks against
the original bean schedule and checks runner arrivals, single shots,
engineer routes, the 16-slot pool, and exits.
It saves title, gameplay, upgrade-map PNGs into the executable's
`screenshots/` directory and exits. On Linux, set
`XDG_DATA_HOME` to a temporary directory to isolate smoke-run scores.

`UfoGame.cs` owns the UFO simulation, drawing, and sprite-sheet regions.
`Game1.cs` hosts the shared rendering, audio, and effects; some original game
helpers remain for reference. `GameMenus.cs` and `ScoreMenus.cs` provide menus.
The generated atlas is `Assets/ufo-atlas.png`; generation details are recorded
in `../ufo-sprites-prompts.md`. Both project files declare the asset for publish.
