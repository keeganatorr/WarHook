using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonogameTest
{
    // A 6x6 pixel font generated at load time — no external asset needed.
    // Each glyph is a 5px-wide pattern inside a 6x6 cell (1px spacing), so
    // strings render crisply at native resolution like the 8x8 atlas font.
    // Supported: space, digits, A-Z, ':', '%' — enough for score popups and
    // the "MUSIC: n" HUD readout.
    sealed class Font6
    {
        public const int Cell = 6;
        public const int GlyphWidth = 5;

        // Glyph patterns, one row per byte (bit 4 = leftmost pixel of 5).
        static readonly Dictionary<char, byte[]> Glyphs = new()
        {
            ['0'] = new byte[] { 0b01110, 0b10001, 0b10011, 0b10101, 0b11001, 0b01110 },
            ['1'] = new byte[] { 0b00100, 0b01100, 0b00100, 0b00100, 0b00100, 0b01110 },
            ['2'] = new byte[] { 0b01110, 0b10001, 0b00001, 0b00010, 0b00100, 0b11111 },
            ['3'] = new byte[] { 0b11111, 0b00010, 0b00100, 0b00010, 0b10001, 0b01110 },
            ['4'] = new byte[] { 0b00010, 0b00110, 0b01010, 0b10010, 0b11111, 0b00010 },
            ['5'] = new byte[] { 0b11111, 0b10000, 0b11110, 0b00001, 0b10001, 0b01110 },
            ['6'] = new byte[] { 0b00110, 0b01000, 0b10000, 0b11110, 0b10001, 0b01110 },
            ['7'] = new byte[] { 0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b01000 },
            ['8'] = new byte[] { 0b01110, 0b10001, 0b01110, 0b10001, 0b10001, 0b01110 },
            ['9'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b01111, 0b00001, 0b01100 },
            ['A'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001 },
            ['B'] = new byte[] { 0b11110, 0b10001, 0b11110, 0b10001, 0b10001, 0b11110 },
            ['C'] = new byte[] { 0b01110, 0b10001, 0b10000, 0b10000, 0b10001, 0b01110 },
            ['D'] = new byte[] { 0b11100, 0b10010, 0b10001, 0b10001, 0b10010, 0b11100 },
            ['E'] = new byte[] { 0b11111, 0b10000, 0b11110, 0b10000, 0b10000, 0b11111 },
            ['F'] = new byte[] { 0b11111, 0b10000, 0b11110, 0b10000, 0b10000, 0b10000 },
            ['G'] = new byte[] { 0b01110, 0b10001, 0b10000, 0b10111, 0b10001, 0b01111 },
            ['H'] = new byte[] { 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001 },
            ['I'] = new byte[] { 0b01110, 0b00100, 0b00100, 0b00100, 0b00100, 0b01110 },
            ['J'] = new byte[] { 0b00111, 0b00010, 0b00010, 0b00010, 0b10010, 0b01100 },
            ['K'] = new byte[] { 0b10001, 0b10010, 0b11100, 0b10010, 0b10001, 0b10001 },
            ['L'] = new byte[] { 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b11111 },
            ['M'] = new byte[] { 0b10001, 0b11011, 0b10101, 0b10001, 0b10001, 0b10001 },
            ['N'] = new byte[] { 0b10001, 0b11001, 0b10101, 0b10011, 0b10001, 0b10001 },
            ['O'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 },
            ['P'] = new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10000, 0b10000 },
            ['Q'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b10101, 0b10010, 0b01101 },
            ['R'] = new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10010, 0b10001 },
            ['S'] = new byte[] { 0b01111, 0b10000, 0b10000, 0b01110, 0b00001, 0b11110 },
            ['T'] = new byte[] { 0b11111, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100 },
            ['U'] = new byte[] { 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 },
            ['V'] = new byte[] { 0b10001, 0b10001, 0b10001, 0b10001, 0b01010, 0b00100 },
            ['W'] = new byte[] { 0b10001, 0b10001, 0b10001, 0b10101, 0b11011, 0b10001 },
            ['X'] = new byte[] { 0b10001, 0b10001, 0b01010, 0b00100, 0b01010, 0b10001 },
            ['Y'] = new byte[] { 0b10001, 0b10001, 0b01010, 0b00100, 0b00100, 0b00100 },
            ['Z'] = new byte[] { 0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b11111 },
            [':'] = new byte[] { 0b00000, 0b00100, 0b00000, 0b00000, 0b00100, 0b00000 },
            ['%'] = new byte[] { 0b11001, 0b11010, 0b00010, 0b00100, 0b01011, 0b10011 },
            [' '] = new byte[] { 0, 0, 0, 0, 0, 0 },
        };

        // Atlas layout: one row, glyphs in dictionary order.
        public Texture2D Atlas { get; }
        readonly Dictionary<char, int> index = new();

        public Font6(GraphicsDevice device)
        {
            var keys = new List<char>(Glyphs.Keys);
            Atlas = new Texture2D(device, Cell * keys.Count, Cell);
            var pixels = new Color[Atlas.Width * Atlas.Height];
            for (int g = 0; g < keys.Count; g++)
            {
                index[keys[g]] = g;
                byte[] rows = Glyphs[keys[g]];
                for (int y = 0; y < Cell; y++)
                {
                    byte bits = y < rows.Length ? rows[y] : (byte)0;
                    for (int x = 0; x < GlyphWidth; x++)
                    {
                        bool on = (bits & (1 << (GlyphWidth - 1 - x))) != 0;
                        if (on) pixels[y * Atlas.Width + g * Cell + x] = Color.White;
                    }
                }
            }
            Atlas.SetData(pixels);
        }

        public Vector2 Measure(string text) => new(text.Length * Cell, Cell);

        public void Draw(SpriteBatch batch, string text, Vector2 position, Color color)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = char.ToUpperInvariant(text[i]);
                if (!index.TryGetValue(c, out int g)) g = index[' '];
                Rectangle src = new(g * Cell, 0, Cell, Cell);
                batch.Draw(Atlas, new Vector2((int)position.X + i * Cell, (int)position.Y), src, color);
            }
        }
    }
}
