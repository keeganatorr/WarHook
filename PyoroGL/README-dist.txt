WarHook

Windows: run WarHook.exe.
Linux via Wine: wine WarHook.exe
On first launch under Wine, WarHook enables its own GLX compatibility setting
and restarts automatically. Other Wine applications are unaffected.
Native Linux build: ./WarHook

The build includes the .NET runtime; no separate .NET installation is needed.
Keep Assets/ and Content/ beside the executable.
The Windows build also needs its included SDL2.dll and openal.dll beside
WarHook.exe. Distribute this entire folder, not just the executable.

Controls
  Left/Right       move
  X                Game A tractor beam / Game B instant shot
  1-5              change gameplay music
  Escape           pause
  Enter/X          select menu items

After game over, qualifying scores can be saved with three initials.
Type letters or use the arrow keys, then Enter to save.
R opens the retry picker; Escape returns to the main menu after entry.
The score table overlays the live game. Scores are stored in your user data
folder, separately for Game A and Game B.
