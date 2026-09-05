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
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        RenderTarget2D _nativeRenderTarget;
        RasterizerState playfieldRasterizer;

        const int NATIVE_WIDTH = 288;
        const int NATIVE_HEIGHT = 162;
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
            Content.RootDirectory = "Content";
            Window.AllowUserResizing = true;
            // Allow starting with the debug menu enabled: run with "--debug".
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                if (arg == "--debug" || arg == "-d")
                    showDebugMenu = true;
            }
        }

        // Compute the largest integer scale factor that fits the current window,
        // preserving the 288x162 aspect ratio. Also center the render target
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
            // keeping the 16:9 aspect ratio.
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
        SpriteFont smallfont;
        // Pixel-perfect 8x8 bitmap font atlas (Assets/font8x8_atlas.png):
        // 16 columns of ASCII 32..126, binary alpha, zero anti-aliasing.
        Texture2D fontAtlas;
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
        private Texture2D block;
        // 4-variant 8x8 brick-face atlas (Assets/block_atlas.png) for floor blocks.
        private Texture2D blockAtlas;
        private Texture2D collisionblock;
        private Texture2D beamPixel;
        private int beamWidth = 3;
        private KeyboardState previousBeamKeys;
        // Start with the debug menu open when "--debug" is passed on the
        // command line (or START_DEBUG_MENU is set to true below).
        private const bool START_DEBUG_MENU = false;
        private bool showDebugMenu = START_DEBUG_MENU;
        private KeyboardState previousDebugKeys;

        // Dynamic animated border drawn behind the scaled game. A slowly
        // drifting diagonal colour wash whose speed follows the game speed and
        // whose hue shifts red while Pyoro is dead.
        // Camo knobs mirror camo_full.py; window scaling stays nearest-neighbor.
        const int CAMO_WIDTH = 96;
        const int CAMO_HEIGHT = 54;
        const int CAMO_FRAMES = 140;
        const double CAMO_LOOP_SECONDS = 9.3;
        private Texture2D borderCamo;
        private readonly Color[] borderPixels = new Color[CAMO_WIDTH * CAMO_HEIGHT];
        private int borderFrame = -1;
        private double borderTime;

        // Visual thickness in native pixels, measured perpendicular to the beam.
        public int BeamWidth
        {
            get { return beamWidth; }
            set { beamWidth = Math.Clamp(value, 1, 20); }
        }
        private Texture2D select;
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
        private Texture2D scoreLabel;
        private Texture2D highScoreLabel;

        private Texture2D gameoversprite;
        private Texture2D scoresSprite;

        //Timer t1sec = new Timer(1000);
        int blockamount = BLOCK_COUNT;
        
        float speed = 1.0f;
        MouseState mouseState;
        int gameSize = 4;
        bool[] blocks;
        float tongueX, tongueY;
        int facingright = 1;
        int tongueoffsetX, tongueoffsetY;
        int rightoffset = 13;
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
        bool readytounpause = false;
        int pyorosquat = 0;

        
        int max_time = 0xB4;
        int tmpmax = 0x0;
        int randnum = 0x0;
        int time_until_new_bean = 0x0;
        int score = 0;
        int highScore = 10000;
        
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
        Random rnd = new Random(DateTime.Now.Millisecond);
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

        // Play a 3-frame 16x16 explosion animation at a native (288x162) position.
        // Mirrors the pico-8 smoke/burst used when a block or bean disappears.
        void spawnExplosion(float x, float y)
        {
            // Only start one explosion per position at a time.
            if (!explosions.Exists(e => e.x == x && e.y == y))
                explosions.Add(new Explosion(x, y));
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
                // Explosion where the bean disappears.
                spawnExplosion(bean_x[j], bean_y[j]);
            }
        }

        // Render active explosions centred on their position.
        void drawExplosions(SpriteBatch batch)
        {
            foreach (Explosion e in explosions)
            {
                int frame = (int)(e.timer / e.frameDuration);
                if (frame > 2) frame = 2;
                // explosion.png is 48x16 = 3 frames of 16x16 in a row.
                Rectangle src = new Rectangle(frame * 16, 0, 16, 16);
                // Centre the 16x16 sprite on the given position.
                float dx = e.x - 8;
                float dy = e.y - 8;
                batch.Draw(explosionSprite, new Vector2(dx, dy), src, Color.White);
            }
        }

        // Spawn an angel that descends to the given block column and rebuilds
        // it on landing. Existing angels never suppress a new explicit request.
        void spawnAngel(int column)
        {
            if (column < 0 || column >= blockamount) return;
            angels.Add(new Angel(column));
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

        // Render angels: 2-frame 16x16 animation (angel.png is 32x16). When the
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
        // at the given native (288x162) position, mirroring the pico-8 version.
        void addScore(float x, float y, int pts)
        {
            score += pts;
            highScore = Math.Max(highScore, score);
            scorePopups.Add(new ScorePopup(x, y, pts));
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
            // TODO: Add your initialization logic here
            _nativeRenderTarget = new RenderTarget2D(GraphicsDevice, NATIVE_WIDTH, NATIVE_HEIGHT);
            Window.ClientSizeChanged += Window_ClientSizeChanged;

            // Start the 16:9 window at 4x scale (1152x648).
            graphics.PreferredBackBufferWidth = NATIVE_WIDTH * gameSize;
            graphics.PreferredBackBufferHeight = NATIVE_HEIGHT * gameSize;
            graphics.ApplyChanges();
            computeIntegerScale();
            x = PLAYER_START_X;
            y = PLAYER_START_Y;
            tongueoffsetX = 1; // mirrored barrel tip
            tongueoffsetY = -1; // barrel tip above the player collision box
            tongueX = x + tongueoffsetX;
            tongueY = y + tongueoffsetY;
            tonguecount = 0;
            graphics.SynchronizeWithVerticalRetrace = false; //Vsync
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
            playfieldRasterizer = new RasterizerState { ScissorTestEnable = true, CullMode = CullMode.None };
            arial = Content.Load<SpriteFont>("font");
            smallfont = Content.Load<SpriteFont>("smallfont");
            fontAtlas = loadPng("font8x8_atlas");
            loadPlayerTank();
            pyoro = pyororight;
            loadBackdrop();
            scoreLabel = loadPng("score");
            highScoreLabel = loadPng("highscore");
            loadMortarFrames();
            temp_bean_sprite = bean_centre;
            for (int i = 0; i < max_amount_of_beans; i++)
            {
                current_bean_sprite[i] = bean_centre;
            }
            frame = loadPng("framewide");
            block = Content.Load<Texture2D>("block");
            blockAtlas = loadPng("block_atlas");
            collisionblock = Content.Load<Texture2D>("collisionblock");
            explosionSprite = Content.Load<Texture2D>("explosion");
            angelSprite = Content.Load<Texture2D>("angel");
            beamPixel = new Texture2D(GraphicsDevice, 1, 1);
            beamPixel.SetData(new[] { Color.White });
            borderCamo = new Texture2D(GraphicsDevice, CAMO_WIDTH, CAMO_HEIGHT);
            select = Content.Load<Texture2D>("select");
            gameoversprite = Content.Load<Texture2D>("gameoversprite");
            scoresSprite = Content.Load<Texture2D>("scores");
            // The widescreen playfield uses BLOCK_COUNT eight-pixel floor columns.
            // TODO: use this.Content to load your game content here
        }

        Texture2D loadPng(string name)
        {
            using (Stream stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Assets", name + ".png")))
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
            if (pyorodead || tonguecount <= 0) return;
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

        // Draw an on-screen debug overlay showing live game-state values. Held
        // open with F1; content updates every frame.
        void drawDebugMenu(SpriteBatch batch)
        {
            int panelX = 8, panelY = 8, panelW = NATIVE_WIDTH - 16;
            // Semi-transparent backdrop.
            batch.Draw(beamPixel, new Rectangle(panelX, panelY, panelW, 118),
                new Color(0, 0, 0, 180));

            string[] lines = new string[]
            {
                "=== DEBUG (F1 to close) ===",
                $"BeamWidth (tractor):  {BeamWidth}   ([ / ] adjust)",
                $"Score:                {score}",
                $"HighScore:            {highScore}",
                $"Paused:               {paused}",
                $"GameOver:             {gameover}",
                $"Pyoro x: {x:0.0}   y: {y:0.0}",
                $"Active beans:         {countActiveBeans()}",
                $"Blocks present:       {countActiveBlocks()}"
            };

            int ty = panelY + 6;
            foreach (string line in lines)
            {
                DrawStringBitmap(batch, line, new Vector2(panelX + 6, ty), Color.White);
                ty += 11;
            }
        }

        int countActiveBeans()
        {
            int n = 0;
            for (int i = 0; i < max_amount_of_beans; i++)
                if (bean_active[i]) n++;
            return n;
        }

        int countActiveBlocks()
        {
            int n = 0;
            for (int i = 0; i < blockamount; i++)
                if (blocks[i]) n++;
            return n;
        }

        void drawFrame()
        {
            // Nine-slice the supplied frame. The new frame sprite has a
            // uniform 8px border on all four sides, so slice at 8 everywhere
            // to keep the inner outline intact at the seams.
            int[] sourceX = { 0, 8, frame.Width - 8, frame.Width };
            int[] sourceY = { 0, 8, frame.Height - 8, frame.Height };
            int[] targetX = { 0, 8, NATIVE_WIDTH - 8, NATIVE_WIDTH };
            int[] targetY = { 0, 8, NATIVE_HEIGHT - 8, NATIVE_HEIGHT };
            for (int row = 0; row < 3; row++)
                for (int column = 0; column < 3; column++)
                {
                    if (row == 1 && column == 1) continue;
                    spriteBatch.Draw(frame,
                        new Rectangle(targetX[column], targetY[row], targetX[column + 1] - targetX[column], targetY[row + 1] - targetY[row]),
                        new Rectangle(sourceX[column], sourceY[row], sourceX[column + 1] - sourceX[column], sourceY[row + 1] - sourceY[row]), Color.White);
                }
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
            foreach (Texture2D mortar in mortarFrames) mortar.Dispose();
            pyororight.Dispose();
            pyoroleft.Dispose();
            borderCamo.Dispose();
            beamPixel.Dispose();
            background.Dispose();
            frame.Dispose();
            scoreLabel.Dispose();
            highScoreLabel.Dispose();
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
            KeyboardState beamKeys = Keyboard.GetState();
            if (beamKeys.IsKeyDown(Keys.OemOpenBrackets) && previousBeamKeys.IsKeyUp(Keys.OemOpenBrackets)) BeamWidth--;
            if (beamKeys.IsKeyDown(Keys.OemCloseBrackets) && previousBeamKeys.IsKeyUp(Keys.OemCloseBrackets)) BeamWidth++;
            previousBeamKeys = beamKeys;

            // Toggle the debug menu with F1.
            if (beamKeys.IsKeyDown(Keys.F1) && previousDebugKeys.IsKeyUp(Keys.F1))
                showDebugMenu = !showDebugMenu;
            previousDebugKeys = beamKeys;
            //rand_number = (109 * rand_number) + 1021; // rand_number = (0x6D * rand_number) + 0x3FD;
            /*rand_number = ((0x6D * rand_number) + 0x3FD);
            rand_number = ((rand_number & 0x0000FFFF));
            max_tmp = max_time << 14;
            max_tmp = max_tmp >> 16;
            rand_number = max_tmp * rand_number;
            rand_number = rand_number << 16;
            rand_number = rand_number >> 16;
            tmp_number = max_tmp * rand_number;
            tmp_number = ((tmp_number & 0x00FF0000)>>16);*/
            //tmp_number = ((rand_number & 0x000000FF));


            /*int max_speed = 0x40;
            int tmpspeed = 0x0;
            int randnum2 = 0x0;
            int beanspeed = 0;

            tmpspeed = max_speed << 14;
            tmpspeed = tmpspeed >> 16;
            tmpspeed = tmpspeed << 16;
            tmpspeed = tmpspeed >> 16;
            randnum = (0x6D * randnum) + 0x3FD;
            randnum = (randnum & 0x0000FFFF);
            tmpspeed = tmpspeed * randnum;
            tmpspeed = tmpspeed >> 16;
            tmpspeed = tmpspeed << 16;
            tmpspeed = tmpspeed >> 16;
            tmpspeed += 0x40;
            beanspeed = tmpspeed;
            /*tmpspeed = max_speed - tmpspeed;
            tmpspeed = tmpspeed << 8;
            beanspeed = tmpspeed / bigspeed;*/


            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }
            // TODO: Add your update logic here
            if (Keyboard.GetState().IsKeyDown(Keys.Enter))
            {
                paused = true;
                //bean_active[2] = false;
                //readytounpause = false;
            }
            if (!Keyboard.GetState().IsKeyDown(Keys.Enter))
            {
                //paused = false;
                readytounpause = false;
            }
            /*if (!Keyboard.GetState().IsKeyDown(Keys.Enter))
            {
                readytounpause = true;
            }*/
            if (paused)
            {
                if (!Keyboard.GetState().IsKeyDown(Keys.Enter))
                {
                    //paused = false;
                    readytounpause = true;
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                {
                    if (readytounpause)
                    {
                        paused = false;
                        //readytounpause = false;
                    }
                    //readytounpause = false;
                }
                /*if (!Keyboard.GetState().IsKeyDown(Keys.Enter))
                {
                    readytounpause = true;
                }
                if (readytounpause)
                {
                    if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                    {
                        paused = false;
                        readytounpause = false;
                    }
                }*/
            }
            if (paused == false)
            {

                
                if (time_until_new_bean <= 0)
                {
                    /// TIME UNTIL NEW BEAN ////
                    tmpmax = max_time << 14;
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
                    ////////////////////////////////////

                    /*tmpspeed = max_speed << 14;
                    tmpspeed = tmpspeed >> 16;*/
                    //////// SPEED OF NEW BEAN ///////
                    tmpspeed = 0x40 << 16;
                    tmpspeed = tmpspeed >> 16;
                    randnum2 = (0x6D * randnum2) + 0x3FD;
                    randnum2 = (randnum2 & 0x0000FFFF);
                    tmpspeed = tmpspeed * randnum2;
                    tmpspeed = tmpspeed >> 16;
                    tmpspeed = tmpspeed << 16;
                    tmpspeed = tmpspeed >> 16;
                    tmpspeed += 0x40;
                    beanspeed = tmpspeed;
                    //beanspeed = (beanspeed & 0x000000FF);
                    //time_max_rand(max_time);
                    ///// BEAN X 
                    beanxrandom = (PLAYFIELD_RIGHT - PLAYFIELD_LEFT - 16) << 16;
                    beanxrandom = beanxrandom >> 16;
                    randnum3 = (0x6D * randnum3) + 0x3FD;
                    randnum3 = (randnum3 & 0x0000FFFF);
                    beanxrandom = beanxrandom * randnum3;
                    beanxrandom = beanxrandom >> 16;
                    beanxrandom = beanxrandom << 16;
                    beanxrandom = beanxrandom >> 16;
                    beanxrandom += PLAYFIELD_LEFT + 8;
                    currentbeanx = beanxrandom;
                    ///// BEAN TYPE 
                    beantype = 0x09 << 16;
                    beantype = beantype >> 16;
                    randnum4 = (0x6D * randnum4) + 0x3FD;
                    randnum4 = (randnum4 & 0x0000FFFF);
                    beantype = beantype * randnum4;
                    beantype = beantype >> 16;
                    beantype = beantype << 16;
                    beantype = beantype >> 19;
                    currentbeantype = beantype;

                    if (score >= risingscore)// && spawnedrainbowforever == 0)
                    {
                        currentbeantype = 2;
                        beanspeed = 0x40;
                        ///spawnedrainbowforever = 1;
                        if (risingscore >= 5000 && risingscore < 7000)
                        {
                            risingscore = 7000;
                        }
                        else if (risingscore >= 7000 && risingscore < 9000)
                        {
                            risingscore = 9000;
                        }
                        else if (risingscore >= 9000 && risingscore < 10000)
                        {
                            risingscore = 10000;
                        }
                        else if (risingscore >= 10000)
                        {
                            risingscore += 1000;
                        }
                    }


                    create_new_bean = false;
                    for (int i = 0; i < max_amount_of_beans; i++)
                    {
                        if (bean_active[i] == false && create_new_bean == false)
                        {
                            bean_active[i] = true;
                            create_new_bean = true;
                            new_bean_number_debug = i;
                            bean_speed[i] = beanspeed;
                            bean_y[i] = -20;
                            bean_x[i] = currentbeanx; // r.Next(PLAYFIELD_LEFT + 8, PLAYFIELD_RIGHT - 8);
                            bean_type[i] = currentbeantype;                        //bean_type[i] = currentbeantype;
                        }
                    }
                }
                if (time_until_new_bean > 0)
                {
                    time_until_new_bean--;
                }
                int tmpy = 0;
                float tmpfloat = 0;
                int tmpbigspeed = 0;
                for (int i = 0; i < max_amount_of_beans; i++)
                {
                    if (bean_active[i] == true)
                    {
                        //bean_y[i] = (bean_y[i] + ((bean_speed[i] * bigspeed)/256)/128);
                        //bean_y[i] = (bean_y[i] + ((bean_speed[i] * (bigspeed/256))));
                        if (bean_type[i] == 2)
                        {
                            tmpbigspeed = ((bigspeed + 0x200) / 3);
                            tmpy = (((bean_speed[i] * tmpbigspeed) >> 8));// >> 16);
                                                                          //tmpy = (tmpy & 0x000000FF);
                            tmpfloat = tmpy / 256f;
                            bean_y[i] = (bean_y[i] + tmpfloat);
                        }
                        else
                        {
                            tmpy = (((bean_speed[i] * bigspeed) >> 8));// >> 16);
                                                                       //tmpy = (tmpy & 0x000000FF);
                            tmpfloat = tmpy / 256f;
                            bean_y[i] = (bean_y[i] + tmpfloat);
                        }

                    }
                    int blocktocheckagainstbean = (int)System.Math.Ceiling((bean_x[i] - PLAYFIELD_LEFT) / 8);
                    if (bean_y[i] > PLAYER_START_Y && blocks[blocktocheckagainstbean] == true && bean_active[i])
                    {
                        bean_active[i] = false;
                        blocks[blocktocheckagainstbean] = false;
                        bean_y[i] = -20;
                        // Explosion where the block disappears.
                        spawnExplosion(PLAYFIELD_LEFT + blocktocheckagainstbean * 8, BLOCK_FLOOR_Y);
                    }
                    if (bean_y[i] > NATIVE_HEIGHT + 20)
                    {
                        bean_active[i] = false;
                        bean_y[i] = -20;
                    }
                }
                //bean_animation();















                if(pyorodead==false)
                {
                    speedloop();
                }

                random_number_main();

                bean_animation();
                //float tempspeed = bigspeed;
                speed = (float)bigspeed / 256;
                if (Keyboard.GetState().IsKeyDown(Keys.Left) && !pyorodead)// && x > PLAYFIELD_LEFT)
                {
                    if (spaceheld == 0)
                    {
                        pyorosquat++;
                        if (pyorosquat > 5)
                        {
                            pyorosquat = 0;
                        }
                        if (pyorosquat < 2)
                        {
                            pyoro = pyorosquatleft;
                        }
                        if (pyorosquat >= 2)
                        {
                            pyoro = pyoroleft;
                        }
                        float tmp = x - speed;
                        blocktocheck = (int)System.Math.Ceiling((x - speed - PLAYFIELD_LEFT) / 8);
                        if (blocktocheck < 0)
                        {
                            blocktocheck = 0;
                        }
                        if (blocktocheck >= BLOCK_COUNT)
                        {
                            blocktocheck = BLOCK_COUNT - 1;
                        }
                        if (blocks[blocktocheck] == false)
                        {
                            x = ((blocktocheck) * 8) + PLAYFIELD_LEFT;
                        }
                        else
                        {
                            x = tmp;
                        }
                        //pyoro = pyoroleft;
                        facingright = -1;
                    }
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Right) && !pyorodead)// && x < 184)
                {
                    if (spaceheld == 0)
                    {

                        /*float checkxhex = x + speed; // pyoro_x+current_movement_speed = current_x_in_memory
                        int checkx = (int)System.Math.Floor(checkxhex); // current_x_in_memory>>8
                        int subtract = checkx - PLAYFIELD_LEFT - 2; // ((current_x_in_memory>>8)-0x28)
                        blocktocheck = (subtract >> 3) + 1;*/
                        /*if (blocks[blocktocheck] == true)
                        {
                            x = checkxhex;
                            pyoro = pyororight;
                            facingright = 1;
                        }
                        else if (blocks[blocktocheck] == false)
                        {
                            x = blocktocheck*8;
                            pyoro = pyororight;
                            facingright = 1;
                        }*/

                        pyorosquat++;
                        if (pyorosquat >= 5)
                        {
                            pyorosquat = 0;
                        }
                        if(pyorosquat < 2)
                        {
                            pyoro = pyorosquatright;
                        }
                        if (pyorosquat >= 2)
                        {
                            pyoro = pyororight;
                        }
                        float tmp = x + speed;
                        blocktocheck = (int)System.Math.Ceiling((x + speed - PLAYFIELD_LEFT + 1) / 8);
                        if (blocktocheck < 0)
                        {
                            blocktocheck = 0;
                        }
                        if (blocktocheck >= BLOCK_COUNT)
                        {
                            blocktocheck = BLOCK_COUNT - 1;
                        }
                        if (blocks[blocktocheck] == false)
                        {
                            x = ((blocktocheck - 1) * 8) + PLAYFIELD_LEFT - 1;
                        }
                        else
                        {
                            x = tmp;
                        }
                        
                        facingright = 1;
                        /*blocktocheck = (int)System.Math.Floor(((tmp - 36)/8)+1);
                        if (blocks[blocktocheck] == true)
                        {
                            x = tmp;
                            pyoro = pyororight;
                            facingright = 1;
                            rightmove = 1;
                            leftmove = 1;
                        }
                        else if (blocks[blocktocheck] == false)
                        {
                            x = ((blocktocheck) * 10);
                            pyoro = pyororight;
                            facingright = 1;
                            rightmove = 0;
                            leftmove = 1;
                        }*/

                    }
                }
                if (!Keyboard.GetState().IsKeyDown(Keys.X) && !pyorodead) // !Keyboard.GetState().IsKeyDown(Keys.Left) && !Keyboard.GetState().IsKeyDown(Keys.Right) && 
                {

                    //tonguecount = 0;
                    if (tonguecount <= 0)
                    {
                        recall = false;
                        tonguecollide = false;
                        caughtbean = 0;
                        spaceheld = 0;
                        tongueX = x;
                        tongueY = y + tongueoffsetY;
                    }
                    if (tonguecount > 0)
                    {
                        recall = true;
                        spaceheld = 1;
                    }
                    if (recall == true)
                    {
                        if (tonguecount > 0)
                        {
                            tonguecount -= 8 * speed;
                        }
                        if (tonguecount <= 0)
                        {
                            recall = false;
                            tonguecollide = false;
                            caughtbean = 0;
                            spaceheld = 0;
                            tongueX = x;
                            tongueY = y + tongueoffsetY;
                            tonguecount = 0;
                        }
                    }

                    //tongueX = x + tongueoffsetX;// + (facingright * rightoffset);
                    tongueY = y + tongueoffsetY;
                    if (recall == false)
                    {
                        switch (facingright)
                        {
                            case -1:
                                if (pyorosquat < 2)
                                {
                                    pyoro = pyorosquatleft;
                                }
                                if (pyorosquat >= 2)
                                {
                                    pyoro = pyoroleft;
                                }
                                break;
                            case 1:
                                if (pyorosquat < 2)
                                {
                                    pyoro = pyorosquatright;
                                }
                                if (pyorosquat >= 2)
                                {
                                    pyoro = pyororight;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
                if (!Keyboard.GetState().IsKeyDown(Keys.X) && !pyorodead) // !Keyboard.GetState().IsKeyDown(Keys.Left) && !Keyboard.GetState().IsKeyDown(Keys.Right) && 
                {
                    if (spaceheld == 0)
                    {
                        if (tonguecount <= 0)
                        {
                            if (recall == true)
                            {
                                recall = false;
                                spaceheld = 0;
                                tongueX = x;
                                tongueY = y + tongueoffsetY;
                                caughtbean = 0;
                                tonguecount = 0;
                            }
                        }
                    }
                    
                }
                if (Keyboard.GetState().IsKeyDown(Keys.X) && !pyorodead) // !Keyboard.GetState().IsKeyDown(Keys.Left) && !Keyboard.GetState().IsKeyDown(Keys.Right) && 
                {

                    if (recall == false)
                    {
                        spaceheld = 1;
                        if ((float)System.Math.Round((decimal)tongueX + ((decimal)(tonguecount+(2 * speed)) * facingright)) < PLAYFIELD_RIGHT + 2 && (float)System.Math.Round((decimal)tongueX + ((decimal)(tonguecount + (2 * speed)) * facingright)) > PLAYFIELD_LEFT - 3)
                        {
                            if (tongueY - tonguecount > PLAYFIELD_TOP)
                            {
                                tonguecount += 2 * speed; 
                                for (int i = 0; i < max_amount_of_beans; i++)
                                {
                                    if (bean_active[i] == true)
                                    {
                                        if ((float)System.Math.Round((decimal)tongueX + ((decimal)(tonguecount+(2*speed)) * facingright)) >= bean_x[i] )
                                        {
                                            if ((float)System.Math.Round((decimal)tongueX + ((decimal)(tonguecount + (2 * speed)) * facingright)) <= bean_x[i] + 16)
                                            {
                                                if ((tongueY - (tonguecount + (2 * speed))) >= bean_y[i] )
                                                {
                                                    if ((tongueY - (tonguecount + (2 * speed))) <= bean_y[i] + 16)
                                                    {
                                                        bean_active[i] = false;
                                                        tonguecollide = true;
                                                        recall = true;
                                                        caughtbean = i;
                                                        if(bean_y[i]>=35)
                                                        {
                                                            if (bean_y[i] >= 55)
                                                            {
                                                                if (bean_y[i] >= 87)
                                                                {
                                                                    if (bean_y[i] >= 115)
                                                                    {
                                                                            addScore(bean_x[i], bean_y[i], 10);
                                                                    }
                                                                    else
                                                                    {
                                                                        addScore(bean_x[i], bean_y[i], 50);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    addScore(bean_x[i], bean_y[i], 100);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                addScore(bean_x[i], bean_y[i], 300);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            addScore(bean_x[i], bean_y[i], 1000);
                                                        }
                                                        if (bean_type[i] == 1)
                                                        {
                                                            block_recovery();
                                                            /*int blocks_to_recover = 20;
                                                            for (int j = 0; j < blocks_to_recover; j++)
                                                            {
                                                                block_recovery();
                                                            }*/
                                                        }
                                                        if (bean_type[i] == 2)
                                                        {
                                                            // Queue the other active beans to disappear one by one,
                                                            // lowest first (highest y first).
                                                            bool wasClearing = rainbowClearQueue.Count > 0;
                                                            for (int j = 0; j < max_amount_of_beans; j++)
                                                            {
                                                                if (bean_active[j] && !rainbowClearQueue.Contains(j))
                                                                    rainbowClearQueue.Add(j);
                                                            }
                                                            // Sort descending by y so the lowest bean clears first.
                                                            rainbowClearQueue.Sort((a, b) => bean_y[b].CompareTo(bean_y[a]));
                                                            if (!wasClearing) rainbowClearTimer = 0;
                                                            for (int j = 0; j < rainbowbeantotal; j++)
                                                            {
                                                                requestBlockRecovery(true);
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //tonguecollide = false;
                                                    }
                                                }
                                                else
                                                {
                                                    //tonguecollide = false;
                                                }
                                            }
                                            else
                                            {
                                                //tonguecollide = false;
                                            }
                                        }
                                        else
                                        {
                                            //tonguecollide = false;
                                        }
                                    }
                                }
                                //if ((float)System.Math.Round((decimal)tongueX + ((decimal)tonguecount * facingright)) >= leftx)
                                //if ((float)System.Math.Round((decimal)tongueX + ((decimal)tonguecount * facingright)) <= rightx)
                                /*
                                if ((float)System.Math.Round((decimal)tongueX + ((decimal)tonguecount * facingright)) >= ((float)System.Math.Floor(((decimal)mouseState.X / gameSize) / 8) * 8) - 1)
                                {
                                    if ((float)System.Math.Round((decimal)tongueX + ((decimal)tonguecount * facingright)) <= ((float)System.Math.Floor(((decimal)mouseState.X / gameSize) / 8) * 8) + 7)
                                    {

                                        if ((tongueY - tonguecount) >= ((float)System.Math.Floor(((decimal)mouseState.Y / gameSize) / 8) * 8) - 1)
                                         {
                                             if ((tongueY - tonguecount) <= ((float)System.Math.Floor(((decimal)mouseState.Y / gameSize) / 8) * 8) + 7)
                                             {
                                                 tonguecollide = true;
                                                 recall = true;
                                             }
                                             else
                                             {
                                                 tonguecollide = false;
                                             }
                                             //if (tongueY + tonguecount < ((float)System.Math.Floor(((decimal)mouseState.Y / gameSize) / 8) * 8) + 8)
                                             //{
                                             //    tonguecollide = true;
                                             //}
                                         }
                                         else
                                         {
                                             tonguecollide = false;
                                         }
                                    }
                                    else
                                    {
                                        tonguecollide = false;
                                    }

                                }                                
                                else
                                {
                                    tonguecollide = false;
                                }*/
                            }
                            else
                            {
                                recall = true;
                            }
                        }
                        else
                        {
                            recall = true;
                            /*if(tonguecount>0)
                            {
                                tonguecount -= 2;
                            }*/
                        }
                    }
                    else
                    {
                        if (tonguecount > 0)
                        {
                            tonguecount -= 8 * speed;
                        }
                        else if (tonguecount <= 0)
                        {
                            //spaceheld = 0;
                            //recall = false;
                            spaceheld = 0;
                            tongueX = x;
                            tongueY = y + tongueoffsetY;
                            tonguecount = 0;                
                            switch (facingright)
                            {
                                case -1:
                                    pyoro = pyoroleft;
                                    break;
                                case 1:
                                    pyoro = pyororight;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    if (tonguecount > 0)
                    {

                        switch (facingright)
                        {
                            case -1:
                                pyoro = pyoroopenleft;
                                break;
                            case 1:
                                pyoro = pyoroopenright;
                                break;
                            default:
                                break;
                        }
                    }
                        /*if(recall = false && tonguecount == 0)
                        {
                            switch (facingright)
                            {
                                case -1:
                                    pyoro = pyoroleft;
                                    break;
                                case 1:
                                    pyoro = pyororight;
                                    break;
                                default:
                                    break;
                            }
                        }*/
                    }

                //check bean and pyoro collision
                for (int i = 0; i < max_amount_of_beans; i++)
                {
                    if (x+16 >= bean_x[i] && bean_active[i])
                    {
                        if (x <= bean_x[i] + 16 && bean_active[i])
                        {
                            if (y+16 >= bean_y[i] && bean_active[i])
                            {
                                if (y <= bean_y[i] + 14 && bean_active[i])
                                {
                                    bean_active[i] = false;
                                    pyorodead = true;
                                }
                            }
                        }
                    }
                }


                switch (facingright)
                {
                    case -1:
                        tongueX = x + tongueoffsetX;
                        break;
                    case 1:
                        tongueX = x + tongueoffsetX + rightoffset;
                        break;
                    default:
                        break;
                }
                if (x < PLAYFIELD_LEFT)
                {
                    x = PLAYFIELD_LEFT;
                }
                if (x > PLAYFIELD_RIGHT - 17)
                {
                    x = PLAYFIELD_RIGHT - 17;
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Add))
                {
                    bigspeed += 0x100;
                }
                if (Keyboard.GetState().IsKeyDown(Keys.Subtract))
                {
                    bigspeed -= 0x100;
                }
                // Press Q to manually trigger an angel that restores the
                // nearest missing block to Pyoro (mirrors pico-8 one_angel).
                if (Keyboard.GetState().IsKeyDown(Keys.Q))
                {
                    block_recovery();
                }
                if(bigspeed<0x100)
                {
                    bigspeed = 0x100;
                }
                if (bigspeed > 0x800)
                {
                    bigspeed = 0x800;
                }
                if (score == 0)
                {
                    max_time = 0xB4;
                    score = 0;
                }
                if (score >= 1000 && score < 2999)
                {
                    max_time = 0x78;
                }
                if (score >= 3000 && score < 4999)
                {
                    max_time = 0x5F;
                }
                if(score >= 5000 && score < 7999)
                {
                    max_time = 0x50;
                }
                if (score >= 8000 && score < 9999)
                {
                    max_time = 0x41;
                }
                if (score >= 10000)
                {
                    max_time = 0x32;
                }
                mouseState = Mouse.GetState();
                updates = 1 / (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (tryGetMouseColumn(out int click))
                {
                    if (mouseState.LeftButton == ButtonState.Pressed) blocks[click] = false;
                    if (mouseState.RightButton == ButtonState.Pressed) blocks[click] = true;
                }

                if(pyorodead)
                {
                    y+=speed/4;
                    if (facingright == 1)
                    {
                        pyoro = pyorodeadright;
                    }
                    if (facingright == -1)
                    {
                        pyoro = pyorodeadleft;
                    }
                    if(y > NATIVE_HEIGHT)
                    {
                        gameover = true;
                    }
                }



                if (Keyboard.GetState().IsKeyDown(Keys.R) && gameover) /// RESTART GAME ///
                {
                    x = PLAYER_START_X;
                    y = PLAYER_START_Y;
                    tongueoffsetX = 1; // mirrored barrel tip
                    tongueoffsetY = -1; // barrel tip above the player collision box
                    tongueX = x + tongueoffsetX;
                    tongueY = y + tongueoffsetY;
                    tonguecount = 0;
                    TargetElapsedTime = System.TimeSpan.FromMilliseconds(1000.0f / targetFPS);
                    for (int i = 0; i < blockamount; i++)
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
                    pyorodead = false;
                    gameover = false;

                    risingscore = 5000;
                    tongueX = 0;
                    tongueY = 0;
                    facingright = 1;
                    spaceheld = 0;
                    tonguecount = 0;
                    recall = false;
                    tonguecollide = false;

                    smallspeed = 0xFF;
                    bigspeed = 0x100;
                    blocktocheck = 0;
                    paused = false;
                    readytounpause = false;
                    pyorosquat = 0;


                    
                    time_until_new_bean = 0x0;
                    score = 0;
                    scorePopups.Clear();
                    explosions.Clear();
                    angels.Clear();
                    angelQueue.Clear();
                    angelQueueTimer = 0;
                    rainbowClearQueue.Clear();

                    /*
                    max_time = 0xB4;
                    tmpmax = 0x0;
                    randnum = 0x0;
                    tmpspeed = 0x0;
                    randnum2 = 0x0;
                    beanspeed = 0;
                    beantype = 0;
                    
                    currentbeanx = 0;
                    randnum3 = 0x0;
                    randnum4 = 0x0;
                    beanxrandom = 0;

                    randnummain = 0;*/
                    beantype = 0;
                    randnum4 = 0x0;
                    currentbeantype = 0;

                    rainbowbeantotal = 10;
                    risingscore = 5000;
                    create_new_bean = false;
                    new_bean_number_debug = 0;
                    maxdisappear = 10;
                    dissappearcounter = 0;
                    triggerdisappear = false;
                    falsebeans = 0;
                }



            }
            //test = ((((int)x << 8 + bigspeed) - 0x28) >> 3);
            updateScorePopups();
            updateExplosions();
            updateAngelQueue();
            updateAngels();
            updateRainbowClear();
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // SET RENDERTARGET TO _nativeRenderTarget
            GraphicsDevice.SetRenderTarget(_nativeRenderTarget);

            // DRAWING INSIDE RENDERTARGET            
            // Let the animated outer camo show through outside the frame.
            GraphicsDevice.Clear(Color.Transparent);
            frameRate = 1 / (float)gameTime.ElapsedGameTime.TotalSeconds;
            // Clip every game sprite to the inside edge, including wide beams and effects.
            GraphicsDevice.ScissorRectangle = new Rectangle(PLAYFIELD_LEFT, PLAYFIELD_TOP,
                PLAYFIELD_RIGHT - PLAYFIELD_LEFT, BLOCK_FLOOR_Y + BLOCK_SIZE - PLAYFIELD_TOP);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: playfieldRasterizer);
            drawScenery();
            /*spriteBatch.DrawString(arial, string.Format("FPS {0:0.00}/s\nUPDATES: {1:0.00000}/s", frameRate, updates), new Vector2(40, 20), Color.White);
            spriteBatch.DrawString(arial, string.Format("X: {0}\nY: {1}\nSPEED: 0x{2:0.0000}", recall, spaceheld, speed), new Vector2(40, 50), Color.White);
            spriteBatch.DrawString(arial, string.Format("MouseX: {0}\nMosueY: {1}\nPyoroBoxX: {2}\nTongueX: {3} Space: {4} TC: {5}", mouseState.X/gameSize, mouseState.Y / gameSize, System.Math.Ceiling((x+speed - PLAYFIELD_LEFT)/8), tongueY - (tonguecount), spaceheld, tonguecollide), new Vector2(40, 90), Color.White); // ((mouseState.X / gameSize - PLAYFIELD_LEFT)/8) mouse on small rendertarget
            */

            //spriteBatch.Draw(current_bean_sprite, new Vector2(160,120), Color.White);

            drawTractorBeam();
            // Angels descending to restore blocks (drawn behind existing blocks)
            drawAngels(spriteBatch);

            for (int i = 0; i < blockamount; i++)
            {
                int blockx = PLAYFIELD_LEFT + (i * 8);
                int blocky = BLOCK_FLOOR_Y;

                if (blocks[i])
                {
                    spriteBatch.Draw(blockAtlas, new Vector2(blockx, blocky), Color.White);
                }
            }
            // Explosions draw ON TOP of the blocks they mark.
            drawExplosions(spriteBatch);
            //}
            // Center the native-size tank over the existing collision box, with treads on the floor.
            spriteBatch.Draw(pyoro, new Vector2((float)Math.Round(x) + (16 - pyoro.Width) / 2,
                (float)Math.Round(y) + 16 - pyoro.Height), Color.White);
            //spriteBatch.Draw(collisionblock, new Vector2((float)System.Math.Round((decimal)x), y), Color.White);

            for (int i = 0; i < max_amount_of_beans; i++)
            {
                if (bean_active[i] == true)
                {
                    spriteBatch.Draw(current_bean_sprite[i], new Vector2((float)Math.Round((decimal)bean_x[i]), (float)Math.Round((decimal)bean_y[i])), Color.White);
                    //spriteBatch.DrawString(arial, string.Format("{0}", bean_type[i]), new Vector2((float)Math.Round((decimal)bean_x[i]), (float)Math.Round((decimal)bean_y[i])), Color.White);
                    //spriteBatch.DrawString(arial, string.Format("{0}", bean_rainbow_counter[i]), new Vector2((float)Math.Round((decimal)bean_x[i]+10), (float)Math.Round((decimal)bean_y[i])), Color.White);

                    //spriteBatch.Draw(collisionblock, new Vector2((float)Math.Round((decimal)bean_x[i]), (float)Math.Round((decimal)bean_y[i])), Color.White);



                    //bean_y[i];
                }
                //temp_bean_sprite = current_bean_sprite[i];
            }
            if (tonguecollide && recall && tonguecount > 0 && !pyorodead)
            {
                spriteBatch.Draw(current_bean_sprite[caughtbean],
                    new Vector2((float)Math.Round(tongueX + tonguecount * facingright) - 8,
                        (float)Math.Round(tongueY - tonguecount) - 8), Color.White);
            }

            if(gameover)
            {
                // Bitmap-font "GAME OVER" centred in the playfield.
                string gameOverText = "GAME OVER";
                float goWidth = MeasureStringBitmap(gameOverText).X;
                DrawStringBitmap(spriteBatch, gameOverText, new Vector2((NATIVE_WIDTH - goWidth) / 2f, NATIVE_HEIGHT / 2), Color.White);
                // Small centred hint below the game-over text.
                string retry = "Press R to Retry";
                float retryWidth = MeasureStringBitmap(retry).X;
                DrawStringBitmap(spriteBatch, retry, new Vector2((NATIVE_WIDTH - retryWidth) / 2f, NATIVE_HEIGHT / 2 + 12), Color.White);
            }
            
            


            // HUD labels and counters use the pixel-perfect 8x8 bitmap font.
            DrawStringBitmap(spriteBatch, "SCORE", new Vector2(PLAYFIELD_LEFT + 4, 10), Color.White);
            DrawStringBitmap(spriteBatch, score.ToString("D6"), new Vector2(PLAYFIELD_LEFT + 4 + 6 * FONT_CELL, 10), Color.White);
            DrawStringBitmap(spriteBatch, "HIGH", new Vector2(PLAYFIELD_RIGHT - 4 - (4 + 6 + 2) * FONT_CELL, 10), Color.White);
            DrawStringBitmap(spriteBatch, highScore.ToString("D6"), new Vector2(PLAYFIELD_RIGHT - 4 - 6 * FONT_CELL, 10), Color.White);
            //spriteBatch.DrawString(arial, string.Format("tonguecollide {0}\nrecall {1}\ntonguecount {2}\n{3}", tonguecollide, recall, tonguecount,dissappearcounter), new Vector2(0, 0), Color.White);
            //spriteBatch.DrawString(arial, string.Format("smlspeed: 0x{0:X2}\nbigspeed: 0x{1:X2}", smallspeed, bigspeed), new Vector2(150, 10), Color.White);
            //spriteBatch.DrawString(arial, string.Format("max_time: 0x{0:X2}\ntmpmax: 0x{1:X2}\nrandnum: 0x{2:X2}\ntime_until_new_bean: 0x{3:X2}\nscore: {4}\nbigspeed: {5:X2}\nbeanspeed: {6:X2}\nnew_bean_number_debug: {7}", max_time, tmpmax, randnum, time_until_new_bean, score, bigspeed, beanspeed, new_bean_number_debug), new Vector2(50, 10), Color.White);
            //spriteBatch.DrawString(arial, string.Format(" rightblockcount {0} \n leftblockcount {1} \n rightblocktorecover {2} \n leftblocktorecover {3}", rightblockcount, leftblockcount, rightblocktorecover, leftblocktorecover), new Vector2(50, 10), Color.White);

            // Score popups: draw the "+pts" sprite where a bean was just caught,
            // fading out. The sprite regions come from scores.png.
            foreach (ScorePopup p in scorePopups)
            {
                Color c = Color.White;
                // Fade toward the end of its lifetime in the native colour space.
                float alpha = MathHelper.Clamp(p.timer / 30f, 0f, 1f);
                c *= alpha;

                // Look up the sprite region for this point value.
                Rectangle src = ScorePopupSpriteRegion(p.points);
                if (src.Width > 0)
                {
                    spriteBatch.Draw(scoresSprite, new Vector2(p.x, p.y), src, c);
                }
            }
            
            // Align the editor highlight with the floor, including after resizing.
            if (tryGetMouseColumn(out int hoverColumn))
                spriteBatch.Draw(select, new Vector2(PLAYFIELD_LEFT + hoverColumn * BLOCK_SIZE, BLOCK_FLOOR_Y), Color.Purple);

            // Keep overlays within the same clip, then draw the frame in a separate pass.
            if (showDebugMenu)
                drawDebugMenu(spriteBatch);
            spriteBatch.End();

            spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: RasterizerState.CullNone);
            drawFrame();
            spriteBatch.End();

            // SET RENDERTARGET TO NOTHING
            GraphicsDevice.SetRenderTarget(null);
            // Dynamic animated border fills the whole window behind the game.
            drawDynamicBorder(gameTime);

            // DRAW _nativeRenderTarget TO SCREEN at the integer scale
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            
            spriteBatch.Draw(_nativeRenderTarget, rect, Color.White);
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

        KeyboardState previousTabKeys;
        int screenshotPending;

        // Capture the current backbuffer and write it to a PNG file. The GPU
        // readback happens here (main thread), then the file write is deferred
        // to a worker thread so the game loop doesn't stall on disk I/O.
        void SaveScreenshot()
        {
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

        void drawDynamicBorder(GameTime gameTime)
        {
            borderTime += gameTime.ElapsedGameTime.TotalSeconds;
            int currentFrame = (int)((borderTime % CAMO_LOOP_SECONDS) * CAMO_FRAMES / CAMO_LOOP_SECONDS);
            if (currentFrame != borderFrame)
            {
                borderFrame = currentFrame;
                double t = currentFrame * CAMO_LOOP_SECONDS / CAMO_FRAMES;
                for (int y = 0; y < CAMO_HEIGHT; y++)
                {
                    double py = y / (double)(CAMO_HEIGHT - 1);
                    for (int x = 0; x < CAMO_WIDTH; x++)
                    {
                        double px = x / (double)(CAMO_WIDTH - 1);
                        double plasma = Math.Sin(px * 12.0 + t * 1.3) * Math.Sin(py * 15.0 - t * 1.1)
                            + 0.6 * Math.Sin(px * 7.0 + t * 1.1) * Math.Sin(py * 9.0 - t * 0.9);
                        bool tan = plasma > 0;
                        double shade = 0.55 + 0.45 * Math.Clamp(plasma, -1.0, 1.0);
                        // Eight shades per hue give a stable 16-color palette without dithering.
                        double darkest = tan ? 0.55 : 0.10;
                        shade = darkest + Math.Round((shade - darkest) / 0.45 * 7.0) * 0.45 / 7.0;
                        borderPixels[y * CAMO_WIDTH + x] = new Color(
                            (byte)((tan ? 118 : 56) * shade),
                            (byte)((tan ? 94 : 60) * shade),
                            (byte)((tan ? 54 : 32) * shade));
                    }
                }
                borderCamo.SetData(borderPixels);
            }
            int width = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int height = GraphicsDevice.PresentationParameters.BackBufferHeight;
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
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
            public float x, y;      // native (288x162) centre position
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
