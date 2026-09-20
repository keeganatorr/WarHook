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
            impactShakeTime = .30f;
            impactShakeAge = 0;
            impactShakeStrength = 3.4f;
            impactShakeSeed += 1.37f;

            // Push in the rocket's travel direction, with a stronger immediate
            // displacement and a visible rotational impulse so each hit reads
            // clearly before the normal flight easing takes over.
            shipVelocity += heading * 92;
            shipX = MathHelper.Clamp(shipX + heading.X * 4f, ShipSideMargin, NATIVE_WIDTH - ShipSideMargin);
            shipY = MathHelper.Clamp(shipY + heading.Y * 4f, ShipMinY, ShipMaxY);
            shipTilt = MathHelper.Clamp(shipTilt + heading.X * .34f, -.48f, .48f);
        }

        Matrix ImpactShakeTransform()
        {
            if (impactShakeTime <= 0) return Matrix.Identity;
            float fade = MathHelper.Clamp(impactShakeTime / .30f, 0, 1);
            float x = (float)Math.Sin(impactShakeAge * 91 + impactShakeSeed) * impactShakeStrength * fade;
            float y = (float)Math.Cos(impactShakeAge * 113 + impactShakeSeed * 1.7f) * impactShakeStrength * .65f * fade;
            return Matrix.CreateTranslation(x, y, 0);
        }
    }
}
