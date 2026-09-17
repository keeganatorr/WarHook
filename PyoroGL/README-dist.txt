WarHook: UFO Abduction

Windows: run WarHook.exe.
Linux via Wine: wine WarHook.exe
Native Linux: ./WarHook

The build includes the .NET runtime. Keep Assets/ and Content/ beside the
executable, plus SDL2.dll and openal.dll in the Windows build.
Wine automatically gets a per-executable GLX compatibility setting and a
one-time restart on first launch.

New Game starts fresh in one of three save slots. Continue lets you choose a
save's launch music before opening its upgrade map. Each slot keeps separate
currency, upgrades and launch music. Replacing an occupied slot requires confirmation. Existing progress
is automatically copied to Save 1, keeping the old save as a backup.
Continue resumes progression between rounds, not an unfinished flight;
use Pause > End Round to bank rewards before leaving.

Fly the compact UFO around a 288x216 screen. The ship banks smoothly as you
move and can descend to low rooftop height. The city skyline shows your altitude;
buildings are background scenery. Soldiers run to their original bean spawn position, fire one
missile aimed at your UFO's position, then keep running offscreen. Dodge the
missiles: they follow a fixed heading. Engineers run without firing.

Hold X or Space to fire bullets downward, initially every 0.8 seconds. One hit defeats
a soldier or engineer, or destroys an incoming missile, for 50 points.
The bullet is consumed. Held tractor payloads are protected.
Hold Z or Shift for a narrow tractor cone that slowly lifts one person.
You can move and shoot while lifting. Captured people slowly drift
toward the beam centre as they rise. Moving too far away or releasing Z drops
the payload. Dropped people
fall and resume running, and can be caught again. Rockets ignore the tractor
beam: dodge them or shoot them down.

The UFO starts with 100 hull health; missile hits cost 25. An abducted engineer
repairs 5 hull at rank 1. Engineer Tools rank 1 unlocks engineers and their 5-health repair;
each further rank adds 5 repair, up to 25 at rank 5. Hull Plating rank 2 is
required to buy the unlock. Before unlocking, engineer spawns are ordinary
soldiers. Soldiers add one CREW currency at base value; the Soldier Value
upgrade raises that value by one per rank.
The survival timer starts at zero. The reward multiplier starts at 1.00x and
increases by 0.50x per minute; both freeze while paused. Death or End Round
from the pause menu banks soldiers times their value times multiplier, retaining hundredths.
Engineers and kills do not earn currency.

Spend currency on the permanent 34-node tech web between rounds. Routes fork
and merge across weapons, beams, flight/hull and yield. Unlock twin/triple
guns, missile interception blasts, five-person beams, auto-repair and
compounding reward growth. Mothership Link needs any three branch capstones.
Purchases apply next flight. Old save IDs are retained; removed excess Array
and Warp ranks are refunded at their original prices.
Arrows/WASD choose nearby nodes; Enter/X buys. Drag pans; wheel or the zoom
button switches 0.5x/1x/2x views. Click the miniature overview to travel the
map. C/Home returns to the core. R launches; Escape returns to the menu.
Controller: D-pad selects, A buys, right shoulder zooms, left-stick click
centres the core, Start launches.
UFO rounds start with 1.5x original missile speed and 6x spawn difficulty:
spawn events every 0.25–0.33 seconds, subject to the 16-slot pool.
Each abducted person, including engineers, makes the difficulty clock advance
25% faster for that round (10 people = 3.5x). Survival rewards still use actual
play time. Difficulty bonuses reset each round; pause freezes both clocks.

Altitude danger: flying below the dashed line warns at both side launch
points, then fires six alternating rockets after 0.85 seconds, 0.18 seconds
apart. Staying low repeats the volley after a 3.5-second cooldown. Rockets
aim when fired and can be shot down. Flight Clearance (after Thrusters 1)
lowers the line by 14 pixels per rank, up to five ranks.

Controls
  Arrows, WASD     fly horizontally and descend to rooftop height
  X, Space        hold to fire bullets
  Z, Shift        hold tractor cone; release to drop
  1-5             change gameplay music
  Escape          pause
  Enter/X         select menu items
  Controller      D-pad/stick move, A fire/confirm, Y tractor, Start pause

Web letter controls use physical QWERTY positions on all keyboard layouts.
Use Space to fire and Shift for the tractor if the letter labels differ.
Arrow keys work for movement on every layout. Score initials use typed letters.

After each round, rewards are banked immediately. The results screen counts
up crew, multiplier, and total reward one line at a time. Once it finishes,
press Enter/X (controller A) or click the button to open upgrades. The UFO
variant has no score table.
The tally overlays the gameplay scene while the destroyed UFO crash-lands
with smoke and fire.
Slots save as Warhook/ufo-save-1.json through ufo-save-3.json in your user
data folder (web: localStorage keys warhook.ufo.save.1.v1 through .3.v1).
