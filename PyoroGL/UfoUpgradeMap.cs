using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonogameTest
{
    public partial class Game1
    {
        // The research graph uses a 576x432 logical canvas rendered at 2x.
        // These controls live in that larger coordinate space as well.
        static readonly Rectangle UpgradeMapView = new Rectangle(0, 0, 576, 432);
        static readonly Rectangle UpgradeLaunchButton = new Rectangle(450, 382, 110, 34);
        static readonly Rectangle UpgradeBuyButton = new Rectangle(18, 382, 150, 34);
        static readonly Rectangle UpgradeMenuButton = new Rectangle(530, 10, 32, 28);
        static readonly Rectangle UpgradeMiniMap = new Rectangle(496, 50, 66, 42);
        static readonly Rectangle UpgradeZoomButton = new Rectangle(458, 18, 28, 22);
        // The research map is rendered above the native game resolution so its
        // dense graph and bitmap type stay crisp when shown in the menu.
        const int UpgradeRenderScale = 4;
        // Keep map geometry on whole pixels at the 4x render scale. The
        // smaller logical footprint leaves room for the complete web and
        // prevents labels from crowding the connectors at the default view.
        const float MapNodeVisualScale = 1.6f;
        const float MapConnectorScale = .9f;
        const float MapLabelScale = 1f;
        const int UpgradeMapRenderScale = 2;
        int mapSelection = UfoProgression.Index("core");
        Vector2 mapCamera, mapDragCamera, mapDragStart;
        float mapZoom = .5f;
        double mapPulse;
        bool mapInputReady, mapDragging;
        MouseState mapPreviousMouse;
        string mapMessage = "";
        double mapMessageTime;
        static string Money(double value) => value >= 10000 ? (value / 1000).ToString("F1", CultureInfo.InvariantCulture) + "K"
            : value.ToString("F2", CultureInfo.InvariantCulture);
        static string FormatRoundTime(double seconds) => ((int)seconds / 60).ToString("D2") + ":" + ((int)seconds % 60).ToString("D2");
        static Vector2 MapViewCenter => new Vector2(UpgradeMapView.Center.X, UpgradeMapView.Center.Y);
        float MapWorldScale => mapZoom * 1.2f;
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
            // Header buttons use the compact 288x216 shell. The graph is a
            // separate 576x432 logical surface, so promote map input before
            // hit testing, panning, and node selection.
            Vector2 mapPointer = pointer * UpgradeMapRenderScale;
            Point mapPoint = new Point((int)mapPointer.X, (int)mapPointer.Y);
            bool down = mouse.LeftButton == ButtonState.Pressed && mapPreviousMouse.LeftButton == ButtonState.Released;
            bool up = mouse.LeftButton == ButtonState.Released && mapPreviousMouse.LeftButton == ButtonState.Pressed;
            bool inMap = UpgradeMapView.Contains(mapPoint);
            if (down && mapZoom > .5f && UpgradeMiniMap.Contains(mapPoint))
            {
                mapCamera = new Vector2((mapPointer.X - UpgradeMiniMap.X) / UpgradeMiniMap.Width * 860 - 430,
                    (mapPointer.Y - UpgradeMiniMap.Y) / UpgradeMiniMap.Height * 445 - 185);
                ClampMapCamera();
            }
            else if (down && inMap) { mapDragging = true; mapDragStart = mapPointer; mapDragCamera = mapCamera; }
            if (mapDragging && mouse.LeftButton == ButtonState.Pressed)
            { mapCamera = mapDragCamera - (mapPointer - mapDragStart) / MapWorldScale; ClampMapCamera(); }
            if (up && mapDragging)
            {
                if (Vector2.Distance(mapPointer, mapDragStart) < 8 && inMap)
                    for (int i = 0; i < UfoProgression.Nodes.Length; i++)
                        if (UpgradeNodeRect(i).Contains(mapPoint)) { mapSelection = i; beamAudio?.PlayMenuBlip(); break; }
                mapDragging = false;
            }
            int wheel = mouse.ScrollWheelValue - mapPreviousMouse.ScrollWheelValue;
            if (inMap && wheel != 0) ZoomUpgradeMap(wheel > 0 ? Math.Min(2, mapZoom * 2) : Math.Max(.5f, mapZoom / 2), mapPointer);
            if ((down && UpgradeZoomButton.Contains(mapPoint)) || padPressed(Buttons.RightShoulder))
                ZoomUpgradeMap(mapZoom >= 2 ? .5f : mapZoom * 2, MapViewCenter);
            bool buy = down && UpgradeBuyButton.Contains(mapPoint);
            bool launch = down && UpgradeLaunchButton.Contains(mapPoint);
            cancel |= down && UpgradeMenuButton.Contains(mapPoint);
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
        void RoundedBox(Rectangle box, Color color, int radius = 2)
        {
            if (box.Width <= 0 || box.Height <= 0) return;
            radius = Math.Min(radius, Math.Min(box.Width, box.Height) / 2);
            if (radius <= 0)
            {
                spriteBatch.Draw(beamPixel, box, color);
                return;
            }

            // Pixel-cut corners keep the box native-pixel sharp while giving
            // the dense map a softer silhouette than a square rectangle.
            spriteBatch.Draw(beamPixel,
                new Rectangle(box.X + radius, box.Y, Math.Max(1, box.Width - radius * 2), box.Height), color);
            spriteBatch.Draw(beamPixel,
                new Rectangle(box.X, box.Y + radius, radius, Math.Max(1, box.Height - radius * 2)), color);
            spriteBatch.Draw(beamPixel,
                new Rectangle(box.Right - radius, box.Y + radius, radius, Math.Max(1, box.Height - radius * 2)), color);
        }

        void TechBox(Rectangle box, Color border, Color fill, int thickness = 1)
        {
            RoundedBox(box, border, 2);
            RoundedBox(new Rectangle(box.X + thickness, box.Y + thickness,
                Math.Max(1, box.Width - thickness * 2), Math.Max(1, box.Height - thickness * 2)), fill, 1);
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
                UfoUpgradeEffect.SoldierValue => "SOLDIER VALUE X" + (rank + 1),
                UfoUpgradeEffect.BeamWidth => "CONE WIDTH " + percent,
                UfoUpgradeEffect.Growth => "REWARD GROWTH " + percent,
                UfoUpgradeEffect.StartingBonus => "START +" + (node.Amount * rank).ToString("F2", CultureInfo.InvariantCulture) + "X",
                UfoUpgradeEffect.Capacity => "BEAM CAPACITY " + (rank > 0 ? (int)node.Amount : (int)node.Amount - 1),
                UfoUpgradeEffect.TwinShot => rank > 0 ? "TWO PARALLEL SHOTS" : "ONE SHOT",
                UfoUpgradeEffect.TripleShot => rank > 0 ? "THREE PARALLEL SHOTS" : "TWO SHOTS",
                UfoUpgradeEffect.Plasma => "FIRE/BULLET SPEED " + percent,
                UfoUpgradeEffect.PointDefense => "INTERCEPT +" + rank * 3 + "PX / BLAST " + rank * 8 + "PX",
                UfoUpgradeEffect.Shield => rank > 0
                    ? "SHIELD REGEN " + Math.Max(.5, 2 - Math.Max(0, rank - 1) * node.Amount).ToString("F2", CultureInfo.InvariantCulture) + " SEC"
                    : "SHIELD OFFLINE",
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
            // drawTitleScene starts a compact batch for the other menu scenes.
            // The research screen replaces it with a full 576x432 canvas.
            spriteBatch.End();
            GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, NATIVE_WIDTH * UpgradeRenderScale, NATIVE_HEIGHT * UpgradeRenderScale);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: playfieldRasterizer,
                transformMatrix: Matrix.CreateScale(UpgradeMapRenderScale));

            // Deep blue scan-lined space gives the tree a dedicated research
            // surface, with a quiet frame instead of a stack of menu strips.
            spriteBatch.Draw(beamPixel, UpgradeMapView, new Color(5, 9, 38));
            for (int y = 2; y < UpgradeMapView.Height; y += 2)
                spriteBatch.Draw(beamPixel, new Rectangle(0, y, UpgradeMapView.Width, 1), new Color(20, 26, 84, 52));
            for (int i = 0; i < 180; i++)
            {
                int x = (i * 97) % UpgradeMapView.Width;
                int y = (i * 53) % UpgradeMapView.Height;
                int size = i % 11 == 0 ? 2 : 1;
                spriteBatch.Draw(beamPixel, new Rectangle(x, y, size, size), i % 7 == 0 ? new Color(40, 52, 107) : new Color(17, 24, 72));
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
                Vector2 pos = mapZoom == .5f ? overview * UpgradeMapRenderScale : MapToScreen(world);
                var size = font6.Measure(label) * MapLabelScale;
                spriteBatch.Draw(beamPixel, new Rectangle((int)pos.X - 3, (int)pos.Y - 2, (int)size.X + 6, 10), new Color(5, 9, 38));
                font6.Draw(spriteBatch, label, pos, BranchColor(branch) * .8f, MapLabelScale);
            }
            Label("WEAPONS", new Vector2(-295, -130), new Vector2(12, 18), UfoBranch.Weapons);
            Label("BEAM SYSTEMS", new Vector2(210, -182), new Vector2(410, 18), UfoBranch.Beam);
            Label("SHIP SYSTEMS", new Vector2(-293, 207), new Vector2(12, 390), UfoBranch.Ship);
            Label("YIELD SYSTEMS", new Vector2(210, 177), new Vector2(410, 390), UfoBranch.Yield);

            // Header and wallet follow the reference's quiet, information-first
            // layout. The selected node becomes a compact purchase tooltip.
            font6.Draw(spriteBatch, "UFO RESEARCH", new Vector2(18, 12), new Color(175, 188, 238));
            string wallet = "CREW " + Money(progression.Balance);
            TechBox(new Rectangle(18, 48, 108, 28), new Color(176, 184, 224), new Color(16, 19, 56));
            font6.Draw(spriteBatch, wallet, new Vector2(28, 59), new Color(255, 224, 138));
            TechBox(UpgradeMenuButton, new Color(255, 100, 93), new Color(82, 18, 29));
            font6.Draw(spriteBatch, "X", new Vector2(541, 18), Color.White, 2);
            TechBox(UpgradeZoomButton, new Color(75, 110, 193), new Color(14, 22, 67));
            font6.Draw(spriteBatch, mapZoom.ToString("0.#", CultureInfo.InvariantCulture) + "X", new Vector2(463, 25), Color.White);

            var selectedNode = UfoProgression.Nodes[mapSelection];
            Color selectedTint = BranchColor(selectedNode.Branch);
            TechBox(new Rectangle(190, 18, 210, 78), new Color(153, 161, 191), new Color(30, 31, 47));
            spriteBatch.Draw(beamPixel, new Rectangle(190, 76, 210, 20), selectedTint * .65f);
            int current = progression.Rank(mapSelection);
            font6.Draw(spriteBatch, selectedNode.Title, new Vector2(202, 27), Color.White);
            string level = "LV " + current + "/" + selectedNode.MaxRank;
            font6.Draw(spriteBatch, level, new Vector2(202, 39), new Color(215, 221, 250));
            string effect = UpgradeEffectAt(mapSelection, current);
            if (current < selectedNode.MaxRank)
            {
                string next = UpgradeEffectAt(mapSelection, current + 1);
                // Show compact current -> next values for the common percent stats.
                int plus = next.IndexOf('+');
                effect = plus >= 0 && effect.Contains('+') ? effect + " > " + next.Substring(plus) : "NEXT: " + next;
            }
            if (effect.Length > 44) effect = "NEXT: " + UpgradeEffectAt(mapSelection, Math.Min(selectedNode.MaxRank, current + 1));
            if (effect.Length > 29) effect = effect.Substring(0, 29);
            font6.Draw(spriteBatch, effect, new Vector2(202, 51), Color.White);
            string requirement = mapMessageTime > 0 ? mapMessage : UpgradeRequirementText(mapSelection);
            if (requirement.Length > 30) requirement = requirement.Substring(0, 30);
            font6.Draw(spriteBatch, requirement, new Vector2(202, 64), new Color(194, 201, 220));
            font6.Draw(spriteBatch, "COST " + progression.Cost(mapSelection) + " CREW", new Vector2(202, 82), Color.White);

            TechBox(UpgradeBuyButton, progression.CanBuy(mapSelection) ? selectedTint : new Color(78, 82, 112), new Color(17, 20, 57));
            font6.Draw(spriteBatch, mapInputReady ? "ENTER BUY" : "RELEASE", new Vector2(35, 395), Color.White);
            TechBox(UpgradeLaunchButton, new Color(75, 141, 200), new Color(23, 59, 91));
            font6.Draw(spriteBatch, "START", new Vector2(480, 395), Color.White, 1.5f);
            font6.Draw(spriteBatch, "DRAG  WHEEL ZOOM  ARROWS SELECT", new Vector2(194, 404), new Color(116, 133, 190));
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
                case UfoUpgradeEffect.Shield:
                    L(-7, -3, -4, -7); L(-4, -7, 4, -7); L(4, -7, 7, -3);
                    L(7, -3, 7, 3); L(7, 3, 4, 7); L(4, 7, -4, 7);
                    L(-4, 7, -7, 3); L(-7, 3, -7, -3); P(-2, -1, 4, 2); break;
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
                case UfoUpgradeEffect.SoldierValue:
                    Person(-1, -7); P(-7, 2, 14, 2); P(-5, 5, 10, 2); P(-2, 8, 4, 2); break;
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
