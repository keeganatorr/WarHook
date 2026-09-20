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
            repairLevel = 1; // Replay original special beans with engineers unlocked.
            // Isolate the original arithmetic from the UFO's harder opening.
            bigspeed = 0x100; smallspeed = 0xFF; max_time = 0xB4;
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
            if (events != 99 || hash != 0x6BA769F5 || bigspeed != 503 || time_until_new_bean != 19)
                throw new InvalidOperationException("UFO spawn schedule diverged from original bean replay");
            bigspeed = missileDifficultySpeed = 0x100;
            if (BeanMissileSpeed(0x40) != 15 || BeanMissileSpeed(0x7F) != 29.765625f)
                throw new InvalidOperationException("Rocket lost original bean speed");
            bigspeed = 0x7F0; smallspeed = 1;
            UpdateBeanDifficulty();
            if (bigspeed != 0x7F0) throw new InvalidOperationException("Original speed cap was lost");

            foreach (int target in new[] { 24, 260 })
                foreach (int kind in new[] { 0, 1, 2 })
                {
                    resetGame();
                    repairLevel = 1;
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

        void VerifyUfoDifficulty()
        {
            void Require(bool condition, string message)
            { if (!condition) throw new InvalidOperationException(message); }
            resetGame(); screen = MenuScreen.Playing;
            Require(BeanMissileSpeed(64) == 22.5f && max_time == 120,
                "Harder opening speed/spawn interval was lost");
            randnum = 0;
            UpdateBeanSpawns();
            Require(time_until_new_bean >= 14 && time_until_new_bean <= 19,
                "Opening spawn cadence is not 0.25 to 0.33 seconds");
            people.Clear();
            UpdateUfoDifficulty(16);
            long ordinaryGain = bigspeed - StartingUfoSpeed;
            Require(ordinaryGain == 60 && max_time == 120 && missileDifficultySpeed == StartingMissileSpeed + 60, "Base ramp or harder interval failed");
            resetGame();
            repairLevel = 1;
            for (int i = 0; i < 10; i++)
            {
                SpawnPerson(shipX, i == 9, 1);
                DeliverPerson(people[people.Count - 1]);
            }
            Require(roundAbductions == 10 && roundSoldiers == 9,
                "Soldiers and engineers must accelerate difficulty; only soldiers earn currency");
            UpdateUfoDifficulty(16);
            Require(bigspeed - StartingUfoSpeed == ordinaryGain * 3.5
                && missileDifficultySpeed - StartingMissileSpeed == ordinaryGain * 3.5,
                "Ten abductions did not produce the 3.5x difficulty ramp");
            Require(roundSeconds == 0 && RoundMultiplier == 1 && RoundReward == 9,
                "Difficulty clock advanced survival rewards");
            long before = bigspeed; int spawnTimer = time_until_new_bean;
            paused = true;
            UpdateUfo(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1)), new KeyboardState());
            Require(bigspeed == before && time_until_new_bean == spawnTimer && roundSeconds == 0,
                "Pause advanced difficulty");
            resetGame();
            Require(roundAbductions == 0 && difficultyTickRemainder == 0 && bigspeed == StartingUfoSpeed,
                "New round retained accelerated difficulty");
            // Fractional rates must not round away at 60 Hz.
            roundAbductions = 1;
            for (int i = 0; i < 960; i++) UpdateUfoDifficulty(1f / 60);
            Require(bigspeed - StartingUfoSpeed == 75, "Fractional difficulty ticks were lost");
            score = 10000;
            UpdateUfoDifficulty(1);
            Require(max_time == 0x32, "Score-based spawn tiers stopped working");
            bigspeed = 0x7F0; smallspeed = 1;
            UpdateUfoDifficulty(1);
            Require(bigspeed > 0x7F0, "UFO spawn difficulty stopped at the original speed cap");
            long previousMissileSpeed = missileDifficultySpeed;
            UpdateUfoDifficulty(16);
            Require(missileDifficultySpeed > previousMissileSpeed,
                "Missile ramp stopped after spawn difficulty passed its original cap");
            missileDifficultySpeed = 0x7F0;
            UpdateUfoDifficulty(1);
            Require(missileDifficultySpeed > 0x7F0, "UFO missile speed stopped at the original cap");
            resetGame();
            Require(Math.Abs(UfoDifficultyLevel - 1) < .001, "New flight did not start at difficulty level 1.0");
            missileDifficultySpeed = StartingMissileSpeed + MissileSpeedPerDifficultyLevel - 1;
            Require(UfoDifficultyLevel > 1.9 && UfoDifficultyLevel < 2,
                "Difficulty decimal did not track progress within its speed band");
            missileDifficultySpeed++;
            Require(Math.Abs(UfoDifficultyLevel - 2) < .001, "Difficulty level did not advance with missile speed");
            missileDifficultySpeed = 0x7F0;
            Require(UfoDifficultyLevel > 13.8, "Difficulty level did not advance beyond the original cap");
            missileDifficultySpeed = 1_000_000;
            Require(UfoDifficultyLevel > 7_800, "Difficulty level display imposed a fixed maximum");
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
            Require(ShipMaxY == GroundY - 38, "Lower flight limit was not moved toward the rooftops");
            MoveShip(1, dt);
            Require(shipX > NATIVE_WIDTH / 2f && shipTilt > 0 && shipTilt < .18f, "Movement tilt did not ease in");
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
            Require(SmokeAbductee == null && roundSoldiers == 1 && score == 0,
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
            beamCapacity = 2;
            SpawnPerson(shipX, false, 1);
            SpawnPerson(shipX + 8, false, -1);
            UpdateTractor(dt, true);
            Require(abductees.Count == 1, "Tractor acquired more than one person at once");
            UpdateTractor(TractorAcquireCooldown, true);
            Require(abductees.Count == 2, "Tractor handoff delay was not nearly instant");

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
            Require(SmokeAbductee == null && people.Count == 0 && score == 0 && weaponLevel == 1 && roundSoldiers == 1
                && Math.Abs(shotCooldown - .6f) < .0001f, "Soldier pickup changed gun before an upgrade choice");
            shipHealth = 50; repairLevel = 1;
            SpawnPerson(shipX, true, 1);
            UpdateTractor(.3f, true);
            for (int i = 0; i < 400 && SmokeAbductee != null; i++) UpdateTractor(dt, true);
            Require(shipHealth == 55 && score == 0 && weaponLevel == 1 && roundSoldiers == 1, "Engineer did not repair ship");
            SpawnPerson(shipX, true, 1);
            UpdateTractor(.3f, true);
            shipHealth = 98;
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
                shipY = previousShipY = target < NATIVE_WIDTH / 2f ? ShipMinY : ShipMaxY;
                LaunchMissile(new GroundPerson { TargetX = 144, BeanSpeed = 96 });
                UfoMissile rocket = missiles[0];
                Vector2 expected = Vector2.Normalize(ShipPosition - rocket.Position);
                Require(Vector2.Distance(rocket.Heading, expected) < .0001f && rocket.Velocity.Y < 0,
                    "Missile did not aim at the launch-time UFO position");
                shipX = previousShipX = NATIVE_WIDTH - target;
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
            Require(soldier.Health == 0 && shipBullets.Count == 0 && score == 0,
                "One UFO bullet did not instantly kill the soldier");
            Require(people.Count == 1 && people[0].Engineer && people[0].Health == 100,
                "Bullet pierced its first target");
            shipBullets.Add(new ShipBullet { Position = new Vector2(shipX, GroundY - 30), Velocity = Vector2.UnitY * BulletSpeed });
            UpdateShipBullets(.3f);
            Require(people.Count == 0 && score == 0 && shipBullets.Count == 0,
                "Engineer did not die from one bullet");

            // Opposing fast projectiles cross between frames; the bullet
            // must intercept before the rocket can damage the UFO.
            resetGame();
            missiles.Add(new UfoMissile { Position = new Vector2(shipX, 130), Velocity = new Vector2(0, -500) });
            shipBullets.Add(new ShipBullet { Position = new Vector2(shipX, 60), Velocity = Vector2.UnitY * BulletSpeed });
            UpdateShipBullets(.2f); UpdateMissiles(.2f);
            Require(missiles.Count == 0 && shipBullets.Count == 0 && shipHealth == 100 && score == 0,
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
                && shipBullets.Count == 0 && score == 0, "Bullet pierced a missile or chose the wrong first target");
            missiles.Clear();
            SmokeAbductee = people[0];
            shipBullets.Add(new ShipBullet { Position = new Vector2(shipX, 60), Velocity = Vector2.UnitY * BulletSpeed });
            UpdateShipBullets(1);
            Require(missiles.Count == 0 && people.Count == 1 && score == 0,
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
            Require(tractorActive && SmokeAbductee != null && shipBullets.Count == 1 && shipX > NATIVE_WIDTH / 2f,
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
            shipHealth = 50; repairLevel = 1;
            SpawnPerson(shipX, true, 1); DeliverPerson(people[0]);
            Require(shipHealth == 55 && roundSoldiers == 4, "Engineer affected currency");
            Require(RoundMultiplier == 1, "Multiplier must start at one");
            roundSeconds = 60;
            Require(Math.Abs(RoundMultiplier - 1.5) < .000001 && RoundReward == 6, "Survival reward growth failed");
            paused = true;
            UpdateUfo(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1)), new KeyboardState());
            Require(roundSeconds == 60, "Paused time increased rewards");
            EndIncrementalRound();
            Require(screen == MenuScreen.Upgrades && roundBanked && progression.Balance == 6,
                "Round end did not bank fractional rewards and open the map");
            EndIncrementalRound();
            Require(progression.Balance == 6, "Round paid out twice");
            Require(!progression.Buy(4) && progression.Buy(0) && progression.Balance == 3,
                "Purchase cost or prerequisite failed");
            Require(!progression.Buy(0), "Upgrade allowed overspending");
            resetGame();
            Require(weaponLevel == 2 && Math.Abs(ShotInterval - (.8f / 1.2f)) < .0001f && roundSoldiers == 0
                && roundSeconds == 0 && progression.Balance == 3, "Permanent upgrades failed across rounds");
            progression.Bank("smoke-funds", 10000);
            foreach (int node in new[] { 0, 1, 1, 2, 3, 4, 5, 8 }) Require(progression.Buy(node), "Tree unlock failed");
            resetGame(); screen = MenuScreen.Playing;
            Require(beamCapacity == 2 && shipHealth == 125 && Math.Abs(FlightSpeed - 103.5f) < .001f && TractorLiftSpeed == 42,
                "Purchased flight stats failed");
            roundSeconds = 60;
            Require(Math.Abs(RoundMultiplier - 1.6) < .000001, "Multiplier growth upgrade failed");
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
            progression.Bank("smoke-complete-tree", 100000000);
            // Catalog order is not graph order: buy all reachable ranks until
            // the complete DAG is traversed, including multi-parent nodes.
            int bought;
            do
            {
                bought = 0;
                for (int i = 0; i < UfoProgression.Nodes.Length; i++)
                    while (UfoProgression.Nodes[i].Effect != UfoUpgradeEffect.Prestige && progression.CanBuy(i))
                    { Require(progression.Buy(i), "Graph purchase failed"); bought++; }
            } while (bought > 0);
            for (int i = 0; i < UfoProgression.Nodes.Length; i++)
                if (UfoProgression.Nodes[i].Effect == UfoUpgradeEffect.Prestige)
                    Require(progression.Rank(i) == 0 && progression.CanBuy(i), "Prestige did not unlock at the three-capstone gate");
                else if (UfoProgression.Nodes[i].Id == "capacity5")
                    Require(progression.Rank(i) > 0 && progression.Rank(i) < UfoProgression.Nodes[i].MaxRank,
                        "Fleet Abduction should stay available beyond the former five-person cap");
                else
                    Require(progression.Rank(i) == UfoProgression.Nodes[i].MaxRank && !progression.Buy(i),
                        "Graph has unreachable nodes, insufficient fixture funds, or uncapped ranks");
            Require(progression.Rank("tractor") == 10
                && Math.Abs(progression.Bonus(UfoUpgradeEffect.Tractor) - 2f) < .0001f,
                "Tractor Drive ranks did not increase lift/pull to the 3x cap");
            progression.Bank("smoke-uncapped-beam", 1000000000);
            int fleetIndex = UfoProgression.Index("capacity5");
            while (progression.Rank(fleetIndex) < 105)
                Require(progression.Buy(fleetIndex), "Fleet Abduction could not advance beyond rank 100");
            Require(UfoProgression.Nodes[UfoProgression.Index("capacity5")].MaxRank == int.MaxValue
                && progression.Capacity > 104,
                "Fleet Abduction must keep increasing beam capacity beyond 104 people");
            int highBeamCapacity = progression.Capacity;
            resetGame();
            Require(beamCapacity == highBeamCapacity && MaxShipHealth == 500 && Math.Abs(RoundMultiplier - 3) < .0001
                && Math.Abs(TractorLiftSpeed - 90f) < .001f && Math.Abs(TractorPullSpeed - 45f) < .001f,
                "Full tree capacity, hull, tractor speed, or starting multiplier failed");
            shipHealth = 100;
            SpawnPerson(shipX, true, 1); DeliverPerson(people[0]);
            Require(shipHealth == 125 && roundSoldiers == 0, "Engineer repair upgrade failed");
#if !WEB
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "warhook-progression-" + Guid.NewGuid() + ".json");
            try
            {
                var saved = new UfoProgression(false, path);
                Require(saved.Bank("persistent-round", 16.25) && saved.Buy(1), "Progression save failed");
                var loaded = new UfoProgression(false, path);
                Require(loaded.Balance == 13.25 && loaded.Rank(1) == 1 && loaded.Bank("persistent-round", 16.25)
                    && loaded.Balance == 13.25, "Progression reload or payout idempotency failed");
                string slotPath = path + ".slot";
                try
                {
                    var slot = new UfoProgression(false, slotPath);
                    Require(!slot.Exists && slot.Import(loaded), "Legacy save migration failed");
                    Require(slot.SavePreferences(1, 4), "Save preferences failed");
                    var resumed = new UfoProgression(false, slotPath);
                    Require(resumed.Exists && resumed.Balance == 13.25 && resumed.Rank(1) == 1
                        && resumed.GameMode == 0 && resumed.Music == 4, "Save slot did not preserve progress/settings");
                    Require(resumed.StartNew() && resumed.Balance == 0 && resumed.Rank(1) == 0
                        && resumed.GameMode == 0 && resumed.Music == 1, "New game retained previous progression");
                    var original = new UfoProgression(false, path);
                    Require(original.Balance == 13.25 && original.Rank(1) == 1,
                        "Replacing a save modified a different slot");
                    var empty = new UfoProgression(false, slotPath);
                    Require(empty.Exists && empty.Balance == 0 && empty.Rank(1) == 0,
                        "Empty new game did not persist as a continuable save");
                }
                finally { System.IO.File.Delete(slotPath); }
                var failed = new UfoProgression(false, path + "/invalid.json");
                Require(!failed.Bank("failure", 100) && failed.Balance == 0, "Failed save committed a reward");
            }
            finally { System.IO.File.Delete(path); }
