# WarHook: UFO Abduction

A UFO variant of WarHook, built with .NET 8 and MonoGame DesktopGL.
The namespace `MonogameTest` and executable name `WarHook` are retained.

## Saves

Choose **New Game** to start fresh in one of three save slots, or **Continue**
to select an existing save and open its upgrade map. Each slot keeps its own
currency, upgrades, mode, and launch music. Continue resumes progression
between rounds; an unfinished flight is not restored. End the round from
pause to bank its rewards before leaving.

New Game prefers an empty slot. Selecting an occupied slot asks you to
confirm replacement, with Cancel selected by default. Other slots are kept.
Existing single-save progress is copied into Save 1 automatically, with the
old save retained as a backup. Saves are local to this browser/device.

## Play

```bash
cd PyoroGL
dotnet run
```

Fly a compact 44×20 UFO around a 288×216 playfield.
Left/right movement banks the ship with smoothly eased tilt; Up/Down lets
you descend from the upper sky to low rooftop height (the UFO centre can reach
Y=159). A layered city skyline
with lit windows and rooftop tanks makes altitude easier to judge. Buildings
are background scenery; the UFO flies in front of them.

A dashed **DANGER BELOW** line starts at Y=64, just below your starting
height. Descending below it triggers flashing launch markers on both screen
edges. After 0.85 seconds, six rockets fire alternately from those exact
positions, 0.18 seconds apart, aimed at your position when each fires.
An announced volley completes even if you retreat; staying low triggers a
new warning after a 3.5-second cooldown. Side rockets can be shot down and
do not consume the ground-enemy spawn pool. **Flight Clearance**, unlocked
after Thrusters rank 1, lowers the line by 14 pixels per rank (five ranks,
down to Y=134). Purchases take effect on your next flight.

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
Engineer Tools rank 1 unlocks engineers after Hull Plating rank 2. Until then,
their scheduled spawns are ordinary soldiers. Engineers wear yellow hardhats
and orange overalls; delivering one restores 5 hull at rank 1, plus 5 per
additional Engineer Tools rank (25 at rank 5),
capped at maximum hull. Existing Engineer Tools purchases retain the unlock.

Delivering a soldier adds one unit of **CREW** currency at the base value.
The Soldier Value upgrade raises that value to 2, 3, and so on per rank.
The survival timer starts at zero and the reward multiplier at **1.00×**,
growing by **0.50× per minute** before upgrades. Pausing freezes the timer.
When the hull is destroyed, or you choose **End Round** from pause, your
collected soldiers × their current value × the multiplier are banked (to two
decimal places).
Engineers and shooting enemies do not earn currency.
The results screen reveals crew collected, multiplier, and total reward in
sequence, counting each up with a small bounce. After the tally finishes,
press Enter/X (controller A) or click the button to open upgrades. Rewards
are already saved during the animation.
The tally overlays the gameplay scene while the destroyed UFO crash-lands
with smoke and fire.

The end-of-round **Tech Web** has 33 nodes, including the owned UFO Core.
Routes fork and reconnect through multiple prerequisites. Weapon nodes are
coral, beam nodes cyan, flight nodes blue, hull nodes green, and yield nodes
gold. Mothership Link is a purple hybrid requiring any three completed branch
capstones. Already purchased upgrades remain usable when loading older saves.

- Weapons: Rapid Fire (+20% per rank), Pulse Accelerator (+15%), twin guns,
  Point Defence (+3px interception radius and 8px missile blast per rank),
  Plasma Cycler (+10% fire rate and bullet speed), and a three-shot Particle Array.
- Beam: Tractor Drive (+20% lift/pull), wider aperture, capacity 2 through 5,
  stronger horizontal pull within 64px of the UFO, and a wider occupied cone.
- Ship: Thrusters/Ion Engines (+15% movement), hull plating, engineer tools,
  Warp Core (+25% acceleration/vertical speed), Nanohull (+50 hull), and
  Auto Repair (2 hull/second after five seconds without a hit).
- Yield: +20% multiplier growth per Survival/Compound rank; Launch/Colony
  starting bonuses; Long Haul boosts growth after two minutes; Interest adds
  +1% rewards per 100 unspent CREW per rank (capped at +10% per rank), fixed
  when the flight starts. Exponential Yield compounds growth at 5% per minute
  per rank. Mothership adds 25% fire rate, movement, lift/pull, hull and rewards.

Costs remain base cost × (rank + 1)². Purchases take effect next flight and
persist with unspent currency in the selected save slot. Old Particle Array
ranks beyond one and Warp ranks beyond three are refunded at their original
prices; their save IDs, along with all other existing upgrade IDs, are retained.

Arrows/WASD or the controller D-pad select the nearest node in that direction.
Click an icon to inspect it; drag to pan freely, and use the wheel or zoom
button for 0.5×/1×/2× views; 0.5× fits the whole web. The miniature overview
is clickable when zoomed in. C/Home or
controller left-stick click returns to the core; right shoulder cycles zoom.
Enter/X or controller A buys the selected rank. The panel shows current/next
effects, cost and unmet prerequisites; partially completed connections light up.
Press **R** (controller Start) or click **Next Flight** to launch; Escape
returns to the main menu. Use Continue to select another save's tech web.
Web slots use `warhook.ufo.save.1.v1` through `warhook.ufo.save.3.v1` in
browser localStorage; desktop slots use `Warhook/ufo-save-1.json` through
`ufo-save-3.json` in your user data folder.
Friendly bullets do not occupy enemy spawn slots.

Both **Abduct** and **Siege** use the original bean spawn arithmetic, random
positions, and 16-slot limit, with separate spawn and missile difficulty: missiles start at 1.5× original
speed while spawn difficulty starts at 6×. Spawn events start every 0.25–0.33 seconds,
subject to the 16-slot pool limit.
The original white/special bean events produce engineers once unlocked,
otherwise ordinary soldiers. There are no extra
opening enemies, replacement engineers, or repeat-fire timers. Each runner
has a 3-second run-in, preserving the spacing between scheduled shots;
engineers and soldiers abducted before firing do not produce a rocket.
After reaching their target, they walk offscreen at 24 native pixels per second.

Spawn intervals tighten at the original score thresholds (1,000, 3,000,
5,000, 8,000, 10,000). Rocket speed and spawn frequency use the original speed ramp, but each person
delivered (soldier or engineer) adds 25% to its clock rate for that round:
4 abductions = 2×, 10 = 3.5×. The survival/reward timer still counts real play
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
your current initials when the round ends. Continue from the results tally to the upgrade map.
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
landing and recapture, repairs, permanent currency, survival rewards, multi-parent prerequisites, capstones, persistence/migration, spatial navigation, new tech effects, multi-person beams, aimed missiles, rocket suction immunity, pause, game over and reset. It also replays 4,200 ticks against
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
