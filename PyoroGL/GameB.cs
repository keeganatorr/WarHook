using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonogameTest
{
    public partial class Game1
    {
        bool gameB;
        bool previousShotDown;
        Texture2D yellowTankRight, yellowTankLeft;
        readonly List<int> shotHits = new List<int>();

        void updateGameBShot(KeyboardState keys)
        {
            bool down = keys.IsKeyDown(Keys.X);
            if (down && !previousShotDown && !pyorodead && !gameover)
                fireGameBShot();
            previousShotDown = down;
        }

        void fireGameBShot()
        {
            // Same barrel tip and 45-degree direction as the tractor beam.
            float originX = (float)Math.Round(x + tongueoffsetX + (facingright == 1 ? rightoffset : 0));
            float originY = (float)Math.Round(y + tongueoffsetY);
            float limit = Math.Min(originY - PLAYFIELD_TOP,
                facingright == 1 ? PLAYFIELD_RIGHT - originX : originX - PLAYFIELD_LEFT);
            shotHits.Clear();
            for (int i = 0; i < max_amount_of_beans; i++)
            {
                if (!bean_active[i]) continue;
                Texture2D sprite = current_bean_sprite[i];
                if (shotIntersectsMortar(originX, originY, facingright, limit,
                    bean_x[i], bean_y[i], sprite.Width, sprite.Height))
                    shotHits.Add(i);
            }

            int points = shotHits.Count >= 4 ? 1000 : shotHits.Count == 3 ? 300 : shotHits.Count == 2 ? 100 : 50;
            foreach (int i in shotHits)
            {
                bean_active[i] = false;
                float centerX = bean_x[i] + current_bean_sprite[i].Width / 2f;
                float centerY = bean_y[i] + current_bean_sprite[i].Height / 2f;
                addScore(centerX, centerY, points);
                spawnExplosion(centerX, centerY);
            }
        }

        // Intersect a finite, zero-width ray with the mortar's sprite bounds.
        // Parameter t travels (direction * t, -t), so no stepping can skip a hit.
        static bool shotIntersectsMortar(float ox, float oy, int direction, float limit,
            float left, float top, float width, float height)
        {
            float nearX = direction == 1 ? left - ox : ox - left - width;
            float farX = direction == 1 ? left + width - ox : ox - left;
            float enter = Math.Max(0, Math.Max(nearX, oy - top - height));
            float leave = Math.Min(limit, Math.Min(farX, oy - top));
            return enter <= leave;
        }

        // Shift green paint to yellow once at load time, preserving the original
        // saturation, brightness, alpha, and neutral metal/outline pixels.
        Texture2D makeYellowTank(Texture2D source)
        {
            Color[] pixels = new Color[source.Width * source.Height];
            source.GetData(pixels);
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                if (c.A == 0) continue;
                float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f;
                float max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
                float chroma = max - min;
                if (chroma == 0) continue;
                float hue = max == r ? 60 * ((g - b) / chroma % 6)
                    : max == g ? 60 * ((b - r) / chroma + 2) : 60 * ((r - g) / chroma + 4);
                if (hue < 0) hue += 360;
                if (hue >= 65 && hue <= 170)
                    pixels[i] = new Color(max, max, min, c.A / 255f);
            }
            Texture2D result = new Texture2D(GraphicsDevice, source.Width, source.Height);
            result.SetData(pixels);
            return result;
        }
    }
}
