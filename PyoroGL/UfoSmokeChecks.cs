using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
namespace MonogameTest
{
    public partial class Game1
    {
        GroundPerson SmokeAbductee
        {
            get => abductees.Count > 0 ? abductees[0] : null;
            set { abductees.Clear(); if (value != null) abductees.Add(value); }
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
            for (int i = 0; i < 120 && SmokeAbductee != null; i++) UpdateTractor(dt, true);
            Require(SmokeAbductee == null && roundSoldiers == 1 && score == 100,
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
            Require(SmokeAbductee == soldier, "Cone failed single-person acquisition");
            float lockedX = soldier.X;
            for (int i = 0; i < 30; i++) { UpdateGroundPeople(dt); UpdateTractor(dt, true); }
            Require(soldier.X == lockedX && soldier.Y < GroundY - 10 && people[1].Y == GroundY,
                "Tractor did not lift exactly one unit at its world X");
            MoveShip(1, .05f);
            UpdateTractor(dt, true);
            Require(SmokeAbductee == soldier && soldier.X > lockedX && soldier.X < shipX,
                "Tractor did not gradually centre the unit while the ship moved");
            lockedX = soldier.X;
            MoveShip(1, .4f);
            UpdateTractor(dt, true);
            Require(SmokeAbductee == null && soldier.Falling, "Leaving cone range did not drop the unit");
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
                Require(SmokeAbductee == soldier && Math.Abs(soldier.X - shipX) < .001f,
                    "Stationary cone failed to centre an off-axis soldier");
            }

            resetGame();
            SpawnPerson(shipX, false, 1);
            soldier = people[0];
            UpdateTractor(dt, true);
            UpdateTractor(.5f, true);
            UpdateTractor(dt, false);
            Require(SmokeAbductee == null && soldier.Falling && !tractorActive, "Releasing Z did not drop payload");
            UpdateGroundPeople(.1f);
            UpdateTractor(.3f, true);
            Require(SmokeAbductee == soldier && !soldier.Falling, "Falling unit could not be caught again");
            shotCooldown = .6f;
            for (int i = 0; i < 400 && SmokeAbductee != null; i++) UpdateTractor(dt, true);
            Require(SmokeAbductee == null && people.Count == 0 && score == 100 && weaponLevel == 1 && roundSoldiers == 1
                && Math.Abs(shotCooldown - .6f) < .0001f, "Soldier pickup changed gun before an upgrade choice");
            shipHealth = 50;
            SpawnPerson(shipX, true, 1);
            UpdateTractor(.3f, true);
            for (int i = 0; i < 400 && SmokeAbductee != null; i++) UpdateTractor(dt, true);
            Require(shipHealth == 75 && score == 350 && weaponLevel == 1 && roundSoldiers == 1, "Engineer did not repair ship");
            SpawnPerson(shipX, true, 1);
            UpdateTractor(.3f, true);
            shipHealth = 90;
            for (int i = 0; i < 400 && SmokeAbductee != null; i++) UpdateTractor(dt, true);
            Require(shipHealth == 100, "Repair exceeded full health");

            // Beam holds reserve the original spawn slot until delivery.
            resetGame();
            SpawnBeanRunner(144, 0, 64);
            UpdateGroundPeople(1.5f);
            shipX = previousShipX = people[0].X;
            UpdateTractor(dt, true);
            Require(SmokeAbductee != null && ActiveBeanSpawnCount() == 1, "Capture lost approaching bean slot");
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
            Require(SmokeAbductee == null && missiles.Count == 1 && fastRocket.Position.Y == 110,
                "Empty tractor cone still affected a rocket");
            UpdateMissiles(.2f);
            Require(shipHealth == 75 && missiles.Count == 0 && score == 0,
                "Tractor prevented rocket damage or awarded disarm points");
            resetGame();
            SpawnPerson(shipX, false, 1);
            missiles.Add(new UfoMissile { Position = new Vector2(shipX, 120), Velocity = new Vector2(0, -30) });
            UpdateTractor(dt, true); UpdateMissiles(.5f);
            Require(SmokeAbductee == people[0] && missiles[0].Position.Y == 105,
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
            SmokeAbductee = people[0];
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
            Require(!tractorActive && SmokeAbductee == null, "Old keyboard Y binding still activates the tractor");
            UpdateUfo(tickTime, new KeyboardState(Keys.Z, Keys.X, Keys.Right));
            Require(tractorActive && SmokeAbductee != null && shipBullets.Count == 1 && shipX > 144,
                "Simultaneous movement / X / Z input failed");
            paused = true;
            float savedX = shipX, savedY = SmokeAbductee.Y, savedCooldown = shotCooldown;
            UpdateUfo(tickTime, new KeyboardState(Keys.X, Keys.Z, Keys.Right));
            Require(shipX == savedX && SmokeAbductee.Y == savedY && shotCooldown == savedCooldown, "Pause advanced flight");
            paused = false; gameover = true;
            UpdateUfo(tickTime, new KeyboardState(Keys.X, Keys.Z, Keys.Right));
            Require(shipX == savedX && shotCooldown == savedCooldown, "Game over advanced flight");
            resetGame();
            Require(shipHealth == 100 && weaponLevel == 1 && shipTilt == 0 && shipY == ShipStartY
                && previousShipY == ShipStartY && !tractorActive
                && SmokeAbductee == null && shipBullets.Count == 0,
                "Round reset retained flight state");
        }

        void VerifyUpgradeRewards()
        {
            void Require(bool condition, string message)
            { if (!condition) throw new InvalidOperationException(message); }
            progression = new UfoProgression(true);
            resetGame(); screen = MenuScreen.Playing; transition = MenuTransition.None;
            for (int i = 0; i < 4; i++)
            {
                SpawnPerson(shipX, false, 1);
                DeliverPerson(people[people.Count - 1]);
            }
            Require(roundSoldiers == 4 && weaponLevel == 1 && screen == MenuScreen.Playing,
                "Soldier pickups must accumulate without interrupting the round");
            shipHealth = 50;
            SpawnPerson(shipX, true, 1); DeliverPerson(people[0]);
            Require(shipHealth == 75 && roundSoldiers == 4, "Engineer affected currency");
            Require(RoundMultiplier == 1, "Multiplier must start at one");
            roundSeconds = 60;
            Require(Math.Abs(RoundMultiplier - 1.1) < .000001 && RoundReward == 4.4, "Survival reward growth failed");
            paused = true;
            UpdateUfo(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1)), new KeyboardState());
            Require(roundSeconds == 60, "Paused time increased rewards");
            EndIncrementalRound();
            Require(screen == MenuScreen.Upgrades && roundBanked && progression.Balance == 4.4,
                "Round end did not bank fractional rewards and open the map");
            EndIncrementalRound();
            Require(progression.Balance == 4.4, "Round paid out twice");
            Require(!progression.Buy(4) && progression.Buy(0) && progression.Balance == 1.4,
                "Purchase cost or prerequisite failed");
            Require(!progression.Buy(0), "Upgrade allowed overspending");
            resetGame();
            Require(weaponLevel == 2 && Math.Abs(ShotInterval - .64f) < .0001f && roundSoldiers == 0
                && roundSeconds == 0 && progression.Balance == 1.4, "Permanent upgrades failed across rounds");
            progression.Bank("smoke-funds", 10000);
            foreach (int node in new[] { 0, 1, 1, 2, 3, 4, 5, 8 }) Require(progression.Buy(node), "Tree unlock failed");
            resetGame(); screen = MenuScreen.Playing;
            Require(beamCapacity == 2 && shipHealth == 125 && Math.Abs(FlightSpeed - 108) < .001f && TractorLiftSpeed == 45,
                "Purchased flight stats failed");
            roundSeconds = 60;
            Require(Math.Abs(RoundMultiplier - 1.125) < .000001, "Multiplier growth upgrade failed");
            for (int i = 0; i < 3; i++) SpawnPerson(shipX + i * 3, false, 1);
            UpdateTractor(.3f, true); UpdateTractor(.3f, true); UpdateTractor(.3f, true);
            Require(abductees.Count == 2 && people[2].Y == GroundY, "Multi-person beam capacity failed");
            GroundPerson first = abductees[0], second = abductees[1];
            first.X = shipX + 100; tractorCooldown = .25f;
            UpdateTractor(.01f, true);
            Require(first.Falling && abductees.Count == 1 && abductees[0] == second, "Individual beam drop failed");
            DeliverPerson(second);
            Require(roundSoldiers == 1 && abductees.Count == 0, "Multi-person delivery failed");
            UpdateTractor(.3f, true); UpdateTractor(.1f, false);
            Require(abductees.Count == 0 && people[1].Falling, "Beam release failed");
            progression.Bank("smoke-complete-tree", 100000);
            for (int i = 0; i < UfoProgression.Nodes.Length; i++)
            {
                while (progression.Rank(i) < UfoProgression.Nodes[i].MaxRank)
                    Require(progression.Buy(i), "Deep upgrade node could not be purchased");
                double balance = progression.Balance;
                Require(!progression.Buy(i) && progression.Balance == balance, "Max-rank node charged currency");
            }
            resetGame();
            Require(beamCapacity == 5 && MaxShipHealth == 350 && RoundMultiplier == 2,
                "Full tree capacity, hull, or starting multiplier failed");
            shipHealth = 100;
            SpawnPerson(shipX, true, 1); DeliverPerson(people[0]);
            Require(shipHealth == 150 && roundSoldiers == 0, "Engineer repair upgrade failed");
#if !WEB
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "warhook-progression-" + Guid.NewGuid() + ".json");
            try
            {
                var saved = new UfoProgression(false, path);
                Require(saved.Bank("persistent-round", 16.25) && saved.Buy(1), "Progression save failed");
                var loaded = new UfoProgression(false, path);
                Require(loaded.Balance == 13.25 && loaded.Rank(1) == 1 && loaded.Bank("persistent-round", 16.25)
                    && loaded.Balance == 13.25, "Progression reload or payout idempotency failed");
                var failed = new UfoProgression(false, path + "/invalid.json");
                Require(!failed.Bank("failure", 100) && failed.Balance == 0, "Failed save committed a reward");
            }
            finally { System.IO.File.Delete(path); }
#endif
            progression = new UfoProgression(true);
            resetGame();
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
                Console.WriteLine("UFO flight checks passed: eased tilt, horizontal/vertical controls and bounds, smaller collision box, moving gun and missile aim; X fire, one-hit kills, missile destruction and shootable engineers; persistent soldier currency, survival multiplier, upgrade tree purchases and multi-person beams; one-unit Z cone, gradual horizontal centring, movement, drop, landing, recapture, delivery and repairs; aimed missiles, rocket suction immunity, damage and game over; pause/reset; original bean replay (99 events / 4200 ticks), speed ramp, pool and one-shot runners.");
                resetGame();
                shipX = previousShipX = 146; shipY = previousShipY = ShipMaxY - 16;
                shipTilt = .13f; shipHealth = 75; weaponLevel = 3;
                SpawnPerson(47, false, 1); SpawnPerson(90, true, 1);
                SpawnPerson(143, false, -1); SpawnPerson(202, false, -1); SpawnPerson(248, false, -1);
                SmokeAbductee = people[2]; SmokeAbductee.Y = 169;
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
                roundSoldiers = 12; roundSeconds = 180;
                EndIncrementalRound();
                NextShotStage();
            }
            else if (shotStage == 3 && shotTimer > .8)
            {
                if (screen != MenuScreen.Upgrades) throw new InvalidOperationException("Upgrade map closed without input");
                SaveScreenshot();
                ufoShotFrozen = false;
                transition = MenuTransition.None;
                mapSelection = 17; FocusUpgradeNode();
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
