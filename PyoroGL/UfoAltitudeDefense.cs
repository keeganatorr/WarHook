using System;
using Microsoft.Xna.Framework;

namespace MonogameTest
{
    public partial class Game1
    {
        const float BaseAltitudeLineY = 64;
        const float AltitudeWarningSeconds = .85f, AltitudeShotInterval = .18f;
        const float AltitudeVolleyCooldown = 3.5f, AltitudeRocketSpeed = 90;
        const int AltitudeVolleySize = 6;
        float altitudeLineY, altitudeTimer, altitudeCooldown, altitudePulse;
        int altitudeShotsRemaining;
        Vector2 altitudeLeftSpawn, altitudeRightSpawn;

        void ResetAltitudeDefense()
        {
            altitudeLineY = BaseAltitudeLineY + progression.Bonus(UfoUpgradeEffect.FlightClearance);
            altitudeTimer = altitudeCooldown = altitudePulse = 0;
            altitudeShotsRemaining = 0;
            altitudeLeftSpawn = altitudeRightSpawn = Vector2.Zero;
        }

        void UpdateAltitudeDefense(float dt)
        {
            if (paused || gameover || screen != MenuScreen.Playing) return;
            altitudePulse += dt;
            altitudeCooldown = Math.Max(0, altitudeCooldown - dt);
            if (altitudeShotsRemaining == 0)
            {
                if (shipY <= altitudeLineY || altitudeCooldown > 0) return;
                // Keep the launch points fixed throughout the warning and volley.
                float spawnY = Math.Clamp(shipY + 32, 90, GroundY - 22);
                altitudeLeftSpawn = new Vector2(7, spawnY);
                altitudeRightSpawn = new Vector2(NATIVE_WIDTH - 7, spawnY);
                altitudeShotsRemaining = AltitudeVolleySize;
                altitudeTimer = AltitudeWarningSeconds;
                altitudePulse = 0;
                beamAudio?.PlayMenuBlip();
                return;
            }
            altitudeTimer -= dt;
            if (altitudeTimer > 0) return;
            Vector2 origin = (AltitudeVolleySize - altitudeShotsRemaining) % 2 == 0
                ? altitudeLeftSpawn : altitudeRightSpawn;
            Vector2 heading = Vector2.Normalize(ShipPosition - origin);
            missiles.Add(new UfoMissile {
                Position = origin, Heading = heading, Velocity = heading * AltitudeRocketSpeed,
                AltitudeDefense = true
            });
            altitudeShotsRemaining--;
            // At most one launch per frame, even following a long browser frame.
            altitudeTimer = AltitudeShotInterval;
            if (altitudeShotsRemaining == 0) altitudeCooldown = AltitudeVolleyCooldown;
        }

        void DrawAltitudeLine()
        {
            bool below = shipY > altitudeLineY;
            bool warning = below || altitudeShotsRemaining > 0;
            float pulse = warning ? .55f + .45f * (float)Math.Abs(Math.Sin(altitudePulse * 14)) : 1;
            Color color = warning ? new Color(255, 113, 86) : new Color(147, 143, 96);
            int y = (int)altitudeLineY;
            float lineAlpha = warning ? .35f + .6f * pulse : .55f;
            for (int x = 4; x < NATIVE_WIDTH - 4; x += 10)
                spriteBatch.Draw(beamPixel, new Rectangle(x, y, 6, 1), color * lineAlpha);
            const string label = "DANGER BELOW";
            float textScale = warning ? 1 + .08f * pulse : 1;
            Vector2 size = font6.Measure(label) * textScale;
            Vector2 position = new Vector2((NATIVE_WIDTH - size.X) / 2, y - 9);
            spriteBatch.Draw(beamPixel, new Rectangle((int)position.X - 3, y - 10, (int)size.X + 6, 9), new Color(7, 13, 30) * .8f);
            font6.Draw(spriteBatch, label, position, color * (warning ? .7f + .3f * pulse : 1), textScale);
        }

        void DrawAltitudeWarnings()
        {
            if (altitudeShotsRemaining == 0 || gameover) return;
            float pulse = .55f + .45f * (float)Math.Abs(Math.Sin(altitudePulse * 14));
            Color color = new Color(255, 106, 68) * pulse;
            void Blip(Vector2 position, int direction)
            {
                int x = (int)position.X, y = (int)position.Y;
                spriteBatch.Draw(beamPixel, new Rectangle(x - 6, y - 7, 13, 15), new Color(35, 10, 17));
                // The exclamation mark's centre is the exact rocket spawn position.
                spriteBatch.Draw(beamPixel, new Rectangle(x - 1, y - 5, 3, 6), color);
                spriteBatch.Draw(beamPixel, new Rectangle(x - 1, y + 3, 3, 2), color);
                for (int i = 0; i < 4; i++)
                    spriteBatch.Draw(beamPixel, new Rectangle(x + direction * (9 + i), y - 3 + i, 1, 7 - i * 2), color);
            }
            if (altitudeShotsRemaining > 1) Blip(altitudeLeftSpawn, 1);
            Blip(altitudeRightSpawn, -1);
        }
    }
}
