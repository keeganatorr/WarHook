using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MonogameTest
{
    public partial class Game1
    {
        readonly UfoProgression[] saveSlots = new UfoProgression[3];
        int saveSelection, activeSaveSlot = -1;
        bool creatingSave, confirmReplace, replaceAccepted;
        string saveMenuError = "";
        MouseState savePreviousMouse;

        void LoadSaveSlots()
        {
            saveMenuError = "";
            bool anySave = false;
            for (int i = 0; i < saveSlots.Length; i++)
            {
                saveSlots[i] = new UfoProgression(shotsMode, slot: i + 1);
                anySave |= saveSlots[i].Exists;
            }
            // Keep the legacy save intact as a backup. Only migrate when there
            // are no slot saves yet, so subsequent new games cannot reimport it.
            if (!shotsMode && !anySave)
            {
                var legacy = new UfoProgression();
                if (!string.IsNullOrEmpty(legacy.Error)) saveMenuError = legacy.Error;
                else if (legacy.Exists && !saveSlots[0].Import(legacy)) saveMenuError = saveSlots[0].Error;
            }
        }

        void OpenSaveSlots(bool create)
        {
            LoadSaveSlots();
            creatingSave = create; confirmReplace = replaceAccepted = false;
            saveSelection = activeSaveSlot >= 0 ? activeSaveSlot : 0;
            for (int i = 0; i < saveSlots.Length; i++)
                if (create ? !saveSlots[i].Exists : saveSlots[i].Exists) { saveSelection = i; break; }
            savePreviousMouse = Mouse.GetState();
            screen = MenuScreen.SaveSlots;
        }

        void ActivateSave(bool create)
        {
            var slot = saveSlots[saveSelection];
            if (create)
            {
                if (!slot.StartNew()) { saveMenuError = slot.Error; return; }
            }
            else if (!slot.Exists || !string.IsNullOrEmpty(slot.Error))
            {
                saveMenuError = !slot.Exists ? "EMPTY SLOT - CHOOSE NEW GAME" : slot.Error;
                return;
            }
            progression = slot;
            activeSaveSlot = saveSelection;
            selectedMusic = previousMusic = gameplayMusic = retryMusic = slot.Music;
            musicSelectionForContinue = !create;
            roundId = null; roundActive = roundBanked = false;
            roundSoldiers = 0; roundSeconds = 0;
            paused = gameover = pyorodead = false;
            mapSelection = UfoProgression.Index("core"); mapCamera = Vector2.Zero; mapZoom = 1;
            saveMenuError = ""; confirmReplace = false;
            screen = MenuScreen.ModeSelect;
        }

        static Rectangle SaveSlotRect(int slot) => new Rectangle(18, 42 + slot * 43, 252, 38);
        static readonly Rectangle SaveBackButton = new Rectangle(18, 184, 100, 17);
        static readonly Rectangle SaveOpenButton = new Rectangle(153, 184, 117, 17);
        static readonly Rectangle ReplaceCancelButton = new Rectangle(35, 124, 91, 21);
        static readonly Rectangle ReplaceAcceptButton = new Rectangle(151, 124, 102, 21);

        void UpdateSaveSlots(Func<Keys, bool> pressed, Func<Buttons, bool> padPressed, bool accept, bool cancel)
        {
            MouseState mouse = Mouse.GetState();
            var pointer = new Point((int)((mouse.X - rect.X) * NATIVE_WIDTH / (float)Math.Max(1, rect.Width)),
                (int)((mouse.Y - rect.Y) * NATIVE_HEIGHT / (float)Math.Max(1, rect.Height)));
            bool click = mouse.LeftButton == ButtonState.Pressed && savePreviousMouse.LeftButton == ButtonState.Released;
            savePreviousMouse = mouse;
            if (confirmReplace)
            {
                if (cancel || (click && ReplaceCancelButton.Contains(pointer))) { confirmReplace = false; return; }
                if (pressed(Keys.Left) || pressed(Keys.Right) || pressed(Keys.A) || pressed(Keys.D)
                    || padPressed(Buttons.DPadLeft) || padPressed(Buttons.DPadRight)) replaceAccepted = !replaceAccepted;
                if (click && ReplaceAcceptButton.Contains(pointer)) { replaceAccepted = true; accept = true; }
                if (accept)
                {
                    if (replaceAccepted) ActivateSave(true);
                    else confirmReplace = false;
                }
                return;
            }
            if (cancel || (click && SaveBackButton.Contains(pointer))) { screen = MenuScreen.Main; return; }
            int move = (pressed(Keys.Down) || pressed(Keys.S) || padPressed(Buttons.DPadDown) ? 1 : 0)
                - (pressed(Keys.Up) || pressed(Keys.W) || padPressed(Buttons.DPadUp) ? 1 : 0);
            if (move != 0) { saveSelection = (saveSelection + move + 3) % 3; beamAudio?.PlayMenuBlip(); }
            if (click)
            {
                for (int i = 0; i < saveSlots.Length; i++) if (SaveSlotRect(i).Contains(pointer)) saveSelection = i;
                if (SaveOpenButton.Contains(pointer)) accept = true;
            }
            if (!accept) return;
            beamAudio?.PlayMenuConfirm();
            if (creatingSave && saveSlots[saveSelection].Exists)
            {
                confirmReplace = true; replaceAccepted = false;
            }
            else ActivateSave(creatingSave);
        }

        void DrawSaveSlots()
        {
            spriteBatch.Draw(beamPixel, new Rectangle(0, 0, NATIVE_WIDTH, NATIVE_HEIGHT), new Color(7, 14, 28));
            DrawStringBitmap(spriteBatch, creatingSave ? "NEW GAME" : "CONTINUE", new Vector2(18, 12), new Color(110, 244, 235));
            font6.Draw(spriteBatch, creatingSave ? "CHOOSE A SLOT FOR YOUR NEW GAME" : "CHOOSE A SAVE TO CONTINUE", new Vector2(18, 29), Color.White);
            for (int i = 0; i < saveSlots.Length; i++)
            {
                var slot = saveSlots[i]; var box = SaveSlotRect(i);
                Color border = i == saveSelection ? new Color(255, 218, 123) : new Color(45, 88, 108);
                spriteBatch.Draw(beamPixel, box, border);
                spriteBatch.Draw(beamPixel, new Rectangle(box.X + 1, box.Y + 1, box.Width - 2, box.Height - 2), new Color(15, 31, 46));
                font6.Draw(spriteBatch, "SAVE " + (i + 1) + (slot.Exists ? "" : "  - EMPTY"), new Vector2(box.X + 8, box.Y + 7), border);
                int ranks = 0;
                for (int n = 0; n < UfoProgression.Nodes.Length; n++) if (UfoProgression.Nodes[n].Effect != UfoUpgradeEffect.Core) ranks += slot.Rank(n);
                string detail = !string.IsNullOrEmpty(slot.Error) ? "SAVE UNAVAILABLE"
                    : slot.Exists ? "CREW " + Money(slot.Balance) + "   UPGRADES " + ranks
                    : "START A FRESH COLONY";
                font6.Draw(spriteBatch, detail, new Vector2(box.X + 8, box.Y + 22), Color.White);
            }
            font6.Draw(spriteBatch, saveMenuError, new Vector2(18, 174), new Color(255, 150, 130));
            spriteBatch.Draw(beamPixel, SaveBackButton, new Color(30, 49, 64));
            font6.Draw(spriteBatch, "ESC BACK", new Vector2(28, 190), Color.White);
            spriteBatch.Draw(beamPixel, SaveOpenButton, new Color(34, 91, 96));
            font6.Draw(spriteBatch, "ENTER/X SELECT", new Vector2(160, 190), Color.White);
            if (!confirmReplace) return;
            spriteBatch.Draw(beamPixel, new Rectangle(0, 0, NATIVE_WIDTH, NATIVE_HEIGHT), Color.Black * .8f);
            spriteBatch.Draw(beamPixel, new Rectangle(24, 68, 240, 88), new Color(28, 39, 52));
            font6.Draw(spriteBatch, "REPLACE SAVE " + (saveSelection + 1) + "?", new Vector2(40, 80), new Color(255, 218, 123));
            font6.Draw(spriteBatch, "ITS CURRENCY AND UPGRADES WILL", new Vector2(40, 96), Color.White);
            font6.Draw(spriteBatch, "BE RESET. OTHER SAVES ARE KEPT.", new Vector2(40, 106), Color.White);
            spriteBatch.Draw(beamPixel, ReplaceCancelButton, !replaceAccepted ? new Color(110, 91, 51) : new Color(43, 58, 73));
            spriteBatch.Draw(beamPixel, ReplaceAcceptButton, replaceAccepted ? new Color(150, 65, 50) : new Color(43, 58, 73));
            font6.Draw(spriteBatch, "CANCEL", new Vector2(56, 132), Color.White);
            font6.Draw(spriteBatch, "REPLACE", new Vector2(175, 132), Color.White);
        }
    }
}
