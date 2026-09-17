using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonogameTest
{
    public partial class Game1
    {
        static readonly Rectangle UpgradeMapView = new Rectangle(4, 28, 280, 128);
        static readonly Rectangle UpgradeLaunchButton = new Rectangle(7, 200, 91, 13);
        static readonly Rectangle UpgradeBuyButton = new Rectangle(106, 200, 82, 13);
        static readonly Rectangle UpgradeMenuButton = new Rectangle(197, 200, 47, 13);
        static readonly Rectangle UpgradeMiniMap = new Rectangle(238, 32, 42, 26);
        static readonly Rectangle UpgradeZoomButton = new Rectangle(253, 18, 29, 9);
        const float MapNodeVisualScale = .84f;
        const float MapConnectorScale = .78f;
        const float MapLabelScale = .88f;
        int mapSelection = UfoProgression.Index("core");
        Vector2 mapCamera, mapDragCamera, mapDragStart;
        float mapZoom = 1;
        double mapPulse;
        bool mapInputReady, mapDragging;
        MouseState mapPreviousMouse;
        string mapMessage = "";
        double mapMessageTime;
        static string Money(double value) => value >= 10000 ? (value / 1000).ToString("F1", CultureInfo.InvariantCulture) + "K"
            : value.ToString("F2", CultureInfo.InvariantCulture);
        static string FormatRoundTime(double seconds) => ((int)seconds / 60).ToString("D2") + ":" + ((int)seconds % 60).ToString("D2");
        static Vector2 MapViewCenter => new Vector2(UpgradeMapView.Center.X, UpgradeMapView.Center.Y);
        float MapWorldScale => mapZoom * .625f;
        Vector2 MapToScreen(Vector2 world) => MapViewCenter + (world - mapCamera) * MapWorldScale;
        Vector2 UpgradeNodePosition(int index) => MapToScreen(UfoProgression.Nodes[index].Position);
        void ClampMapCamera() => mapCamera = Vector2.Clamp(mapCamera, new Vector2(-430, -185), new Vector2(430, 260));
        void FocusUpgradeNode()
        {
            mapCamera = mapZoom == .5f ? new Vector2(0, 40) : UfoProgression.Nodes[mapSelection].Position;
            ClampMapCamera();
        }
        Rectangle UpgradeNodeRect(int index)
        {
            var p = UpgradeNodePosition(index);
            int size = (int)((UfoProgression.Nodes[index].Effect == UfoUpgradeEffect.Core ? 30 : 23)
                * mapZoom * MapNodeVisualScale);
            return new Rectangle((int)Math.Round(p.X) - size / 2, (int)Math.Round(p.Y) - size / 2, size, size);
        }

        int SpatialUpgradeNeighbor(int selected, Vector2 direction)
        {
            int best = selected; float distance = float.PositiveInfinity;
            for (int i = 0; i < UfoProgression.Nodes.Length; i++)
            {
                Vector2 delta = UfoProgression.Nodes[i].Position - UfoProgression.Nodes[selected].Position;
                float forward = Vector2.Dot(delta, direction);
                if (forward <= 0) continue;
                float sideways = Math.Abs(delta.X * direction.Y - delta.Y * direction.X);
                float weight = forward + sideways * 1.5f;
                if (weight < distance) { distance = weight; best = i; }
            }
            return best;
        }

        void ZoomUpgradeMap(float next, Vector2 anchor)
        {
            Vector2 world = mapCamera + (anchor - MapViewCenter) / MapWorldScale;
            mapZoom = next;
            mapCamera = world - (anchor - MapViewCenter) / MapWorldScale;
            if (next == .5f) mapCamera = new Vector2(0, 40); // Overview fits the complete web.
            ClampMapCamera(); mapDragging = false;
        }
        void EndIncrementalRound(bool showResults = false)
        {
            if (roundActive)
            {
                roundActive = false;
                if (!shotsMode && highScores.Qualifies(gameB, score))
                    highScores.Submit(gameB, new string(scoreInitials), score);
                gameover = pyorodead = true;
                paused = false; tractorActive = false;
                DropPayload();
                roundBanked = progression.Bank(roundId, RoundReward);
            }
            if (showResults) OpenRoundResults();
            else OpenUpgradeMap();
        }

        void OpenUpgradeMap()
        {
            screen = MenuScreen.Upgrades;
            paused = false;
            mapInputReady = false; mapDragging = false;
            mapPreviousMouse = Mouse.GetState();
            mapMessage = ""; mapMessageTime = 0;
            FocusUpgradeNode();
            beamAudio?.Stop();
            music?.Request(MusicTracks.Track.Menu);
        }


        void PurchaseUpgrade()
        {
            if (!roundActive && roundId != null && !roundBanked)
            {
                roundBanked = progression.Bank(roundId, RoundReward);
                if (!roundBanked) { mapMessage = progression.Error; mapMessageTime = 3; return; }
            }
            if (progression.Buy(mapSelection))
            {
                mapMessage = "UPGRADE SAVED FOR NEXT FLIGHT";
                beamAudio?.PlayMenuConfirm();
            }
            else mapMessage = !string.IsNullOrEmpty(progression.Error) ? progression.Error
                : !progression.Unlocked(mapSelection) ? "PREREQUISITES NOT MET"
                : progression.Rank(mapSelection) >= UfoProgression.Nodes[mapSelection].MaxRank ? "NODE COMPLETE"
                : "COLLECT MORE SOLDIERS NEXT ROUND";
            mapMessageTime = 2;
        }

        void UpdateUpgradeMap(GameTime time, Func<Keys, bool> pressed, Func<Buttons, bool> padPressed,
            bool accept, bool acceptHeld, bool cancel)
        {
            mapPulse += time.ElapsedGameTime.TotalSeconds;
            mapMessageTime = Math.Max(0, mapMessageTime - time.ElapsedGameTime.TotalSeconds);
            if (!acceptHeld) mapInputReady = true;
            int oldSelection = mapSelection;
            Vector2 direction = Vector2.Zero;
            if (pressed(Keys.Left) || pressed(Keys.A) || padPressed(Buttons.DPadLeft)) direction = -Vector2.UnitX;
            if (pressed(Keys.Right) || pressed(Keys.D) || padPressed(Buttons.DPadRight)) direction = Vector2.UnitX;
            if (pressed(Keys.Up) || pressed(Keys.W) || padPressed(Buttons.DPadUp)) direction = -Vector2.UnitY;
            if (pressed(Keys.Down) || pressed(Keys.S) || padPressed(Buttons.DPadDown)) direction = Vector2.UnitY;
            if (direction != Vector2.Zero) mapSelection = SpatialUpgradeNeighbor(mapSelection, direction);
            if (pressed(Keys.C) || pressed(Keys.Home) || padPressed(Buttons.LeftStick)) mapSelection = UfoProgression.Index("core");
            if (oldSelection != mapSelection || pressed(Keys.C) || pressed(Keys.Home) || padPressed(Buttons.LeftStick))
            { FocusUpgradeNode(); beamAudio?.PlayMenuBlip(); }
            MouseState mouse = Mouse.GetState();
            Vector2 pointer = new Vector2((mouse.X - rect.X) * NATIVE_WIDTH / (float)Math.Max(1, rect.Width),
                (mouse.Y - rect.Y) * NATIVE_HEIGHT / (float)Math.Max(1, rect.Height));
            Point point = new Point((int)pointer.X, (int)pointer.Y);
            bool down = mouse.LeftButton == ButtonState.Pressed && mapPreviousMouse.LeftButton == ButtonState.Released;
            bool up = mouse.LeftButton == ButtonState.Released && mapPreviousMouse.LeftButton == ButtonState.Pressed;
            bool inMap = UpgradeMapView.Contains(point);
            if (down && mapZoom > .5f && UpgradeMiniMap.Contains(point))
            {
                mapCamera = new Vector2((pointer.X - UpgradeMiniMap.X) / UpgradeMiniMap.Width * 860 - 430,
                    (pointer.Y - UpgradeMiniMap.Y) / UpgradeMiniMap.Height * 445 - 185);
                ClampMapCamera();
            }
            else if (down && inMap) { mapDragging = true; mapDragStart = pointer; mapDragCamera = mapCamera; }
            if (mapDragging && mouse.LeftButton == ButtonState.Pressed)
            { mapCamera = mapDragCamera - (pointer - mapDragStart) / MapWorldScale; ClampMapCamera(); }
            if (up && mapDragging)
            {
                if (Vector2.Distance(pointer, mapDragStart) < 4 && inMap)
                    for (int i = 0; i < UfoProgression.Nodes.Length; i++)
                        if (UpgradeNodeRect(i).Contains(point)) { mapSelection = i; beamAudio?.PlayMenuBlip(); break; }
                mapDragging = false;
            }
            int wheel = mouse.ScrollWheelValue - mapPreviousMouse.ScrollWheelValue;
            if (inMap && wheel != 0) ZoomUpgradeMap(wheel > 0 ? Math.Min(2, mapZoom * 2) : Math.Max(.5f, mapZoom / 2), pointer);
            if ((down && UpgradeZoomButton.Contains(point)) || padPressed(Buttons.RightShoulder))
                ZoomUpgradeMap(mapZoom >= 2 ? .5f : mapZoom * 2, MapViewCenter);
            bool buy = down && UpgradeBuyButton.Contains(point);
            bool launch = down && UpgradeLaunchButton.Contains(point);
            cancel |= down && UpgradeMenuButton.Contains(point);
            mapPreviousMouse = mouse;
            if (buy || (mapInputReady && accept)) PurchaseUpgrade();
            if (launch || pressed(Keys.R) || padPressed(Buttons.Start) || cancel)
            {
                if (!roundActive && roundId != null && !roundBanked && !progression.Bank(roundId, RoundReward))
                { mapMessage = progression.Error; mapMessageTime = 3; return; }
                roundBanked = true;
                beginTransition(cancel ? MenuScreen.Main : MenuScreen.Playing, false);
            }
        }

        static Color BranchColor(UfoBranch branch) => branch switch {
            UfoBranch.Weapons => new Color(255, 115, 91), UfoBranch.Beam => new Color(57, 238, 231),
            UfoBranch.Ship => new Color(68, 177, 255), UfoBranch.Hull => new Color(107, 230, 151),
            UfoBranch.Yield => new Color(255, 216, 83), UfoBranch.Hybrid => new Color(213, 141, 255),
            _ => new Color(116, 250, 249)
        };
        void TechBox(Rectangle box, Color border, Color fill, int thickness = 1)
        {
            spriteBatch.Draw(beamPixel, box, border);
            spriteBatch.Draw(beamPixel, new Rectangle(box.X + thickness, box.Y + thickness,
                Math.Max(1, box.Width - thickness * 2), Math.Max(1, box.Height - thickness * 2)), fill);
        }
        void TechLine(Vector2 from, Vector2 to, Color color, bool dashed, float fraction = 1)
        {
            float length = Vector2.Distance(from, to);
            if (length < .5f) return;
            if (!dashed) { drawBeamStroke(from, Vector2.Lerp(from, to, fraction), Math.Max(.7f, mapZoom * MapConnectorScale), color); return; }
            for (float d = 0; d < length * fraction; d += 6 * mapZoom)
                drawBeamStroke(Vector2.Lerp(from, to, d / length), Vector2.Lerp(from, to, Math.Min(length * fraction, d + 3 * mapZoom) / length),
                    Math.Max(.7f, mapZoom * MapConnectorScale), color);
        }
        void DrawTechEdge(int parent, int child, int requiredRank)
        {
            Vector2 from = UpgradeNodePosition(parent), to = UpgradeNodePosition(child);
            Vector2 direction = Vector2.Normalize(to - from);
            from += direction * 11 * mapZoom * MapNodeVisualScale;
            to -= direction * 11 * mapZoom * MapNodeVisualScale;
            Vector2 bend = new Vector2((from.X + to.X) / 2, from.Y);
            Vector2 bend2 = new Vector2(bend.X, to.Y);
            Color tint = BranchColor(UfoProgression.Nodes[child].Branch);
            float progress = Math.Clamp(progression.Rank(parent) / (float)requiredRank, 0, 1);
            Vector2[] path = { from, bend, bend2, to };
            float total = 0; for (int i = 1; i < path.Length; i++) total += Vector2.Distance(path[i - 1], path[i]);
            float remaining = total * progress;
            for (int i = 1; i < path.Length; i++)
            {
                float length = Vector2.Distance(path[i - 1], path[i]);
                TechLine(path[i - 1], path[i], new Color(39, 63, 84), true);
                if (remaining > 0 && length > 0) TechLine(path[i - 1], path[i], tint * .75f, false, Math.Min(1, remaining / length));
                remaining -= length;
            }
        }

        string UpgradeEffectAt(int index, int rank)
        {
            var node = UfoProgression.Nodes[index];
            string percent = "+" + Math.Round(node.Amount * rank * 100).ToString(CultureInfo.InvariantCulture) + "%";
            return node.Effect switch {
                UfoUpgradeEffect.Fire => "FIRE RATE " + percent,
                UfoUpgradeEffect.Tractor => "LIFT/PULL " + percent,
                UfoUpgradeEffect.Engine => "FLIGHT SPEED " + percent,
                UfoUpgradeEffect.FlightClearance => "DANGER LINE +" + (int)(node.Amount * rank) + "PX LOWER",
                UfoUpgradeEffect.Hull or UfoUpgradeEffect.Nanohull => "HULL +" + (int)(node.Amount * rank),
                UfoUpgradeEffect.Repair => rank == 0 ? "ENGINEERS LOCKED"
                    : rank == 1 ? "UNLOCK ENGINEERS: REPAIR 5 HULL" : "ENGINEERS REPAIR " + (int)(node.Amount * rank) + " HULL",
                UfoUpgradeEffect.BeamWidth => "CONE WIDTH " + percent,
                UfoUpgradeEffect.Growth => "REWARD GROWTH " + percent,
                UfoUpgradeEffect.StartingBonus => "START +" + (node.Amount * rank).ToString("F2", CultureInfo.InvariantCulture) + "X",
                UfoUpgradeEffect.Capacity => "BEAM CAPACITY " + (rank > 0 ? (int)node.Amount : (int)node.Amount - 1),
                UfoUpgradeEffect.TwinShot => rank > 0 ? "TWO PARALLEL SHOTS" : "ONE SHOT",
                UfoUpgradeEffect.TripleShot => rank > 0 ? "THREE PARALLEL SHOTS" : "TWO SHOTS",
                UfoUpgradeEffect.Plasma => "FIRE/BULLET SPEED " + percent,
                UfoUpgradeEffect.PointDefense => "INTERCEPT +" + rank * 3 + "PX / BLAST " + rank * 8 + "PX",
                UfoUpgradeEffect.Focus => "PULL NEAR UFO " + percent,
                UfoUpgradeEffect.Matrix => "CONE WHEN CARRYING " + percent,
                UfoUpgradeEffect.Warp => "ACCEL/VERTICAL SPEED " + percent,
                UfoUpgradeEffect.AutoRepair => rank > 0 ? "2 HULL/SEC AFTER 5 SEC SAFE" : "NO PASSIVE REPAIR",
                UfoUpgradeEffect.LongHaul => "GROWTH AFTER 2 MIN " + percent,
                UfoUpgradeEffect.Interest => "+" + rank + "% /100 SAVED (MAX " + rank * 10 + "%)",
                UfoUpgradeEffect.Exponential => "GROWTH COMPOUNDS " + rank * 5 + "% /MIN",
                UfoUpgradeEffect.Mothership => "SYSTEMS AND REWARDS " + percent,
                _ => "YOUR COLONY STARTS HERE"
            };
        }
        string UpgradeRequirementText(int index)
        {
            var node = UfoProgression.Nodes[index];
            if (progression.Rank(index) >= node.MaxRank) return node.Effect == UfoUpgradeEffect.Core ? "OWNED - CHOOSE A RESEARCH ROUTE" : "RESEARCH COMPLETE";
            if (progression.Unlocked(index)) return "COST " + progression.Cost(index) + " CREW";
            if (node.RequiredCount > 0)
            {
                int met = 0; foreach (var req in node.Requires) if (progression.Rank(req.Node) >= req.Rank) met++;
                return "NEEDS ANY " + node.RequiredCount + " CAPSTONES (" + met + "/" + node.RequiredCount + ")";
            }
            string needs = "NEEDS ";
            foreach (var req in node.Requires)
                if (progression.Rank(req.Node) < req.Rank)
                    needs += (needs.Length > 6 ? " + " : "") + UfoProgression.Nodes[UfoProgression.Index(req.Node)].ShortName + " " + req.Rank;
            return needs;
        }

        void DrawUpgradeMap()
        {
            spriteBatch.Draw(beamPixel, new Rectangle(0, 0, NATIVE_WIDTH, NATIVE_HEIGHT), new Color(5, 12, 24));
            DrawStringBitmap(spriteBatch, "UPGRADES", new Vector2(7, 5), new Color(130, 250, 244));
            int owned = 0; for (int i = 0; i < UfoProgression.Nodes.Length; i++) if (progression.Rank(i) > 0) owned++;
            font6.Draw(spriteBatch, owned + "/" + UfoProgression.Nodes.Length, new Vector2(99, 7), new Color(137, 171, 197));
            string wallet = "CREW " + Money(progression.Balance);
            font6.Draw(spriteBatch, wallet, new Vector2(281 - font6.Measure(wallet).X, 7), new Color(255, 220, 128));
            string status = "SAVE " + (activeSaveSlot + 1) + "  UFO RESEARCH & DEVELOPMENT";
            if (roundId != null && !roundActive)
                status = FormatRoundTime(roundSeconds) + "  " + RoundMultiplier.ToString("F2", CultureInfo.InvariantCulture) + "X  +" + Money(RoundReward) + (roundBanked ? " CREW" : " UNSAVED");
            font6.Draw(spriteBatch, status, new Vector2(7, 20), new Color(127, 163, 188));
            TechBox(UpgradeZoomButton, new Color(35, 87, 109), new Color(10, 31, 47));
            font6.Draw(spriteBatch, mapZoom.ToString("0.#", CultureInfo.InvariantCulture) + "X", new Vector2(257, 20), Color.Cyan);
            spriteBatch.End();
            GraphicsDevice.ScissorRectangle = new Rectangle(UpgradeMapView.X * 2, UpgradeMapView.Y * 2,
                UpgradeMapView.Width * 2, UpgradeMapView.Height * 2);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: playfieldRasterizer,
                transformMatrix: Matrix.CreateScale(2f));
            spriteBatch.Draw(beamPixel, UpgradeMapView, new Color(6, 16, 30));
            for (int i = 0; i < 180; i++)
            {
                var star = MapToScreen(new Vector2((i * 127 % 860) - 430, (i * 73 % 445) - 185));
                spriteBatch.Draw(beamPixel, new Rectangle((int)star.X, (int)star.Y, 1, 1), i % 7 == 0 ? new Color(62, 130, 173) : new Color(21, 49, 71));
            }
            for (int i = 0; i < UfoProgression.Nodes.Length; i++)
                foreach (var req in UfoProgression.Nodes[i].Requires) DrawTechEdge(UfoProgression.Index(req.Node), i, req.Rank);
            for (int i = 0; i < UfoProgression.Nodes.Length; i++)
            {
                var node = UfoProgression.Nodes[i]; var box = UpgradeNodeRect(i);
                if (!UpgradeMapView.Intersects(new Rectangle(box.X - 22, box.Y - 4, box.Width + 44, box.Height + 16))) continue;
                bool bought = progression.Rank(i) > 0, unlocked = progression.Unlocked(i), selected = i == mapSelection;
                bool canBuy = progression.CanBuy(i);
                bool hasUpgrade = progression.Rank(i) < node.MaxRank;
                Color tint = BranchColor(node.Branch);
                bool unaffordable = unlocked && hasUpgrade && !canBuy;
                Color border = bought && !unaffordable ? tint
                    : unlocked ? tint * (canBuy
                        ? .9f + .1f * (float)Math.Sin(mapPulse * 4) : .09f)
                    : new Color(47, 73, 96);
                // Any available rank glows, including the next rank of an
                // already-owned node. The glow is the purchase affordance.
                if (canBuy)
                {
                    float pulse = .55f + .45f * (float)Math.Sin(mapPulse * 4);
                    Color glow = tint * (.2f + .14f * pulse);
                    spriteBatch.Draw(beamPixel, new Rectangle(box.X - 5, box.Y - 5, box.Width + 10, box.Height + 10), glow);
                    spriteBatch.Draw(beamPixel, new Rectangle(box.X - 2, box.Y - 2, box.Width + 4, box.Height + 4), tint * (.24f + .12f * pulse));
                }
                bool selectedActive = canBuy || (bought && !unaffordable);
                if (selected) TechBox(new Rectangle(box.X - 3, box.Y - 3, box.Width + 6, box.Height + 6),
                    selectedActive ? new Color(239, 247, 229) : new Color(75, 86, 96), new Color(35, 48, 60));
                else if (bought) spriteBatch.Draw(beamPixel, new Rectangle(box.X - 1, box.Y - 1, box.Width + 2, box.Height + 2), tint * .18f);
                TechBox(box, border, selected ? new Color(20, 42, 58) : new Color(8, 23, 38),
                    Math.Max(1, (int)(mapZoom * MapConnectorScale)));
                Color iconTint = !unlocked ? new Color(63, 87, 110)
                    : !unaffordable ? tint * (canBuy ? 1.05f + .1f * (float)Math.Sin(mapPulse * 4) : 1)
                    : tint * .12f;
                DrawTechIcon(node, new Vector2(box.Center.X, box.Center.Y), mapZoom * MapNodeVisualScale, iconTint);
                if (node.MaxRank > 1 && mapZoom >= 1)
                    for (int rank = 0; rank < node.MaxRank; rank++)
                        spriteBatch.Draw(beamPixel, new Rectangle(box.Center.X - node.MaxRank * 2 + rank * 4, box.Bottom - 3, 1, 1),
                            rank < progression.Rank(i) ? tint : new Color(32, 51, 69));
                if (mapZoom >= 1) font6.Draw(spriteBatch, node.ShortName,
                    new Vector2(box.Center.X - font6.Measure(node.ShortName).X * MapLabelScale / 2, box.Bottom + 4),
                    selected ? Color.White : border, MapLabelScale);
            }
            void Label(string label, Vector2 world, Vector2 overview, UfoBranch branch)
            {
                Vector2 pos = mapZoom == .5f ? overview : MapToScreen(world);
                var size = font6.Measure(label) * MapLabelScale;
                spriteBatch.Draw(beamPixel, new Rectangle((int)pos.X - 2, (int)pos.Y - 1, (int)size.X + 4, 8), new Color(6, 16, 30));
                font6.Draw(spriteBatch, label, pos, BranchColor(branch) * .8f, MapLabelScale);
            }
            Label("WEAPONS", new Vector2(-295, -130), new Vector2(10, 30), UfoBranch.Weapons);
            Label("BEAM SYSTEMS", new Vector2(210, -182), new Vector2(166, 30), UfoBranch.Beam);
            Label("SHIP SYSTEMS", new Vector2(-293, 207), new Vector2(10, 146), UfoBranch.Ship);
            Label("YIELD SYSTEMS", new Vector2(210, 177), new Vector2(202, 146), UfoBranch.Yield);
            // A clickable overview keeps far-away branches discoverable.
            if (mapZoom > .5f)
            {
                TechBox(UpgradeMiniMap, new Color(42, 92, 119), new Color(4, 10, 20));
                foreach (var node in UfoProgression.Nodes)
                {
                    int x = UpgradeMiniMap.X + 1 + (int)((node.Position.X + 430) / 860 * (UpgradeMiniMap.Width - 2));
                    int y = UpgradeMiniMap.Y + 1 + (int)((node.Position.Y + 185) / 445 * (UpgradeMiniMap.Height - 2));
                    spriteBatch.Draw(beamPixel, new Rectangle(x, y, 1, 1), BranchColor(node.Branch));
                }
                int cameraX = UpgradeMiniMap.X + (int)((mapCamera.X + 430) / 860 * UpgradeMiniMap.Width);
                int cameraY = UpgradeMiniMap.Y + (int)((mapCamera.Y + 185) / 445 * UpgradeMiniMap.Height);
                spriteBatch.Draw(beamPixel, new Rectangle(cameraX - 1, cameraY - 1, 3, 3), Color.White);
            }
            spriteBatch.End();
            GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, NATIVE_WIDTH * 2, NATIVE_HEIGHT * 2);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: Matrix.CreateScale(2f));
            font6.Draw(spriteBatch, "DRAG PAN  WHEEL ZOOM  C CORE  ARROWS SELECT", new Vector2(7, 158), new Color(103, 143, 167));
            var selectedNode = UfoProgression.Nodes[mapSelection];
            Color selectedTint = BranchColor(selectedNode.Branch);
            TechBox(new Rectangle(4, 166, 280, 31), new Color(38, 74, 98), new Color(11, 27, 42));
            int current = progression.Rank(mapSelection);
            font6.Draw(spriteBatch, selectedNode.Title, new Vector2(8, 169), selectedTint);
            string level = "LV " + current + "/" + selectedNode.MaxRank;
            font6.Draw(spriteBatch, level, new Vector2(280 - font6.Measure(level).X, 169), new Color(190, 227, 237));
            string effect = UpgradeEffectAt(mapSelection, current);
            if (current < selectedNode.MaxRank)
            {
                string next = UpgradeEffectAt(mapSelection, current + 1);
                // Show compact current -> next values for the common percent stats.
                int plus = next.IndexOf('+');
                effect = plus >= 0 && effect.Contains('+') ? effect + " > " + next.Substring(plus) : "NEXT: " + next;
            }
            if (effect.Length > 44) effect = "NEXT: " + UpgradeEffectAt(mapSelection, Math.Min(selectedNode.MaxRank, current + 1));
            font6.Draw(spriteBatch, effect, new Vector2(8, 178), Color.White);
            font6.Draw(spriteBatch, mapMessageTime > 0 ? mapMessage : UpgradeRequirementText(mapSelection), new Vector2(8, 188), new Color(152, 183, 201));
            TechBox(UpgradeLaunchButton, new Color(75, 141, 200), new Color(23, 59, 91));
            font6.Draw(spriteBatch, "R NEXT FLIGHT", new Vector2(13, 204), Color.White);
            TechBox(UpgradeBuyButton, progression.CanBuy(mapSelection) ? selectedTint : new Color(46, 73, 88), new Color(15, 46, 48));
            font6.Draw(spriteBatch, mapInputReady ? "ENTER/X BUY" : "RELEASE FIRE", new Vector2(112, 204), Color.White);
            font6.Draw(spriteBatch, "ESC MENU", new Vector2(198, 204), new Color(141, 176, 195));
        }

        void DrawTechIcon(UfoUpgrade node, Vector2 center, float scale, Color tint)
        {
            void P(int x, int y, int w = 1, int h = 1) => spriteBatch.Draw(beamPixel,
                new Rectangle((int)Math.Round(center.X + x * scale), (int)Math.Round(center.Y + y * scale),
                    Math.Max(1, (int)Math.Round(w * scale)), Math.Max(1, (int)Math.Round(h * scale))), tint);
            void L(int x, int y, int xx, int yy) => drawBeamStroke(center + new Vector2(x, y) * scale,
                center + new Vector2(xx, yy) * scale, Math.Max(1, scale), tint);
            void Person(int x, int y) { P(x, y, 2, 2); P(x - 1, y + 3, 4, 4); P(x - 1, y + 7, 1, 2); P(x + 2, y + 7, 1, 2); }
            switch (node.Effect)
            {
                case UfoUpgradeEffect.Core:
                case UfoUpgradeEffect.Mothership:
                    DrawUfoSprite(0, new Rectangle((int)(center.X - 12 * scale), (int)(center.Y - 6 * scale), (int)(24 * scale), (int)(12 * scale)), tint);
                    if (node.Effect == UfoUpgradeEffect.Mothership) { L(-7, -8, 7, -8); P(-1, -10, 2, 2); }
                    break;
                case UfoUpgradeEffect.Fire:
                case UfoUpgradeEffect.TwinShot:
                case UfoUpgradeEffect.TripleShot:
                    int shots = node.Effect == UfoUpgradeEffect.TripleShot ? 3 : 2;
                    for (int i = 0; i < shots; i++) { int y = -5 + i * 4; P(-6, y, 9, 2); P(3, y - 1, 2, 4); P(5, y, 2, 2); }
                    break;
                case UfoUpgradeEffect.Plasma:
                    L(-5, -3, -1, -7); L(1, -7, 5, -3); L(5, 0, 1, 4); L(-1, 4, -5, 0); P(-1, -3, 3, 3); break;
                case UfoUpgradeEffect.Tractor:
                    P(-6, -6, 3, 9); P(3, -6, 3, 9); P(-3, 2, 6, 3); P(-5, -7, 1, 2); P(4, -7, 1, 2); break;
                case UfoUpgradeEffect.BeamWidth:
                case UfoUpgradeEffect.Matrix:
                    P(-2, -7, 4, 2); L(-2, -3, -6, 5); L(2, -3, 6, 5); P(-1, -2, 2, 8);
                    if (node.Effect == UfoUpgradeEffect.Matrix) { P(-7, 5, 4, 2); P(4, 5, 4, 2); }
                    break;
                case UfoUpgradeEffect.Capacity:
                    int count = (int)node.Amount;
                    for (int i = 0; i < Math.Min(3, count); i++) Person(-6 + i * 5, -5);
                    if (count > 3) { P(-5, -8, 2, 2); P(4, -8, 2, 2); }
                    break;
                case UfoUpgradeEffect.Focus:
                case UfoUpgradeEffect.PointDefense:
                    L(-4, -5, 4, -5); L(-4, 4, 4, 4); L(-5, -4, -5, 3); L(5, -4, 5, 3);
                    L(-8, 0, -2, 0); L(2, 0, 8, 0); L(0, -8, 0, -2); L(0, 2, 0, 7); P(-1, -1, 2, 2); break;
                case UfoUpgradeEffect.Engine:
                case UfoUpgradeEffect.Warp:
                    for (int i = 0; i < 3; i++) { L(-6 + i * 4, -5, -2 + i * 4, -1); L(-2 + i * 4, -1, -6 + i * 4, 3); }
                    if (node.Effect == UfoUpgradeEffect.Warp) { P(-6, -8, 12, 1); P(-6, 6, 12, 1); }
                    break;
                case UfoUpgradeEffect.FlightClearance:
                    L(-7, -6, 7, -6); L(0, -3, 0, 4); L(-4, 0, 0, 4); L(4, 0, 0, 4); L(-7, 7, 7, 7); break;
                case UfoUpgradeEffect.Hull:
                case UfoUpgradeEffect.Nanohull:
                    L(-6, -6, 0, -8); L(0, -8, 6, -6); L(-6, -6, -5, 1); L(6, -6, 5, 1); L(-5, 1, 0, 6); L(0, 6, 5, 1);
                    if (node.Effect == UfoUpgradeEffect.Nanohull) { P(-2, -4, 4, 5); P(-4, -2, 8, 1); }
                    else { P(-3, -4, 6, 5); P(-1, 1, 2, 2); }
                    break;
                case UfoUpgradeEffect.Repair:
                    L(-6, 5, 3, -4); L(-5, 5, 4, -4); P(1, -7, 2, 4); P(5, -7, 2, 4); P(2, -3, 4, 2); break;
                case UfoUpgradeEffect.AutoRepair:
                    P(-2, -7, 4, 13); P(-7, -2, 14, 4); break;
                case UfoUpgradeEffect.LongHaul:
                    L(-4, -6, 4, -6); L(-4, 5, 4, 5); L(-6, -4, -6, 3); L(6, -4, 6, 3); L(-4, -6, -6, -4); L(4, -6, 6, -4); L(-4, 5, -6, 3); L(4, 5, 6, 3); L(0, -4, 0, 0); L(0, 0, 4, 0); break;
                case UfoUpgradeEffect.StartingBonus:
                    P(-2, -5, 4, 7); P(-1, -7, 2, 2); P(-4, 0, 2, 4); P(2, 0, 2, 4); L(-1, 3, -1, 7); L(1, 3, 1, 5); break;
                case UfoUpgradeEffect.Interest:
                    for (int i = 0; i < 4; i++) { P(-5, -6 + i * 3, 10, 1); P(-6, -5 + i * 3, 12, 1); }
                    break;
                case UfoUpgradeEffect.Exponential:
                    L(-7, 6, -7, -7); L(-7, 6, 7, 6); L(-5, 4, 0, 1); L(0, 1, 4, -6); L(4, -6, 7, -6); L(4, -6, 4, -2); break;
                default:
                    P(-6, 0, 3, 5); P(-1, -4, 3, 9); P(4, -8, 3, 13); break;
            }
        }
    }
}
