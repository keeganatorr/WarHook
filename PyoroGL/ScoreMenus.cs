using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonogameTest
{
    public partial class Game1
    {
        bool scoresAfterGame, scoresGameB, enteringInitials;
        bool rainbowScoreActive;
        double scoreScrollTime;
        double scoreRainbowTime;
        int initialCursor;
        int completedScore;
        char[] scoreInitials = { 'A', 'A', 'A' };
        string savedScoreId;
        const string InitialAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        void openScores(bool modeB, bool afterGame)
        {
            scoresGameB = modeB;
            scoresAfterGame = afterGame;
            if (afterGame) completedScore = score;
            scoreScrollTime = afterGame ? 0 : 2;
            scoreRainbowTime = 0;
            enteringInitials = afterGame && highScores.Qualifies(modeB, completedScore);
            rainbowScoreActive = enteringInitials;
            initialCursor = 0;
            savedScoreId = null;
            screen = MenuScreen.Scores;
        }

        void updateScores(GameTime time, Func<Keys, bool> pressed, Func<Buttons, bool> padPressed)
        {
            scoreScrollTime = Math.Min(2, scoreScrollTime + time.ElapsedGameTime.TotalSeconds);
            if (rainbowScoreActive) scoreRainbowTime += time.ElapsedGameTime.TotalSeconds;
            if (OnlineScores.Enabled && OnlineScores.ShouldFetch(scoresGameB))
                OnlineScores.BeginFetch(scoresGameB);
            bool back = pressed(Keys.Escape) || padPressed(Buttons.B) || padPressed(Buttons.Back);
            if (!scoresAfterGame)
            {
                if (pressed(Keys.Left) || pressed(Keys.Right) || padPressed(Buttons.DPadLeft) || padPressed(Buttons.DPadRight))
                    scoresGameB = !scoresGameB;
                if (back || pressed(Keys.Enter) || padPressed(Buttons.A)) screen = MenuScreen.Main;
                return;
            }
            // Let the table finish its arcade entrance before accepting initials.
            if (scoreScrollTime < 2) return;
            if (enteringInitials)
            {
                if (pressed(Keys.Left) || padPressed(Buttons.DPadLeft)) initialCursor = (initialCursor + 2) % 3;
                if (pressed(Keys.Right) || padPressed(Buttons.DPadRight)) initialCursor = (initialCursor + 1) % 3;
                int change = (pressed(Keys.Up) || padPressed(Buttons.DPadUp) ? 1 : 0)
                    - (pressed(Keys.Down) || padPressed(Buttons.DPadDown) ? 1 : 0);
                int index = InitialAlphabet.IndexOf(scoreInitials[initialCursor]);
                scoreInitials[initialCursor] = InitialAlphabet[(index + change + InitialAlphabet.Length) % InitialAlphabet.Length];
                for (int key = (int)Keys.A; key <= (int)Keys.Z; key++)
                    if (pressed((Keys)key))
                    {
                        scoreInitials[initialCursor] = (char)('A' + key - (int)Keys.A);
                        initialCursor = (initialCursor + 1) % 3;
                        break;
                    }
                if (pressed(Keys.Back)) initialCursor = (initialCursor + 2) % 3;
                if (pressed(Keys.Enter) || padPressed(Buttons.A))
                {
                    savedScoreId = highScores.Submit(scoresGameB, new string(scoreInitials), completedScore);
                    enteringInitials = false;
                    rainbowScoreActive = savedScoreId != null;
                    beamAudio?.PlayMenuConfirm();
                }
                else if (back)
                {
                    enteringInitials = false;
                    rainbowScoreActive = false;
                }
                return;
            }
            if (pressed(Keys.R) || pressed(Keys.Enter) || padPressed(Buttons.A))
            {
                rainbowScoreActive = false;
                screen = MenuScreen.Playing;
                retryMusicVisible = true;
                retryMusic = gameplayMusic;
                retryRow = 0;
            }
            else if (back)
            {
                rainbowScoreActive = false;
                beginTransition(MenuScreen.Main, false);
            }
        }

        void drawScores()
        {
            if (scoresAfterGame)
                spriteBatch.Draw(beamPixel, new Rectangle(0, 0, NATIVE_WIDTH, NATIVE_HEIGHT), Color.Black * .7f);
            else
                DrawUfoLandscape();
            DrawStringBitmap(spriteBatch, scoresAfterGame ? "GAME OVER" : "HIGH SCORES",
                new Vector2(scoresAfterGame ? 108 : 100, 8), new Color(255, 225, 145));
            string mode = scoresGameB ? "SIEGE" : "ABDUCT";
            font6.Draw(spriteBatch, mode, new Vector2((NATIVE_WIDTH - mode.Length * Font6.Cell) / 2, 22), Color.White);

            // The ranked list rises from below the screen and settles below its heading.
            int offset = (int)Math.Round(130 * Math.Pow(1 - Math.Clamp((scoreScrollTime - .35) / 1.65, 0, 1), 2));
            var local = highScores.Entries(scoresGameB);
            var online = OnlineScores.Enabled ? OnlineScores.GetSnapshot(scoresGameB) : null;
            if (scoresAfterGame)
            {
                drawScoreColumn("LOCAL TOP 10", 4, 132, local, offset, enteringInitials);
                DrawUfoScoreGuide();
            }
            else
            {
                drawScoreColumn("LOCAL TOP 10", 4, 132, local, offset, false);
                DrawUfoScoreGuide();
            }

            if (scoresAfterGame)
            {
                font6.Draw(spriteBatch, "YOUR SCORE " + completedScore.ToString("D6"), new Vector2(93, 160), new Color(255, 225, 145));
                if (scoreScrollTime < 2) return;
                if (enteringInitials)
                {
                    font6.Draw(spriteBatch, "INITIALS", new Vector2(82, 176), Color.White);
                    for (int i = 0; i < 3; i++)
                    {
                        font6.Draw(spriteBatch, scoreInitials[i].ToString(), new Vector2(145 + i * 12, 176), new Color(255, 225, 145));
                        if (i == initialCursor) spriteBatch.Draw(beamPixel, new Rectangle(145 + i * 12, 183, 5, 1), Color.White);
                    }
                    font6.Draw(spriteBatch, "ARROWS OR TYPE  ENTER SAVE  ESC SKIP", new Vector2(42, NATIVE_HEIGHT - 14), Color.White);
                }
                else
                    font6.Draw(spriteBatch, "R RETRY   ESC MAIN MENU", new Vector2(78, NATIVE_HEIGHT - 14), Color.White);
            }
            else
            {
                string hint = "LEFT / RIGHT MODE   ESC BACK";
                font6.Draw(spriteBatch, hint, new Vector2((NATIVE_WIDTH - hint.Length * Font6.Cell) / 2, NATIVE_HEIGHT - 14), Color.White);
            }
        }

        void DrawUfoScoreGuide()
        {
            string[] lines = {
                "FLIGHT MANUAL", "", "ARROWS: FLY", "X/SPACE: FIRE", "Z/SHIFT: TRACTOR", "",
                "SOLDIER: 100", "ENGINEER: 250", "ENGINEERS REPAIR", "CREW BUYS UPGRADES", "MISSILE: 50", "MOVE TOO FAR: DROP"
            };
            for (int i = 0; i < lines.Length; i++)
                font6.Draw(spriteBatch, lines[i], new Vector2(157, 32 + i * 7),
                    i == 0 ? new Color(96, 230, 222) : Color.White);
        }

        const int ScoreEntryWidth = 17 * Font6.Cell;
        const int ScoreRowsY = 42;
        static readonly Color[] RainbowColors =
        {
            new Color(255, 75, 75),
            new Color(255, 220, 70),
            new Color(80, 235, 105),
            new Color(70, 205, 255),
            new Color(115, 100, 255),
            new Color(245, 85, 235)
        };

        void drawScoreColumn(string heading, int columnX, int columnWidth,
            HighScoreStore.Entry[] entries, int offset, bool showPending)
        {
            font6.Draw(spriteBatch, heading,
                new Vector2(columnX + (columnWidth - heading.Length * Font6.Cell) / 2, 32), Color.White);
            int pendingIndex = -1;
            if (showPending)
            {
                pendingIndex = 0;
                while (pendingIndex < entries.Length && entries[pendingIndex].Score >= completedScore)
                    pendingIndex++;
                pendingIndex = Math.Min(pendingIndex, 9);
            }
            for (int i = 0; i < 10; i++)
            {
                int rowY = ScoreRowsY + i * 8 + offset;
                if (rowY > 114) continue;
                bool pending = i == pendingIndex;
                int entryIndex = pending ? -1 : i - (pendingIndex >= 0 && i > pendingIndex ? 1 : 0);
                bool hasEntry = entryIndex >= 0 && entryIndex < entries.Length;
                string initials = pending ? new string(scoreInitials) : hasEntry ? entries[entryIndex].Initials : "...";
                string points = pending ? completedScore.ToString("D6") : hasEntry ? entries[entryIndex].Score.ToString("D6") : "000000";
                Color color = pending || hasEntry && entries[entryIndex].Id == savedScoreId && rainbowScoreActive
                    ? RainbowColor()
                    : hasEntry && entries[entryIndex].Id == savedScoreId ? new Color(255, 225, 145) : Color.White;
                string row = (i + 1).ToString("D2") + "   " + initials + "   " + points;
                font6.Draw(spriteBatch, row,
                    new Vector2(columnX + (columnWidth - ScoreEntryWidth) / 2, rowY), color);
            }
        }

        Color RainbowColor()
        {
            double position = (scoreRainbowTime * 1.5) % RainbowColors.Length;
            int index = (int)position;
            float blend = (float)(position - index);
            return Color.Lerp(RainbowColors[index], RainbowColors[(index + 1) % RainbowColors.Length], blend);
        }

        void drawOnlineScoreColumn(int columnX, int columnWidth,
            OnlineScores.Snapshot online, int offset)
        {
            const string heading = "ONLINE TOP 10";
            font6.Draw(spriteBatch, heading,
                new Vector2(columnX + (columnWidth - heading.Length * Font6.Cell) / 2, 32), Color.White);
            bool showingPreviousRowsWhileRefreshing = rainbowScoreActive &&
                online != null && online.Status == OnlineScores.Status.Loading && online.Entries.Length > 0;
            if ((online == null || online.Status != OnlineScores.Status.Loaded) && !showingPreviousRowsWhileRefreshing)
            {
                string statusText;
                if (!OnlineScores.Enabled)
                    statusText = "NOT CONFIGURED";
                else if (online != null && online.Status == OnlineScores.Status.Failed)
                    statusText = "CONNECTION FAILED";
                else
                {
                    int dots = 1 + (int)(scoreScrollTime * 3) % 3;
                    statusText = "LOADING" + new string('.', dots);
                }
                font6.Draw(spriteBatch, statusText,
                    new Vector2(columnX + (columnWidth - statusText.Length * Font6.Cell) / 2, 68),
                    new Color(255, 225, 145));
                return;
            }

            int pendingIndex = -1;
            int savedOnlineIndex = -1;
            if (rainbowScoreActive)
            {
                if (!enteringInitials)
                {
                    for (int i = 0; i < online.Entries.Length; i++)
                    {
                        if (online.Entries[i].IsMine && online.Entries[i].Initials == new string(scoreInitials) &&
                            online.Entries[i].Score == completedScore)
                        {
                            savedOnlineIndex = i;
                            break;
                        }
                    }
                }
                if (savedOnlineIndex < 0)
                {
                    pendingIndex = 0;
                    while (pendingIndex < online.Entries.Length && online.Entries[pendingIndex].Score >= completedScore)
                        pendingIndex++;
                    if (pendingIndex >= 10) pendingIndex = -1;
                }
            }

            for (int i = 0; i < 10; i++)
            {
                int rowY = ScoreRowsY + i * 8 + offset;
                if (rowY > 114) continue;
                bool pending = i == pendingIndex;
                int entryIndex = pending ? -1 : i - (pendingIndex >= 0 && i > pendingIndex ? 1 : 0);
                bool hasEntry = entryIndex >= 0 && entryIndex < online.Entries.Length;
                string initials = pending ? new string(scoreInitials) : hasEntry ? online.Entries[entryIndex].Initials : "...";
                string points = pending ? completedScore.ToString("D6") : hasEntry ? online.Entries[entryIndex].Score.ToString("D6") : "000000";
                bool isSavedScore = hasEntry && online.Entries[entryIndex].IsMine && entryIndex == savedOnlineIndex;
                bool isMine = hasEntry && online.Entries[entryIndex].IsMine;
                Color color = pending || isSavedScore ? RainbowColor() : isMine
                    ? new Color(255, 225, 145) : Color.White;
                string row = (i + 1).ToString("D2") + "   " + initials + "   " + points;
                font6.Draw(spriteBatch, row,
                    new Vector2(columnX + (columnWidth - ScoreEntryWidth) / 2, rowY), color);
            }
        }
    }
}
