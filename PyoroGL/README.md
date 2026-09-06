# PyoroGL

A faithful port of the original **Pyoro** (a Game Boy-style "Yoshi"-inspired
tongue game) from the old Windows-only MonoGame 3.0 project (`MonogameTest/`)
to **MonoGame 3.8 DesktopGL**, so it runs natively on Linux with the .NET SDK.

## Why a new project?

The original `MonogameTest/` project targets `.NET Framework 4.5` + MonoGame 3.0
for **Windows/SharpDX** (WinForms + DirectX). That backend requires Windows
(`shell32.dll`, `SharpDX`), so even under `mono` it crashes on Linux. The game
code itself (`Game1.cs`) is pure XNA/MonoGame API and needs **no changes** to
run cross-platform, so this project simply reuses the exact same source. The
sprite/font content is already compiled to `.xnb` (format 5, compatible with
MonoGame 3.8), so no content pipeline build is required.

## Layout

```
PyoroGL/
  PyoroGL.csproj      # net8.0, MonoGame.Framework.DesktopGL 3.8.4.1
  Game1.cs            # copied verbatim from MonogameTest/Game1.cs
  Program.cs          # copied verbatim from MonogameTest/Program.cs
  Content/*.xnb       # precompiled content copied from the old build output
```

## Run

```bash
cd PyoroGL
dotnet build
dotnet run
```

Native OpenGL/audio libraries (`runtimes/linux-x64/native/*.so`) are resolved
automatically by .NET at runtime.

## Controls

- **Left / Right arrows** — move Pyoro
- **X** — Game A: hold/release the tractor beam; Game B: one instant shot per press
- **Up / Down** (or **W / S**) — select a menu item
- **Enter / Space** — confirm a menu selection
- **Esc** — pause / resume gameplay, or return from Options
- **Left / Right** in Options — toggle fullscreen
- **F2** — toggle mouse floor editing (off by default)
- **Mouse click**, with editing enabled — left removes blocks, right restores; hover highlights the block
- **[ / ]** — decrease / increase tractor-beam width (1–20 native pixels)

The tractor beam and claw are generated in code. Set `Game1.BeamWidth` to change
the visual thickness (default: 6); this setting scales the glow and claw too.

The game opens on the main menu. Start blinks the selection brackets twice, then
fades through black into a fresh round. The pause menu offers Resume, Restart,
Options, Main Menu, and Exit. Gameplay and queued effects stay frozen while paused.

Select **Game B** on the mode picker to use the yellow tank. Each press of X
destroys every mortar intersecting the invisible 45-degree line from the barrel
in the direction the tank faces. Each destroyed mortar explodes and scores
50 points for a one-hit shot, 100 each for two, 300 each for three, or 1000 each
for four or more. Shots only destroy mortars on the line.
