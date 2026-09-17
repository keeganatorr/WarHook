using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonogameTest
{
    public partial class Game1
    {
        static readonly Rectangle UpgradeMapView = new Rectangle(8, 49, 272, 105);
        static readonly Rectangle UpgradeLaunchButton = new Rectangle(8, 200, 92, 13);
        static readonly Rectangle UpgradeBuyButton = new Rectangle(192, 200, 88, 13);
        int mapSelection;
        float mapScroll;
        bool mapInputReady, mapDragging;
        MouseState mapPreviousMouse;
        Vector2 mapDragStart;
        float mapDragScroll;
        string mapMessage = "";
        double mapMessageTime;
        static string Money(double value) => value >= 10000 ? (value / 1000).ToString("F1", CultureInfo.InvariantCulture) + "K"
            : value.ToString("F2", CultureInfo.InvariantCulture);
        static string FormatRoundTime(double seconds) => ((int)seconds / 60).ToString("D2") + ":" + ((int)seconds % 60).ToString("D2");

        void EndIncrementalRound()
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
            OpenUpgradeMap();
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

        void FocusUpgradeNode()
        {
            mapScroll = Math.Clamp(20 + mapSelection / 4 * 64 - UpgradeMapView.Height / 2f, 0, 224);
        }

        Vector2 UpgradeNodePosition(int index) => new Vector2(42 + index % 4 * 68,
            UpgradeMapView.Y + 20 + index / 4 * 64 - mapScroll);
        Rectangle UpgradeNodeRect(int index)
        {
            Vector2 p = UpgradeNodePosition(index);
            return new Rectangle((int)p.X - 29, (int)p.Y - 14, 58, 28);
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
                : !progression.Unlocked(mapSelection) ? "UNLOCK THE CONNECTED NODE FIRST"
                : progression.Rank(mapSelection) >= UfoProgression.Nodes[mapSelection].MaxRank ? "NODE COMPLETE"
                : "COLLECT MORE SOLDIERS NEXT ROUND";
            mapMessageTime = 2;
        }

        void UpdateUpgradeMap(GameTime time, Func<Keys, bool> pressed, Func<Buttons, bool> padPressed,
            bool accept, bool acceptHeld, bool cancel)
        {
            mapMessageTime = Math.Max(0, mapMessageTime - time.ElapsedGameTime.TotalSeconds);
            if (!acceptHeld) mapInputReady = true;
            int oldSelection = mapSelection;
            if (pressed(Keys.Left) || pressed(Keys.A) || padPressed(Buttons.DPadLeft)) mapSelection -= mapSelection % 4 > 0 ? 1 : 0;
            if (pressed(Keys.Right) || pressed(Keys.D) || padPressed(Buttons.DPadRight)) mapSelection += mapSelection % 4 < 3 ? 1 : 0;
            if (pressed(Keys.Up) || pressed(Keys.W) || padPressed(Buttons.DPadUp)) mapSelection = Math.Max(mapSelection % 4, mapSelection - 4);
            if (pressed(Keys.Down) || pressed(Keys.S) || padPressed(Buttons.DPadDown)) mapSelection = Math.Min(16 + mapSelection % 4, mapSelection + 4);
            if (oldSelection != mapSelection) { FocusUpgradeNode(); beamAudio?.PlayMenuBlip(); }

            MouseState mouse = Mouse.GetState();
            Vector2 pointer = new Vector2((mouse.X - rect.X) * NATIVE_WIDTH / (float)Math.Max(1, rect.Width),
                (mouse.Y - rect.Y) * NATIVE_HEIGHT / (float)Math.Max(1, rect.Height));
            bool mouseDown = mouse.LeftButton == ButtonState.Pressed && mapPreviousMouse.LeftButton == ButtonState.Released;
            bool mouseUp = mouse.LeftButton == ButtonState.Released && mapPreviousMouse.LeftButton == ButtonState.Pressed;
            bool inMap = UpgradeMapView.Contains((int)pointer.X, (int)pointer.Y);
            if (inMap && mouse.ScrollWheelValue != mapPreviousMouse.ScrollWheelValue)
                mapScroll = Math.Clamp(mapScroll - (mouse.ScrollWheelValue - mapPreviousMouse.ScrollWheelValue) / 4f, 0, 224);
            if (mouseDown && inMap) { mapDragging = true; mapDragStart = pointer; mapDragScroll = mapScroll; }
            if (mapDragging && mouse.LeftButton == ButtonState.Pressed)
                mapScroll = Math.Clamp(mapDragScroll - (pointer.Y - mapDragStart.Y), 0, 224);
            if (mouseUp && mapDragging)
            {
                if (Vector2.Distance(pointer, mapDragStart) < 4 && inMap)
                    for (int i = 0; i < UfoProgression.Nodes.Length; i++)
                        if (UpgradeNodeRect(i).Contains((int)pointer.X, (int)pointer.Y)) { mapSelection = i; beamAudio?.PlayMenuBlip(); break; }
                mapDragging = false;
            }
            bool buyClicked = mouseDown && UpgradeBuyButton.Contains((int)pointer.X, (int)pointer.Y);
            bool launchClicked = mouseDown && UpgradeLaunchButton.Contains((int)pointer.X, (int)pointer.Y);
            mapPreviousMouse = mouse;
            if (buyClicked || (mapInputReady && accept)) PurchaseUpgrade();
            if (launchClicked || pressed(Keys.R) || padPressed(Buttons.Start) || cancel)
            {
                // Preserve an unbanked payout if the browser storage is full.
                if (!roundActive && roundId != null && !roundBanked && !progression.Bank(roundId, RoundReward))
                { mapMessage = progression.Error; mapMessageTime = 3; return; }
                roundBanked = true;
                beginTransition(cancel ? MenuScreen.Main : MenuScreen.Playing, false);
            }
        }

        void DrawUpgradeMap()
        {
            spriteBatch.Draw(beamPixel, new Rectangle(0, 0, NATIVE_WIDTH, NATIVE_HEIGHT), new Color(7, 14, 28));
            DrawStringBitmap(spriteBatch, "UPGRADE MAP", new Vector2(8, 6), new Color(110, 244, 235));
            string wallet = "CREW " + Money(progression.Balance);
            font6.Draw(spriteBatch, wallet, new Vector2(NATIVE_WIDTH - 8 - font6.Measure(wallet).X, 8), new Color(255, 220, 128));
            if (roundId != null && !roundActive)
            {
                string reward = roundSoldiers + " SOLDIERS  " + RoundMultiplier.ToString("F2", CultureInfo.InvariantCulture) + "X  EARNED " + Money(RoundReward);
                font6.Draw(spriteBatch, reward, new Vector2(8, 22), Color.White);
                font6.Draw(spriteBatch, "SURVIVED " + FormatRoundTime(roundSeconds) + (roundBanked ? "   REWARDS BANKED" : "   SAVE PENDING"),
                    new Vector2(8, 32), new Color(140, 179, 198));
            }
            else font6.Draw(spriteBatch, "PERMANENT UPGRADES FOR EVERY FLIGHT", new Vector2(8, 25), Color.White);
            font6.Draw(spriteBatch, "ARROWS SELECT  WHEEL/DRAG SCROLL", new Vector2(8, 41), new Color(122, 158, 180));
            spriteBatch.End();
            GraphicsDevice.ScissorRectangle = UpgradeMapView;
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: playfieldRasterizer);
            spriteBatch.Draw(beamPixel, UpgradeMapView, new Color(10, 22, 38));
            for (int yy = -((int)mapScroll % 12); yy < UpgradeMapView.Height; yy += 12)
                for (int xx = 14; xx < NATIVE_WIDTH; xx += 12)
                    spriteBatch.Draw(beamPixel, new Rectangle(xx, UpgradeMapView.Y + yy, 1, 1), new Color(25, 46, 60));
            for (int i = 0; i < UfoProgression.Nodes.Length; i++)
            {
                var node = UfoProgression.Nodes[i];
                if (node.Parent >= 0)
                    drawBeamStroke(UpgradeNodePosition(node.Parent) + new Vector2(0, 14), UpgradeNodePosition(i) - new Vector2(0, 14), 2,
                        progression.Unlocked(i) ? new Color(57, 145, 150) : new Color(34, 52, 71));
            }
            for (int i = 0; i < UfoProgression.Nodes.Length; i++)
            {
                var node = UfoProgression.Nodes[i];
                Rectangle box = UpgradeNodeRect(i);
                bool unlocked = progression.Unlocked(i), bought = progression.Rank(i) > 0, selected = i == mapSelection;
                Color border = selected ? new Color(255, 218, 123) : bought ? new Color(80, 222, 183)
                    : unlocked ? new Color(63, 157, 181) : new Color(44, 62, 80);
                spriteBatch.Draw(beamPixel, box, border);
                spriteBatch.Draw(beamPixel, new Rectangle(box.X + 1, box.Y + 1, box.Width - 2, box.Height - 2),
                    selected ? new Color(39, 53, 61) : new Color(13, 29, 45));
                Vector2 size = font6.Measure(node.ShortName);
                font6.Draw(spriteBatch, node.ShortName, new Vector2(box.Center.X - size.X / 2, box.Y + 5), unlocked ? Color.White : new Color(97, 118, 137));
                string status = !unlocked ? "LOCKED" : progression.Rank(i) + "/" + node.MaxRank;
                font6.Draw(spriteBatch, status, new Vector2(box.Center.X - font6.Measure(status).X / 2, box.Y + 17), border);
            }
            spriteBatch.End();
            GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, NATIVE_WIDTH, NATIVE_HEIGHT);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            // The scroll thumb shows that the network extends below the view.
            spriteBatch.Draw(beamPixel, new Rectangle(282, 49, 2, 105), new Color(30, 52, 67));
            spriteBatch.Draw(beamPixel, new Rectangle(282, 49 + (int)(mapScroll / 224 * 77), 2, 28), new Color(95, 217, 214));
            var selectedNode = UfoProgression.Nodes[mapSelection];
            spriteBatch.Draw(beamPixel, new Rectangle(8, 158, 272, 38), new Color(18, 34, 49));
            font6.Draw(spriteBatch, selectedNode.Title, new Vector2(12, 161), new Color(255, 220, 128));
            font6.Draw(spriteBatch, selectedNode.Description, new Vector2(12, 173), Color.White);
            string detail = !progression.Unlocked(mapSelection)
                ? "NEEDS " + UfoProgression.Nodes[selectedNode.Parent].ShortName + " RANK " + selectedNode.RequiredRank
                : progression.Rank(mapSelection) == selectedNode.MaxRank ? "ALL RANKS PURCHASED"
                : "COST " + progression.Cost(mapSelection) + " CREW   RANK " + progression.Rank(mapSelection) + "/" + selectedNode.MaxRank;
            if (mapMessageTime > 0) detail = mapMessage;
            font6.Draw(spriteBatch, detail, new Vector2(12, 185), new Color(136, 191, 208));
            spriteBatch.Draw(beamPixel, UpgradeLaunchButton, new Color(32, 93, 97));
            font6.Draw(spriteBatch, "R NEXT FLIGHT", new Vector2(14, 204), Color.White);
            font6.Draw(spriteBatch, "ESC MENU", new Vector2(116, 204), new Color(145, 178, 194));
            spriteBatch.Draw(beamPixel, UpgradeBuyButton, progression.CanBuy(mapSelection) ? new Color(99, 77, 37) : new Color(30, 43, 54));
            font6.Draw(spriteBatch, mapInputReady ? "ENTER/X BUY" : "RELEASE FIRE", new Vector2(199, 204), Color.White);
        }
    }
}
