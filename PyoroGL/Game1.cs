using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;

namespace MonogameTest
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public partial class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        RenderTarget2D _nativeRenderTarget;
        RasterizerState playfieldRasterizer;

        const int NATIVE_WIDTH = 288;
        const int NATIVE_HEIGHT = 216;
        const int PLAYFIELD_LEFT = 8;
        const int PLAYFIELD_RIGHT = NATIVE_WIDTH - 8;
        const int PLAYFIELD_TOP = 8;
        const int BLOCK_SIZE = 8;
        const int BLOCK_COUNT = (PLAYFIELD_RIGHT - PLAYFIELD_LEFT) / BLOCK_SIZE;
        const int BLOCK_FLOOR_Y = NATIVE_HEIGHT - 16;
        const int PLAYER_START_X = NATIVE_WIDTH / 2 - 9;
        const int PLAYER_START_Y = BLOCK_FLOOR_Y - 16;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.SynchronizeWithVerticalRetrace = true;
            Content.RootDirectory = "Content";
            Window.AllowUserResizing = true;
            Exiting += (sender, args) => highScores.Flush();
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                // --shots: verify UFO mechanics and capture screenshots, then exit.
                if (arg == "--shots")
                    shotsMode = true;
            }
            progression = new UfoProgression(shotsMode);
        }

        // Compute the largest integer scale factor that fits the current window,
        // preserving the 288x216 aspect ratio. Also center the render target
        // destination and fill leftover space with the border colour.
        void computeIntegerScale()
        {
            int winW = Window.ClientBounds.Width;
            int winH = Window.ClientBounds.Height;

            // Guard against degenerate sizes.
            if (winW < 1 || winH < 1)
            {
                gameSize = 1;
                rect = new Rectangle(0, 0, NATIVE_WIDTH, NATIVE_HEIGHT);
                return;
            }

            // Largest integer multiplier where both dimensions still fit,
            // keeping the 4:3 aspect ratio.
            int s = Math.Min(winW / NATIVE_WIDTH, winH / NATIVE_HEIGHT);
            if (s < 1) s = 1;
            gameSize = s;

            int dstW = NATIVE_WIDTH * s;
            int dstH = NATIVE_HEIGHT * s;
            int offX = (winW - dstW) / 2;
            int offY = (winH - dstH) / 2;
            rect = new Rectangle(offX, offY, dstW, dstH);
        }

        void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            // Recompute the integer scale and centered destination rectangle so
            // the scaled game always snaps to an even multiplier with the
            // remaining window area filled by the clear colour.
            computeIntegerScale();
        }

        int targetFPS = 60;

        SpriteFont arial;
        SpriteFont smallfont;        // Pixel-perfect 8x8 bitmap font atlas (Assets/font8x8_atlas.png):
        // 16 columns of ASCII 32..126, binary alpha, zero anti-aliasing.
        Texture2D fontAtlas;
        // 6x6 pixel font for score popups and the MUSIC HUD readout.
        Font6 font6;
        const int FONT_CELL = 8;
        const int FONT_COLS = 16;
        const int FONT_FIRST_CHAR = 32;
        Rectangle rect;
        float frameRate;
        float updates;
        float x, y;
        //KeyboardState state;
        private Texture2D pyororight;
        private Texture2D pyoroleft;
        private Texture2D pyoroopenright;
        private Texture2D pyoroopenleft;
        private Texture2D pyorosquatright;
        private Texture2D pyorosquatleft;
        private Texture2D pyoro;
        private Texture2D pyorodeadleft;
        private Texture2D pyorodeadright;
        private Texture2D frame;
        // 4-variant 8x8 brick-face atlas (Assets/block_atlas.png) for floor blocks.
        private Texture2D blockAtlas;
        private int beamWidth = 3;
        private KeyboardState previousBeamKeys;
        private BeamAudio beamAudio;

        // Continuous camo plasma at native pixel resolution, scaled without smoothing.
        const int CAMO_WIDTH = 96;
        const int CAMO_HEIGHT = 54;
        private Texture2D borderCamo;
        private readonly Color[] borderPixels = new Color[CAMO_WIDTH * CAMO_HEIGHT];
        private readonly double[] camoXWave1 = new double[CAMO_WIDTH];
        private readonly double[] camoXWave2 = new double[CAMO_WIDTH];
        private readonly Color[] camoPalette = makeCamoPalette();
        private double borderTime;
        private readonly Rectangle[] frameSources = new Rectangle[8];
        private readonly Rectangle[] frameDestinations = new Rectangle[8];

        // Visual thickness in native pixels, measured perpendicular to the beam.
        public int BeamWidth
        {
            get { return beamWidth; }
            set { beamWidth = Math.Clamp(value, 1, 20); }
        }
        MusicTracks music;
        private Texture2D[] mortarFrames;
        private Texture2D bean_centre;
        private Texture2D bean_left;
        private Texture2D bean_right;
        private Texture2D beanw_centre;
        private Texture2D beanw_left;
        private Texture2D beanw_right;
        private Texture2D beanb_centre;
        private Texture2D beanb_left;
        private Texture2D beanb_right;
        private Texture2D[] current_bean_sprite;
        private Texture2D temp_bean_sprite;
        private Texture2D background;
        private Rectangle backgroundSource;
        private Texture2D beamPixel;


        //Timer t1sec = new Timer(1000);
        int blockamount = BLOCK_COUNT;
        
        float speed = 1.0f;
        MouseState mouseState;
        int gameSize = 4;
        bool[] blocks;
        float tongueX, tongueY;
        int facingright = 1;
        int tongueoffsetX, tongueoffsetY;
        int rightoffset = 14; // 14: one pixel right of the barrel tip
        int spaceheld = 0;
        float tonguecount = 0;
        bool recall = false;
        bool tonguecollide = false;
        

        int max_amount_of_beans = 16;
        bool[] bean_active;
        int[] bean_speed;
        float[] bean_y;
        float[] bean_x;
        int[] bean_type;
        float[] bean_rainbow_counter;
        int smallspeed = 0xFF;
        int bigspeed = 0x100;
        int blocktocheck = 0;
        bool paused = false;
        int pyorosquat = 0;

        
        int max_time = 0xB4;
        int tmpmax = 0x0;
        int randnum = 0x0;
        int time_until_new_bean = 0x0;
        int score = 0;
        int highScore = 10000;
        readonly HighScoreStore highScores = new HighScoreStore(System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Warhook", "ufo-highscores.json"));
        
        int tmpspeed = 0x0;
        int randnum2 = 0x0;
        int beanspeed = 0;
        int beantype = 0;
        int currentbeantype = 0;
        int currentbeanx = 0;
        int randnum3 = 0x0;
        int randnum4 = 0x0;
        int beanxrandom = 0;

        int randnummain = 0;

        int rainbowbeantotal = 10;
        //int rainbowbeanset = 0;
        //int spawnedrainbowforever = 0;
        int risingscore = 5000;
        bool create_new_bean = false;
        int new_bean_number_debug = 0;
        float maxdisappear = 10;
        float dissappearcounter = 0;
        bool triggerdisappear = false;
        int falsebeans = 0;

        // Score popups: a small "+points" text that briefly appears where a
        // bean was caught, then fades out (mirrors the pico-8 version).
        List<ScorePopup> scorePopups = new List<ScorePopup>();

        // A 3-frame 16x16 explosion plays where a block or bean disappears
        // (mirrors the pico-8 smoke/burst effect on destruction).
        List<Explosion> explosions = new List<Explosion>();
        // Tank-hit explosions are drawn in a second pass AFTER the tank sprite
        // so the blast renders above the tank at the point of collision.
        List<Explosion> overlayExplosions = new List<Explosion>();
        private Texture2D explosionSprite;

        // An angel descends to a missing block column and rebuilds it on
        // landing (mirrors the pico-8 angel effect when a white seed/rainbow
        // bean restores floor blocks). angel.png is 32x16 = 2 frames of 16x16.
        List<Angel> angels = new List<Angel>();
        Queue<int> angelQueue = new Queue<int>();
        int angelQueueTimer = 0;
        private Texture2D angelSprite;

        // Screenshot counter for Tab-key PNG saves.
        int screenshotCount = 0;

        // UFO smoke checks and screenshot capture (--shots); see UfoSmokeChecks.cs.
        bool shotsMode;
        int shotStage;
        double shotTimer;

        // When a rainbow bean is grabbed, other beans disappear one by one
        // (lowest first) instead of all at once. This queue holds the bean
        // indices still waiting to be cleared, and the timer paces them.
        List<int> rainbowClearQueue = new List<int>();
        int rainbowClearTimer = 0;

        bool pyorodead = false;
        bool gameover = false;

        int caughtbean = 0;
        
        //int time_until_new_bean = 0x0;
        ///int score = 0;
        //int beanspeed = 0;
        //int test = 0;
        Random rnd = new Random();
        Random r = new Random();
        float[] bean_anim_counter;
        


        void speedloop()
        {
            if(smallspeed>0)
            {
                smallspeed--;
            }
            if(smallspeed==0)
            {
                smallspeed = 0x10;
                bigspeed++;
            }
            if (bigspeed < 0x100)
            {
                bigspeed = 0x100;
            }
            if (bigspeed>=0x7F0)
            {
                bigspeed = 0x7F0;
            }
        }

        void bean_animation()
        {
            for (int i = 0; i < max_amount_of_beans; i++)
            {
                bean_anim_counter[i] += 0.5f;
                bean_rainbow_counter[i] += 0.5f;
                if (bean_rainbow_counter[i] >= 6)
                {
                    bean_rainbow_counter[i] = 0;
                }
                if (bean_anim_counter[i] >= 0 && bean_anim_counter[i] <= 17)
                {
                    if(bean_type[i] == 0)
                    {
                        current_bean_sprite[i] = bean_left;
                    }
                    if (bean_type[i] == 1)
                    {
                        current_bean_sprite[i] = beanw_left;
                    }
                    if (bean_type[i] == 2)
                    {
                        //bean_rainbow_counter[i] += 0.5f;
                        if (bean_rainbow_counter[i] > 0 && bean_rainbow_counter[i] <= 1)
                        {
                            current_bean_sprite[i] = bean_left;
                        }
                        if (bean_rainbow_counter[i] > 1 && bean_rainbow_counter[i] <= 3)
                        {
                            current_bean_sprite[i] = beanw_left;
                        }
                        if (bean_rainbow_counter[i] > 3 && bean_rainbow_counter[i] <= 5)
                        {
                            current_bean_sprite[i] = beanb_left;
                        }
                        if (bean_rainbow_counter[i] > 5)
                        {
                            bean_rainbow_counter[i] = 0;
                        }
                    }
                }
                if (bean_anim_counter[i] >= 18 && bean_anim_counter[i] <= 25)
                {
                    if (bean_type[i] == 0)
                    {
                        current_bean_sprite[i] = bean_centre;
                    }
                    if (bean_type[i] == 1)
                    {
                        current_bean_sprite[i] = beanw_centre;
                    }
                    if (bean_type[i] == 2)
                    {
                        if (bean_rainbow_counter[i] > 0 && bean_rainbow_counter[i] <= 1)
                        {
                            current_bean_sprite[i] = bean_centre;
                        }
                        if (bean_rainbow_counter[i] > 1 && bean_rainbow_counter[i] <= 3)
                        {
                            current_bean_sprite[i] = beanw_centre;
                        }
                        if (bean_rainbow_counter[i] > 3 && bean_rainbow_counter[i] <= 5)
                        {
                            current_bean_sprite[i] = beanb_centre;
                        }
                        if (bean_rainbow_counter[i] > 5)
                        {
                            bean_rainbow_counter[i] = 0;
                        }
                    }
                }
                if (bean_anim_counter[i] >= 26 && bean_anim_counter[i] <= 43)
                {
                    if (bean_type[i] == 0)
                    {
                        current_bean_sprite[i] = bean_right;
                    }
                    if (bean_type[i] == 1)
                    {
                        current_bean_sprite[i] = beanw_right;
                    }
                    if (bean_type[i] == 2)
                    {
                        if (bean_rainbow_counter[i] > 0 && bean_rainbow_counter[i] <= 1)
                        {
                            current_bean_sprite[i] = bean_right;
                        }
                        if (bean_rainbow_counter[i] > 1 && bean_rainbow_counter[i] <= 3)
                        {
                            current_bean_sprite[i] = beanw_right;
                        }
                        if (bean_rainbow_counter[i] > 3 && bean_rainbow_counter[i] <= 5)
                        {
                            current_bean_sprite[i] = beanb_right;
                        }
                        if (bean_rainbow_counter[i] > 5)
                        {
                            bean_rainbow_counter[i] = 0;
                        }
                    }
                }
                if (bean_anim_counter[i] >= 44 && bean_anim_counter[i] <= 51)
                {
                    if (bean_type[i] == 0)
                    {
                        current_bean_sprite[i] = bean_centre;
                    }
                    if (bean_type[i] == 1)
                    {
                        current_bean_sprite[i] = beanw_centre;
                    }
                    if (bean_type[i] == 2)
                    {
                        if (bean_rainbow_counter[i] > 0 && bean_rainbow_counter[i] <= 1)
                        {
                            current_bean_sprite[i] = bean_centre;
                        }
                        if (bean_rainbow_counter[i] > 1 && bean_rainbow_counter[i] <= 3)
                        {
                            current_bean_sprite[i] = beanw_centre;
                        }
                        if (bean_rainbow_counter[i] > 3 && bean_rainbow_counter[i] <= 5)
                        {
                            current_bean_sprite[i] = beanb_centre;
                        }
                        if (bean_rainbow_counter[i] > 5)
                        {
                            bean_rainbow_counter[i] = 0;
                        }
                    }
                    //current_bean_sprite[i] = bean_centre;
                }
                if (bean_anim_counter[i] >= 52)
                {
                    bean_anim_counter[i] = 0;
                }
            }
        }
        void block_recovery()
        {
            requestBlockRecovery(false);
        }

        bool needsRecovery(int column)
        {
            return column >= 0 && column < blockamount && !blocks[column]
                && !angelQueue.Contains(column)
                && !angels.Exists(a => a.column == column && !a.blockPlaced);
        }

        void requestBlockRecovery(bool queued)
        {
            int start = Math.Clamp((int)Math.Ceiling((x + speed - PLAYFIELD_LEFT + 2) / 8), 0, blockamount - 1);
            for (int distance = 0; distance < blockamount; distance++)
            {
                int left = start - distance;
                int right = start + distance;
                int column = needsRecovery(left) ? left : needsRecovery(right) ? right : -1;
                if (column < 0) continue;
                if (queued)
                {
                    if (angelQueue.Count == 0) angelQueueTimer = 0;
                    angelQueue.Enqueue(column);
                }
                else
                    spawnAngel(column);
                return;
            }
        }

        void updateAngelQueue()
        {
            if (angelQueue.Count == 0) return;
            if (angelQueueTimer > 0) angelQueueTimer--;
            if (angelQueueTimer > 0) return;
            spawnAngel(angelQueue.Dequeue());
            // The original rainbow effect releases an angel every 16 frames.
            angelQueueTimer = 16;
        }

        // Play a 3-frame 16x16 explosion animation at a native (288x216) position.
        // Mirrors the pico-8 smoke/burst used when a block or bean disappears.
        void spawnExplosion(float x, float y, bool playSound = true)
        {
            // Only start one explosion per position at a time.
            if (!explosions.Exists(e => e.x == x && e.y == y))
            {
                explosions.Add(new Explosion(x, y));
                if (playSound) beamAudio?.PlayExplosion();
            }
        }

        // Advance all active explosions through their 3-frame animation.
        void updateExplosions()
        {
            for (int i = explosions.Count - 1; i >= 0; i--)
            {
                explosions[i].timer++;
                if (explosions[i].timer >= explosions[i].frameDuration * 3)
                    explosions.RemoveAt(i);
            }
            for (int i = overlayExplosions.Count - 1; i >= 0; i--)
            {
                overlayExplosions[i].timer++;
                if (overlayExplosions[i].timer >= overlayExplosions[i].frameDuration * 3)
                    overlayExplosions.RemoveAt(i);
            }
        }

        // Spawn a tank-hit explosion that draws ABOVE the tank sprite at the
        // point of collision (midpoint between tank centre and mortar centre).
        void spawnTankExplosion(float hitX, float hitY)
        {
            overlayExplosions.Add(new Explosion(hitX, hitY));
            beamAudio?.PlayExplosion();
        }

        // Shared rainbow burst: append living mortars without restarting an active wave.
        void queueRainbowClear()
        {
            bool wasClearing = rainbowClearQueue.Count > 0;
            for (int i = 0; i < max_amount_of_beans; i++)
                if (bean_active[i] && !rainbowClearQueue.Contains(i))
                    rainbowClearQueue.Add(i);
            rainbowClearQueue.Sort((a, b) => bean_y[b].CompareTo(bean_y[a]));
            if (!wasClearing) rainbowClearTimer = 0;
        }

        // Clear queued beans one by one (lowest first) after a rainbow bean is
        // grabbed. One bean clears every few frames.
        void updateRainbowClear()
        {
            if (rainbowClearQueue.Count == 0) return;

            rainbowClearTimer++;
            if (rainbowClearTimer < 6) return; // pace: one bean per 6 frames
            rainbowClearTimer = 0;

            int j = rainbowClearQueue[0];
            rainbowClearQueue.RemoveAt(0);
            if (j >= 0 && j < max_amount_of_beans && bean_active[j])
            {
                bean_active[j] = false;
                addScore(bean_x[j], bean_y[j], 50);
                // Use the mortar sprite centre; its position is the top-left corner.
                spawnExplosion(bean_x[j] + current_bean_sprite[j].Width / 2f,
                    bean_y[j] + current_bean_sprite[j].Height / 2f);
            }
        }

        // Render active explosions centred on their position.
        void drawExplosions(SpriteBatch batch)
        {
            foreach (Explosion e in explosions)
            {
                int frame = (int)(e.timer / e.frameDuration);
                if (frame > 2) frame = 2;
                // Generated explosion sheet is sampled into three 16x16 cells at load time.
                Rectangle src = new Rectangle(frame * 16, 0, 16, 16);
                // Centre the 16x16 sprite on the given position.
                float dx = e.x - 8;
                float dy = e.y - 8;
                batch.Draw(explosionSprite, new Vector2(dx, dy), src, Color.White);
            }
        }

        // Second-pass explosion draw for tank hits (above the tank sprite).
        void drawOverlayExplosions(SpriteBatch batch)
        {
            foreach (Explosion e in overlayExplosions)
            {
                int frame = (int)(e.timer / e.frameDuration);
                if (frame > 2) frame = 2;
                Rectangle src = new Rectangle(frame * 16, 0, 16, 16);
                batch.Draw(explosionSprite, new Vector2(e.x - 8, e.y - 8), src, Color.White);
            }
        }

        // Spawn an angel that descends to the given block column and rebuilds
        // it on landing. Existing angels never suppress a new explicit request.
        void spawnAngel(int column)
        {
            if (column < 0 || column >= blockamount) return;
            angels.Add(new Angel(column));
            // Friendly supply-drop "bwoop" as the parachute appears.
            beamAudio?.PlayParachute();
        }

        // Advance angels: accelerate downward, place the block on reaching the floor
// row, then keep decelerating until the angel flies back up off-screen
// (mirrors pico-8: block placed at y>=100, angel removed at y<-9).
        void updateAngels()
        {
            for (int i = angels.Count - 1; i >= 0; i--)
            {
                Angel a = angels[i];
                a.speed -= a.acceleration;
                a.y += a.speed;
                a.timer += 1f;

                // Place the block once the angel's carried tile (drawn at
                // y+16) reaches the floor row, so it doesn't overshoot.
                if (!a.blockPlaced && a.y + 16 >= BLOCK_FLOOR_Y)
                {
                    if (a.column >= 0 && a.column < blockamount)
                        blocks[a.column] = true;
                    a.blockPlaced = true;
                    // Reverse direction immediately so the angel flies back up
                    // instead of continuing to descend past the floor.
                    a.speed = -a.speed;
                }

                // Remove once the angel has flown back up off-screen.
                if (a.y < -9)
                {
                    angels.RemoveAt(i);
                }
            }
        }

        // Render supply parachutes using two 16x16 sway frames. When the
        // angel is falling fast enough it carries a block tile below it,
        // mirroring pico-8's sspr(44,12,6,6) carried tile.
        void drawAngels(SpriteBatch batch)
        {
            foreach (Angel a in angels)
            {
                int frame = ((int)a.timer / 4) % 2; // alternate every 4 ticks
                Rectangle src = new Rectangle(frame * 16, 0, 16, 16);
                int blockx = PLAYFIELD_LEFT + a.column * 8 - 4; // centre on 8px block
                float dx = blockx;
                // Round to whole pixels so the pixel-art sprite stays crisp
                // (no sub-pixel smoothing while the angel moves).
                float dy = (float)System.Math.Round(a.y);
                batch.Draw(angelSprite, new Vector2(dx, dy), src, Color.White);

                // Carried block tile below the angel once it is moving down.
                if (a.speed > 0)
                {
                    batch.Draw(blockAtlas, new Vector2(PLAYFIELD_LEFT + a.column * 8, (float)System.Math.Round(a.y + 16)), Color.White);
                }
            }
        }

        // Add points to the running score and spawn a short-lived "+pts" popup
        // at the given native (288x216) position, mirroring the pico-8 version.
        void addScore(float x, float y, int pts)
        {
            // Background effects continue after game over, but the result is final.
            if (gameover) return;
            score += pts;
            highScore = highScores.Record(gameB, score);
            scorePopups.Add(new ScorePopup(x, y, pts));
        }

        // Draw a score popup in the 6x6 pixel font (e.g. "+300") drifting up
        // from where the mortar/bean was picked up, centred on that point.
        void DrawScorePopup6(SpriteBatch batch, ScorePopup p)
        {
            float alpha = MathHelper.Clamp(p.timer / 30f, 0f, 1f);
            Vector2 size = font6.Measure(p.points.ToString());
            font6.Draw(batch, p.points.ToString(), new Vector2(p.x , p.y), Color.White * alpha);
        }

        // Update active score popups: drift upward slightly and expire by timer.
        // Called near the end of Update so popups animate while the game runs.
        void updateScorePopups()
        {
            for (int i = scorePopups.Count - 1; i >= 0; i--)
            {
                scorePopups[i].timer -= 1f;
                scorePopups[i].y -= 0.25f; // drift up a touch
                if (scorePopups[i].timer <= 0)
                {
                    scorePopups.RemoveAt(i);
                }
            }
        }

        void random_number_main()
        {
            randnummain = (0x6D * randnummain) + 0x3FD;
            randnummain = (randnummain & 0x0000FFFF);
        }

        // Return the sprite region in scores.png for a given score-popup value.
        // The sheet contains 10, 50, 100, three 300 variants, then two 1000
        // variants. Use the first variant of each value; an empty rectangle
        // indicates that the value has no sprite.
        Rectangle ScorePopupSpriteRegion(int points)
        {
            switch (points)
            {
                case 10:  return new Rectangle(3, 0, 5, 7);
                case 50:  return new Rectangle(12, 0, 7, 7);
                case 100: return new Rectangle(23, 0, 9, 7);
                case 300: return new Rectangle(36, 0, 11, 7);
                case 1000:return new Rectangle(81, 0, 13, 7);
                default:  return new Rectangle(0, 0, 0, 0); // not found
            }
        }
        /*void time_max_rand(int tmptorand)
        {
            tmpmax = tmptorand << 14;
            tmpmax = tmpmax >> 16;
            tmpmax = tmpmax << 16;
            tmpmax = tmpmax >> 16;
            randnum = (0x6D * randnum) + 0x3FD;
            randnum = (randnum & 0x0000FFFF);
            tmpmax = tmpmax * randnum;
            tmpmax = tmpmax >> 16;
            tmpmax = tmpmax << 16;
            tmpmax = tmpmax >> 16;
            tmpmax = max_time - tmpmax;
            tmpmax = tmpmax << 8;
            time_until_new_bean = tmpmax / bigspeed;
        }*/
        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            highScore = highScores.Get(gameB);
            // TODO: Add your initialization logic here
            _nativeRenderTarget = new RenderTarget2D(GraphicsDevice, NATIVE_WIDTH, NATIVE_HEIGHT);
            Window.ClientSizeChanged += Window_ClientSizeChanged;

            if (GameAssets.IsWeb)
            {
                // The Blazor host sizes the canvas before Game1 starts. Do not
                // use the desktop monitor/2 heuristic inside an itch.io iframe.
                graphics.PreferredBackBufferWidth = Math.Max(NATIVE_WIDTH, Window.ClientBounds.Width);
                graphics.PreferredBackBufferHeight = Math.Max(NATIVE_HEIGHT, Window.ClientBounds.Height);
            }
            else
            {
                // Half each monitor dimension gives one-quarter of its screen area.
                DisplayMode monitor = GraphicsDevice.Adapter.CurrentDisplayMode;
                graphics.PreferredBackBufferWidth = Math.Max(NATIVE_WIDTH, monitor.Width / 2);
                graphics.PreferredBackBufferHeight = Math.Max(NATIVE_HEIGHT, monitor.Height / 2);
            }
            Window.Title = "WarHook: UFO Abduction";
            graphics.ApplyChanges();
            computeIntegerScale();
            x = PLAYER_START_X;
            y = PLAYER_START_Y;
            tongueoffsetX = 1; // mirrored barrel tip
            tongueoffsetY = -1; // barrel tip above the player collision box
            tongueX = x + tongueoffsetX;
            tongueY = y + tongueoffsetY;
            tonguecount = 0;
            IsFixedTimeStep = true;
            TargetElapsedTime = System.TimeSpan.FromMilliseconds(1000.0f / targetFPS);
            this.IsMouseVisible = true;
            blocks = new bool[blockamount];
            bean_active = new bool[max_amount_of_beans];
            bean_speed = new int[max_amount_of_beans];
            bean_y = new float[max_amount_of_beans];
            bean_x = new float[max_amount_of_beans];
            bean_type = new int[max_amount_of_beans];
            current_bean_sprite = new Texture2D[max_amount_of_beans];
            bean_anim_counter = new float[max_amount_of_beans];
            bean_rainbow_counter = new float[max_amount_of_beans];
            for (int i=0;i< blockamount; i++)
            {
                blocks[i] = true;
            }
            for (int i = 0; i < max_amount_of_beans; i++)
            {
                bean_active[i] = false;
                bean_speed[i] = 0x0;
                bean_y[i] = -20;
                bean_x[i] = r.Next(PLAYFIELD_LEFT + 8, PLAYFIELD_RIGHT - 8);
                bean_anim_counter[i] = rnd.Next(0, 43);
                bean_rainbow_counter[i] = rnd.Next(0, 5);
                bean_type[i] = 0;
            }

            random_number_main();

            base.Initialize();



            /* restart game
            x = PLAYER_START_X;
            y = PLAYER_START_Y;
            tongueoffsetX = 1; // mirrored barrel tip
            tongueoffsetY = -1; // barrel tip above the player collision box
            tongueX = x + tongueoffsetX;
            tongueY = y + tongueoffsetY;
            tonguecount = 0;
            TargetElapsedTime = System.TimeSpan.FromMilliseconds(1000.0f / targetFPS);
            for (int i=0;i< blockamount; i++)
            {
                blocks[i] = true;
            }
            for (int i = 0; i < max_amount_of_beans; i++)
            {
                bean_active[i] = false;
                bean_speed[i] = 0x0;
                bean_y[i] = -20;
                bean_x[i] = r.Next(PLAYFIELD_LEFT + 8, PLAYFIELD_RIGHT - 8);
                bean_anim_counter[i] = rnd.Next(0, 43);
                bean_rainbow_counter[i] = rnd.Next(0, 5);
                bean_type[i] = 0;
            }
            */
        }
        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            beamAudio = new BeamAudio(GameAssets.BeamAudioConfigPath());
            // UFO rules are incompatible with the original online leaderboard.
            HighScoreStore.OnlineSubmitHook = null;
            music = new MusicTracks(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets"));
            music.Request(MusicTracks.Track.Menu);
            ApplyVolumes();
            mainMenuBackground = loadPng("mainmenu");
            mainMenuTitle = loadPng("title");
            playfieldRasterizer = new RasterizerState { ScissorTestEnable = true, CullMode = CullMode.None };
            arial = Content.Load<SpriteFont>("font");
            smallfont = Content.Load<SpriteFont>("smallfont");
            fontAtlas = loadPng("font8x8_atlas");
            font6 = new Font6(GraphicsDevice);
            loadPlayerTank();
            loadMuzzleFlash();
            yellowTankRight = makeYellowTank(pyororight);
            yellowTankLeft = makeYellowTank(pyoroleft);
            pyoro = pyororight;
            loadBackdrop();
            loadMortarFrames();
            temp_bean_sprite = bean_centre;
            for (int i = 0; i < max_amount_of_beans; i++)
            {
                current_bean_sprite[i] = bean_centre;
            }
            frame = loadPng("framewide");
            prepareFrameSlices();
            blockAtlas = loadPng("block_atlas");
            explosionSprite = loadEffectSheet("explosion-new", 3);
            angelSprite = loadEffectSheet("parachute", 2);
            beamPixel = new Texture2D(GraphicsDevice, 1, 1);
            beamPixel.SetData(new[] { Color.White });
            LoadUfoAssets();
            borderCamo = new Texture2D(GraphicsDevice, CAMO_WIDTH, CAMO_HEIGHT);
            // The widescreen playfield uses BLOCK_COUNT eight-pixel floor columns.
            // TODO: use this.Content to load your game content here
        }

        Texture2D loadPng(string name)
        {
            using (Stream stream = GameAssets.Open(name + ".png"))
            {
                Texture2D texture = Texture2D.FromStream(GraphicsDevice, stream);
                Color[] pixels = new Color[texture.Width * texture.Height];
                texture.GetData(pixels);
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = Color.FromNonPremultiplied(pixels[i].R, pixels[i].G, pixels[i].B, pixels[i].A);
                texture.SetData(pixels);
                return texture;
            }
        }

        // Measure a string in the 8x8 bitmap font (monospace: 8px per char).
        Vector2 MeasureStringBitmap(string text)
        {
            return new Vector2(text.Length * FONT_CELL, FONT_CELL);
        }

        // Draw a string with the pixel-perfect 8x8 bitmap font. Every glyph is
        // an unfiltered 8x8 cell from the atlas, so there is zero anti-aliasing.
        void DrawStringBitmap(SpriteBatch batch, string text, Vector2 position, Color color)
        {
            for (int i = 0; i < text.Length; i++)
            {
                int index = text[i] - FONT_FIRST_CHAR;
                if (index < 0 || index >= (fontAtlas.Width / FONT_CELL) * (fontAtlas.Height / FONT_CELL))
                    index = '?' - FONT_FIRST_CHAR;
                Rectangle src = new Rectangle(
                    (index % FONT_COLS) * FONT_CELL,
                    (index / FONT_COLS) * FONT_CELL,
                    FONT_CELL, FONT_CELL);
                batch.Draw(fontAtlas, new Vector2((int)position.X + i * FONT_CELL, (int)position.Y), src, color);
            }
        }

        Texture2D loadEffectSheet(string name, int frameCount)
        {
            const int frameSize = 16;
            using (Texture2D sheet = loadPng(name))
            {
                Color[] source = new Color[sheet.Width * sheet.Height];
                sheet.GetData(source);
                int sourceWidth = sheet.Width / frameCount;
                int atlasWidth = frameCount * frameSize;
                Color[] pixels = new Color[atlasWidth * frameSize];
                for (int frame = 0; frame < frameCount; frame++)
                    for (int y = 0; y < frameSize; y++)
                        for (int x = 0; x < frameSize; x++)
                        {
                            int sx = frame * sourceWidth + (int)((x + 0.5f) * sourceWidth / frameSize);
                            int sy = (int)((y + 0.5f) * sheet.Height / frameSize);
                            pixels[y * atlasWidth + frame * frameSize + x] = source[sy * sheet.Width + sx];
                        }
                Texture2D atlas = new Texture2D(GraphicsDevice, atlasWidth, frameSize);
                atlas.SetData(pixels);
                return atlas;
            }
        }

        void loadPlayerTank()
        {
            using (Texture2D sheet = loadPng("tank-pixelart"))
            {
                Color[] source = new Color[sheet.Width * sheet.Height];
                sheet.GetData(source);
                Color backgroundColor = source[0];
                // Remove the solid background and one-pixel outer gutter, retaining native pixels.
                int width = sheet.Width - 2, height = sheet.Height - 2;
                Color[] right = new Color[width * height];
                Color[] left = new Color[right.Length];
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        Color pixel = source[(y + 1) * sheet.Width + x + 1];
                        if (pixel == backgroundColor) pixel = Color.Transparent;
                        right[y * width + x] = pixel;
                        left[y * width + width - 1 - x] = pixel;
                    }
                pyororight = new Texture2D(GraphicsDevice, width, height);
                pyororight.SetData(right);
                pyoroleft = new Texture2D(GraphicsDevice, width, height);
                pyoroleft.SetData(left);
            }
            // The supplied tank has one pose; use it for every player state.
            pyorosquatright = pyoroopenright = pyorodeadright = pyororight;
            pyorosquatleft = pyoroopenleft = pyorodeadleft = pyoroleft;
        }

        void loadMortarFrames()
        {
            // Rows: green, white, blue. Columns: upright, leaning left, leaning right.
            // The sheet's columns are unevenly spaced; split at the transparent gutters.
            int[] columns = { 0, 24, 51, 78 };
            mortarFrames = new Texture2D[9];
            using (Texture2D sheet = loadPng("mortars-pixelart-cleaned"))
            {
                Color[] source = new Color[sheet.Width * sheet.Height];
                sheet.GetData(source);
                for (int row = 0; row < 3; row++)
                    for (int pose = 0; pose < 3; pose++)
                    {
                        Rectangle region = new Rectangle(columns[pose], row * 36, columns[pose + 1] - columns[pose], 36);
                        // Fit into the existing collision space without squashing the artwork.
                        const int size = 16;
                        int width = (int)Math.Round(region.Width * (float)size / region.Height);
                        Color[] pixels = new Color[size * size];
                        for (int y = 0; y < size; y++)
                            for (int x = 0; x < width; x++)
                            {
                                int sourceX = region.X + (int)((x + 0.5f) * region.Width / width);
                                int sourceY = region.Y + (int)((y + 0.5f) * region.Height / size);
                                pixels[y * size + (size - width) / 2 + x] = source[sourceY * sheet.Width + sourceX];
                            }
                        Texture2D frame = new Texture2D(GraphicsDevice, size, size);
                        frame.SetData(pixels);
                        mortarFrames[row * 3 + pose] = frame;
                    }
            }
            bean_centre = mortarFrames[0];
            bean_left = mortarFrames[1];
            bean_right = mortarFrames[2];
            beanw_centre = mortarFrames[3];
            beanw_left = mortarFrames[4];
            beanw_right = mortarFrames[5];
            beanb_centre = mortarFrames[6];
            beanb_left = mortarFrames[7];
            beanb_right = mortarFrames[8];
        }

        void loadBackdrop()
        {
            background = loadPng("newbackdrop");
            Color[] pixels = new Color[background.Width * background.Height];
            background.GetData(pixels);
            // Ignore the PNG's transparent bottom padding so the scenery meets the floor.
            int height = background.Height;
            while (height > 1)
            {
                bool visible = false;
                for (int x = 0; x < background.Width; x++)
                    visible |= pixels[(height - 1) * background.Width + x].A != 0;
                if (visible) break;
                height--;
            }
            backgroundSource = new Rectangle(0, 0, background.Width, height);
        }

        void drawScenery()
        {
            // Draw the backdrop shifted down 8px at full height, so its bottom
            // tucks behind the floor blocks (no gap shows when blocks
            // disappear). The exposed 8px strip above the image is filled with
            // the image's own top row so the sky isn't cut off.
            const int BG_DOWN = 8;
            spriteBatch.Draw(background,
                new Rectangle(PLAYFIELD_LEFT, PLAYFIELD_TOP + BG_DOWN,
                    PLAYFIELD_RIGHT - PLAYFIELD_LEFT, BLOCK_FLOOR_Y - PLAYFIELD_TOP),
                backgroundSource, Color.White);
            spriteBatch.Draw(background,
                new Rectangle(PLAYFIELD_LEFT, PLAYFIELD_TOP, PLAYFIELD_RIGHT - PLAYFIELD_LEFT, BG_DOWN),
                new Rectangle(backgroundSource.X, backgroundSource.Y, backgroundSource.Width, 1),
                Color.White);
        }

        void drawTractorBeam()
        {
            if (gameB || pyorodead || tonguecount <= 0) return;
            Vector2 start = new Vector2((float)Math.Round(tongueX), (float)Math.Round(tongueY));
            Vector2 end = start + new Vector2(tonguecount * facingright, -tonguecount);
            Vector2 direction = Vector2.Normalize(end - start);
            Vector2 normal = new Vector2(-direction.Y, direction.X);
            float length = Vector2.Distance(start, end);
            float headDepth = Math.Min(length, Math.Max(6f, BeamWidth * 1.5f));
            float headRadius = Math.Min(headDepth * 0.75f, Math.Max(4f, BeamWidth));
            Vector2 neck = end - direction * headDepth;

            // Draw every layer over the entire beam, so adjoining strokes share a glow.
            drawBeamLayer(start, neck, end, direction, normal, headDepth, headRadius,
                BeamWidth, new Color(0, 100, 220));
            drawBeamLayer(start, neck, end, direction, normal, headDepth, headRadius,
                Math.Max(1f, BeamWidth * 0.65f), new Color(0, 235, 255));
            drawBeamLayer(start, neck, end, direction, normal, headDepth, headRadius,
                Math.Max(1f, BeamWidth * 0.25f), new Color(225, 255, 255));
        }

        void drawBeamLayer(Vector2 start, Vector2 neck, Vector2 end, Vector2 direction,
            Vector2 normal, float depth, float radius, float width, Color color)
        {
            drawBeamStroke(start, neck, width, color);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 shoulder = end - direction * (depth * 0.45f) + normal * (radius * side);
                Vector2 prong = end + direction * (depth * 0.25f) + normal * (radius * side);
                Vector2 tip = end + direction * (depth * 0.45f) + normal * (radius * 0.55f * side);
                drawBeamStroke(neck, shoulder, width, color);
                drawBeamStroke(shoulder, prong, width, color);
                drawBeamStroke(prong, tip, width, color);
            }
        }

        void drawBeamStroke(Vector2 start, Vector2 end, float width, Color color)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length < 0.01f) return;
            float angle = (float)Math.Atan2(delta.Y, delta.X);
            // Slightly overlap the ends so claw joints stay connected at every width.
            spriteBatch.Draw(beamPixel, start, null, color, angle, new Vector2(0.5f, 0.5f),
                new Vector2(width, width), SpriteEffects.None, 0f);
            spriteBatch.Draw(beamPixel, start, null, color, angle, new Vector2(0, 0.5f),
                new Vector2(length, width), SpriteEffects.None, 0f);
            spriteBatch.Draw(beamPixel, end, null, color, angle, new Vector2(0.5f, 0.5f),
                new Vector2(width, width), SpriteEffects.None, 0f);
        }

        void prepareFrameSlices()
        {
            // Nine-slice the supplied frame. The new frame sprite has a
            // uniform 8px border on all four sides, so slice at 8 everywhere
            // to keep the inner outline intact at the seams.
            int[] sourceX = { 0, 8, frame.Width - 8, frame.Width };
            int[] sourceY = { 0, 8, frame.Height - 8, frame.Height };
            int[] targetX = { 0, 8, NATIVE_WIDTH - 8, NATIVE_WIDTH };
            int[] targetY = { 0, 8, NATIVE_HEIGHT - 8, NATIVE_HEIGHT };
            int slice = 0;
            for (int row = 0; row < 3; row++)
                for (int column = 0; column < 3; column++)
                {
                    if (row == 1 && column == 1) continue;
                    frameDestinations[slice] = new Rectangle(targetX[column], targetY[row], targetX[column + 1] - targetX[column], targetY[row + 1] - targetY[row]);
                    frameSources[slice] = new Rectangle(sourceX[column], sourceY[row], sourceX[column + 1] - sourceX[column], sourceY[row + 1] - sourceY[row]);
                    slice++;
                }
        }

        void drawFrame()
        {
            for (int i = 0; i < frameSources.Length; i++)
                spriteBatch.Draw(frame, frameDestinations[i], frameSources[i], Color.White);
        }

        bool tryGetMouseColumn(out int column)
        {
            column = -1;
            if (!rect.Contains(mouseState.X, mouseState.Y)) return false;
            int nativeX = (mouseState.X - rect.X) * NATIVE_WIDTH / rect.Width;
            int nativeY = (mouseState.Y - rect.Y) * NATIVE_HEIGHT / rect.Height;
            if (nativeX < PLAYFIELD_LEFT || nativeX >= PLAYFIELD_RIGHT || nativeY < PLAYFIELD_TOP || nativeY >= BLOCK_FLOOR_Y + BLOCK_SIZE)
                return false;
            column = (nativeX - PLAYFIELD_LEFT) / BLOCK_SIZE;
            return true;
        }

        Texture2D loadScoreDigits()
        {
            // numbers.png contains ten 8x9 cells, ordered 0 through 9.
            Texture2D numbers = loadPng("numbers");
            Color[] pixels = new Color[numbers.Width * numbers.Height];
            numbers.GetData(pixels);
            for (int i = 0; i < pixels.Length; i++)
            {
                // Match the reference's white HUD ink while preserving transparency.
                byte alpha = pixels[i].A;
                pixels[i] = new Color(alpha, alpha, alpha, alpha);
            }
            numbers.SetData(pixels);
            return numbers;
        }


        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            highScores.Flush();
            ufoAtlas.Dispose();
            beamAudio?.Dispose();
            foreach (Texture2D mortar in mortarFrames) mortar.Dispose();
            muzzleFlashSprite.Dispose();
            yellowTankRight.Dispose();
            yellowTankLeft.Dispose();
            pyororight.Dispose();
            pyoroleft.Dispose();
            explosionSprite.Dispose();
            angelSprite.Dispose();
            mainMenuBackground.Dispose();
            mainMenuTitle.Dispose();
            borderCamo.Dispose();
            beamPixel.Dispose();
            background.Dispose();
            frame.Dispose();
            playfieldRasterizer.Dispose();
            _nativeRenderTarget.Dispose();
            spriteBatch.Dispose();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            KeyboardState beamKeys = ReadGameKeyboard();
            if (shotsMode) { UpdateShots(gameTime); }
            if (updateMenus(gameTime, beamKeys))
            {
                bool audioPaused = screen == MenuScreen.Playing || screen == MenuScreen.Pause || (screen == MenuScreen.Options && optionsParent == MenuScreen.Pause);
                if (!audioPaused) beamAudio?.StopBeamVoices();
                beamAudio?.Update(false, false, false, audioPaused, gameTime.ElapsedGameTime.TotalSeconds);
                music?.Update(gameTime.ElapsedGameTime.TotalSeconds);
                // Gameplay music keeps playing even while paused; the track
                // itself swaps on menu transitions (in GameMenus).
                previousShotDown = beamKeys.IsKeyDown(Keys.X);
                previousBeamKeys = beamKeys;
                base.Update(gameTime);
                return;
            }
            UpdateUfo(gameTime, beamKeys);
            previousBeamKeys = beamKeys;
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        void drawGameplay(GameTime gameTime) => DrawUfoGameplay();

        protected override void Draw(GameTime gameTime)
        {
            // Upload before either render pass, and unbind the previous frame's target.
            GraphicsDevice.Textures[0] = null;
            updateCamo(gameTime.ElapsedGameTime.TotalSeconds);
            GraphicsDevice.SetRenderTarget(_nativeRenderTarget);
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, NATIVE_WIDTH, NATIVE_HEIGHT);

            // DRAWING INSIDE RENDERTARGET
            // Let the animated outer camo show through outside the frame.
            GraphicsDevice.Clear(Color.Transparent);
            if (usesTitleScene())
                drawTitleScene();
            else
                drawGameplay(gameTime);

            // SET RENDERTARGET TO NOTHING
            GraphicsDevice.SetRenderTarget(null);
            // Dynamic animated border fills the whole window behind the game.
            drawDynamicBorder();

            // DRAW _nativeRenderTarget TO SCREEN at the integer scale
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            
            spriteBatch.Draw(_nativeRenderTarget, rect, Color.White);
            float fade = transitionOpacity();
            if (fade > 0)
                spriteBatch.Draw(beamPixel, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.Black * fade);
            //spriteBatch.DrawString(arial, string.Format("X: {0}\nY: {1}\nSPEED: {2:0.00}", x, y, speed), new Vector2(40, 50), Color.White);
            spriteBatch.End();

            // Press Tab to save the current framebuffer as a PNG (edge-triggered, so
            // holding the key only captures once; saving happens off-thread to
            // avoid hitching the game loop).
            KeyboardState tabKeys = Keyboard.GetState();
            if (tabKeys.IsKeyDown(Keys.Tab) && previousTabKeys.IsKeyUp(Keys.Tab))
            {
                SaveScreenshot();
            }
            previousTabKeys = tabKeys;

            base.Draw(gameTime);
        }

        void NextShotStage() { shotStage++; shotTimer = 0; }

        KeyboardState previousTabKeys;
        int screenshotPending;

        // Capture the current backbuffer and write it to a PNG file. The GPU
        // readback happens here (main thread), then the file write is deferred
        // to a worker thread so the game loop doesn't stall on disk I/O.
        void SaveScreenshot()
        {
            if (GameAssets.IsWeb) return; // no writable file system in the browser
            try
            {
                int w = GraphicsDevice.PresentationParameters.BackBufferWidth;
                int h = GraphicsDevice.PresentationParameters.BackBufferHeight;
                Color[] pixels = new Color[w * h];
                GraphicsDevice.GetBackBufferData(pixels);

                int count = screenshotCount++;
                string dir = Path.Combine(AppContext.BaseDirectory, "screenshots");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, "screenshot_" + count + ".png");

                // Encode the PNG on the main thread (SaveAsPng touches GPU
                // state), then hand the finished bytes to a worker to write.
                byte[] pngData;
                using (Texture2D tex = new Texture2D(GraphicsDevice, w, h))
                {
                    tex.SetData(pixels);
                    using (var ms = new System.IO.MemoryStream())
                    {
                        tex.SaveAsPng(ms, w, h);
                        pngData = ms.ToArray();
                    }
                }

                var thread = new System.Threading.Thread(() =>
                {
                    try
                    {
                        System.IO.File.WriteAllBytes(file, pngData);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Screenshot write failed: " + ex.Message);
                    }
                });
                thread.IsBackground = true;
                thread.Start();
            }
            catch (Exception ex)
            {
                // Don't crash the game if a screenshot fails.
                System.Diagnostics.Debug.WriteLine("Screenshot failed: " + ex.Message);
            }
        }

        // Web host only: called after the browser unlocks audio on the first
        // user gesture, so the active music track can actually start playing.
        public void UnlockAudio()
        {
            music?.UnlockRetry();
        }

        static Color[] makeCamoPalette()
        {
            Color[] palette = new Color[16];
            for (int hue = 0; hue < 2; hue++)
                for (int level = 0; level < 8; level++)
                {
                    double shade = (hue == 1 ? 0.55 : 0.10) + level * 0.45 / 7.0;
                    palette[hue * 8 + level] = new Color(
                        (byte)((hue == 1 ? 24 : 12) * shade),
                        (byte)((hue == 1 ? 74 : 28) * shade),
                        (byte)((hue == 1 ? 98 : 53) * shade));
                }
            return palette;
        }

        void updateCamo(double elapsedSeconds)
        {
            // No frame limit or loop reset: phase advances continuously for the entire session.
            borderTime += elapsedSeconds;
            for (int x = 0; x < CAMO_WIDTH; x++)
            {
                double px = x / (double)(CAMO_WIDTH - 1);
                camoXWave1[x] = Math.Sin(px * 12.0 + borderTime * 1.3);
                camoXWave2[x] = Math.Sin(px * 7.0 + borderTime * 1.1);
            }
            for (int y = 0; y < CAMO_HEIGHT; y++)
            {
                double py = y / (double)(CAMO_HEIGHT - 1);
                double wave1 = Math.Sin(py * 15.0 - borderTime * 1.1);
                double wave2 = 0.6 * Math.Sin(py * 9.0 - borderTime * 0.9);
                for (int x = 0; x < CAMO_WIDTH; x++)
                {
                    double plasma = Math.Clamp(camoXWave1[x] * wave1 + camoXWave2[x] * wave2, -1.0, 1.0);
                    int hue = plasma > 0 ? 8 : 0;
                    int shade = (int)Math.Round((plasma > 0 ? plasma : plasma + 1.0) * 7.0);
                    borderPixels[y * CAMO_WIDTH + x] = camoPalette[hue + shade];
                }
            }
            borderCamo.SetData(borderPixels);
        }

        void drawDynamicBorder()
        {
            int width = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int height = GraphicsDevice.PresentationParameters.BackBufferHeight;
            // Reset target-dependent state and overwrite every backbuffer pixel each frame.
            GraphicsDevice.Viewport = new Viewport(0, 0, width, height);
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, width, height);
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin(blendState: BlendState.Opaque, samplerState: SamplerState.PointClamp,
                rasterizerState: RasterizerState.CullNone);
            spriteBatch.Draw(borderCamo, new Rectangle(0, 0, width, height), Color.White);
            spriteBatch.End();
        }

        // A short-lived "+points" indicator shown where a bean was caught.
        class ScorePopup
        {
            public float x, y;
            public int points;
            public float timer; // remaining lifetime (frames)

            public ScorePopup(float x, float y, int points)
            {
                this.x = x;
                this.y = y;
                this.points = points;
                // Longer display time for bigger scores, mirroring the pico-8 version.
                if (points >= 1000) timer = 108;
                else if (points >= 300) timer = 84;
                else if (points >= 100) timer = 60;
                else if (points >= 50) timer = 42;
                else timer = 30;
            }
        }

        // A short-lived explosion that plays the 3-frame 16x16 animation at a
        // native position. The explosion.png spritesheet holds the 3 frames
        // side-by-side (48x16), mirroring the pico-8 spritesheet burst effect.
        class Explosion
        {
            public float x, y;      // native (288x216) centre position
            public int timer;           // elapsed frames
            public int frameDuration;   // frames per sprite frame

            public Explosion(float x, float y)
            {
                this.x = x;
                this.y = y;
                this.timer = 0;
                this.frameDuration = 6; // 3 frames * 6 = 18 total frames (0.3s at 60fps)
            }
        }

        // An angel that descends to a missing block column and rebuilds the
        // block when it lands (mirrors the pico-8 angel effect).
        class Angel
        {
            public int column;
            public float y;
            public float speed;
            public float acceleration;
            public float timer;         // used for 2-frame animation
            public bool blockPlaced;    // whether the block has been restored

            // Mirrors pico-8: starts above screen with spd=5.4, acc=0x0.218.
            // The pico-8 floor is at y=100, but this game's floor is at
            // BLOCK_FLOOR_Y=144, so the initial speed is raised to 6.5 so the
            // angel actually reaches the bottom row.
            public Angel(int column)
            {
                this.column = column;
                this.y = -8;
                this.speed = 6.5f;
                this.acceleration = 0.13f; // 0x0.218
                this.timer = 0;
                this.blockPlaced = false;
            }
        }
    }
}
