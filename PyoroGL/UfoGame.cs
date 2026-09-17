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
        const float ShipStartY = 38, ShipMinY = 26, ShipMaxY = GroundY - 58, ShipSpeed = 90;
        const float ShipWidth = 44, ShipHeight = 20, ShipSideMargin = 23;
        const float EnemyRunInSeconds = 3f, EnemyExitSpeed = 24;
        const float LiftSpeed = 30, TractorCenterSpeed = 15, BulletSpeed = 160;
        const int SoldierMaxHealth = 100, BulletDamage = SoldierMaxHealth;
        Texture2D ufoAtlas;
        readonly Rectangle[] ufoSprites = new Rectangle[16];
        readonly List<GroundPerson> people = new List<GroundPerson>();
        readonly List<UfoMissile> missiles = new List<UfoMissile>();
        readonly List<ShipBullet> shipBullets = new List<ShipBullet>();
        float shipX, shipY, previousShipX, previousShipY, shipTilt, shotCooldown, muzzleFlash;
        float hurtTime, repairTime, tractorCooldown, messageTime, beamAnimation;
        int shipHealth, weaponLevel, tractorLevel, engineLevel;
        int soldiersTowardUpgrade, soldierUpgradeTarget, upgradeSelection;
        bool upgradePending, upgradeInputReady;
        bool tractorActive, ufoShotFrozen;
        GroundPerson abductee;
        string ufoMessage = "";
        Vector2 ShipPosition => new Vector2(shipX, shipY);
        Vector2 PreviousShipPosition => new Vector2(previousShipX, previousShipY);
        Vector2 ShipCollisionHalfSize => new Vector2(19, 10);
        Vector2 TractorOrigin => new Vector2(shipX, shipY + 8);
        float ShotInterval => .8f / (1 + (weaponLevel - 1) * .25f);
        float FlightSpeed => ShipSpeed * (1 + engineLevel * .2f);
        float TractorLiftSpeed => LiftSpeed * (1 + tractorLevel * .25f);
        float TractorPullSpeed => TractorCenterSpeed * (1 + tractorLevel * .25f);

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
            // Zero is reserved for scripted smoke-check missiles.
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
            shipTilt = shotCooldown = muzzleFlash = hurtTime = repairTime = 0;
            tractorCooldown = messageTime = beamAnimation = 0;
            shipHealth = 100;
            weaponLevel = 1;
            tractorLevel = engineLevel = soldiersTowardUpgrade = upgradeSelection = 0;
            soldierUpgradeTarget = 3;
            upgradePending = upgradeInputReady = false;
            tractorActive = ufoShotFrozen = false;
            abductee = null;
            people.Clear(); missiles.Clear(); shipBullets.Clear();
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
                Engineer = beanType != 0, Direction = direction, BeanSpeed = beanSpeed
            });
        }

        int ActiveBeanSpawnCount()
        {
            int count = missiles.Count;
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

        float BeanMissileSpeed(int beanSpeed) => ((beanSpeed * bigspeed) >> 8) / 256f * targetFPS;

        void UpdateGroundPeople(float dt)
        {
            for (int i = people.Count - 1; i >= 0; i--)
            {
                GroundPerson person = people[i];
                person.PreviousX = person.X;
                person.PreviousY = person.Y;
                person.Animation += dt;
                person.HitFlash = Math.Max(0, person.HitFlash - dt);
                if (person == abductee) continue;
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

        void SetUfoMessage(string message, float seconds = 1.8f)
        {
            ufoMessage = message;
            messageTime = seconds;
        }

        void MoveShip(float direction, float dt, float verticalDirection = 0)
        {
            previousShipX = shipX;
            previousShipY = shipY;
            shipX = Math.Clamp(shipX + direction * FlightSpeed * dt, ShipSideMargin, NATIVE_WIDTH - ShipSideMargin);
            shipY = Math.Clamp(shipY + verticalDirection * FlightSpeed * .4f * dt, ShipMinY, ShipMaxY);
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

        void DamageShip()
        {
            if (gameover || hurtTime > 0) return;
            shipHealth = Math.Max(0, shipHealth - 25);
            hurtTime = 1;
            spawnExplosion(shipX, shipY + 6);
            SetUfoMessage("SHIP HIT - ENGINEERS REPAIR");
            if (shipHealth > 0) return;
            pyorodead = gameover = true;
            tractorActive = false;
            DropPayload();
            beamAudio?.StopTankAndBeamVoices();
            gameoverMusicPlaying = true;
            music?.Request(MusicTracks.Track.Gameover);
        }

        float ConeHalfWidth(float y) => MathHelper.Lerp(7, 22,
            Math.Clamp((y - TractorOrigin.Y) / (GroundY - TractorOrigin.Y), 0, 1));

        bool InsideTractor(Vector2 point) => point.Y >= TractorOrigin.Y && point.Y <= GroundY
            && Math.Abs(point.X - shipX) <= ConeHalfWidth(point.Y);

        void DropPayload()
        {
            if (abductee != null)
            {
                // Rebase the remaining approach from the beam-shifted X so
                // landing never snaps the person back to their old route.
                if (!abductee.ReachedTarget)
                {
                    float progress = Math.Min(.99999f, abductee.ApproachTime / EnemyRunInSeconds);
                    abductee.RunFromX = (abductee.X - abductee.TargetX * progress) / (1 - progress);
                }
                abductee.Falling = true;
                abductee.FallSpeed = 0;
                abductee = null;
            }
            tractorCooldown = .25f;
        }

        void DeliverPayload()
        {
            if (abductee != null)
            {
                people.Remove(abductee);
                addScore(shipX - 8, shipY + 20, abductee.Engineer ? 250 : 100);
                if (abductee.Engineer)
                {
                    shipHealth = Math.Min(100, shipHealth + 25);
                    repairTime = .7f;
                    SetUfoMessage("ENGINEER ABOARD - SHIP REPAIRED");
                    beamAudio?.PlayParachute();
                }
                else
                {
                    soldiersTowardUpgrade++;
                    SetUfoMessage("SOLDIERS " + soldiersTowardUpgrade + "/" + soldierUpgradeTarget);
                    if (soldiersTowardUpgrade >= soldierUpgradeTarget)
                    {
                        upgradePending = true;
                        upgradeInputReady = false;
                        upgradeSelection = 0;
                    }
                    beamAudio?.PlayMenuBlip();
                }
                abductee = null;
            }
            tractorCooldown = .25f;
        }

        void UpdateUpgradeChoice(bool up, bool down, bool confirm, bool acceptHeld)
        {
            // X also fires. Require a release before accepting so a held shot
            // cannot silently spend the player's choice when the panel opens.
            if (!upgradeInputReady)
            {
                if (!acceptHeld) upgradeInputReady = true;
                return;
            }
            if (up || down)
            {
                upgradeSelection = (upgradeSelection + (down ? 1 : 2)) % 3;
                beamAudio?.PlayMenuBlip();
            }
            if (confirm) ApplyUfoUpgrade();
        }

        void ApplyUfoUpgrade()
        {
            if (!upgradePending) return;
            if (upgradeSelection == 0)
            {
                float oldInterval = ShotInterval;
                weaponLevel++;
                shotCooldown *= ShotInterval / oldInterval;
                SetUfoMessage("RAPID FIRE UPGRADED");
            }
            else if (upgradeSelection == 1)
            {
                tractorLevel++;
                SetUfoMessage("TRACTOR SPEED UPGRADED");
            }
            else
            {
                engineLevel++;
                SetUfoMessage("THRUSTERS UPGRADED");
            }
            soldiersTowardUpgrade = 0;
            soldierUpgradeTarget += 2;
            upgradePending = upgradeInputReady = false;
            beamAudio?.PlayMenuBlip();
        }

        void DrawUpgradeChoice()
        {
            spriteBatch.Draw(beamPixel, new Rectangle(0, 0, NATIVE_WIDTH, NATIVE_HEIGHT), Color.Black * .8f);
            spriteBatch.Draw(beamPixel, new Rectangle(20, 26, 248, 165), new Color(12, 26, 43));
            DrawStringBitmap(spriteBatch, "CHOOSE AN UPGRADE", new Vector2(80, 36), new Color(255, 222, 128));
            string progress = soldierUpgradeTarget + " SOLDIERS RESCUED";
            font6.Draw(spriteBatch, progress, new Vector2((NATIVE_WIDTH - font6.Measure(progress).X) / 2, 51), Color.White);
            string[] titles = { "RAPID FIRE", "TRACTOR BOOST", "THRUSTERS" };
            string[] descriptions = { "25% MORE BASE FIRE RATE", "25% MORE BASE LIFT AND PULL", "20% MORE BASE FLIGHT SPEED" };
            int[] levels = { weaponLevel - 1, tractorLevel, engineLevel };
            for (int i = 0; i < 3; i++)
            {
                int yy = 68 + i * 33;
                Color color = i == upgradeSelection ? new Color(100, 245, 235) : new Color(150, 180, 200);
                spriteBatch.Draw(beamPixel, new Rectangle(30, yy, 228, 29),
                    i == upgradeSelection ? new Color(27, 69, 81) : new Color(15, 34, 52));
                if (i == upgradeSelection)
                    spriteBatch.Draw(beamPixel, new Rectangle(30, yy, 2, 29), color);
                font6.Draw(spriteBatch, titles[i] + "   RANK " + (levels[i] + 1), new Vector2(40, yy + 5), color);
                font6.Draw(spriteBatch, descriptions[i], new Vector2(40, yy + 17), Color.White);
            }
            font6.Draw(spriteBatch, "UP/DOWN PICK  ENTER/X CONFIRM", new Vector2(60, 177), Color.White);
        }

        void UpdateTractor(float dt, bool held)
        {
            tractorActive = held;
            beamAnimation += dt;
            tractorCooldown = Math.Max(0, tractorCooldown - dt);
            if (abductee != null)
            {
                Vector2 payload = new Vector2(abductee.X, abductee.Y - PersonHeight / 2f);
                if (!held || !InsideTractor(payload))
                {
                    DropPayload();
                    SetUfoMessage("PAYLOAD DROPPED", .8f);
                    return;
                }
                // People drift toward the cone centre, much slower than the
                // UFO can fly. Moving out of range still drops the payload.
                payload.X += Math.Clamp(shipX - payload.X, -TractorPullSpeed * dt, TractorPullSpeed * dt);
                payload.Y = Math.Max(TractorOrigin.Y + 3, payload.Y - TractorLiftSpeed * dt);
                if (!InsideTractor(payload)) { DropPayload(); return; }
                abductee.X = payload.X;
                abductee.Y = payload.Y + PersonHeight / 2f;
                if (payload.Y <= TractorOrigin.Y + 3) DeliverPayload();
                return;
            }
            if (!held || tractorCooldown > 0) return;

            // Only people respond to suction; rockets pass through freely.
            GroundPerson personHit = null;
            float nearestY = float.PositiveInfinity;
            foreach (GroundPerson person in people)
            {
                Vector2 center = new Vector2(person.X, person.Y - PersonHeight / 2f);
                if (InsideTractor(center) && center.Y < nearestY)
                { personHit = person; nearestY = center.Y; }
            }
            abductee = personHit;
            if (abductee != null) { abductee.Falling = false; abductee.FallSpeed = 0; }
        }

        void UpdateShipWeapon(float dt, bool fire)
        {
            shotCooldown = Math.Max(0, shotCooldown - dt);
            muzzleFlash = Math.Max(0, muzzleFlash - dt);
            if (!fire || shotCooldown > .00001f) return;
            shipBullets.Add(new ShipBullet {
                Position = TractorOrigin + new Vector2(0, 3), Velocity = Vector2.UnitY * BulletSpeed
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
                    DamageShip();
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
                    if (person == abductee) continue;
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
                    missiles.Remove(missileHit);
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

        void UpdateUfo(GameTime time, KeyboardState keys)
        {
            if (ufoShotFrozen || paused || upgradePending) return;
            float dt = (float)time.ElapsedGameTime.TotalSeconds;
            updateExplosions(); updateScorePopups();
            if (!gameover && screen == MenuScreen.Playing)
            {
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
                foreach (UfoMissile missile in missiles)
                    if (missile.BeanSpeed > 0)
                        missile.Velocity = missile.Heading * BeanMissileSpeed(missile.BeanSpeed);
                UpdateTractor(dt, tractor);
                if (upgradePending)
                {
                    beamAudio?.StopBeamVoices();
                    music?.Update(dt);
                    return;
                }
                // Resolve traveling bullets against predicted missile movement
                // before applying any surviving missile's ship impact.
                UpdateShipBullets(dt);
                UpdateMissiles(dt);
                if (!gameover)
                {
                    UpdateShipWeapon(dt, fire);
                    UpdateBeanDifficulty();
                }
            }
            beamAudio?.Update(tractorActive && !gameover, false, abductee != null, false, dt);
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
            Color glow = abductee != null
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
            spriteBatch.Draw(beamPixel, new Rectangle((int)shipX - 22, GroundY, 44, 1), glow * .8f);
        }

        void DrawUfoGameplay()
        {
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawUfoLandscape();
            DrawTractor();
            foreach (GroundPerson person in people) DrawUfoPerson(person, new Vector2(person.X, person.Y));
            foreach (UfoMissile missile in missiles) DrawUfoMissile(missile, missile.Position);
            foreach (ShipBullet shot in shipBullets)
            {
                drawBeamStroke(shot.Position - Vector2.UnitY * 4, shot.Position, 2, new Color(35, 139, 169));
                spriteBatch.Draw(beamPixel, new Rectangle((int)shot.Position.X, (int)shot.Position.Y - 2, 1, 3), new Color(220, 255, 255));
            }
            if (!gameover)
            {
                Rectangle source = ufoSprites[0];
                Color shipColor = hurtTime > 0 && (int)(hurtTime * 12) % 2 == 0
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
            drawExplosions(spriteBatch);
            foreach (ScorePopup popup in scorePopups) DrawScorePopup6(spriteBatch, popup);
            if (screen != MenuScreen.Scores)
            {
                spriteBatch.Draw(beamPixel, new Rectangle(0, 0, NATIVE_WIDTH, 12), new Color(6, 12, 22));
                font6.Draw(spriteBatch, "SCORE " + score.ToString("D6"), new Vector2(8, 3), Color.White);
                font6.Draw(spriteBatch, "HULL " + shipHealth, new Vector2(119, 3), new Color(96, 230, 222));
                font6.Draw(spriteBatch, "BEST " + highScore.ToString("D6"), new Vector2(214, 3), new Color(255, 215, 128));
                font6.Draw(spriteBatch, "CREW " + soldiersTowardUpgrade + "/" + soldierUpgradeTarget,
                    new Vector2(8, NATIVE_HEIGHT - 10), new Color(95, 245, 255));
                font6.Draw(spriteBatch, "X/SPACE FIRE Z/SHIFT BEAM", new Vector2(132, NATIVE_HEIGHT - 10), new Color(180, 210, 220));
            }
            if (messageTime > 0 && !gameover)
            {
                Vector2 size = font6.Measure(ufoMessage);
                spriteBatch.Draw(beamPixel, new Rectangle((int)(NATIVE_WIDTH - size.X) / 2 - 3, 69, (int)size.X + 6, 10), new Color(7, 13, 30) * .85f);
                font6.Draw(spriteBatch, ufoMessage, new Vector2((NATIVE_WIDTH - size.X) / 2, 71), new Color(255, 222, 128));
            }
            if (gameover && screen != MenuScreen.Scores)
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
            if (upgradePending) DrawUpgradeChoice();
            drawPauseOverlay();
            if (screen == MenuScreen.Scores && scoresAfterGame) drawScores();
            spriteBatch.End();
        }

        void VerifyBeanSpawnSchedule()
        {
            resetGame();
            if (people.Count != 0 || missiles.Count != 0)
                throw new InvalidOperationException("Round started with unscheduled enemies");
            randnum = 0x1234; randnum2 = 0x5678; randnum3 = 0x9ABC; randnum4 = 0xDEF0;
            int[] tierScores = { 0, 1000, 3000, 5000, 8000, 10000, 20000 };
            uint hash = 2166136261;
            int events = 0;
            // Reference fixture replayed from the original Game1.Update bean
            // operations over 4,200 ticks, with a free pool and these seeds.
            // Includes threshold transitions and special-bean milestones.
            for (int tick = 0; tick < 4200; tick++)
            {
                score = tierScores[tick / 600];
                UpdateBeanSpawns();
                foreach (GroundPerson person in people)
                {
                    events++;
                    foreach (int value in new[] { tick, (int)person.TargetX, person.BeanSpeed, person.Engineer ? 1 : 0 })
                        hash = unchecked((hash ^ (uint)value) * 16777619);
                }
                people.Clear();
                UpdateBeanDifficulty();
            }
            if (events != 99 || hash != 0x1689FA91 || bigspeed != 503 || time_until_new_bean != 19)
                throw new InvalidOperationException("UFO spawn schedule diverged from original bean replay");
            bigspeed = 0x100;
            if (BeanMissileSpeed(0x40) != 15 || BeanMissileSpeed(0x7F) != 29.765625f)
                throw new InvalidOperationException("Rocket lost original bean speed");
            bigspeed = 0x7F0; smallspeed = 1;
            UpdateBeanDifficulty();
            if (bigspeed != 0x7F0) throw new InvalidOperationException("Original speed cap was lost");

            foreach (int target in new[] { 24, 260 })
                foreach (int kind in new[] { 0, 1, 2 })
                {
                    resetGame();
                    SpawnBeanRunner(target, kind, 96);
                    GroundPerson runner = people[0];
                    int direction = runner.Direction;
                    if (runner.X >= -5 && runner.X <= NATIVE_WIDTH + 5)
                        throw new InvalidOperationException("Runner did not enter from offscreen");
                    UpdateGroundPeople(EnemyRunInSeconds / 2);
                    if (missiles.Count != 0 || runner.ReachedTarget)
                        throw new InvalidOperationException("Runner fired before reaching its target");
                    UpdateGroundPeople(EnemyRunInSeconds / 2 + .1f);
                    int expectedRockets = kind == 0 ? 1 : 0;
                    if (!runner.ReachedTarget || missiles.Count != expectedRockets)
                        throw new InvalidOperationException("Runner failed its single-shot assignment");
                    if (kind == 0 && (missiles[0].Position.X != target || missiles[0].Velocity.Y >= 0 || missiles[0].BeanSpeed != 96))
                        throw new InvalidOperationException("Rocket lost its assigned position or speed");
                    if (direction * (runner.X - target) <= 0 || runner.Direction != direction)
                        throw new InvalidOperationException("Runner did not continue through its firing point");
                    UpdateGroundPeople((NATIVE_WIDTH + 12) / EnemyExitSpeed + 1);
                    if (people.Count != 0 || missiles.Count != expectedRockets)
                        throw new InvalidOperationException("Runner repeated fire or failed to exit");
                }

            resetGame();
            for (int i = 0; i < max_amount_of_beans; i++) SpawnBeanRunner(20 + i * 8, 0, 64);
            int oldRandom = randnum;
            UpdateBeanSpawns();
            if (people.Count != 16 || randnum == oldRandom)
                throw new InvalidOperationException("Full bean pool changed spawn/RNG behaviour");
            UpdateGroundPeople(EnemyRunInSeconds);
            if (missiles.Count != 16 || ActiveBeanSpawnCount() != 16)
                throw new InvalidOperationException("Reserved bean slots did not transfer to rockets");
            missiles.RemoveAt(0);
            time_until_new_bean = 0;
            UpdateBeanSpawns();
            if (ActiveBeanSpawnCount() != 16 || people.Count != 17)
                throw new InvalidOperationException("Freed bean slot was not reused");
            resetGame();
        }

        void VerifyUfoFlight()
        {
            void Require(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
            }
            const float dt = 1f / 60;
            resetGame(); screen = MenuScreen.Playing;
            MoveShip(1, dt);
            Require(shipX > 144 && shipTilt > 0 && shipTilt < .18f, "Movement tilt did not ease in");
            float tilt = shipTilt;
            MoveShip(0, dt);
            Require(shipTilt > 0 && shipTilt < tilt, "Movement tilt did not ease out");
            MoveShip(-1, 10);
            Require(shipX == ShipSideMargin && shipTilt < 0, "Left flight limit / tilt failed");
            MoveShip(1, 10);
            Require(shipX == NATIVE_WIDTH - ShipSideMargin, "Right flight limit failed");

            MoveShip(0, 10, -1);
            Require(shipY == ShipMinY, "Upper flight limit failed");
            MoveShip(0, 10, 1);
            Require(shipY == ShipMaxY && previousShipY == ShipMinY, "Lower flight limit failed");
            resetGame();
            MoveShip(0, 10, 1);
            SpawnPerson(shipX, false, 1);
            UpdateTractor(dt, true);
            for (int i = 0; i < 120 && abductee != null; i++) UpdateTractor(dt, true);
            Require(abductee == null && soldiersTowardUpgrade == 1 && score == 100,
                "Low-altitude tractor failed to deliver a person");
            UpdateShipWeapon(dt, true);
            Require(shipBullets[0].Position.Y == ShipMaxY + 11,
                "Low-altitude shot did not start below the UFO");
            foreach (Keys key in new[] { Keys.Up, Keys.W, Keys.Down, Keys.S })
            {
                resetGame(); time_until_new_bean = 1000;
                UpdateUfo(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(dt)), new KeyboardState(key, Keys.X));
                bool movingUp = key == Keys.Up || key == Keys.W;
                Require(movingUp ? shipY < ShipStartY : shipY > ShipStartY, "Vertical keyboard flight input failed");
                Require(shipBullets.Count == 1 && shipBullets[0].Position.Y == TractorOrigin.Y + 3,
                    "Gun muzzle did not follow vertical ship movement");
            }
            resetGame();
            missiles.Add(new UfoMissile { Position = new Vector2(shipX, 63) });
            MoveShip(0, 1, 1);
            UpdateMissiles(1);
            Require(shipHealth == 75 && missiles.Count == 0, "Missile collision missed vertical ship movement");
            resetGame();
            missiles.Add(new UfoMissile { Position = new Vector2(shipX + 21, 100), Velocity = new Vector2(0, -100) });
            UpdateMissiles(1);
            Require(shipHealth == 100, "Shrunken UFO retained the old wider collision box");

            resetGame();
            SpawnPerson(shipX, false, 1);
            SpawnPerson(shipX + 8, true, -1);
            GroundPerson soldier = people[0];
            UpdateTractor(dt, true);
            Require(abductee == soldier, "Cone failed single-person acquisition");
            float lockedX = soldier.X;
            for (int i = 0; i < 30; i++) { UpdateGroundPeople(dt); UpdateTractor(dt, true); }
            Require(soldier.X == lockedX && soldier.Y < GroundY - 10 && people[1].Y == GroundY,
                "Tractor did not lift exactly one unit at its world X");
            MoveShip(1, .05f);
            UpdateTractor(dt, true);
            Require(abductee == soldier && soldier.X > lockedX && soldier.X < shipX,
                "Tractor did not gradually centre the unit while the ship moved");
            lockedX = soldier.X;
            MoveShip(1, .4f);
            UpdateTractor(dt, true);
            Require(abductee == null && soldier.Falling, "Leaving cone range did not drop the unit");
            float oldY = soldier.Y;
            UpdateGroundPeople(.1f);
            Require(soldier.Y > oldY && soldier.X == lockedX, "Dropped unit did not fall vertically");
            for (int i = 0; i < 180; i++) UpdateGroundPeople(dt);
            Require(!soldier.Falling && soldier.Y == GroundY && soldier.X > lockedX,
                "Dropped unit did not land and resume running");

            foreach (int side in new[] { -1, 1 })
            {
                resetGame();
                SpawnPerson(shipX + side * 18, false, 1);
                soldier = people[0];
                UpdateTractor(dt, true);
                float originalX = soldier.X;
                UpdateTractor(.5f, true);
                Require(Math.Abs(soldier.X - shipX) < Math.Abs(originalX - shipX)
                    && Math.Abs(soldier.X - originalX) <= TractorCenterSpeed * .5f + .001f
                    && Math.Abs(soldier.X - shipX) > 1, "Tractor centring snapped or pulled away from centre");
                for (int i = 0; i < 90; i++) UpdateTractor(dt, true);
                Require(abductee == soldier && Math.Abs(soldier.X - shipX) < .001f,
                    "Stationary cone failed to centre an off-axis soldier");
            }

            resetGame();
            SpawnPerson(shipX, false, 1);
            soldier = people[0];
            UpdateTractor(dt, true);
            UpdateTractor(.5f, true);
            UpdateTractor(dt, false);
            Require(abductee == null && soldier.Falling && !tractorActive, "Releasing Z did not drop payload");
            UpdateGroundPeople(.1f);
            UpdateTractor(.3f, true);
            Require(abductee == soldier && !soldier.Falling, "Falling unit could not be caught again");
            shotCooldown = .6f;
            for (int i = 0; i < 400 && abductee != null; i++) UpdateTractor(dt, true);
            Require(abductee == null && people.Count == 0 && score == 100 && weaponLevel == 1 && soldiersTowardUpgrade == 1 && !upgradePending
                && Math.Abs(shotCooldown - .6f) < .0001f, "Soldier pickup changed gun before an upgrade choice");
            shipHealth = 50;
            SpawnPerson(shipX, true, 1);
            UpdateTractor(.3f, true);
            for (int i = 0; i < 400 && abductee != null; i++) UpdateTractor(dt, true);
            Require(shipHealth == 75 && score == 350 && weaponLevel == 1 && soldiersTowardUpgrade == 1, "Engineer did not repair ship");
            SpawnPerson(shipX, true, 1);
            UpdateTractor(.3f, true);
            shipHealth = 90;
            for (int i = 0; i < 400 && abductee != null; i++) UpdateTractor(dt, true);
            Require(shipHealth == 100, "Repair exceeded full health");

            // Beam holds reserve the original spawn slot until delivery.
            resetGame();
            SpawnBeanRunner(144, 0, 64);
            UpdateGroundPeople(1.5f);
            shipX = previousShipX = people[0].X;
            UpdateTractor(dt, true);
            Require(abductee != null && ActiveBeanSpawnCount() == 1, "Capture lost approaching bean slot");
            shipX += 6;
            UpdateTractor(.5f, true); UpdateTractor(dt, false);
            float shiftedX = people[0].X;
            for (int i = 0; i < 45 && people[0].Falling; i++) UpdateGroundPeople(dt);
            UpdateGroundPeople(0);
            Require(Math.Abs(people[0].X - shiftedX) < .001f, "Dropped runner snapped back to its old X");
            UpdateGroundPeople(1.5f);
            Require(missiles.Count == 1, "Dropped approaching soldier failed its original shot assignment");

            foreach (float target in new[] { 50f, 230f })
            {
                resetGame(); shipX = previousShipX = target;
                shipY = previousShipY = target < 144 ? ShipMinY : ShipMaxY;
                LaunchMissile(new GroundPerson { TargetX = 144, BeanSpeed = 96 });
                UfoMissile rocket = missiles[0];
                Vector2 expected = Vector2.Normalize(ShipPosition - rocket.Position);
                Require(Vector2.Distance(rocket.Heading, expected) < .0001f && rocket.Velocity.Y < 0,
                    "Missile did not aim at the launch-time UFO position");
                shipX = previousShipX = 288 - target;
                UpdateMissiles(.1f);
                Require(Vector2.Distance(Vector2.Normalize(rocket.Velocity), expected) < .0001f,
                    "Missile unexpectedly homed after launch");
            }
            resetGame();
            var fastRocket = new UfoMissile { Position = new Vector2(shipX, 110), Velocity = new Vector2(0, -800) };
            missiles.Add(fastRocket);
            UpdateTractor(.2f, true);
            Require(abductee == null && missiles.Count == 1 && fastRocket.Position.Y == 110,
                "Empty tractor cone still affected a rocket");
            UpdateMissiles(.2f);
            Require(shipHealth == 75 && missiles.Count == 0 && score == 0,
                "Tractor prevented rocket damage or awarded disarm points");
            resetGame();
            SpawnPerson(shipX, false, 1);
            missiles.Add(new UfoMissile { Position = new Vector2(shipX, 120), Velocity = new Vector2(0, -30) });
            UpdateTractor(dt, true); UpdateMissiles(.5f);
            Require(abductee == people[0] && missiles[0].Position.Y == 105,
                "Rocket blocked person capture or stopped moving inside the tractor");
            resetGame();
            for (int hit = 0; hit < 4; hit++)
            {
                hurtTime = 0;
                missiles.Add(new UfoMissile { Position = new Vector2(shipX, 100), Velocity = new Vector2(0, -500) });
                UpdateMissiles(.2f);
                Require(shipHealth == 75 - hit * 25, "Swept rocket hit failed to damage ship");
                if (hit == 0) { DamageShip(); Require(shipHealth == 75, "Hit grace period failed"); }
            }
            Require(gameover && pyorodead, "Empty hull health did not end the round");

            resetGame();
            UpdateShipWeapon(1, false);
            Require(shipBullets.Count == 0, "UFO fired without X");
            UpdateShipWeapon(dt, true);
            Require(shipBullets.Count == 1 && shipBullets[0].Velocity == Vector2.UnitY * BulletSpeed,
                "X failed to fire downward");
            for (int i = 0; i < 46; i++) UpdateShipWeapon(dt, true);
            Require(shipBullets.Count == 1, "Base fire rate was too fast");
            UpdateShipWeapon(.05f, true);
            Require(shipBullets.Count == 2, "Held X did not repeat slowly");
            shipBullets.Clear();
            SpawnPerson(shipX, false, 1);
            SpawnPerson(shipX, true, 1);
            soldier = people[0];
            shipBullets.Add(new ShipBullet { Position = new Vector2(shipX, GroundY - 30), Velocity = Vector2.UnitY * BulletSpeed });
            UpdateShipBullets(.3f);
            Require(soldier.Health == 0 && shipBullets.Count == 0 && score == 50,
                "One UFO bullet did not instantly kill the soldier for 50 points");
            Require(people.Count == 1 && people[0].Engineer && people[0].Health == 100,
                "Bullet pierced its first target");
            shipBullets.Add(new ShipBullet { Position = new Vector2(shipX, GroundY - 30), Velocity = Vector2.UnitY * BulletSpeed });
            UpdateShipBullets(.3f);
            Require(people.Count == 0 && score == 100 && shipBullets.Count == 0,
                "Engineer did not die from one bullet");

            // Opposing fast projectiles cross between frames; the bullet
            // must intercept before the rocket can damage the UFO.
            resetGame();
            missiles.Add(new UfoMissile { Position = new Vector2(shipX, 130), Velocity = new Vector2(0, -500) });
            shipBullets.Add(new ShipBullet { Position = new Vector2(shipX, 60), Velocity = Vector2.UnitY * BulletSpeed });
            UpdateShipBullets(.2f); UpdateMissiles(.2f);
            Require(missiles.Count == 0 && shipBullets.Count == 0 && shipHealth == 100 && score == 50,
                "Bullet failed to destroy a crossing missile before ship impact");

            // Consume the shot on its first target, regardless of list order.
            resetGame();
            SpawnPerson(shipX, false, 1);
            var fartherRocket = new UfoMissile { Position = new Vector2(shipX, 140) };
            missiles.Add(fartherRocket);
            missiles.Add(new UfoMissile { Position = new Vector2(shipX, 95) });
            shipBullets.Add(new ShipBullet { Position = new Vector2(shipX, 60), Velocity = Vector2.UnitY * BulletSpeed });
            UpdateShipBullets(1);
            Require(missiles.Count == 1 && missiles[0] == fartherRocket && people.Count == 1
                && shipBullets.Count == 0 && score == 50, "Bullet pierced a missile or chose the wrong first target");
            missiles.Clear();
            abductee = people[0];
            shipBullets.Add(new ShipBullet { Position = new Vector2(shipX, 60), Velocity = Vector2.UnitY * BulletSpeed });
            UpdateShipBullets(1);
            Require(missiles.Count == 0 && people.Count == 1 && score == 50,
                "Friendly bullet destroyed a held person");

            resetGame();
            missiles.Add(new UfoMissile { Position = new Vector2(shipX + 16, 80), Velocity = new Vector2(0, -300) });
            shipBullets.Add(new ShipBullet { Position = new Vector2(shipX + 16, 20), Velocity = Vector2.UnitY * BulletSpeed });
            UpdateShipBullets(.2f); UpdateMissiles(.2f);
            Require(shipHealth == 75 && score == 0, "Late interception incorrectly prevented an earlier ship impact");

            foreach (Keys shift in new[] { Keys.LeftShift, Keys.RightShift })
            {
                resetGame(); time_until_new_bean = 1000;
                UpdateUfo(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(dt)), new KeyboardState(shift, Keys.Space));
                Require(tractorActive && shipBullets.Count == 1, "Space/Shift fallback controls failed");
            }

            resetGame(); time_until_new_bean = 1000;
            SpawnPerson(shipX, false, 1);
            var tickTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(dt));
            UpdateUfo(tickTime, new KeyboardState(Keys.Y));
            Require(!tractorActive && abductee == null, "Old keyboard Y binding still activates the tractor");
            UpdateUfo(tickTime, new KeyboardState(Keys.Z, Keys.X, Keys.Right));
            Require(tractorActive && abductee != null && shipBullets.Count == 1 && shipX > 144,
                "Simultaneous movement / X / Z input failed");
            paused = true;
            float savedX = shipX, savedY = abductee.Y, savedCooldown = shotCooldown;
            UpdateUfo(tickTime, new KeyboardState(Keys.X, Keys.Z, Keys.Right));
            Require(shipX == savedX && abductee.Y == savedY && shotCooldown == savedCooldown, "Pause advanced flight");
            paused = false; gameover = true;
            UpdateUfo(tickTime, new KeyboardState(Keys.X, Keys.Z, Keys.Right));
            Require(shipX == savedX && shotCooldown == savedCooldown, "Game over advanced flight");
            resetGame();
            Require(shipHealth == 100 && weaponLevel == 1 && shipTilt == 0 && shipY == ShipStartY
                && previousShipY == ShipStartY && !tractorActive
                && abductee == null && shipBullets.Count == 0,
                "Round reset retained flight state");
        }

        void VerifyUpgradeRewards()
        {
            void Require(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
            }
            void DeliverSoldier()
            {
                SpawnPerson(shipX, false, 1);
                abductee = people[people.Count - 1];
                DeliverPayload();
            }
            resetGame(); screen = MenuScreen.Playing; transition = MenuTransition.None;
            DeliverSoldier();
            Require(soldiersTowardUpgrade == 1 && weaponLevel == 1 && !upgradePending,
                "A single soldier still granted an automatic gun upgrade");
            shipHealth = 50;
            SpawnPerson(shipX, true, 1); abductee = people[0]; DeliverPayload();
            Require(shipHealth == 75 && soldiersTowardUpgrade == 1 && !upgradePending,
                "Engineer delivery affected soldier upgrade progress");
            DeliverSoldier(); DeliverSoldier();
            Require(upgradePending && soldiersTowardUpgrade == 3 && weaponLevel == 1,
                "Three soldiers failed to open an unspent upgrade choice");
            var tick = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1f / 60));
            missiles.Add(new UfoMissile { Position = new Vector2(40, 140), Velocity = new Vector2(0, -30) });
            shipBullets.Add(new ShipBullet { Position = new Vector2(90, 80), Velocity = Vector2.UnitY * BulletSpeed });
            float savedShipX = shipX;
            int savedTimer = time_until_new_bean, savedSpeed = bigspeed;
            UpdateUfo(tick, new KeyboardState(Keys.X, Keys.Z, Keys.Right));
            Require(shipX == savedShipX && time_until_new_bean == savedTimer && bigspeed == savedSpeed
                && missiles[0].Position.Y == 140 && shipBullets[0].Position.Y == 80,
                "Upgrade choice did not freeze gameplay");
            Require(updateMenus(tick, new KeyboardState(Keys.X)) && upgradePending && !upgradeInputReady,
                "Held fire automatically selected an upgrade");
            updateMenus(tick, new KeyboardState());
            updateMenus(tick, new KeyboardState(Keys.Down));
            Require(upgradeSelection == 1, "Upgrade menu navigation failed");
            Require(updateMenus(tick, new KeyboardState(Keys.Enter)), "Upgrade confirm did not consume input");
            Require(!upgradePending && tractorLevel == 1 && weaponLevel == 1 && engineLevel == 0
                && soldiersTowardUpgrade == 0 && soldierUpgradeTarget == 5
                && TractorLiftSpeed == 37.5f && TractorPullSpeed == 18.75f,
                "Tractor upgrade or next soldier target was incorrect");

            for (int i = 0; i < 4; i++) DeliverSoldier();
            Require(!upgradePending && soldiersTowardUpgrade == 4, "Second target triggered early");
            DeliverSoldier();
            Require(upgradePending, "Second soldier target failed to open choices");
            shotCooldown = .6f;
            float intervalBefore = ShotInterval;
            upgradeSelection = 0; ApplyUfoUpgrade();
            Require(weaponLevel == 2 && tractorLevel == 1 && engineLevel == 0 && soldierUpgradeTarget == 7
                && Math.Abs(ShotInterval - .64f) < .0001f
                && Math.Abs(shotCooldown / ShotInterval - .6f / intervalBefore) < .0001f,
                "Rapid fire upgrade lost reload progress or modified another upgrade");
            for (int i = 0; i < 7; i++) DeliverSoldier();
            Require(upgradePending, "Third soldier target failed to open choices");
            upgradeSelection = 2; ApplyUfoUpgrade();
            Require(engineLevel == 1 && Math.Abs(FlightSpeed - 108) < .001f && soldierUpgradeTarget == 9,
                "Thruster upgrade did not increase ship speed");
            float beforeMove = shipX;
            MoveShip(1, .1f);
            Require(Math.Abs(shipX - beforeMove - 10.8f) < .001f, "Flight did not use the thruster upgrade");
            ApplyUfoUpgrade();
            Require(engineLevel == 1 && soldierUpgradeTarget == 9, "Upgrade applied twice from one reward");
            resetGame();
            Require(!upgradePending && !upgradeInputReady && soldiersTowardUpgrade == 0 && soldierUpgradeTarget == 3
                && weaponLevel == 1 && tractorLevel == 0 && engineLevel == 0,
                "Round reset retained reward progress or chosen upgrades");
        }

        void UpdateShots(GameTime time)
        {
            shotTimer += time.ElapsedGameTime.TotalSeconds;
            if (shotStage == 0 && shotTimer > 1)
            {
                SaveScreenshot();
                resetGame(); screen = MenuScreen.Playing;
                NextShotStage();
            }
            else if (shotStage == 1 && shotTimer > 1)
            {
                VerifyBeanSpawnSchedule();
                VerifyUfoFlight();
                VerifyUpgradeRewards();
                Console.WriteLine("UFO flight checks passed: eased tilt, horizontal/vertical controls and bounds, smaller collision box, moving gun and missile aim; X fire, one-hit kills, missile destruction and shootable engineers; soldier targets and selectable upgrades; one-unit Z cone, gradual horizontal centring, movement, drop, landing, recapture, delivery and repairs; aimed missiles, rocket suction immunity, damage and game over; pause/reset; original bean replay (99 events / 4200 ticks), speed ramp, pool and one-shot runners.");
                resetGame();
                shipX = previousShipX = 146; shipY = previousShipY = ShipMaxY - 16;
                shipTilt = .13f; shipHealth = 75; weaponLevel = 3;
                SpawnPerson(47, false, 1); SpawnPerson(90, true, 1);
                SpawnPerson(143, false, -1); SpawnPerson(202, false, -1); SpawnPerson(248, false, -1);
                abductee = people[2]; abductee.Y = 169;
                tractorActive = true; beamAnimation = 1.2f;
                LaunchMissile(new GroundPerson { TargetX = 50, BeanSpeed = 96 });
                missiles[0].Position = new Vector2(83, 163);
                LaunchMissile(new GroundPerson { TargetX = 240, BeanSpeed = 96 });
                missiles[1].Position = new Vector2(212, 154);
                shipBullets.Add(new ShipBullet { Position = new Vector2(160, 168), Velocity = Vector2.UnitY * BulletSpeed });
                shipBullets.Add(new ShipBullet { Position = new Vector2(148, 148), Velocity = Vector2.UnitY * BulletSpeed });
                messageTime = 0;
                ufoShotFrozen = true;
                NextShotStage();
            }
            else if (shotStage == 2 && shotTimer > .6)
            {
                SaveScreenshot();
                upgradePending = true;
                soldiersTowardUpgrade = soldierUpgradeTarget;
                upgradeSelection = 1;
                NextShotStage();
            }
            else if (shotStage == 3 && shotTimer > .8)
            {
                if (!upgradePending) throw new InvalidOperationException("Upgrade choice closed without player input");
                SaveScreenshot();
                ufoShotFrozen = false;
                transition = MenuTransition.None;
                selectedGame = 1; resetGame(); shipHealth = 25; DamageShip();
                NextShotStage();
            }
            else if (shotStage == 4 && shotTimer > 2.5)
            {
                SaveScreenshot(); NextShotStage();
            }
            else if (shotStage == 5 && shotTimer > .5) Exit();
        }
    }
}
