using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonogameTest
{
    public partial class Game1
    {
        const int GroundY = NATIVE_HEIGHT - 19;
        const int PersonHeight = 14;
        const float ShipStartY = 38, ShipMinY = 34, ShipMaxY = GroundY - 38, ShipSpeed = 90;
        const float ShipWidth = 44, ShipHeight = 20, ShipSideMargin = 23;
        const float EnemyRunInSeconds = 3f, EnemyExitSpeed = 24;
        const float LiftSpeed = 30, TractorCenterSpeed = 15, BulletSpeed = 160;
        const int SoldierMaxHealth = 100, BulletDamage = SoldierMaxHealth;
        const int StartingUfoSpeed = 0x600, StartingUfoSpawnInterval = 0x78;
        const int StartingMissileSpeed = 0x180;
        const double DifficultyRatePerAbduction = .25;
        Texture2D ufoAtlas;
        readonly Rectangle[] ufoSprites = new Rectangle[16];
        readonly List<GroundPerson> people = new List<GroundPerson>();
        readonly List<UfoMissile> missiles = new List<UfoMissile>();
        readonly List<ShipBullet> shipBullets = new List<ShipBullet>();
        float shipX, shipY, previousShipX, previousShipY, shipTilt, shotCooldown, muzzleFlash;
        float hurtTime, repairTime, tractorCooldown, messageTime, beamAnimation;
        int shipHealth, weaponLevel, tractorLevel, engineLevel;
        float fireBonus, engineBonus, tractorBonus, plasmaBonus, focusBonus, matrixBonus, warpBonus, globalBonus = 1;
        int shotCount = 1, pointDefenseLevel, nanoHull;
        float autoRepairRate, timeWithoutDamage, autoRepairFraction;
        Vector2 shipVelocity;
        double longHaulBonus, exponentialRate, interestBonus;
        UfoProgression progression;
        int roundSoldiers, hullLevel, beamCapacity, repairLevel, widthLevel;
        int roundAbductions;
        int missileDifficultySpeed;
        double difficultyTickRemainder;
        double roundSeconds, roundGrowth, roundStartMultiplier;
        string roundId;
        bool roundActive, roundBanked;
        double RoundMultiplier
        {
            get
            {
                double GrowthIntegral(double t) => exponentialRate > 0
                    ? (Math.Exp(Math.Min(40, exponentialRate * t)) - 1) / exponentialRate : t;
                double growthTime = GrowthIntegral(roundSeconds);
                if (roundSeconds > 120) growthTime += longHaulBonus * (GrowthIntegral(roundSeconds) - GrowthIntegral(120));
                return Math.Min(1_000_000, (roundStartMultiplier + roundGrowth * growthTime) * (1 + interestBonus) * globalBonus);
            }
        }
        double RoundReward => Math.Floor(roundSoldiers * RoundMultiplier * 100 + .000001) / 100;
        int MaxShipHealth => (int)((100 + hullLevel * 25 + nanoHull * 50) * globalBonus);
        bool tractorActive, ufoShotFrozen;
        readonly List<GroundPerson> abductees = new();
        string ufoMessage = "";
        Vector2 ShipPosition => new Vector2(shipX, shipY);
        Vector2 PreviousShipPosition => new Vector2(previousShipX, previousShipY);
        Vector2 ShipCollisionHalfSize => new Vector2(19, 10);
        Vector2 TractorOrigin => new Vector2(shipX, shipY + 8);
        float ShotInterval => .8f / ((1 + fireBonus + plasmaBonus) * globalBonus);
        float ShipBulletSpeed => BulletSpeed * (1 + plasmaBonus);
        float FlightSpeed => ShipSpeed * (1 + engineBonus) * globalBonus;
        float TractorLiftSpeed => LiftSpeed * (1 + tractorBonus) * globalBonus;
        float TractorPullSpeed => TractorCenterSpeed * (1 + tractorBonus) * globalBonus;
        bool CriticalHullFlash => shipHealth <= 25 && ((int)(roundSeconds * 8) & 1) == 0;

        Color MultiplierBandColor(int band)
        {
            return ((Math.Max(1, band) - 1) % 4) switch {
                0 => new Color(255, 220, 130),
                1 => new Color(255, 166, 78),
                2 => new Color(255, 103, 137),
                _ => new Color(185, 143, 255)
            };
        }

        void DrawMultiplierMeter()
        {
            const int x = 112, y = 15, width = 72, height = 5;
            double multiplier = RoundMultiplier;
            int band = Math.Max(1, (int)Math.Floor(Math.Min(multiplier, int.MaxValue - 1)));
            float progress = (float)Math.Clamp(multiplier - band, 0, 1);
            Color tint = MultiplierBandColor(band);
            spriteBatch.Draw(beamPixel, new Rectangle(x - 1, y - 1, width + 2, height + 2), tint * .55f);
            spriteBatch.Draw(beamPixel, new Rectangle(x, y, width, height), new Color(18, 29, 43));
            int filled = (int)Math.Round(width * progress);
            if (filled > 0)
                spriteBatch.Draw(beamPixel, new Rectangle(x, y, filled, height), tint);
        }

        sealed class GroundPerson
        {
            public float X, PreviousX, Y = GroundY, PreviousY = GroundY;
            public float TargetX, RunFromX, ApproachTime, Animation, HitFlash, FallSpeed;
            public int Direction, BeanSpeed;
            public int Health = SoldierMaxHealth;
            public bool Engineer, ReachedTarget, Falling;
        }

        sealed class UfoMissile
        {
            public Vector2 Position, Velocity, Heading;
            public bool AltitudeDefense;
            // Zero keeps a fixed velocity (side volleys and smoke-check missiles).
            public int BeanSpeed;
        }

        sealed class ShipBullet
        {
            public Vector2 Position, Velocity;
            public float Remaining = 3;
        }

        void LoadUfoAssets()
        {
            ufoAtlas = loadPng("ufo-atlas");
            // The generated sheet may differ from its requested resolution.
            // Trim alpha inside each grid cell, preserving the actual pixels.
            var pixels = new Color[ufoAtlas.Width * ufoAtlas.Height];
            ufoAtlas.GetData(pixels);
            for (int i = 0; i < 16; i++)
            {
                int x0 = i % 4 * ufoAtlas.Width / 4;
                int x1 = (i % 4 + 1) * ufoAtlas.Width / 4;
                int y0 = i / 4 * ufoAtlas.Height / 4;
                int y1 = (i / 4 + 1) * ufoAtlas.Height / 4;
                int left = x1, right = x0, top = y1, bottom = y0;
                for (int yy = y0; yy < y1; yy++)
                    for (int xx = x0; xx < x1; xx++)
                        if (pixels[yy * ufoAtlas.Width + xx].A >= 100)
                        {
                            left = Math.Min(left, xx); right = Math.Max(right, xx);
                            top = Math.Min(top, yy); bottom = Math.Max(bottom, yy);
                        }
                ufoSprites[i] = new Rectangle(left, top, right - left + 1, bottom - top + 1);
            }
        }

        void ResetUfo()
        {
            shipX = previousShipX = NATIVE_WIDTH / 2f;
            shipY = previousShipY = ShipStartY;
            ResetCrashLanding();
            ResetImpactEffects();
            shipTilt = shotCooldown = muzzleFlash = hurtTime = repairTime = 0;
            tractorCooldown = messageTime = beamAnimation = 0;
            weaponLevel = 1 + progression.Total(UfoUpgradeEffect.Fire);
            tractorLevel = progression.Total(UfoUpgradeEffect.Tractor);
            engineLevel = progression.Total(UfoUpgradeEffect.Engine);
            hullLevel = progression.Total(UfoUpgradeEffect.Hull);
            repairLevel = progression.Total(UfoUpgradeEffect.Repair);
            widthLevel = progression.Total(UfoUpgradeEffect.BeamWidth);
            beamCapacity = progression.Capacity;
            fireBonus = progression.Bonus(UfoUpgradeEffect.Fire);
            engineBonus = progression.Bonus(UfoUpgradeEffect.Engine);
            tractorBonus = progression.Bonus(UfoUpgradeEffect.Tractor);
            plasmaBonus = progression.Bonus(UfoUpgradeEffect.Plasma);
            focusBonus = progression.Bonus(UfoUpgradeEffect.Focus);
            matrixBonus = progression.Bonus(UfoUpgradeEffect.Matrix);
            warpBonus = progression.Bonus(UfoUpgradeEffect.Warp);
            globalBonus = 1 + progression.Bonus(UfoUpgradeEffect.Mothership);
            nanoHull = progression.Total(UfoUpgradeEffect.Nanohull);
            pointDefenseLevel = progression.Total(UfoUpgradeEffect.PointDefense);
            shotCount = progression.Total(UfoUpgradeEffect.TripleShot) > 0 ? 3 : progression.Total(UfoUpgradeEffect.TwinShot) > 0 ? 2 : 1;
            autoRepairRate = progression.Bonus(UfoUpgradeEffect.AutoRepair);
            timeWithoutDamage = autoRepairFraction = 0;
            shipVelocity = Vector2.Zero;
            shipHealth = MaxShipHealth;
            roundSoldiers = 0; roundSeconds = 0;
            roundAbductions = 0; difficultyTickRemainder = 0;
            bigspeed = StartingUfoSpeed; smallspeed = 0x10;
            missileDifficultySpeed = StartingMissileSpeed;
            max_time = StartingUfoSpawnInterval;
            roundStartMultiplier = 1 + Math.Round(progression.Bonus(UfoUpgradeEffect.StartingBonus), 6);
            // Base survival growth is 0.50x per minute; yield upgrades scale
            // that rate while the timer remains active-only.
            roundGrowth = (1 + Math.Round(progression.Bonus(UfoUpgradeEffect.Growth), 6)) / 120;
            longHaulBonus = Math.Round(progression.Bonus(UfoUpgradeEffect.LongHaul), 6);
            exponentialRate = Math.Log(1 + Math.Round(progression.Bonus(UfoUpgradeEffect.Exponential), 6)) / 60;
            // Interest is fixed at launch: spending crew trades savings for power.
            interestBonus = Math.Round(progression.Bonus(UfoUpgradeEffect.Interest), 6) * Math.Min(10, progression.Balance / 100);
            roundId = Guid.NewGuid().ToString("N");
            roundActive = true; roundBanked = false;
            tractorActive = ufoShotFrozen = false;
            abductees.Clear();
            people.Clear(); missiles.Clear(); shipBullets.Clear();
            ResetAltitudeDefense();
            SetUfoMessage("SPACE/X FIRE - SHIFT/Z BEAM", 4);
        }

        // Used for composed screenshots / beam checks. Real enemies enter
        // through SpawnBeanRunner and inherit the original bean event.
        void SpawnPerson(float position, bool engineer, int direction)
        {
            people.Add(new GroundPerson {
                X = position, PreviousX = position, TargetX = position, Engineer = engineer,
                Direction = direction, ReachedTarget = true
            });
        }

        void SpawnBeanRunner(float targetX, int beanType, int beanSpeed)
        {
            int direction = targetX < NATIVE_WIDTH / 2f ? 1 : -1;
            float entry = direction > 0 ? -6 : NATIVE_WIDTH + 6;
            people.Add(new GroundPerson {
                X = entry, PreviousX = entry, RunFromX = entry, TargetX = targetX,
                Engineer = beanType != 0 && repairLevel > 0, Direction = direction, BeanSpeed = beanSpeed
            });
        }

        int ActiveBeanSpawnCount()
        {
            int count = 0;
            foreach (UfoMissile missile in missiles)
                if (!missile.AltitudeDefense) count++;
            foreach (GroundPerson person in people)
                if (person.Engineer || !person.ReachedTarget) count++;
            return count;
        }

        static int NextBeanRandom(ref int state)
        {
            state = (0x6D * state + 0x3FD) & 0xFFFF;
            return state;
        }

        void UpdateBeanSpawns()
        {
            // Original 60 Hz bean spawn arithmetic and RNG order. Consume all
            // four rolls even when the original 16-slot pool is full.
            if (time_until_new_bean <= 0)
            {
                int jitter = ((max_time >> 2) * NextBeanRandom(ref randnum)) >> 16;
                time_until_new_bean = ((max_time - jitter) << 8) / bigspeed;
                int beanSpeed = 0x40 + ((0x40 * NextBeanRandom(ref randnum2)) >> 16);
                int targetX = PLAYFIELD_LEFT + 8
                    + (((PLAYFIELD_RIGHT - PLAYFIELD_LEFT - 16) * NextBeanRandom(ref randnum3)) >> 16);
                int beanType = ((9 * NextBeanRandom(ref randnum4)) >> 16) >> 3;
                if (score >= risingscore)
                {
                    beanType = 2;
                    beanSpeed = 0x40;
                    if (risingscore < 9000) risingscore += 2000;
                    else risingscore += 1000;
                }
                if (ActiveBeanSpawnCount() < max_amount_of_beans)
                    SpawnBeanRunner(targetX, beanType, beanSpeed);
            }
            if (time_until_new_bean > 0) time_until_new_bean--;
        }

        void UpdateBeanDifficulty()
        {
            // Called after movement/scoring, matching the original update order.
            speedloop();
            if (score == 0) max_time = 0xB4;
            if (score >= 1000 && score < 2999) max_time = 0x78;
            if (score >= 3000 && score < 4999) max_time = 0x5F;
            if (score >= 5000 && score < 7999) max_time = 0x50;
            if (score >= 8000 && score < 9999) max_time = 0x41;
            if (score >= 10000) max_time = 0x32;
        }

        void UpdateUfoDifficulty(float dt)
        {
            // Advance only the original difficulty clock, never enemy movement,
            // spawn rolls, or the survival/reward timer. Preserve fractional ticks.
            difficultyTickRemainder += dt * targetFPS * (1 + roundAbductions * DifficultyRatePerAbduction);
            int ticks = (int)difficultyTickRemainder;
            difficultyTickRemainder -= ticks;
            for (int i = 0; i < ticks; i++)
            {
                UpdateBeanDifficulty();
                // speedloop resets this counter to 16 whenever speed advances.
                // Keep missile speed independent of the denser spawn baseline,
                // and let it keep rising after spawn difficulty reaches its cap.
                if (smallspeed == 0x10)
                    missileDifficultySpeed = Math.Min(0x7F0, missileDifficultySpeed + 1);
            }
            max_time = Math.Min(max_time, StartingUfoSpawnInterval);
        }

        float BeanMissileSpeed(int beanSpeed) => ((beanSpeed * missileDifficultySpeed) >> 8) / 256f * targetFPS;

        void UpdateGroundPeople(float dt)
        {
            for (int i = people.Count - 1; i >= 0; i--)
            {
                GroundPerson person = people[i];
                person.PreviousX = person.X;
                person.PreviousY = person.Y;
                person.Animation += dt;
                person.HitFlash = Math.Max(0, person.HitFlash - dt);
                if (abductees.Contains(person)) continue;
                if (person.Falling)
                {
                    person.FallSpeed = Math.Min(120, person.FallSpeed + 80 * dt);
                    person.Y = Math.Min(GroundY, person.Y + person.FallSpeed * dt);
                    if (person.Y >= GroundY) { person.Falling = false; person.FallSpeed = 0; }
                    continue;
                }
                float exitTime = dt;
                if (!person.ReachedTarget)
                {
                    person.ApproachTime += dt;
                    person.X = MathHelper.Lerp(person.RunFromX, person.TargetX,
                        Math.Min(1, person.ApproachTime / EnemyRunInSeconds));
                    if (person.ApproachTime < EnemyRunInSeconds) continue;
                    person.X = person.TargetX;
                    person.ReachedTarget = true;
                    // Exactly one projectile per ordinary bean event, fired at
                    // the assigned X even if this update overshoots arrival.
                    if (!person.Engineer) LaunchMissile(person);
                    exitTime = person.ApproachTime - EnemyRunInSeconds;
                }
                person.X += person.Direction * EnemyExitSpeed * exitTime;
                if (person.Direction > 0 ? person.X > NATIVE_WIDTH + 6 : person.X < -6)
                    people.RemoveAt(i);
            }
        }

        // Event text is intentionally disabled; gameplay communicates through
        // the HUD, sprites, sound, and effects instead.
        void SetUfoMessage(string message, float seconds = 1.8f) { }

        void MoveShip(float direction, float dt, float verticalDirection = 0)
        {
            previousShipX = shipX;
            previousShipY = shipY;
            Vector2 target = new Vector2(direction * FlightSpeed, verticalDirection * FlightSpeed * .4f * (1 + warpBonus));
            float acceleration = 1200 * (1 + warpBonus) * dt;
            shipVelocity.X += Math.Clamp(target.X - shipVelocity.X, -acceleration, acceleration);
            shipVelocity.Y += Math.Clamp(target.Y - shipVelocity.Y, -acceleration, acceleration);
            shipX = Math.Clamp(shipX + shipVelocity.X * dt, ShipSideMargin, NATIVE_WIDTH - ShipSideMargin);
            shipY = Math.Clamp(shipY + shipVelocity.Y * dt, ShipMinY, ShipMaxY);
            if (shipX == ShipSideMargin || shipX == NATIVE_WIDTH - ShipSideMargin) shipVelocity.X = 0;
            if (shipY == ShipMinY || shipY == ShipMaxY) shipVelocity.Y = 0;
            float velocity = dt > 0 ? (shipX - previousShipX) / dt : 0;
            float targetTilt = velocity / FlightSpeed * .18f;
            shipTilt = MathHelper.Lerp(shipTilt, targetTilt, 1 - (float)Math.Exp(-7 * dt));
        }

        void LaunchMissile(GroundPerson person)
        {
            // One shot at the bean's assigned X, aimed at the ship at launch.
            // The fixed heading allows dodging; only speed follows the bean ramp.
            Vector2 position = new Vector2(person.TargetX, GroundY - PersonHeight + 2);
            Vector2 heading = Vector2.Normalize(ShipPosition - position);
            missiles.Add(new UfoMissile {
                Position = position, Heading = heading, BeanSpeed = person.BeanSpeed,
                Velocity = heading * BeanMissileSpeed(person.BeanSpeed)
            });
        }

        void DamageShip(Vector2? impactDirection = null)
        {
            if (gameover || hurtTime > 0) return;
            shipHealth = Math.Max(0, shipHealth - 25);
            TriggerRocketImpact(impactDirection ?? -Vector2.UnitY);
            timeWithoutDamage = autoRepairFraction = 0;
            hurtTime = 1;
            spawnExplosion(shipX, shipY + 6);
            SetUfoMessage(repairLevel > 0 ? "SHIP HIT - ENGINEERS REPAIR" : "SHIP HIT - KEEP MOVING");
            if (shipHealth > 0) return;
            pyorodead = gameover = true;
            BeginCrashLanding();
            tractorActive = false;
            DropPayload();
            beamAudio?.StopTankAndBeamVoices();
            gameoverMusicPlaying = true;
            music?.Request(MusicTracks.Track.Gameover);
        }

        float ConeHalfWidth(float y) => MathHelper.Lerp(7, 22 * (1 + widthLevel * .1f) * (1 + (abductees.Count > 0 ? matrixBonus : 0)),
            Math.Clamp((y - TractorOrigin.Y) / (GroundY - TractorOrigin.Y), 0, 1));

        bool InsideTractor(Vector2 point) => point.Y >= TractorOrigin.Y && point.Y <= GroundY
            && Math.Abs(point.X - shipX) <= ConeHalfWidth(point.Y);

        void DropPerson(GroundPerson person)
        {
            if (!person.ReachedTarget)
            {
                float progress = Math.Min(.99999f, person.ApproachTime / EnemyRunInSeconds);
                person.RunFromX = (person.X - person.TargetX * progress) / (1 - progress);
            }
            person.Falling = true; person.FallSpeed = 0;
            abductees.Remove(person);
        }

        void DropPayload()
        {
            for (int i = abductees.Count - 1; i >= 0; i--) DropPerson(abductees[i]);
            tractorCooldown = .25f;
        }

        void DeliverPerson(GroundPerson person)
        {
            people.Remove(person); abductees.Remove(person);
            roundAbductions++;
            addScore(shipX - 8, shipY + 20, person.Engineer ? 250 : 100);
            if (person.Engineer)
            {
                shipHealth = Math.Min(MaxShipHealth, shipHealth + repairLevel * 5);
                repairTime = .7f;
                SetUfoMessage("ENGINEER ABOARD - SHIP REPAIRED");
                beamAudio?.PlayParachute();
            }
            else
            {
                roundSoldiers++;
                SetUfoMessage("SOLDIER COLLECTED", .7f);
                beamAudio?.PlayMenuBlip();
            }
        }

        void UpdateTractor(float dt, bool held)
        {
            tractorActive = held;
            beamAnimation += dt;
            tractorCooldown = Math.Max(0, tractorCooldown - dt);
            if (!held)
            {
                if (abductees.Count > 0) DropPayload();
                return;
            }
            for (int i = abductees.Count - 1; i >= 0; i--)
            {
                GroundPerson person = abductees[i];
                Vector2 payload = new Vector2(person.X, person.Y - PersonHeight / 2f);
                if (!InsideTractor(payload)) { DropPerson(person); continue; }
                float pull = TractorPullSpeed * (1 + focusBonus * Math.Clamp(1 - (payload.Y - TractorOrigin.Y) / 64, 0, 1));
                payload.X += Math.Clamp(shipX - payload.X, -pull * dt, pull * dt);
                payload.Y = Math.Max(TractorOrigin.Y + 3, payload.Y - TractorLiftSpeed * dt);
                if (!InsideTractor(payload)) { DropPerson(person); continue; }
                person.X = payload.X; person.Y = payload.Y + PersonHeight / 2f;
                if (payload.Y <= TractorOrigin.Y + 3) DeliverPerson(person);
            }
            if (abductees.Count >= beamCapacity || tractorCooldown > 0) return;
            GroundPerson nearest = null;
            float nearestY = float.PositiveInfinity;
            foreach (GroundPerson person in people)
            {
                if (abductees.Contains(person)) continue;
                Vector2 center = new Vector2(person.X, person.Y - PersonHeight / 2f);
                if (InsideTractor(center) && center.Y < nearestY) { nearest = person; nearestY = center.Y; }
            }
            if (nearest != null)
            {
                abductees.Add(nearest); nearest.Falling = false; nearest.FallSpeed = 0;
                tractorCooldown = .25f;
            }
        }

        void UpdateShipWeapon(float dt, bool fire)
        {
            shotCooldown = Math.Max(0, shotCooldown - dt);
            muzzleFlash = Math.Max(0, muzzleFlash - dt);
            if (!fire || shotCooldown > .00001f) return;
            for (int i = 0; i < shotCount; i++)
                shipBullets.Add(new ShipBullet {
                    Position = TractorOrigin + new Vector2((i - (shotCount - 1) / 2f) * 8, 3),
                    Velocity = Vector2.UnitY * ShipBulletSpeed
                });
            shotCooldown = ShotInterval;
            muzzleFlash = .09f;
            beamAudio?.PlayGameBShot();
        }

        void UpdateMissiles(float dt)
        {
            for (int i = missiles.Count - 1; i >= 0; i--)
            {
                UfoMissile missile = missiles[i];
                Vector2 start = missile.Position;
                Vector2 end = start + missile.Velocity * dt;
                float contact = SweptContactTime(start - PreviousShipPosition,
                    end - ShipPosition, ShipCollisionHalfSize);
                missile.Position = end;
                if (!float.IsPositiveInfinity(contact))
                {
                    missiles.RemoveAt(i);
                    DamageShip(missile.Heading);
                    if (gameover) break;
                }
                else if (end.Y < -12 || end.Y > NATIVE_HEIGHT + 12 || end.X < -12 || end.X > NATIVE_WIDTH + 12)
                    missiles.RemoveAt(i);
            }
        }

        void UpdateShipBullets(float dt)
        {
            for (int i = shipBullets.Count - 1; i >= 0; i--)
            {
                ShipBullet shot = shipBullets[i];
                float travelTime = Math.Min(dt, shot.Remaining);
                Vector2 end = shot.Position + shot.Velocity * travelTime;
                GroundPerson hit = null;
                UfoMissile missileHit = null;
                float first = float.PositiveInfinity;
                foreach (GroundPerson person in people)
                {
                    if (abductees.Contains(person)) continue;
                    Vector2 oldCenter = new Vector2(person.PreviousX, person.PreviousY - PersonHeight / 2f);
                    Vector2 center = new Vector2(person.X, person.Y - PersonHeight / 2f);
                    float contact = SweptContactTime(shot.Position - oldCenter, end - center,
                        new Vector2(6, PersonHeight / 2f + 1));
                    if (contact < first) { first = contact; hit = person; }
                }
                foreach (UfoMissile missile in missiles)
                {
                    Vector2 missileEnd = missile.Position + missile.Velocity * travelTime;
                    Vector2 heading = missile.Velocity.LengthSquared() > 0
                        ? Vector2.Normalize(missile.Velocity) : -Vector2.UnitY;
                    Vector2 halfSize = new Vector2(
                        Math.Abs(heading.Y) * 2.5f + Math.Abs(heading.X) * 6 + 1,
                        Math.Abs(heading.X) * 2.5f + Math.Abs(heading.Y) * 6 + 1);
                    halfSize += new Vector2(pointDefenseLevel * 3);
                    float contact = SweptContactTime(shot.Position - missile.Position, end - missileEnd, halfSize);
                    // An interception must happen before the missile hits the
                    // ship, even when both events occur in one long frame.
                    Vector2 shipEnd = Vector2.Lerp(PreviousShipPosition, ShipPosition,
                        dt > 0 ? travelTime / dt : 0);
                    float shipContact = SweptContactTime(missile.Position - PreviousShipPosition,
                        missileEnd - shipEnd, ShipCollisionHalfSize);
                    if (contact < first && contact <= shipContact)
                    { first = contact; missileHit = missile; hit = null; }
                }
                Vector2 shotStart = shot.Position;
                shot.Position = end;
                shot.Remaining -= dt;
                if (missileHit != null)
                {
                    Vector2 impact = Vector2.Lerp(shotStart, end, first);
                    Vector2 blast = missileHit.Position + missileHit.Velocity * (travelTime * first);
                    missiles.Remove(missileHit);
                    if (pointDefenseLevel > 0)
                        for (int m = missiles.Count - 1; m >= 0; m--)
                        {
                            var nearby = missiles[m];
                            Vector2 atImpact = nearby.Position + nearby.Velocity * (travelTime * first);
                            float shipContact = SweptContactTime(nearby.Position - PreviousShipPosition,
                                nearby.Position + nearby.Velocity * travelTime - Vector2.Lerp(PreviousShipPosition, ShipPosition, dt > 0 ? travelTime / dt : 0), ShipCollisionHalfSize);
                            if (shipContact >= first && Vector2.Distance(atImpact, blast) <= pointDefenseLevel * 8)
                            { missiles.RemoveAt(m); addScore(atImpact.X - 8, atImpact.Y, 50); }
                        }
                    shipBullets.RemoveAt(i);
                    spawnExplosion(impact.X, impact.Y, false);
                    addScore(Math.Clamp(impact.X - 5, 10, 250), impact.Y, 50);
                }
                else if (hit != null)
                {
                    hit.Health = Math.Max(0, hit.Health - BulletDamage);
                    hit.HitFlash = .12f;
                    if (hit.Health == 0)
                    {
                        people.Remove(hit);
                        addScore(Math.Clamp(hit.X - 5, 10, 250), hit.Y - PersonHeight, 50);
                        spawnExplosion(hit.X, hit.Y - PersonHeight / 2f, false);
                    }
                    shipBullets.RemoveAt(i);
                }
                else if (shot.Remaining <= 0 || end.Y > GroundY + 2 || end.X < -4 || end.X > NATIVE_WIDTH + 4)
                    shipBullets.RemoveAt(i);
            }
        }

        static float SweptContactTime(Vector2 start, Vector2 end, Vector2 halfSize)
        {
            Vector2 delta = end - start;
            float enter = 0, leave = 1;
            for (int axis = 0; axis < 2; axis++)
            {
                float position = axis == 0 ? start.X : start.Y;
                float movement = axis == 0 ? delta.X : delta.Y;
                float radius = axis == 0 ? halfSize.X : halfSize.Y;
                if (Math.Abs(movement) < .0001f)
                {
                    if (Math.Abs(position) > radius) return float.PositiveInfinity;
                    continue;
                }
                float a = (-radius - position) / movement;
                float b = (radius - position) / movement;
                enter = Math.Max(enter, Math.Min(a, b));
                leave = Math.Min(leave, Math.Max(a, b));
                if (enter > leave) return float.PositiveInfinity;
            }
            return enter;
        }

        void UpdateAutoRepair(float dt)
        {
            float previousSafeTime = timeWithoutDamage;
            timeWithoutDamage += dt;
            if (autoRepairRate <= 0 || shipHealth >= MaxShipHealth) { autoRepairFraction = 0; return; }
            float healingTime = Math.Max(0, timeWithoutDamage - Math.Max(5, previousSafeTime));
            autoRepairFraction += healingTime * autoRepairRate;
            int healing = (int)autoRepairFraction;
            if (healing > 0)
            {
                shipHealth = Math.Min(MaxShipHealth, shipHealth + healing);
                autoRepairFraction -= healing;
                repairTime = .2f;
            }
        }

        void UpdateUfo(GameTime time, KeyboardState keys)
        {
            if (ufoShotFrozen || paused) return;
            float dt = (float)time.ElapsedGameTime.TotalSeconds;
            UpdateImpactEffects(dt);
            updateExplosions(); updateScorePopups();
            if (!gameover && screen == MenuScreen.Playing)
            {
                roundSeconds += dt;
                UpdateAutoRepair(dt);
                messageTime = Math.Max(0, messageTime - dt);
                hurtTime = Math.Max(0, hurtTime - dt);
                repairTime = Math.Max(0, repairTime - dt);
                GamePadState pad = GamePad.GetState(PlayerIndex.One);
                bool left = keys.IsKeyDown(Keys.Left) || keys.IsKeyDown(Keys.A) || pad.IsButtonDown(Buttons.DPadLeft) || pad.ThumbSticks.Left.X < -.3f;
                bool right = keys.IsKeyDown(Keys.Right) || keys.IsKeyDown(Keys.D) || pad.IsButtonDown(Buttons.DPadRight) || pad.ThumbSticks.Left.X > .3f;
                bool fire = keys.IsKeyDown(Keys.X) || keys.IsKeyDown(Keys.Space) || pad.IsButtonDown(Buttons.A);
                bool tractor = keys.IsKeyDown(Keys.Z) || keys.IsKeyDown(Keys.LeftShift)
                    || keys.IsKeyDown(Keys.RightShift) || pad.IsButtonDown(Buttons.Y);
                bool up = keys.IsKeyDown(Keys.Up) || keys.IsKeyDown(Keys.W) || pad.IsButtonDown(Buttons.DPadUp) || pad.ThumbSticks.Left.Y > .3f;
                bool down = keys.IsKeyDown(Keys.Down) || keys.IsKeyDown(Keys.S) || pad.IsButtonDown(Buttons.DPadDown) || pad.ThumbSticks.Left.Y < -.3f;
                MoveShip((right ? 1 : 0) - (left ? 1 : 0), dt, (down ? 1 : 0) - (up ? 1 : 0));
                UpdateBeanSpawns();
                UpdateGroundPeople(dt);
                UpdateAltitudeDefense(dt);
                foreach (UfoMissile missile in missiles)
                    if (missile.BeanSpeed > 0)
                        missile.Velocity = missile.Heading * BeanMissileSpeed(missile.BeanSpeed);
                UpdateTractor(dt, tractor);
                // Resolve traveling bullets against predicted missile movement
                // before applying any surviving missile's ship impact.
                UpdateShipBullets(dt);
                UpdateMissiles(dt);
                if (!gameover)
                {
                    UpdateShipWeapon(dt, fire);
                    UpdateUfoDifficulty(dt);
                }
            }
            beamAudio?.Update(tractorActive && !gameover, false, abductees.Count > 0, false, dt);
            music?.Update(dt);
        }

        void DrawUfoSprite(int index, Rectangle destination, Color color, bool flip = false)
        {
            spriteBatch.Draw(ufoAtlas, destination, ufoSprites[index], color, 0, Vector2.Zero,
                flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
        }

        void DrawUfoLandscape()
        {
            spriteBatch.Draw(beamPixel, new Rectangle(0, 0, NATIVE_WIDTH, NATIVE_HEIGHT), new Color(7, 13, 30));
            // Two fixed skyline layers provide altitude landmarks without
            // introducing obstacles. People and projectiles draw over them.
            for (int i = 0; i < 48; i++)
                spriteBatch.Draw(beamPixel, new Rectangle(9 + (i * 73 % 270), 12 + (i * 37 % (GroundY - 32)), 1, 1),
                    i % 3 == 0 ? new Color(105, 169, 186) : new Color(36, 66, 94));
            for (int i = 0; i < 16; i++)
            {
                int left = i * 20 - 5;
                int height = 38 + i * 29 % 57;
                int roof = GroundY - height;
                spriteBatch.Draw(beamPixel, new Rectangle(left, roof, 18, height), new Color(13, 25, 42));
                spriteBatch.Draw(beamPixel, new Rectangle(left + 3, roof - 4, 10, 4), new Color(16, 30, 47));
                if (i % 3 == 0)
                    spriteBatch.Draw(beamPixel, new Rectangle(left + 8, roof - 12, 1, 8), new Color(35, 53, 69));
            }
            int[] heights = { 44, 68, 52, 80, 46, 64, 86, 44, 58 };
            for (int i = 0; i < heights.Length; i++)
            {
                int left = i * 34 - 12, width = 28, height = heights[i];
                int roof = GroundY - height;
                Color facade = i % 2 == 0 ? new Color(22, 39, 54) : new Color(25, 42, 58);
                spriteBatch.Draw(beamPixel, new Rectangle(left, roof, width, height), facade);
                spriteBatch.Draw(beamPixel, new Rectangle(left + width - 5, roof + 2, 5, height - 2), new Color(16, 30, 44));
                spriteBatch.Draw(beamPixel, new Rectangle(left - 1, roof, width + 2, 2), new Color(57, 78, 89));
                spriteBatch.Draw(beamPixel, new Rectangle(left + 5, roof - 5, 7, 5), new Color(33, 50, 65));
                // Muted window grids make the buildings read as several
                // storeys, while keeping missiles and ground units prominent.
                for (int row = 0; row < (height - 14) / 10; row++)
                    for (int column = 0; column < 3; column++)
                    {
                        bool lit = (i * 7 + row * 3 + column * 5) % 5 < 2;
                        Color window = lit ? new Color(91, 83, 57) : new Color(13, 27, 40);
                        spriteBatch.Draw(beamPixel, new Rectangle(left + 4 + column * 7, roof + 7 + row * 10, 3, 4), window);
                    }
                spriteBatch.Draw(beamPixel, new Rectangle(left + 10, GroundY - 10, 6, 10), new Color(10, 23, 33));
                if (i == 2 || i == 6)
                {
                    // Rooftop water tanks give the skyline recognisable scale.
                    spriteBatch.Draw(beamPixel, new Rectangle(left + 17, roof - 6, 1, 6), new Color(47, 67, 78));
                    spriteBatch.Draw(beamPixel, new Rectangle(left + 23, roof - 6, 1, 6), new Color(47, 67, 78));
                    spriteBatch.Draw(beamPixel, new Rectangle(left + 15, roof - 15, 11, 9), new Color(35, 55, 70));
                    spriteBatch.Draw(beamPixel, new Rectangle(left + 16, roof - 16, 9, 2), new Color(63, 82, 93));
                }
            }
            spriteBatch.Draw(beamPixel, new Rectangle(0, GroundY, NATIVE_WIDTH, 19), new Color(23, 43, 44));
            spriteBatch.Draw(beamPixel, new Rectangle(0, GroundY, NATIVE_WIDTH, 1), new Color(83, 117, 86));
        }

        void DrawUfoTitle()
        {
            DrawUfoLandscape();
            DrawUfoSprite(0, new Rectangle(150, 26, 128, 43), Color.White);
            DrawStringBitmap(spriteBatch, "WARHOOK", new Vector2(15, 14), Color.White);
            font6.Draw(spriteBatch, "UFO ABDUCTION", new Vector2(16, 28), new Color(79, 238, 239));
            font6.Draw(spriteBatch, "TAKE PEOPLE.", new Vector2(16, 47), new Color(197, 218, 229));
            font6.Draw(spriteBatch, "SAVE YOUR SHIP.", new Vector2(16, 57), new Color(197, 218, 229));
            DrawUfoSprite(8, new Rectangle(235, GroundY - 24, 17, 24), Color.White);
            DrawUfoSprite(5, new Rectangle(264, GroundY - 20, 15, 20), Color.White);
        }

        void DrawUfoPerson(GroundPerson person, Vector2 feet)
        {
            int frame = (int)(person.Animation * 7) % 4;
            DrawUfoSprite((person.Engineer ? 8 : 4) + frame,
                new Rectangle((int)feet.X - 5, (int)feet.Y - PersonHeight, 10, PersonHeight),
                person.HitFlash > 0 ? new Color(255, 135, 135) : Color.White, person.Direction < 0);
            if (person.Engineer)
                spriteBatch.Draw(beamPixel, new Rectangle((int)feet.X - 1, (int)feet.Y - PersonHeight - 3, 3, 1), new Color(255, 212, 78));
            else if (person.Health < SoldierMaxHealth)
            {
                Rectangle bar = new Rectangle((int)feet.X - 5, (int)feet.Y - PersonHeight - 3, 10, 2);
                spriteBatch.Draw(beamPixel, bar, new Color(55, 22, 30));
                bar.Width = (person.Health + 9) / 10;
                spriteBatch.Draw(beamPixel, bar, new Color(244, 142, 100));
            }
        }

        void DrawUfoMissile(UfoMissile missile, Vector2 position)
        {
            float angle = (float)Math.Atan2(missile.Velocity.X, -missile.Velocity.Y);
            Rectangle source = ufoSprites[13];
            spriteBatch.Draw(ufoAtlas, position, source, Color.White, angle,
                new Vector2(source.Width / 2f, source.Height / 2f),
                new Vector2(5f / source.Width, 12f / source.Height), SpriteEffects.None, 0);
        }

        void DrawTractor()
        {
            if (!tractorActive || gameover) return;
            Color glow = abductees.Count > 0
                ? new Color(100, 255, 205) : new Color(70, 210, 245);
            for (int yy = (int)TractorOrigin.Y; yy < GroundY; yy++)
            {
                float width = ConeHalfWidth(yy);
                float strength = .12f + .05f * (float)Math.Sin(yy * .24f + beamAnimation * 5);
                spriteBatch.Draw(beamPixel, new Rectangle((int)(shipX - width), yy, (int)(width * 2), 1), glow * strength);
                spriteBatch.Draw(beamPixel, new Rectangle((int)(shipX - width), yy, 1, 1), glow * .45f);
                spriteBatch.Draw(beamPixel, new Rectangle((int)(shipX + width), yy, 1, 1), glow * .45f);
            }
            for (int i = 0; i < 9; i++)
            {
                float yy = GroundY - (i * 19 + beamAnimation * 34) % (GroundY - TractorOrigin.Y);
                float offset = (i % 3 - 1) * ConeHalfWidth(yy) * .5f;
                spriteBatch.Draw(beamPixel, new Rectangle((int)(shipX + offset), (int)yy, 1, 3), glow * .65f);
            }
            spriteBatch.Draw(beamPixel, new Rectangle((int)(shipX - ConeHalfWidth(GroundY)), GroundY, (int)(2 * ConeHalfWidth(GroundY)), 1), glow * .8f);
        }

        void DrawUfoGameplay()
        {
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: ImpactShakeTransform());
            DrawUfoLandscape();
            DrawAltitudeLine();
            DrawTractor();
            DrawCrashFire();
            foreach (GroundPerson person in people)
            {
                int slot = abductees.IndexOf(person);
                float visualOffset = slot >= 0 && abductees.Count > 1 ? (slot - (abductees.Count - 1) / 2f) * 4 : 0;
                DrawUfoPerson(person, new Vector2(person.X + visualOffset, person.Y));
            }
            foreach (UfoMissile missile in missiles) DrawUfoMissile(missile, missile.Position);
            foreach (ShipBullet shot in shipBullets)
            {
                drawBeamStroke(shot.Position - Vector2.UnitY * 4, shot.Position, 2, new Color(35, 139, 169));
                spriteBatch.Draw(beamPixel, new Rectangle((int)shot.Position.X, (int)shot.Position.Y - 2, 1, 3), new Color(220, 255, 255));
            }
            if (!gameover)
            {
                Rectangle source = ufoSprites[0];
                Color shipColor = CriticalHullFlash ? new Color(255, 74, 74)
                    : hurtTime > 0 && (int)(hurtTime * 12) % 2 == 0
                    ? new Color(255, 120, 120) : repairTime > 0 ? new Color(145, 255, 190) : Color.White;
                spriteBatch.Draw(ufoAtlas, ShipPosition, source, shipColor, shipTilt,
                    new Vector2(source.Width / 2f, source.Height / 2f),
                    new Vector2(ShipWidth / source.Width, ShipHeight / source.Height), SpriteEffects.None, 0);
                spriteBatch.Draw(beamPixel, new Rectangle((int)shipX - 2, (int)TractorOrigin.Y, 4, 2), new Color(90, 235, 240));
                if (muzzleFlash > 0)
                {
                    drawBeamStroke(TractorOrigin, TractorOrigin + Vector2.UnitY * 8, 2, Color.White);
                    drawBeamStroke(TractorOrigin + new Vector2(-4, 3), TractorOrigin + new Vector2(4, 3), 1, Color.White);
                }
            }
            else
            {
                DrawCrashedUfo();
            }
            DrawCrashSmoke();
            drawExplosions(spriteBatch);
            DrawAltitudeWarnings();
            foreach (ScorePopup popup in scorePopups) DrawScorePopup6(spriteBatch, popup);
            if (screen != MenuScreen.Scores)
            {
                spriteBatch.Draw(beamPixel, new Rectangle(0, 0, NATIVE_WIDTH, 12), new Color(6, 12, 22));
                font6.Draw(spriteBatch, "SCORE " + score.ToString("D6"), new Vector2(8, 3), Color.White);
                font6.Draw(spriteBatch, "HULL " + shipHealth, new Vector2(119, 3), new Color(96, 230, 222));
                font6.Draw(spriteBatch, "BEST " + highScore.ToString("D6"), new Vector2(214, 3), new Color(255, 215, 128));
                spriteBatch.Draw(beamPixel, new Rectangle(0, 12, NATIVE_WIDTH, 9), new Color(6, 12, 22));
                font6.Draw(spriteBatch, "TIME " + FormatRoundTime(roundSeconds), new Vector2(8, 13), Color.White);
                DrawMultiplierMeter();
                font6.Draw(spriteBatch, RoundMultiplier.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "X",
                    new Vector2(190, 13), MultiplierBandColor((int)Math.Floor(RoundMultiplier)));
                font6.Draw(spriteBatch, "BEAM " + abductees.Count + "/" + beamCapacity, new Vector2(225, 13), new Color(96, 230, 222));
                font6.Draw(spriteBatch, "CREW " + roundSoldiers,
                    new Vector2(8, NATIVE_HEIGHT - 10), new Color(95, 245, 255));
                font6.Draw(spriteBatch, "X/SPACE FIRE Z/SHIFT BEAM", new Vector2(132, NATIVE_HEIGHT - 10), new Color(180, 210, 220));
            }
            if (gameover && screen != MenuScreen.Scores && screen != MenuScreen.RoundResults)
            {
                spriteBatch.Draw(beamPixel, new Rectangle(24, 85, 240, 63), Color.Black * .85f);
                DrawStringBitmap(spriteBatch, "SHIP DISABLED", new Vector2(92, 90), Color.White);
                if (retryMusicVisible)
                {
                    DrawModeItem("ABDUCT", selectedGame == 0, retryRow == 0 && selectedGame == 0, 40, 106);
                    DrawModeItem("SIEGE", selectedGame == 1, retryRow == 0 && selectedGame == 1, 166, 106);
                    font6.Draw(spriteBatch, "MUSIC " + retryMusic + "   UP/DOWN PICK ROW", new Vector2(48, 122), retryRow == 1 ? Color.Yellow : Color.White);
                    font6.Draw(spriteBatch, "LEFT/RIGHT CHANGE   ENTER LAUNCH", new Vector2(48, 136), Color.White);
                }
            }

            drawPauseOverlay();
            if (screen == MenuScreen.RoundResults) DrawRoundResults();
            if (screen == MenuScreen.Scores && scoresAfterGame) drawScores();
            spriteBatch.End();
        }

    }
}