#endif
            progression = new UfoProgression(true);
            resetGame();
        }

        void VerifyTechWeb()
        {
            void Require(bool condition, string message)
            { if (!condition) throw new InvalidOperationException(message); }
            void BuyTo(string id, int rank)
            {
                int index = UfoProgression.Index(id);
                var node = UfoProgression.Nodes[index];
                foreach (var req in node.Requires) BuyTo(req.Node, req.Rank);
                while (progression.Rank(index) < rank) Require(progression.Buy(index), "Cannot reach " + id);
            }
            progression = new UfoProgression(true); progression.Bank("web-funds", 1000000);
            Require(UfoProgression.Nodes.Length == 34 && progression.Rank("core") == 1
                && !progression.Buy(UfoProgression.Index("core")), "Core must be owned and non-purchasable");
            var seen = new System.Collections.Generic.HashSet<int> { UfoProgression.Index("core") };
            var queue = new System.Collections.Generic.Queue<int>(seen);
            while (queue.Count > 0)
            {
                int node = queue.Dequeue();
                foreach (var direction in new[] { Vector2.UnitX, -Vector2.UnitX, Vector2.UnitY, -Vector2.UnitY })
                {
                    int next = SpatialUpgradeNeighbor(node, direction);
                    if (seen.Add(next)) queue.Enqueue(next);
                }
            }
            Require(seen.Count == UfoProgression.Nodes.Length, "Spatial navigation strands a node");
            BuyTo("twin", 1);
            Require(!progression.Unlocked(UfoProgression.Index("plasma")), "Merge unlocked with one parent");
            resetGame(); UpdateShipWeapon(.1f, true);
            Require(shipBullets.Count == 2 && shipBullets[0].Position.X < shipX && shipBullets[1].Position.X > shipX,
                "Twin cannons failed to split the shot");
            BuyTo("point", 1);
            Require(progression.Unlocked(UfoProgression.Index("plasma")), "Merge remained locked with both parents");
            BuyTo("shield", 1);
            resetGame();
            Require(shieldLevel == 1 && shieldActive && Math.Abs(ShieldRegenDelay - 2f) < .001f,
                "Shield upgrade did not activate at flight start");
            missiles.Add(new UfoMissile {
                Position = new Vector2(shipX, shipY + 28),
                Velocity = -Vector2.UnitY * 100,
                Heading = -Vector2.UnitY
            });
            UpdateMissiles(.4f);
            Require(!shieldActive && shieldRegenTimer > 1.5f && shipHealth == MaxShipHealth,
                "Shield did not absorb the first rocket");
            UpdateShield(1.99f);
            Require(!shieldActive, "Shield regenerated too early");
            UpdateShield(.02f);
            Require(shieldActive, "Shield did not regenerate");
            BuyTo("shield", 3);
            resetGame();
            Require(shieldLevel == 3 && ShieldRegenDelay < 2f, "Shield ranks did not improve regeneration");
            BuyTo("soldier-value", 2);
            resetGame();
            Require(soldierValueLevel == 3 && RoundReward == 0,
                "Soldier value ranks did not apply at flight start");
            roundSoldiers = 2;
            Require(RoundReward == 6, "Soldier value upgrade did not increase round currency");
            OpenRoundResults();
            Require(resultsCrew == 6 && resultsReward == 6,
                "Round results did not snapshot the value-adjusted soldier total");
            screen = MenuScreen.Playing;
            BuyTo("plasma", 1); resetGame();
            Require(ShipBulletSpeed > BulletSpeed, "Plasma did not speed bullets");
            shipBullets.Add(new ShipBullet { Position = new Vector2(shipX, 80), Velocity = Vector2.UnitY * BulletSpeed });
            missiles.Add(new UfoMissile { Position = new Vector2(shipX + 6, 110) });
            missiles.Add(new UfoMissile { Position = new Vector2(shipX + 13, 110) });
            missiles.Add(new UfoMissile { Position = new Vector2(shipX + 40, 110) });
            UpdateShipBullets(.3f);
            Require(missiles.Count == 1 && score == 0, "Point defence radius/blast failed");
            BuyTo("fire3", 1); BuyTo("capacity5", 1);
            Require(!progression.Unlocked(UfoProgression.Index("prestige")), "Prestige unlocked before three capstones");
            resetGame(); UpdateShipWeapon(.1f, true);
            Require(shipBullets.Count == 3 && beamCapacity == 5, "Weapon/beam capstones failed");
            float emptyWidth = ConeHalfWidth(GroundY);
            SpawnPerson(shipX + 5, false, 1); SmokeAbductee = people[0]; SmokeAbductee.Y = TractorOrigin.Y + 25;
            Require(ConeHalfWidth(GroundY) > emptyWidth, "Matrix did not widen an occupied beam");
            float beforeX = SmokeAbductee.X;
            UpdateTractor(.1f, true);
            Require(beforeX - SmokeAbductee.X > TractorPullSpeed * .1f, "Focus did not strengthen near-ship pull");
            DropPayload(); Require(Math.Abs(ConeHalfWidth(GroundY) - emptyWidth) < .001f, "Matrix stayed active with an empty beam");
            BuyTo("auto", 1);
            Require(progression.Unlocked(UfoProgression.Index("prestige")) && progression.Rank("exponential") == 0,
                "Prestige must accept any three complete capstones");
            resetGame();
            Require(nanoHull == 3 && warpBonus > 0, "Ship merge bonuses failed");
            MoveShip(1, .01f, 1);
            Require(shipVelocity.X > 12 && shipVelocity.Y > 12, "Warp acceleration failed");
            MoveShip(0, 1, 1);
            Require(shipVelocity.Y == 0 || shipVelocity.Y > FlightSpeed * .4f, "Warp vertical speed failed");
            shipHealth = 50;
            UpdateAutoRepair(5); Require(shipHealth == 50, "Auto repair started before five safe seconds");
            UpdateAutoRepair(1); Require(shipHealth == 52, "Auto repair rate incorrect");
            shieldActive = false; shieldRegenTimer = ShieldRegenDelay;
            DamageShip(); UpdateAutoRepair(4); Require(shipHealth == 27, "Hit did not reset auto repair delay");
            UpdateAutoRepair(2); Require(shipHealth == 29, "Auto repair failed to restart after safety delay");
            BuyTo("exponential", 3); resetGame();
            Require(interestBonus > 0, "Interest upgrade failed at flight launch");
            roundSeconds = 0; double start = RoundMultiplier;
            roundSeconds = 60; double minute = RoundMultiplier;
            roundSeconds = 120; double twoMinutes = RoundMultiplier;
            Require(twoMinutes - minute > minute - start, "Exponential yield did not accelerate growth");
            roundSeconds = 121; double withLongHaul = RoundMultiplier;
            longHaulBonus = 0;
            Require(withLongHaul > RoundMultiplier, "Long Haul did not activate after two minutes");

            float zoomBeforePrestige = PrestigeWorldScale;
            int poolBeforePrestige = UfoPoolLimit;
            Require(progression.Prestige() && progression.PrestigeCount == 1 && progression.Balance == 0
                && progression.Rank("core") == 1 && progression.Rank("fire") == 0,
                "Prestige did not bank its count and reset crew/upgrades");
            resetGame();
            Require(beamCapacity == 1 && PrestigeWorldScale < zoomBeforePrestige
                && UfoPoolLimit == poolBeforePrestige + 4
                && Vector2.Distance(Vector2.Transform(new Vector2(NATIVE_WIDTH / 2f, GroundY), PrestigeWorldTransform()),
                    new Vector2(NATIVE_WIDTH / 2f, GroundY)) < .001f,
                "Prestige failed to zoom out around the ground or expand the enemy pool");
            progression.Bank("second-prestige-funds", 100000000);
            BuyTo("fire3", 1); BuyTo("capacity5", 1); BuyTo("auto", 1); BuyTo("exponential", 3);
            float firstPrestigeZoom = PrestigeWorldScale;
            Require(progression.Prestige() && progression.PrestigeCount == 2
                && UfoPoolLimit == poolBeforePrestige + 8 && 1 / (1 + progression.PrestigeCount * .06f) < firstPrestigeZoom,
                "Repeated prestige did not keep increasing zoom and pool size");
            resetGame();
            double beforeReset = RoundMultiplier;
            progression.StartNew();
            Require(RoundMultiplier == beforeReset, "Flight bonuses changed after launch");
#if !WEB
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "warhook-tech-migration-" + Guid.NewGuid() + ".json");
            try
            {
                System.IO.File.WriteAllText(path, "{\"version\":1,\"balance\":12.5,\"ranks\":{\"fire3\":5,\"engine3\":5,\"capacity5\":1,\"mothership\":1}}");
                var migrated = new UfoProgression(false, path);
                Require(migrated.Balance == 4157.5 && migrated.Rank("fire3") == 1 && migrated.Rank("engine3") == 3
                    && migrated.Capacity == 5 && migrated.PrestigeCount == 1,
                    "Legacy ranks/capacity/prestige or refund migration failed");
                Require(migrated.SavePreferences(0, 1), "Migrated save write failed");
                var reloaded = new UfoProgression(false, path);
                Require(reloaded.Balance == 4157.5 && reloaded.PrestigeCount == 1,
                    "Migration refunded ranks twice or failed to save prestige");
            }
            finally { System.IO.File.Delete(path); }
