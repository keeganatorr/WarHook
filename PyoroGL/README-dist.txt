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

The UFO has 100 hull health; missile hits cost 25. An abducted engineer repairs
25 health and scores 250. Soldiers score 100 and fill the CREW target.
Rescue 3 soldiers to pause and pick an upgrade: Rapid Fire (25% more base fire
rate), Tractor Boost (25% more base lift/pull speed), or Thrusters (20% more
base flight speed). Pick with Up/Down and Enter/X; release held fire first.
The counter then resets and targets grow to 5, 7, 9 more soldiers, and so on.
Engineers and kills do not count toward the target. All upgrades reset on retry.
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

After game over, qualifying scores can be saved with three initials.
Type letters or use the arrow keys, then Enter to save.
R opens the retry picker; Escape returns to the main menu after entry.
Scores are local and separate for Abduct and Siege, stored in your user data
folder as Warhook/ufo-highscores.json (web: warhook.ufo.highscores).
