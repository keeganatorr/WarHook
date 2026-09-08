# WarHook — agent context

Notes for any agent working in this directory. Loaded automatically at
the start of every session.

## What this is

WarHook is a cross-platform port/extension of an old Windows-only MonoGame 3.0
Pyoro clone (`MonogameTest/` legacy source; game code in `PyoroGL/Game1.cs` and
`PyoroGL/GameB.cs`). It targets .NET 8 with MonoGame 3.8 DesktopGL, runs
natively on Linux and under Wine on Windows builds. Legacy artifacts survive
in the namespace `MonogameTest` and assembly name `WarHook` — do not rename.

## Build, test, run

```bash
cd PyoroGL && dotnet run        # dev run (native Linux)
./build.sh win-x64              # release publish -> dist/win-x64/ (whole folder is the artifact)
./build.sh                      # current OS (linux-x64 / osx-x64)
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
- `dist/<rid>/` — publish output; ship the entire folder, never the bare exe
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
- Online leaderboards are plain Supabase REST calls (no SDK, shared
  `PyoroGL/OnlineScores.cs`). Config: `PyoroGL/supabase.json` is git-ignored
  and embedded into both binaries at compile time (desktop csproj + web csproj
  EmbeddedResource) so no plaintext key file ships; created by
  `setup-leaderboards.sh`, which also applies `supabase/schema.sql` via the
  Management API and refuses service_role keys. Missing config = feature
  disabled. The anon key is public by design; the RPC validates submissions.
