using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonogameTest
{
    public partial class Game1
    {
        bool scoresAfterGame, scoresGameB, enteringInitials;
        double scoreScrollTime;
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
            enteringInitials = afterGame && highScores.Qualifies(modeB, completedScore);
            initialCursor = 0;
            savedScoreId = null;
            screen = MenuScreen.Scores;
        }

        void updateScores(GameTime time, Func<Keys, bool> pressed, Func<Buttons, bool> padPressed)
        {
            scoreScrollTime = Math.Min(2, scoreScrollTime + time.ElapsedGameTime.TotalSeconds);
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
                    beamAudio?.PlayMenuConfirm();
                }
                else if (back) enteringInitials = false;
                return;
            }
            if (pressed(Keys.R) || pressed(Keys.Enter) || padPressed(Buttons.A))
            {
                screen = MenuScreen.Playing;
                retryMusicVisible = true;
                retryMusic = gameplayMusic;
                retryRow = 0;
            }
            else if (back) beginTransition(MenuScreen.Main, false);
        }

        void drawScores()
        {
            if (scoresAfterGame)
                spriteBatch.Draw(beamPixel, new Rectangle(0, 0, NATIVE_WIDTH, NATIVE_HEIGHT), Color.Black * .7f);
            else
                spriteBatch.Draw(mainMenuBackground, new Rectangle(0, 0, NATIVE_WIDTH, NATIVE_HEIGHT), Color.White * .25f);
            DrawStringBitmap(spriteBatch, scoresAfterGame ? "GAME OVER" : "HIGH SCORES",
                new Vector2(scoresAfterGame ? 108 : 100, 8), new Color(255, 225, 145));
            font6.Draw(spriteBatch, scoresGameB ? "GAME B  TOP 10" : "GAME A  TOP 10",
                new Vector2(105, 22), Color.White);

            // The ranked list rises from below the screen and settles below its heading.
            int offset = (int)Math.Round(130 * Math.Pow(1 - Math.Clamp((scoreScrollTime - .35) / 1.65, 0, 1), 2));
            var entries = highScores.Entries(scoresGameB);
            for (int i = 0; i < 10; i++)
            {
                int rowY = 35 + i * 8 + offset;
                if (rowY > 113) continue;
                string initials = i < entries.Length ? entries[i].Initials : "...";
                string points = i < entries.Length ? entries[i].Score.ToString("D6") : "000000";
                Color color = i < entries.Length && entries[i].Id == savedScoreId ? new Color(255, 225, 145) : Color.White;
                font6.Draw(spriteBatch, (i + 1).ToString("D2") + "   " + initials + "   " + points,
                    new Vector2(87, rowY), color);
            }

            if (scoresAfterGame)
            {
                font6.Draw(spriteBatch, "YOUR SCORE " + completedScore.ToString("D6"), new Vector2(93, 122), new Color(255, 225, 145));
                if (scoreScrollTime < 2) return;
                if (enteringInitials)
                {
                    font6.Draw(spriteBatch, "INITIALS", new Vector2(82, 134), Color.White);
                    for (int i = 0; i < 3; i++)
                    {
                        font6.Draw(spriteBatch, scoreInitials[i].ToString(), new Vector2(145 + i * 12, 134), new Color(255, 225, 145));
                        if (i == initialCursor) spriteBatch.Draw(beamPixel, new Rectangle(145 + i * 12, 141, 5, 1), Color.White);
                    }
                    font6.Draw(spriteBatch, "ARROWS OR TYPE  ENTER SAVE  ESC SKIP", new Vector2(42, 146), Color.White);
                }
                else
                    font6.Draw(spriteBatch, "R RETRY   ESC MAIN MENU", new Vector2(78, 148), Color.White);
            }
            else
                font6.Draw(spriteBatch, "LEFT RIGHT MODE   ESC BACK", new Vector2(69, 148), Color.White);
        }
    }
}
