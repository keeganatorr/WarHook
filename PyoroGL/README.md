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
- **1–5** (number row or numpad) — switch gameplay music tracks
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

Game A and Game B keep separate persistent high scores. New records save
automatically in the background and pending writes finish when the game exits.
Saves use the OS local application-data folder: typically
`~/.local/share/Warhook/highscores.json` on Linux (or under `XDG_DATA_HOME`
when set), and `%LOCALAPPDATA%/Warhook/highscores.json` on Windows.
Each mode starts with the existing 10,000-point high-score target.

The main menu's **Scores: Game A** and **Scores: Game B** options open separate
top-ten leaderboards. Left/Right switches tables; Escape returns to the menu.
If online leaderboards are configured, Up/Down switches between the local
(save-file) and global (online) tables.

## Online leaderboards (Supabase)

Global top-ten tables are powered by a Supabase project. The feature is
disabled until configured; the game then falls back to local scores when the
connection fails.

1. Create a project at supabase.com.
2. Create a personal access token at
   https://supabase.com/dashboard/account/tokens.
3. Run `./setup-leaderboards.sh` from the repository root. It prompts for the
   project URL, the anon/publishable key, and the access token; applies
   `supabase/schema.sql` through the Supabase Management API; verifies the
   deployment; and writes `PyoroGL/supabase.json`. That file is git-ignored
   and embedded into both binaries at compile time, so no plaintext key file
   ships in the itch.io ZIP or the desktop folder. Add `--build` to rebuild
   the web and win-x64 artifacts immediately.

The server-side schema (already applied by the script) keeps writes inside a
`submit_score` RPC that validates mode, initials, and score range, and
rate-limits one submission per client IP every 5 seconds. The anon key is
public by design (like a Firebase web key): it grants no admin access, and a
determined user can extract it from a shipped build — expect that, and rely
on the server-side validation and rate limiting against abuse. The
service_role key, database password, and access token never touch the repo or
any release. The setup script refuses a service_role key if you paste one by
mistake.

Qualifying game-over scores are submitted automatically when initials are
saved.

After game over, the leaderboard scrolls up. Qualifying scores can be saved with
three initials: type letters, or use Left/Right to select a character and Up/Down
to change it, then press Enter to save. Escape skips entry. Afterward, R opens
the retry picker and Escape returns to the main menu. Controller D-pad and A/B
also work. Old personal-best saves migrate as `OLD` entries.


## Windows and Wine builds

From the repository root, run `./build.sh win-x64` on Linux or `build.bat` on
Windows. Both publish to `dist/win-x64/`. Building requires the .NET 8 SDK and
the MGCB tool used by the project (`dotnet tool install -g dotnet-mgcb --version 3.8.4.1`).

Run `WarHook.exe` on Windows or `wine dist/win-x64/WarHook.exe` on Linux.
Ship the whole output folder: the self-contained executable, `SDL2.dll`,
`openal.dll`, `Assets/`, and `Content/`. MonoGame loads the native DLLs by
filename, so the Windows publish keeps them beside the executable instead of
embedding them in the single-file bundle. No Windows .NET installation is needed.

Under Wine, WarHook sets `UseEGL=N` in its own
`HKCU/Software/Wine/AppDefaults/WarHook.exe/X11 Driver` key and restarts once
if needed. This uses Wine's GLX path to avoid EGL context-creation failures on
NVIDIA. It does not modify the prefix-wide graphics settings or native Windows.


## Web build

The `WarHookWeb/` Blazor WebAssembly host uses the KNI BlazorGL/WebGL fork
(`nkast.*` 4.3.9001) while sharing the game sources with `PyoroGL/`. Run
`./build-web.sh` from the repository root. It publishes `dist/web/` and creates
`dist/WarHook-web.zip`, whose root contains `index.html`; upload that ZIP to
itch.io as an HTML Game. For local testing, run `./serve-web.sh` and open the
printed `http://127.0.0.1:8000/` URL; do not open `index.html` as `file://`.
Scores use browser localStorage, and music starts after the first user gesture
as required by browser autoplay policy.
