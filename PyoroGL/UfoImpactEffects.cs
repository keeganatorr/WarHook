using System;
using Microsoft.Xna.Framework;

namespace MonogameTest
{
    public partial class Game1
    {
        float impactShakeTime, impactShakeAge, impactShakeStrength, impactShakeSeed;
        Vector2 lastImpactHeading;

        void ResetImpactEffects()
        {
            impactShakeTime = impactShakeAge = impactShakeStrength = impactShakeSeed = 0;
            lastImpactHeading = -Vector2.UnitY;
        }

        void UpdateImpactEffects(float dt)
        {
            if (impactShakeTime <= 0) return;
            impactShakeTime = Math.Max(0, impactShakeTime - dt);
            impactShakeAge += dt;
        }

        void TriggerRocketImpact(Vector2 heading)
        {
            if (heading.LengthSquared() < .001f) heading = -Vector2.UnitY;
            else heading.Normalize();
            lastImpactHeading = heading;
            impactShakeTime = .24f;
            impactShakeAge = 0;
            impactShakeStrength = 2.4f;
            impactShakeSeed += 1.37f;

            // Push in the rocket's travel direction, with a small immediate
            // displacement so the hit reads even if the hull is destroyed.
            shipVelocity += heading * 52;
            shipX = MathHelper.Clamp(shipX + heading.X * 2.5f, ShipSideMargin, NATIVE_WIDTH - ShipSideMargin);
            shipY = MathHelper.Clamp(shipY + heading.Y * 2.5f, ShipMinY, ShipMaxY);
        }

        Matrix ImpactShakeTransform()
        {
            if (impactShakeTime <= 0) return Matrix.Identity;
            float fade = MathHelper.Clamp(impactShakeTime / .24f, 0, 1);
            float x = (float)Math.Sin(impactShakeAge * 91 + impactShakeSeed) * impactShakeStrength * fade;
            float y = (float)Math.Cos(impactShakeAge * 113 + impactShakeSeed * 1.7f) * impactShakeStrength * .65f * fade;
            return Matrix.CreateTranslation(x, y, 0);
        }
    }
}
