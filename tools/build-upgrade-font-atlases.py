#!/usr/bin/env python3
"""Rasterize the CC0 SG pixel fonts into compact, pixel-crisp game atlases."""

import json
import re
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
SOURCE_DIR = ROOT / "PyoroGL" / "Content" / "CC0PixelFonts"
OUTPUT_DIR = ROOT / "PyoroGL" / "Assets" / "upgrade-fonts"
FONT_SIZE = 12
CELL_WIDTH = 16
CELL_HEIGHT = 16
COLUMNS = 16
GLYPH_FIRST = 32
GLYPH_LAST = 126
GLYPH_COUNT = GLYPH_LAST - GLYPH_FIRST + 1


def display_name(path: Path) -> str:
    stem = path.stem
    if stem == "BlockyPixelB":
        return "Blocky Pixel Bold"
    if stem == "NanoSquareO":
        return "Nano Square Outline"
    return re.sub(r"(?<=[a-z])(?=[A-Z])", " ", stem)


def safe_name(path: Path) -> str:
    return re.sub(r"[^a-z0-9]+", "-", path.stem.lower()).strip("-")


def build_font(path: Path) -> dict:
    font = ImageFont.truetype(str(path), FONT_SIZE)
    rows = (GLYPH_COUNT + COLUMNS - 1) // COLUMNS
    atlas = Image.new("RGBA", (COLUMNS * CELL_WIDTH, rows * CELL_HEIGHT), (255, 255, 255, 0))
    draw = ImageDraw.Draw(atlas)
    advances, widths, heights = [], [], []

    for index, codepoint in enumerate(range(GLYPH_FIRST, GLYPH_LAST + 1)):
        character = chr(codepoint)
        left, top, right, bottom = font.getbbox(character, anchor="lt")
        width = max(0, right - left)
        height = max(0, bottom - top)
        if width > CELL_WIDTH or height > CELL_HEIGHT:
            raise ValueError(f"{path.name}: {character!r} ({width}x{height}) exceeds glyph cell")

        x = (index % COLUMNS) * CELL_WIDTH
        y = (index // COLUMNS) * CELL_HEIGHT
        if width and height:
            draw.text((x - left, y - top), character, font=font, fill=(255, 255, 255, 255), anchor="lt")

        advances.append(max(1, int(round(font.getlength(character)))))
        widths.append(width)
        heights.append(height)

    # Pixel fonts should be hard-edged even when a TTF rasterizer produces
    # partial coverage at a glyph boundary.
    alpha = atlas.getchannel("A").point(lambda value: 255 if value >= 128 else 0)
    atlas.putalpha(alpha)
    output_name = safe_name(path) + ".png"
    atlas.save(OUTPUT_DIR / output_name, optimize=True)
    return {
        "name": display_name(path),
        "asset": output_name,
        "advances": advances,
        "widths": widths,
        "heights": heights,
    }


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    fonts = [build_font(path) for path in sorted(SOURCE_DIR.glob("*.ttf"))]
    manifest = {
        "cellWidth": CELL_WIDTH,
        "cellHeight": CELL_HEIGHT,
        "columns": COLUMNS,
        "firstCodepoint": GLYPH_FIRST,
        "lineHeight": CELL_HEIGHT,
        "fonts": fonts,
    }
    (OUTPUT_DIR / "fonts.json").write_text(json.dumps(manifest, separators=(",", ":")) + "\n")
    print(f"Generated {len(fonts)} font atlases in {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