#endif
            progression = new UfoProgression(true); resetGame();
        }

        void VerifyEngineerUnlock()
        {
            void Require(bool condition, string message)
            { if (!condition) throw new InvalidOperationException(message); }
            progression = new UfoProgression(true);
            resetGame();
            for (int kind = 0; kind <= 2; kind++) SpawnBeanRunner(144, kind, 96);
            Require(people.TrueForAll(p => !p.Engineer), "Engineers spawned before their unlock");
            UpdateGroundPeople(EnemyRunInSeconds);
            Require(missiles.Count == 3, "Locked engineer events must become ordinary soldiers");
            progression.Bank("engineer-test", 10000);
            int repair = UfoProgression.Index("repair"), hull = UfoProgression.Index("hull");
            Require(!progression.Buy(repair), "Engineer unlock skipped hull prerequisites");
            Require(progression.Buy(hull) && progression.Buy(hull) && progression.Buy(repair),
                "Engineer unlock could not be purchased");
            for (int rank = 1; rank <= 5; rank++)
            {
                resetGame();
                Require(repairLevel == rank, "Engineer rank did not apply at round start");
                for (int kind = 0; kind <= 2; kind++) SpawnBeanRunner(144, kind, 96);
                Require(!people[0].Engineer && people[1].Engineer && people[2].Engineer,
                    "Unlocked engineers lost their original special-bean schedule");
                UpdateGroundPeople(EnemyRunInSeconds);
                Require(missiles.Count == 1, "Unlocked engineers fired rockets");
                shipHealth = 50;
                DeliverPerson(people[1]);
                Require(shipHealth == 50 + rank * 5 && roundSoldiers == 0 && roundAbductions == 1,
                    "Engineer repair must start at five and add five per rank without earning crew");
                shipHealth = MaxShipHealth - 1;
                DeliverPerson(people[1]);
                Require(shipHealth == MaxShipHealth, "Engineer repair exceeded upgraded hull maximum");
                if (rank < 5) Require(progression.Buy(repair), "Engineer repair rank purchase failed");
            }
            progression = new UfoProgression(true); resetGame();
            SpawnBeanRunner(144, 1, 96);
            Require(!people[0].Engineer, "Engineer unlock leaked into another save");
            Console.WriteLine("Engineer checks passed: locked spawns, upgrade prerequisites, unlock, 5-25 hull repairs, non-firing engineers, hull cap and save isolation.");
            resetGame();
        }

        void VerifyAltitudeDefense()
        {
            void Require(bool condition, string message)
            { if (!condition) throw new InvalidOperationException(message); }
            progression = new UfoProgression(true);
            resetGame(); screen = MenuScreen.Playing;
            Require(altitudeLineY == 64 && shipY + ShipHeight / 2 < altitudeLineY, "Altitude line must start just below the UFO");
            shipY = altitudeLineY;
            UpdateAltitudeDefense(5);
            Require(altitudeShotsRemaining == 0 && missiles.Count == 0, "Safe altitude triggered a volley");
            shipY++;
            UpdateAltitudeDefense(.01f);
            Vector2 left = altitudeLeftSpawn, right = altitudeRightSpawn;
            Require(altitudeShotsRemaining == 6 && missiles.Count == 0 && left.X == 7 && right.X == NATIVE_WIDTH - 7,
                "Crossing the line must warn at both launch points before firing");
            UpdateAltitudeDefense(.4f);
            float timer = altitudeTimer;
            paused = true; UpdateAltitudeDefense(10); paused = false;
            Require(altitudeTimer == timer && missiles.Count == 0, "Pause advanced the warning");
            gameover = true; UpdateAltitudeDefense(10); gameover = false;
            Require(altitudeTimer == timer && missiles.Count == 0, "Death launched pending rockets");
            shipY = ShipStartY; // A warned volley remains committed when the player retreats.
            for (int i = 0; i < 6; i++)
            {
                shipX = 120 + i * 5;
                UpdateAltitudeDefense(i == 0 ? .46f : AltitudeShotInterval + .001f);
                Require(missiles.Count == i + 1, "Volley did not fire one rocket at a time");
                var rocket = missiles[i];
                Vector2 expected = i % 2 == 0 ? left : right;
                Require(rocket.Position == expected && rocket.AltitudeDefense && rocket.BeanSpeed == 0
                    && Vector2.Distance(rocket.Heading, Vector2.Normalize(ShipPosition - expected)) < .0001f,
                    "Rocket launch point moved away from its warning or missed current ship aim");
                Require(Math.Abs(rocket.Velocity.Length() - 90) < .001f, "Side rocket speed changed");
            }
            Require(altitudeShotsRemaining == 0 && ActiveBeanSpawnCount() == 0,
                "Side rockets occupied the original bean pool");
            for (int i = 0; i < 16; i++) SpawnBeanRunner(30 + i * 12, 0, 64);
            Require(ActiveBeanSpawnCount() == 16, "Side volley reduced the ground-enemy pool");
            shipY = altitudeLineY + 1;
            UpdateAltitudeDefense(AltitudeVolleyCooldown - .1f);
            Require(altitudeShotsRemaining == 0, "Volley repeated before its cooldown");
            UpdateAltitudeDefense(.11f);
            Require(altitudeShotsRemaining == 6 && missiles.Count == 6, "Staying low did not start another warned volley");
            resetGame();
            Require(altitudeShotsRemaining == 0 && altitudeCooldown == 0 && missiles.Count == 0,
                "New flight retained altitude threats");
            missiles.Add(new UfoMissile { Position = new Vector2(shipX, shipY + 40), Velocity = -Vector2.UnitY * 90, AltitudeDefense = true });
            UpdateMissiles(.5f);
            Require(shipHealth == 75 && missiles.Count == 0, "Side rockets failed hull collision");
            missiles.Add(new UfoMissile { Position = new Vector2(120, 120), Velocity = -Vector2.UnitY * 90, AltitudeDefense = true });
            shipBullets.Add(new ShipBullet { Position = new Vector2(120, 110), Velocity = Vector2.UnitY * 160 });
            UpdateShipBullets(.1f);
            Require(missiles.Count == 0 && score == 0, "Side rockets cannot be shot down");
            progression.Bank("clearance-test", 10000);
            int clearance = UfoProgression.Index("clearance");
            Require(!progression.Buy(clearance) && progression.Buy(UfoProgression.Index("engine")),
                "Flight Clearance must require Thrusters rank 1");
            for (int rank = 1; rank <= 5; rank++)
            {
                Require(progression.Buy(clearance), "Flight Clearance rank could not be bought");
                resetGame(); shipY = altitudeLineY;
                Require(altitudeLineY == 64 + rank * 14, "Clearance upgrade did not lower the line");
                UpdateAltitudeDefense(1);
                Require(altitudeShotsRemaining == 0, "Upgraded safe altitude still triggered rockets");
                shipY++; UpdateAltitudeDefense(.01f);
                Require(altitudeShotsRemaining == 6, "Upgraded altitude threshold was not enforced");
            }
            Require(!progression.Buy(clearance), "Flight Clearance exceeded its rank cap");
            progression = new UfoProgression(true); resetGame();
            Console.WriteLine("Altitude defense checks passed: threshold, warnings, alternating aimed volley, pause/death, cooldown, pool isolation, collisions/interception, reset and all clearance ranks.");
        }

        void VerifyImpactEffects()
        {
            void Require(bool condition, string message)
            { if (!condition) throw new InvalidOperationException(message); }
            progression = new UfoProgression(true);
            resetGame(); screen = MenuScreen.Playing;
            shipX = previousShipX = NATIVE_WIDTH / 2f; shipY = previousShipY = 80; shipHealth = 100;
            DamageShip(Vector2.UnitX);
            Require(shipHealth == 75 && impactShakeTime > 0 && shipX > NATIVE_WIDTH / 2f && shipVelocity.X > 0,
                "Rocket hit did not apply a directional knockback and shake");
            Matrix transform = ImpactShakeTransform();
            Require(transform.Translation.Length() > 0, "Rocket impact shake had no camera offset");
            UpdateImpactEffects(.12f);
            Require(impactShakeTime > 0 && ImpactShakeTransform().Translation.Length() < transform.Translation.Length() + 3,
                "Rocket impact shake did not decay smoothly");
            UpdateImpactEffects(.2f);
            Require(impactShakeTime == 0 && ImpactShakeTransform() == Matrix.Identity,
                "Rocket impact shake did not settle");
            resetGame();
            Require(impactShakeTime == 0 && shipVelocity == Vector2.Zero, "New flight retained impact motion");
            // A fatal hit should hand its direction and momentum to the wreck,
            // rather than making every crash fall straight down identically.
            shipHealth = 25; shipX = previousShipX = NATIVE_WIDTH / 2f; shipY = previousShipY = 80;
            DamageShip(Vector2.UnitX);
            float crashStartX = shipX, crashStartTilt = crashTilt;
            UpdateCrashLanding(.2f);
            Require(crashLanding && shipX > crashStartX && crashTilt > crashStartTilt,
                "Destroyed UFO did not inherit the final hit's slide and tumble");
            resetGame();
            shipHealth = 25; roundSeconds = 0;
            Require(CriticalHullFlash, "Critical hull warning did not start at 25 hull");
            roundSeconds = .13;
            Require(!CriticalHullFlash, "Critical hull warning did not flash off");
            shipHealth = 26;
            Require(!CriticalHullFlash, "Critical hull warning continued above 25 hull");
            Console.WriteLine("Impact checks passed: directional rocket knockback, subtle decaying shake, settle and reset.");
        }

        void VerifyRoundResults()
        {
            void Require(bool condition, string message)
            { if (!condition) throw new InvalidOperationException(message); }
            progression = new UfoProgression(true);
            resetGame(); screen = MenuScreen.Playing; transition = MenuTransition.None;
            roundSoldiers = 12; roundSeconds = 180;
            shipHealth = 25; hurtTime = 0;
            DamageShip();
            float crashStartY = shipY;
            UpdateRoundResults(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(.1)), false, true);
            Require(crashLanding && shipY > crashStartY && crashTilt > 0,
                "Destroyed UFO did not begin its crash landing");
            var tick = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(.1));
            updateMenus(tick, new KeyboardState(Keys.X));
            Require(screen == MenuScreen.RoundResults && !roundActive && roundBanked && progression.Balance == 30,
                "Death must show results and bank the exact reward immediately");
            Require(resultsCrew == 12 && resultsMultiplier == 2.5
                && resultsReward == 30 && resultsSurvival == 180,
                "Results must snapshot final round statistics");
            Require(ResultsLineAge(0) < 0, "Results tally started without an opening delay");
            UpdateRoundResults(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1)), false, true);
            Require(ResultsLineAge(0) >= 0 && ResultsLineAge(1) < 0, "Crew should appear first");
            UpdateRoundResults(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1)), true, true);
            Require(screen == MenuScreen.RoundResults && ResultsLineAge(1) >= 0 && ResultsLineAge(2) < 0,
                "Multiplier should appear second and early input must not skip the tally");
            for (int i = 0; i < 30; i++) updateMenus(tick, new KeyboardState(Keys.X));
            Require(ResultsComplete && !resultsInputReady && screen == MenuScreen.RoundResults && roundSeconds == 180,
                "Held fire must not dismiss results and the survival clock must stay frozen");
            updateMenus(tick, new KeyboardState());
            updateMenus(tick, new KeyboardState(Keys.X));
            Require(screen == MenuScreen.Upgrades && !mapInputReady && progression.Balance == 30,
                "Fresh confirm should enter upgrades without buying or paying twice");
            updateMenus(tick, new KeyboardState(Keys.X));
            Require(progression.Balance == 30, "Held results confirmation bought an upgrade");
            EndIncrementalRound();
            Require(progression.Balance == 30, "Revisiting round end paid twice");
            resetGame(); screen = MenuScreen.Playing;
            EndIncrementalRound(showResults: true);
            Require(resultsCrew == 0 && resultsMultiplier == 1 && resultsReward == 0 && resultsTime == 0,
                "A zero-crew round inherited an earlier tally");
            Console.WriteLine("Round results checks passed: death routing, timed reveals, exact banking, frozen timer, held-input guard, upgrade handoff and zero-crew reset.");
            progression = new UfoProgression(true); resetGame(); screen = MenuScreen.Playing;
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
                VerifyUfoDifficulty();
                VerifyUfoFlight();
                VerifyUpgradeRewards();
                VerifyTechWeb();
                VerifyEngineerUnlock();
                VerifyAltitudeDefense();
                VerifyImpactEffects();
                VerifyRoundResults();
                Console.WriteLine("UFO flight checks passed: eased tilt, horizontal/vertical controls and bounds, smaller collision box, moving gun and missile aim; X fire, one-hit kills, missile destruction and shootable engineers; persistent currency, survival multiplier, 34-node tech web, multi-parent prerequisites, capstones, save migration, spatial navigation, twin/triple guns, point defence, shield, focus/matrix, warp, auto-repair, interest and exponential rewards; one-unit Z cone, gradual horizontal centring, movement, drop, landing, recapture, delivery and repairs; aimed missiles, rocket suction immunity, damage and game over; pause/reset; original bean replay (99 events / 4200 ticks), speed ramp, pool and one-shot runners.");
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
                UpdateAltitudeDefense(0);
                ufoShotFrozen = true;
                NextShotStage();
            }
            else if (shotStage == 2 && shotTimer > .6)
            {
                SaveScreenshot();
                roundSoldiers = 12; roundSeconds = 180;
                shipHealth = 0;
                EndIncrementalRound(showResults: true);
                NextShotStage();
            }
            else if (shotStage == 3 && shotTimer > 1.2)
            {
                if (screen != MenuScreen.RoundResults) throw new InvalidOperationException("Results closed without input");
                SaveScreenshot(); NextShotStage();
            }
            else if (shotStage == 4 && shotTimer > 1.15)
            {
                SaveScreenshot(); NextShotStage();
            }
            else if (shotStage == 5 && shotTimer > 1.5)
            {
                SaveScreenshot();
                UpdateRoundResults(new GameTime(), true, false);
                NextShotStage();
            }
            else if (shotStage == 6 && shotTimer > .8)
            {
                if (screen != MenuScreen.Upgrades) throw new InvalidOperationException("Upgrade map closed without input");
                SaveScreenshot();
                ufoShotFrozen = false;
                transition = MenuTransition.None;
                mapSelection = 17; FocusUpgradeNode();
                NextShotStage();
            }
            else if (shotStage == 7 && shotTimer > 2.5)
            {
                SaveScreenshot(); NextShotStage();
            }
            else if (shotStage == 8 && shotTimer > .5) Exit();
        }
    }
}
