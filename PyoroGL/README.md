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
- **X** — shoot & hold / retract the tongue
- **Up / Down** (or **W / S**) — select a menu item
- **Enter / Space** — confirm a menu selection
- **Esc** — pause / resume gameplay, or return from Options
- **Left / Right** in Options — adjust beam width or toggle fullscreen
- **Mouse click** — edit floor blocks (left removes, right restores); hover shows select
- **[ / ]** — decrease / increase tractor-beam width (1–20 native pixels)

The tractor beam and claw are generated in code. Set `Game1.BeamWidth` to change
the visual thickness (default: 6); this setting scales the glow and claw too.

The game opens on the main menu. Start blinks the selection brackets twice, then
fades through black into a fresh round. The pause menu offers Resume, Restart,
Options, Main Menu, and Exit. Gameplay and queued effects stay frozen while paused.
