using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonogameTest
{
    public partial class Game1
    {
        bool crashLanding;
        float crashLandVelocity, crashLandTime, crashTilt;

        void ResetCrashLanding()
        {
            crashLanding = false;
            crashLandVelocity = crashLandTime = crashTilt = 0;
        }

        void BeginCrashLanding()
        {
            crashLanding = true;
            crashLandTime = 0;
            crashLandVelocity = 18;
            crashTilt = shipTilt;
        }

        void UpdateCrashLanding(float dt)
        {
            if (!crashLanding) return;
            crashLandTime += dt;
            float landingY = GroundY - ShipHeight / 2;
            if (shipY < landingY)
            {
                crashLandVelocity += 170 * dt;
                shipY = Math.Min(landingY, shipY + crashLandVelocity * dt);
            }
            else
            {
                // A tiny settled bounce keeps the impact readable without
                // moving the ship away from the ground again.
                crashLandVelocity = 0;
                shipY = landingY;
            }
            crashTilt = MathHelper.Lerp(crashTilt, .42f, 1 - (float)Math.Exp(-5 * dt));
        }

        void DrawCrashFire()
        {
            if (!crashLanding) return;
            float flicker = .8f + .2f * (float)Math.Sin(crashLandTime * 31);
            int x = (int)shipX;
            int y = (int)shipY + 9;
            spriteBatch.Draw(beamPixel, new Rectangle(x - 10, y, 20, 3), new Color(228, 67, 32) * flicker);
            spriteBatch.Draw(beamPixel, new Rectangle(x - 6, y + 3, 12, 4), new Color(255, 153, 44) * flicker);
            spriteBatch.Draw(beamPixel, new Rectangle(x - 2, y + 7, 5, 3), new Color(255, 231, 116) * flicker);
            spriteBatch.Draw(beamPixel, new Rectangle(x + 7, y - 2, 4, 5), new Color(255, 111, 37) * flicker);
        }

        void DrawCrashSmoke()
        {
            if (!crashLanding) return;
            for (int i = 0; i < 5; i++)
            {
                float rise = (crashLandTime * (9 + i * 2) + i * 11) % 34;
                float drift = (float)Math.Sin(crashLandTime * 1.7 + i * 2.1) * (3 + i);
                int x = (int)(shipX - 9 + i * 5 + drift);
                int y = (int)(shipY - 8 - rise);
                Color smoke = new Color(107 + i * 8, 119 + i * 7, 124 + i * 8) * (0.72f - i * .08f);
                spriteBatch.Draw(beamPixel, new Rectangle(x - 3, y, 7, 5), smoke);
                spriteBatch.Draw(beamPixel, new Rectangle(x - 1, y - 2, 4, 2), smoke * .8f);
            }
        }

        void DrawCrashedUfo()
        {
            if (!crashLanding) return;
            Rectangle source = ufoSprites[0];
            spriteBatch.Draw(ufoAtlas, ShipPosition, source, new Color(205, 211, 205), crashTilt,
                new Vector2(source.Width / 2f, source.Height / 2f),
                new Vector2(ShipWidth / source.Width, ShipHeight / source.Height), SpriteEffects.None, 0);
        }
    }
}
