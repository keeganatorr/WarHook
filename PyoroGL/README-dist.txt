WarHook: UFO Abduction

Windows: run WarHook.exe.
Linux via Wine: wine WarHook.exe
Native Linux: ./WarHook

The build includes the .NET runtime. Keep Assets/ and Content/ beside the
executable, plus SDL2.dll and openal.dll in the Windows build.
Wine automatically gets a per-executable GLX compatibility setting and a
one-time restart on first launch.

Fly the compact UFO around a 288x216 screen. The ship banks smoothly as you
move and can descend to rooftop height. The city skyline shows your altitude;
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
repairs 25 health and scores 250. Soldiers score 100 and add one CREW currency.
The survival timer starts at zero. The reward multiplier starts at 1.00x and
increases by 0.10x per minute; both freeze while paused. Death or End Round
from the pause menu banks soldiers times multiplier, retaining hundredths.
Engineers and kills do not earn currency.

Spend currency on the permanent 20-node upgrade map between rounds.
Start with firing speed, abduction speed, movement speed, and hull health.
Deeper nodes unlock multi-person beams (up to five), beam width, repairs,
faster multiplier growth, and higher starting multipliers. Costs grow by rank.
Arrows/WASD select, Enter/X buys, R starts the next flight. Mouse clicks select
nodes/buttons; wheel or dragging scrolls. Controller: D-pad select, A buy,
Start launches. Purchases apply next flight and persist between sessions.
Abduct and Siege retain the original bean spawn timing and difficulty ramp.

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

After each round, rewards are banked and the upgrade map opens. Scores are
saved locally with your current initials, separately for Abduct and Siege.
Progress saves as Warhook/ufo-progression.json in your user data folder
(web: browser localStorage key warhook.ufo.progression.v1).
