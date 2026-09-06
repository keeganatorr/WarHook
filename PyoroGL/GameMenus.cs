using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonogameTest
{
    public partial class Game1
    {
        enum MenuScreen { Main, ModeSelect, Playing, Pause, Options }
        enum MenuTransition { None, StartBlink, FadeOut, FadeIn }
        MenuScreen screen = MenuScreen.Main;
        MenuScreen optionsParent = MenuScreen.Main;
        MenuScreen transitionTarget;
        MenuTransition transition;
        double transitionTime;
        const double BLINK_INTERVAL = 0.15;
        const double FADE_SECONDS = 0.45;
        int mainSelection, pauseSelection, optionsSelection, modeSelection;
        // Mode-select choices: 0 = Game A, 1 = Game B; music 1..5. Defaults
        // mirror the original game (Game A, Music 1). modeSelection is the
        // row cursor (0 = games, 1 = music).
        int selectedGame, selectedMusic = 1, previousGame, previousMusic = 1;
        // Music track picked on the game-over screen (Enter/X restarts).
        int retryMusic = 1;
        // Cursor row on the game-over picker: 0 = game, 1 = music.
        int retryRow;
        // False until the player presses R at game over; then the picker shows.
        bool retryMusicVisible;
        // Chosen music track for gameplay (1..5), applied on game start.
        public int gameplayMusic = 1;
        // True while the game-over jingle plays instead of gameplay music.
        bool gameoverMusicPlaying;
        // Volume controls (0..10 steps). Sound defaults to 100%, music to 80%.
        public int soundVolume = 10;
        public int musicVolume = 8;
        const int VolumeSteps = 10;
        static readonly string[] optionsItems = { "FULLSCREEN", "SOUND", "MUSIC", "BACK" };
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
            bool accept = pressed(Keys.Enter) || pressed(Keys.Space) || pressed(Keys.X) || padPressed(Buttons.A);

            if (transition != MenuTransition.None)
            {
                advanceTransition(time.ElapsedGameTime.TotalSeconds);
                return true;
            }
            if (screen == MenuScreen.Playing)
            {
                if (gameover)
                {
                    // Handle overlay input but let the game keep running in
                    // the background (explosions, popups, effects still tick).
                    UpdateGameoverOverlay(pressed, accept, padPressed);
                    return false;
                }
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
                    if (mainSelection == 0) { modeSelection = 0; screen = MenuScreen.ModeSelect; }
                    else if (mainSelection == 1) openOptions(MenuScreen.Main);
                    else Exit();
                }
            }
            else if (screen == MenuScreen.ModeSelect)
            {
                // modeSelection is just the cursor (row: 0 = games, 1 = music).
                // Chosen game and music persist independently of the cursor.
                int previous = modeSelection;
                if (pressed(Keys.Up) || pressed(Keys.W) || padPressed(Buttons.DPadUp)) modeSelection = 0;
                if (pressed(Keys.Down) || pressed(Keys.S) || padPressed(Buttons.DPadDown)) modeSelection = 1;
                if (modeSelection == 0) // games row: pick A or B
                {
                    if (pressed(Keys.Left) || padPressed(Buttons.DPadLeft)) selectedGame = 0;
                    if (pressed(Keys.Right) || padPressed(Buttons.DPadRight)) selectedGame = 1;
                }
                else // music row: pick 1..5
                {
                    if (pressed(Keys.Left) || padPressed(Buttons.DPadLeft)) selectedMusic--;
                    if (pressed(Keys.Right) || padPressed(Buttons.DPadRight)) selectedMusic++;
                    selectedMusic = Math.Clamp(selectedMusic, 1, 5);
                }
                if (modeSelection != previous ||
                    selectedGame != previousGame || selectedMusic != previousMusic)
                    beamAudio?.PlayMenuBlip();
                previousGame = selectedGame;
                previousMusic = selectedMusic;
                gameplayMusic = selectedMusic;
                if (cancel) screen = MenuScreen.Main;
                else if (accept)
                {
                    gameoverMusicPlaying = false;
                    beginTransition(MenuScreen.Playing, true);
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
                optionsSelection = (optionsSelection + move + optionsItems.Length) % optionsItems.Length;
                int adjust = 0;
                if (pressed(Keys.Left) || padPressed(Buttons.DPadLeft)) adjust--;
                if (pressed(Keys.Right) || padPressed(Buttons.DPadRight)) adjust++;
                if (optionsSelection == 0 && (accept || adjust != 0))
                {
                    graphics.HardwareModeSwitch = false;
                    graphics.ToggleFullScreen();
                    computeIntegerScale();
                }
                else if (optionsSelection == 1 && adjust != 0)
                {
                    soundVolume = Math.Clamp(soundVolume + adjust, 0, VolumeSteps);
                    ApplyVolumes();
                    beamAudio?.PlayMenuBlip();
                }
                else if (optionsSelection == 2 && adjust != 0)
                {
                    musicVolume = Math.Clamp(musicVolume + adjust, 0, VolumeSteps);
                    ApplyVolumes();
                    beamAudio?.PlayMenuBlip();
                }
                else if (optionsSelection == 3 && accept)
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

        // Game-over overlay input: stage 1 waits for R, stage 2 lets you pick
        // Game A/B (row 0) and music 1-5 (row 1) with Up/Down, then Enter/X.
        void UpdateGameoverOverlay(Func<Keys, bool> pressed, bool accept, Func<Buttons, bool> padPressed)
        {
            if (!retryMusicVisible)
            {
                if (pressed(Keys.R))
                {
                    retryMusicVisible = true;
                    retryMusic = gameplayMusic; // start from the current track
                    retryRow = 0;               // cursor starts on the game row
                    beamAudio?.PlayMenuBlip();
                }
                return;
            }
            int prevRow = retryRow, prevMusic = retryMusic, prevGame = selectedGame;
            if (pressed(Keys.Up) || pressed(Keys.W) || padPressed(Buttons.DPadUp)) retryRow = 0;
            if (pressed(Keys.Down) || pressed(Keys.S) || padPressed(Buttons.DPadDown)) retryRow = 1;
            if (retryRow == 0)
            {
                if (pressed(Keys.Left) || padPressed(Buttons.DPadLeft)) selectedGame = 0;
                if (pressed(Keys.Right) || padPressed(Buttons.DPadRight)) selectedGame = 1;
            }
            else
            {
                if (pressed(Keys.Left) || padPressed(Buttons.DPadLeft)) retryMusic--;
                if (pressed(Keys.Right) || padPressed(Buttons.DPadRight)) retryMusic++;
                retryMusic = Math.Clamp(retryMusic, 1, 5);
                gameplayMusic = retryMusic;
            }
            if (retryRow != prevRow || retryMusic != prevMusic || selectedGame != prevGame)
                beamAudio?.PlayMenuBlip();
            if (accept || pressed(Keys.X) || padPressed(Buttons.A))
            {
                retryMusicVisible = false;
                beginTransition(MenuScreen.Playing, false);
            }
        }

        // Push current volume settings into the audio systems. Music default
        // is 80% (musicVolume=8), sound effects 100% (soundVolume=10).
        void ApplyVolumes()
        {
            if (music != null) music.Volume = (float)musicVolume / VolumeSteps;
            if (beamAudio != null) beamAudio.VolumeScale = (float)soundVolume / VolumeSteps;
        }

        void resumeGame()
        {
            screen = MenuScreen.Playing;
            paused = false;
            // Returning from pause: restore the normal gameplay track if the
            // game-over jingle was playing (player recovered somehow).
            if (gameoverMusicPlaying) SwitchToGameplayMusic();
        }

        void SwitchToGameplayMusic()
        {
            gameoverMusicPlaying = false;
            music?.Request(TrackForGameplay());
        }

        MusicTracks.Track TrackForGameplay() => gameplayMusic switch
        {
            2 => MusicTracks.Track.Gameplay2,
            3 => MusicTracks.Track.Gameplay3,
            4 => MusicTracks.Track.Gameplay4,
            5 => MusicTracks.Track.Gameplay5,
            _ => MusicTracks.Track.Gameplay1,
        };

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
                    // Swap music at the screen-change point so each track fades
                    // out fully before the next fades in.
                    music?.Request(screen == MenuScreen.Playing ? TrackForGameplay() : MusicTracks.Track.Menu);
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
            return screen == MenuScreen.Main || screen == MenuScreen.ModeSelect
                || (screen == MenuScreen.Options && optionsParent == MenuScreen.Main);
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
            else if (screen == MenuScreen.ModeSelect)
            {
                spriteBatch.Draw(beamPixel, new Rectangle(12, 70, 244, 70), Color.Black * 0.78f);
                DrawStringBitmap(spriteBatch, "SELECT MODE", new Vector2(20, 76), new Color(255, 221, 134));
                // Top row: Game A / Game B side by side. Brackets always mark
                // the chosen game; amber marks the chosen item of the row the
                // cursor is on, so both selections show brackets at once.
                bool onGames = modeSelection == 0;
                DrawModeItem("GAME A", selectedGame == 0, onGames && selectedGame == 0, 28, 92);
                DrawModeItem("GAME B", selectedGame == 1, onGames && selectedGame == 1, 152, 92);
                DrawStringBitmap(spriteBatch, "MUSIC", new Vector2(20, 110), new Color(240, 218, 160));
                // Bottom row: Music 1..5. Brackets mark the chosen music.
                bool onMusic = modeSelection == 1;
                for (int i = 0; i < 5; i++)
                    DrawModeItem($"{i + 1}", i + 1 == selectedMusic, onMusic && i + 1 == selectedMusic, 76 + i * 28, 110);
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

        // Draws a mode-select item. Brackets mark the item the cursor row is
        // on; the currently chosen item of each row stays amber at all times.
        void DrawModeItem(string label, bool brackets, bool chosen, int left, int top)
        {
            Color color = chosen ? new Color(255, 225, 145) : Color.White;
            string text = brackets ? "[" + label + "]" : " " + label + " ";
            DrawStringBitmap(spriteBatch, text, new Vector2(left, top), color);
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
            drawMenuItem("SOUND", optionsSelection == 1, left, top + 18);
            drawMenuItem("MUSIC", optionsSelection == 2, left, top + 36);
            drawMenuItem("BACK", optionsSelection == 3, left, top + 54);
            // Sliders share one right-aligned edge, independent of the labels.
            DrawVolumeBar(NATIVE_WIDTH - 20, top + 18, soundVolume);
            DrawVolumeBar(NATIVE_WIDTH - 20, top + 36, musicVolume);
        }

        // Draws a volume slider like [||||||||--]  80% at bitmap-font scale,
        // with the percentage padded so every row is the same width.
        void DrawVolumeBar(int rightEdge, int top, int value)
        {
            Color color = new Color(255, 225, 145);
            string text = "[" + new string('|', value) + new string('-', VolumeSteps - value) + "] "
                + (value * 10).ToString().PadLeft(4) + "%";
            DrawStringBitmap(spriteBatch, text, new Vector2(rightEdge - text.Length * FONT_CELL, top), color);
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
            gameoverMusicPlaying = false;
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
            // Retrying: restart the selected gameplay track (the game-over
            // jingle may have replaced it, or it may have faded out entirely).
            SwitchToGameplayMusic();
            caughtbean = pyorosquat = blocktocheck = 0;
            speed = 1;
            smallspeed = 0xFF;
            bigspeed = 0x100;
            score = 0;
            risingscore = 5000;
            rainbowbeantotal = 10;
            max_time = 0xB4;
            time_until_new_bean = tmpmax = tmpspeed = beanspeed = 0;
            // Fresh independent 16-bit states keep each round's spawn sequence different.
            randnum = Random.Shared.Next(0x10000);
            randnum2 = Random.Shared.Next(0x10000);
            randnum3 = Random.Shared.Next(0x10000);
            randnum4 = Random.Shared.Next(0x10000);
            randnummain = Random.Shared.Next(0x10000);
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
