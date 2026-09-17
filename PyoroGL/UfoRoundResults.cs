using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MonogameTest
{
    public partial class Game1
    {
        const double ResultsFirstLine = .4;
        const double ResultsLineDelay = 1.15;
        const double ResultsCountDuration = .7;
        const double ResultsCompleteTime = ResultsFirstLine + ResultsLineDelay * 2 + ResultsCountDuration + .25;
        static readonly Rectangle ResultsContinueButton = new Rectangle(52, 141, 184, 18);
        double resultsTime, resultsMultiplier, resultsReward, resultsSurvival;
        int resultsCrew;
        bool resultsInputReady;
        string resultsHeading;
        MouseState resultsPreviousMouse;

        void OpenRoundResults()
        {
            screen = MenuScreen.RoundResults;
            resultsHeading = shipHealth <= 0 ? "SHIP DESTROYED" : "ROUND COMPLETE";
            resultsTime = 0;
            resultsInputReady = false;
            resultsPreviousMouse = Mouse.GetState();
            resultsCrew = roundSoldiers * soldierValueLevel;
            resultsMultiplier = RoundMultiplier;
            resultsReward = RoundReward;
            resultsSurvival = roundSeconds;
            beamAudio?.Stop();
            music?.Request(MusicTracks.Track.Gameover);
        }

        double ResultsLineAge(int line) => resultsTime - ResultsFirstLine - ResultsLineDelay * line;
        bool ResultsComplete => resultsTime >= ResultsCompleteTime;

        void UpdateRoundResults(GameTime time, bool accept, bool acceptHeld)
        {
            float dt = (float)time.ElapsedGameTime.TotalSeconds;
            resultsTime += dt;
            UpdateImpactEffects(dt);
            UpdateCrashLanding(dt);
            MouseState mouse = Mouse.GetState();
            bool clicked = mouse.LeftButton == ButtonState.Pressed && resultsPreviousMouse.LeftButton == ButtonState.Released;
            resultsPreviousMouse = mouse;
            // Require a release after the tally settles, including a held mouse button.
            if (ResultsComplete && !acceptHeld && mouse.LeftButton == ButtonState.Released) resultsInputReady = true;
            Point pointer = new Point((mouse.X - rect.X) * NATIVE_WIDTH / Math.Max(1, rect.Width),
                (mouse.Y - rect.Y) * NATIVE_HEIGHT / Math.Max(1, rect.Height));
            if (ResultsComplete && resultsInputReady && (accept || (clicked && ResultsContinueButton.Contains(pointer))))
            {
                OpenUpgradeMap();
                beamAudio?.PlayMenuConfirm();
            }
        }

        void DrawResultsText(string text, Vector2 center, Color color, float scale = 1)
        {
            font6.Draw(spriteBatch, text, center - font6.Measure(text) * scale / 2, color, scale);
        }

        void DrawResultsLine(int line, string label, double value, Color tint)
        {
            double age = ResultsLineAge(line);
            if (age < 0) return;
            float progress = MathHelper.Clamp((float)(age / ResultsCountDuration), 0, 1);
            float countEase = 1 - (1 - progress) * (1 - progress);
            double displayed = line == 1 ? 1 + (value - 1) * countEase : value * countEase;
            string number = line == 0 ? ((int)displayed).ToString(CultureInfo.InvariantCulture)
                : line == 1 ? displayed.ToString("F2", CultureInfo.InvariantCulture) + "X" : Money(displayed);
            // One gentle overshoot on arrival; all elements grow about the row centre.
            float bounce = 1 + .075f * (float)Math.Sin(Math.PI * Math.Clamp(age / .4, 0, 1));
            float alpha = MathHelper.Clamp((float)(age / .1), 0, 1);
            Vector2 center = new Vector2(144, 63 + line * 24);
            var box = new Rectangle((int)(center.X - 99 * bounce), (int)(center.Y - 10 * bounce),
                (int)(198 * bounce), (int)(20 * bounce));
            spriteBatch.Draw(beamPixel, box, new Color(12, 28, 44) * alpha);
            spriteBatch.Draw(beamPixel, new Rectangle(box.X, box.Y, 2, box.Height), tint * alpha);
            font6.Draw(spriteBatch, label, center + new Vector2(-91, -3) * bounce, tint * alpha, bounce);
            float numberScale = Math.Min(1.25f, 73 / font6.Measure(number).X) * bounce;
            Vector2 numberSize = font6.Measure(number) * numberScale;
            font6.Draw(spriteBatch, number, center + new Vector2(91 * bounce - numberSize.X, -numberSize.Y / 2),
                Color.White * alpha, numberScale);
        }

        void DrawRoundResults()
        {
            // This is drawn over the still-visible crash scene. Keep the panel
            // compact and translucent so the landing, fire, and smoke remain
            // readable around it.
            spriteBatch.Draw(beamPixel, new Rectangle(34, 20, 220, 140), new Color(3, 10, 20) * .88f);
            spriteBatch.Draw(beamPixel, new Rectangle(39, 25, 210, 1), new Color(82, 118, 134) * .9f);
            DrawResultsText(resultsHeading, new Vector2(144, 35), new Color(255, 145, 112), 1.05f);
            DrawResultsText("SURVIVED " + FormatRoundTime(resultsSurvival), new Vector2(144, 47), new Color(129, 162, 184));
            DrawResultsLine(0, "CREW COLLECTED", resultsCrew, new Color(88, 236, 241));
            DrawResultsLine(1, "MULTIPLIER", resultsMultiplier, new Color(139, 181, 255));
            DrawResultsLine(2, "TOTAL REWARD", resultsReward, new Color(255, 220, 128));
            if (!ResultsComplete) return;
            DrawResultsText(roundBanked ? "CREW ADDED TO BALANCE" : "SAVE PENDING - RETRY",
                new Vector2(144, 132), roundBanked ? new Color(129, 162, 184) : new Color(255, 145, 112));
            spriteBatch.Draw(beamPixel, ResultsContinueButton, new Color(20, 66, 89) * .92f);
            DrawResultsText(resultsInputReady ? "ENTER / X / A  UPGRADES" : "RELEASE TO CONTINUE",
                new Vector2(144, 150), new Color(88, 236, 241));
        }
    }
}
