using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonogameTest
{
    public partial class Game1
    {
        enum MenuScreen { Main, Playing, Pause, Options }
        enum MenuTransition { None, StartBlink, FadeOut, FadeIn }
        MenuScreen screen = MenuScreen.Main;
        MenuScreen optionsParent = MenuScreen.Main;
        MenuScreen transitionTarget;
        MenuTransition transition;
        double transitionTime;
        const double BLINK_INTERVAL = 0.15;
        const double FADE_SECONDS = 0.45;
        int mainSelection, pauseSelection, optionsSelection;
        KeyboardState previousMenuKeys;
        GamePadState previousMenuPad;
        Texture2D mainMenuBackground, mainMenuTitle;
        static readonly string[] mainItems = { "START", "OPTIONS", "EXIT" };
        static readonly string[] pauseItems = { "RESUME", "RESTART", "OPTIONS", "MAIN MENU", "EXIT" };

        // True means menus own this update: no movement, spawns, or effect timers advance.
        bool updateMenus(GameTime time, KeyboardState keys)
        {
            KeyboardState oldKeys = previousMenuKeys;
            previousMenuKeys = keys;
            GamePadState pad = GamePad.GetState(PlayerIndex.One);
            GamePadState oldPad = previousMenuPad;
            previousMenuPad = pad;
            bool pressed(Keys key) => keys.IsKeyDown(key) && oldKeys.IsKeyUp(key);
            bool padPressed(Buttons button) => pad.IsButtonDown(button) && oldPad.IsButtonUp(button);
            bool cancel = pressed(Keys.Escape) || padPressed(Buttons.B) || padPressed(Buttons.Back);
            bool accept = pressed(Keys.Enter) || pressed(Keys.Space) || padPressed(Buttons.A);

            if (transition != MenuTransition.None)
            {
                advanceTransition(time.ElapsedGameTime.TotalSeconds);
                return true;
            }
            if (screen == MenuScreen.Playing)
            {
                if (cancel || padPressed(Buttons.Start))
                {
                    screen = MenuScreen.Pause;
                    pauseSelection = 0;
                    paused = true;
                    return true;
                }
                return false;
            }
            if (screen == MenuScreen.Pause && (cancel || padPressed(Buttons.Start)))
            {
                resumeGame();
                return true;
            }
            if (screen == MenuScreen.Options && cancel)
            {
                screen = optionsParent;
                return true;
            }
            int move = 0;
            if (pressed(Keys.Up) || pressed(Keys.W) || padPressed(Buttons.DPadUp)) move--;
            if (pressed(Keys.Down) || pressed(Keys.S) || padPressed(Buttons.DPadDown)) move++;
            // Navigation gets the short blip; accept gets the rising "ba-LEAP".
            if (accept) beamAudio?.PlayMenuConfirm();
            else if (move != 0) beamAudio?.PlayMenuBlip();
            if (screen == MenuScreen.Main)
            {
                mainSelection = (mainSelection + move + mainItems.Length) % mainItems.Length;
                if (accept)
                {
                    if (mainSelection == 0) beginTransition(MenuScreen.Playing, true);
                    else if (mainSelection == 1) openOptions(MenuScreen.Main);
                    else Exit();
                }
            }
            else if (screen == MenuScreen.Pause)
            {
                pauseSelection = (pauseSelection + move + pauseItems.Length) % pauseItems.Length;
                if (accept)
                {
                    switch (pauseSelection)
                    {
                        case 0: resumeGame(); break;
                        case 1: beginTransition(MenuScreen.Playing, false); break;
                        case 2: openOptions(MenuScreen.Pause); break;
                        case 3: beginTransition(MenuScreen.Main, false); break;
                        case 4: Exit(); break;
                    }
                }
            }
            else if (screen == MenuScreen.Options)
            {
                optionsSelection = (optionsSelection + move + 2) % 2;
                int adjust = 0;
                if (pressed(Keys.Left) || padPressed(Buttons.DPadLeft)) adjust--;
                if (pressed(Keys.Right) || padPressed(Buttons.DPadRight)) adjust++;
                if (optionsSelection == 0 && (accept || adjust != 0))
                {
                    graphics.HardwareModeSwitch = false;
                    graphics.ToggleFullScreen();
                    computeIntegerScale();
                }
                else if (optionsSelection == 1 && accept)
                    screen = optionsParent;
            }
            return true;
        }

        void openOptions(MenuScreen parent)
        {
            optionsParent = parent;
            optionsSelection = 0;
            screen = MenuScreen.Options;
        }

        void resumeGame()
        {
            screen = MenuScreen.Playing;
            paused = false;
        }

        void beginTransition(MenuScreen target, bool blink)
        {
            transitionTarget = target;
            transitionTime = 0;
            transition = blink ? MenuTransition.StartBlink : MenuTransition.FadeOut;
        }

        void advanceTransition(double seconds)
        {
            transitionTime += seconds;
            // Preserve leftover time across phases, including a slow frame.
            while (transition != MenuTransition.None)
            {
                double duration = transition == MenuTransition.StartBlink ? BLINK_INTERVAL * 4 : FADE_SECONDS;
                if (transitionTime < duration) break;
                transitionTime -= duration;
                if (transition == MenuTransition.StartBlink)
                    transition = MenuTransition.FadeOut;
                else if (transition == MenuTransition.FadeOut)
                {
                    if (transitionTarget == MenuScreen.Playing) resetGame();
                    screen = transitionTarget;
                    paused = false;
                    mainSelection = 0;
                    transition = MenuTransition.FadeIn;
                }
                else
                {
                    transition = MenuTransition.None;
                    transitionTime = 0;
                }
            }
        }

        bool startBracketsVisible()
        {
            return transition != MenuTransition.StartBlink || ((int)(transitionTime / BLINK_INTERVAL) % 2) == 1;
        }

        float transitionOpacity()
        {
            if (transition == MenuTransition.FadeOut) return MathHelper.Clamp((float)(transitionTime / FADE_SECONDS), 0, 1);
            if (transition == MenuTransition.FadeIn) return 1 - MathHelper.Clamp((float)(transitionTime / FADE_SECONDS), 0, 1);
            return 0;
        }

        bool usesTitleScene()
        {
            return screen == MenuScreen.Main || (screen == MenuScreen.Options && optionsParent == MenuScreen.Main);
        }

        void drawTitleScene()
        {
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: RasterizerState.CullNone);
            spriteBatch.Draw(mainMenuBackground, new Rectangle(0, 0, NATIVE_WIDTH, NATIVE_HEIGHT), Color.White);
            spriteBatch.Draw(mainMenuTitle, new Rectangle(10, 6, 176, 59), Color.White);
            if (screen == MenuScreen.Options)
            {
                spriteBatch.Draw(beamPixel, new Rectangle(12, 70, 244, 70), Color.Black * 0.78f);
                drawOptions(20, 78);
            }
            else
            {
                spriteBatch.Draw(beamPixel, new Rectangle(12, 74, 108, 64), Color.Black * 0.7f);
                for (int i = 0; i < mainItems.Length; i++)
                    drawMenuItem(mainItems[i], i == mainSelection, 20, 82 + i * 18,
                        i != 0 || startBracketsVisible());
            }
            DrawStringBitmap(spriteBatch, "UP/DOWN  ENTER SELECT", new Vector2(14, 148), new Color(240, 218, 160));
            spriteBatch.End();
        }

        void drawPauseOverlay()
        {
            if (screen != MenuScreen.Pause && !(screen == MenuScreen.Options && optionsParent == MenuScreen.Pause)) return;
            spriteBatch.Draw(beamPixel, new Rectangle(PLAYFIELD_LEFT, PLAYFIELD_TOP,
                PLAYFIELD_RIGHT - PLAYFIELD_LEFT, NATIVE_HEIGHT - PLAYFIELD_TOP * 2), Color.Black * 0.7f);
            string heading = screen == MenuScreen.Options ? "OPTIONS" : "PAUSED";
            DrawStringBitmap(spriteBatch, heading, new Vector2((NATIVE_WIDTH - heading.Length * FONT_CELL) / 2, 32), new Color(255, 221, 134));
            if (screen == MenuScreen.Options)
                drawOptions(40, 62);
            else
                for (int i = 0; i < pauseItems.Length; i++)
                    drawMenuItem(pauseItems[i], i == pauseSelection, 84, 54 + i * 17);
        }

        void drawOptions(int left, int top)
        {
            drawMenuItem("FULLSCREEN: " + (graphics.IsFullScreen ? "ON" : "OFF"), optionsSelection == 0, left, top);
            drawMenuItem("BACK", optionsSelection == 1, left, top + 18);
        }

        void drawMenuItem(string label, bool selected, int left, int top, bool bracketsVisible = true)
        {
            Color color = selected ? new Color(255, 225, 145) : Color.White;
            DrawStringBitmap(spriteBatch, label, new Vector2(left + 2 * FONT_CELL, top), color);
            if (selected && bracketsVisible)
            {
                int right = left + (label.Length + 3) * FONT_CELL;
                spriteBatch.Draw(beamPixel, new Rectangle(left, top, 1, 8), color);
                spriteBatch.Draw(beamPixel, new Rectangle(left, top, 4, 1), color);
                spriteBatch.Draw(beamPixel, new Rectangle(left, top + 7, 4, 1), color);
                spriteBatch.Draw(beamPixel, new Rectangle(right + 3, top, 1, 8), color);
                spriteBatch.Draw(beamPixel, new Rectangle(right, top, 4, 1), color);
                spriteBatch.Draw(beamPixel, new Rectangle(right, top + 7, 4, 1), color);
            }
        }

        void resetGame()
        {
            beamAudio?.Stop();
            x = PLAYER_START_X;
            y = PLAYER_START_Y;
            facingright = 1;
            pyoro = pyororight;
            tongueoffsetX = 1;
            tongueoffsetY = -1;
            tongueX = x + tongueoffsetX + rightoffset;
            tongueY = y + tongueoffsetY;
            tonguecount = 0;
            spaceheld = 0;
            recall = tonguecollide = pyorodead = gameover = paused = false;
            caughtbean = pyorosquat = blocktocheck = 0;
            speed = 1;
            smallspeed = 0xFF;
            bigspeed = 0x100;
            score = 0;
            risingscore = 5000;
            rainbowbeantotal = 10;
            max_time = 0xB4;
            time_until_new_bean = tmpmax = tmpspeed = beanspeed = 0;
            randnum = randnum2 = randnum3 = randnum4 = randnummain = 0;
            beantype = currentbeantype = currentbeanx = beanxrandom = 0;
            create_new_bean = false;
            Array.Fill(blocks, true);
            for (int i = 0; i < max_amount_of_beans; i++)
            {
                bean_active[i] = false;
                bean_speed[i] = 0;
                bean_y[i] = -20;
                bean_x[i] = r.Next(PLAYFIELD_LEFT + 8, PLAYFIELD_RIGHT - 8);
                bean_anim_counter[i] = rnd.Next(0, 43);
                bean_rainbow_counter[i] = rnd.Next(0, 5);
                bean_type[i] = 0;
                current_bean_sprite[i] = bean_centre;
            }
            scorePopups.Clear();
            explosions.Clear();
            angels.Clear();
            angelQueue.Clear();
            rainbowClearQueue.Clear();
            angelQueueTimer = rainbowClearTimer = 0;
        }
    }
}
