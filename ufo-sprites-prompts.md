# UFO sprites

Generated with the built-in image generation tool. Final asset:
`PyoroGL/Assets/ufo-atlas.png` (shared by desktop and web).
The transparent 4x4 atlas is sampled directly; no external image editing was used.
The generator returned 1254x1254 pixels, so the runtime derives cell boundaries
from the actual size and trims transparent margins inside each cell.

## Final prompt

Use case: stylized-concept
Asset type: production pixel-art sprite atlas for a tiny 288x162 arcade UFO abduction game.
Create one transparent PNG sprite sheet, exactly 1024x1024, a 4 by 4 grid of 256x256 cells. Genuine transparent background. Every sprite stays within its cell with 16 pixels padding. Crisp chunky pixel art, limited navy/steel/cyan palette, no text, no labels, no shadows outside silhouettes. Row 1: four identical wide silver UFO motherships in side view, cyan cockpit dome, horizontal layered saucer hull, glowing cyan underside; each whole ship fits its cell. Row 2: four walking animation poses of the same tiny olive uniform soldier with helmet and handheld upward rocket launcher, facing right, full body. Row 3: four walking animation poses of the same tiny engineer with bright yellow hardhat and orange overalls carrying a wrench, facing right, full body. Row 4: cell 1 a small silver cyan tractor beam turret mounted from a ceiling, barrel pointing down-right at exactly 45 degrees; cell 2 an upward-pointing red-tipped missile with orange exhaust; cell 3 a square intact metal hull rail tile with cyan light; cell 4 a square damaged metal hull tile with jagged broken center and orange sparks. Flat side view suitable for a platform game, strong silhouettes readable at 12 to 20 pixels tall. All four walking sprites have matching baseline and scale. No scenery, no checkerboard background.
