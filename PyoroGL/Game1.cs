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

        const int NATIVE_WIDTH = 240;
        const int NATIVE_HEIGHT = 160;
        const int BLOCK_FLOOR_Y = 144;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            Window.AllowUserResizing = true;
        }

        // Compute the largest integer scale factor that fits the current window,
        // preserving the 240x160 aspect ratio. Also center the render target
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
            // keeping the 240:160 (3:2) aspect ratio.
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
        private Texture2D collisionblock;
        private Texture2D tongue;
        private Texture2D tonguesprite;
        private Texture2D tonguespriteright;
        private Texture2D tonguespriteleft;
        private Texture2D tonguecollision;
        private Texture2D select;
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
        private Texture2D scoreLabel;
        private Texture2D highScoreLabel;
        private Texture2D scoreDigits;

        private Texture2D gameoversprite;
        private Texture2D scoresSprite;

        //Timer t1sec = new Timer(1000);
        int blockamount = 20;
        
        float speed = 1.0f;
        MouseState mouseState;
        int gameSize = 4;
        bool[] blocks;
        float tongueX, tongueY;
        int facingright = 1;
        int tongueoffsetX, tongueoffsetY;
        int rightoffset = 9;
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
        private Texture2D angelSprite;

        // Screenshot counter for Tab-key PNG saves.
        int screenshotCount = 0;

        // When a rainbow bean is grabbed, other beans disappear one by one
        // (lowest first) instead of all at once. This queue holds the bean
        // indices still waiting to be cleared, and the timer paces them.
        List<int> rainbowClearQueue = new List<int>();
        int rainbowClearTimer = 0;


        int max_amount_of_blocks = 20;
        int rightblockcount = 0;
        int leftblockcount = 0;
        int rightblocktorecover = 0;
        int leftblocktorecover = 0;
        bool rightblockcheck = false;
        bool leftblockcheck = false;
        bool blockcheck = false;

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
            rightblocktorecover = (int)System.Math.Ceiling((x + speed - 40 + 4) / 8);
            leftblocktorecover = (int)System.Math.Ceiling((x + speed - 40 + 2) / 8); ;
            for (int i = rightblocktorecover; i < max_amount_of_blocks; i++)
            {
                if (rightblockcheck == false)
                {
                    if (blocks[i] == true)
                    {
                        rightblockcount++;
                    }
                    if (blocks[i] == false)
                    {
                        rightblocktorecover = i;
                        rightblockcheck = true;
                        blockcheck = true;
                    }
                }
            }
            for (int i = leftblocktorecover; i > 0; i--)
            {
                if (leftblockcheck == false)
                {
                    if (blocks[i] == true)
                    {
                        leftblockcount++;
                    }
                    if (blocks[i] == false)
                    {
                        leftblocktorecover = i;
                        leftblockcheck = true;
                        blockcheck = true;
                    }
                }
            }
            if (blockcheck == true)
            {
                if (rightblockcheck && leftblockcheck)
                {
                    if ((rightblockcount < leftblockcount)) // set to && rightblcok true?
                    {
                        //RIGHT BLOCK
                        spawnAngel(rightblocktorecover);
                        rightblockcount = 0;
                        leftblockcount = 0;
                        rightblocktorecover = 0;
                        leftblocktorecover = 0;
                        leftblockcheck = false;
                        rightblockcheck = false;
                        blockcheck = false;
                    }
                    else if (rightblockcount >= leftblockcount)
                    {
                        //LEFT BLOCK
                        spawnAngel(leftblocktorecover);
                        rightblockcount = 0;
                        leftblockcount = 0;
                        rightblocktorecover = 0;
                        leftblocktorecover = 0;
                        leftblockcheck = false;
                        rightblockcheck = false;
                        blockcheck = false;
                    }
                }
                if (rightblockcheck && !leftblockcheck)
                {
                        //RIGHT BLOCK
                        spawnAngel(rightblocktorecover);
                        rightblockcount = 0;
                        leftblockcount = 0;
                        rightblocktorecover = 0;
                        leftblocktorecover = 0;
                        leftblockcheck = false;
                        rightblockcheck = false;
                        blockcheck = false;
                }
                if (!rightblockcheck && leftblockcheck)
                {
                    //RIGHT BLOCK
                    spawnAngel(leftblocktorecover);
                    rightblockcount = 0;
                    leftblockcount = 0;
                    rightblocktorecover = 0;
                    leftblocktorecover = 0;
                    leftblockcheck = false;
                    rightblockcheck = false;
                    blockcheck = false;
                }
            }
            rightblockcount = 0;
            leftblockcount = 0;
            rightblocktorecover = 0;
            leftblocktorecover = 0;
            leftblockcheck = false;
            rightblockcheck = false;
            blockcheck = false;
        }

        // Play a 3-frame 16x16 explosion animation at a native (240x160) position.
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
        // it on landing (mirrors pico-8 one_angel). One angel per tile.
        void spawnAngel(int column)
        {
            if (column < 0 || column >= blockamount) return;
            if (angels.Exists(a => a.column == column)) return;
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
                int blockx = 40 + a.column * 8 - 4; // centre on 8px block
                float dx = blockx;
                // Round to whole pixels so the pixel-art sprite stays crisp
                // (no sub-pixel smoothing while the angel moves).
                float dy = (float)System.Math.Round(a.y);
                batch.Draw(angelSprite, new Vector2(dx, dy), src, Color.White);

                // Carried block tile below the angel once it is moving down.
                if (a.speed > 0)
                {
                    batch.Draw(block, new Vector2(40 + a.column * 8, (float)System.Math.Round(a.y + 16)), Color.White);
                }
            }
        }

        // Add points to the running score and spawn a short-lived "+pts" popup
        // at the given native (240x160) position, mirroring the pico-8 version.
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
            _nativeRenderTarget = new RenderTarget2D(GraphicsDevice, 240, 160);
            Window.ClientSizeChanged += Window_ClientSizeChanged;

            // Start the window at 4x scale (960x640).
            graphics.PreferredBackBufferWidth = 960; // 960
            graphics.PreferredBackBufferHeight = 640; // 640
            graphics.ApplyChanges();
            computeIntegerScale();
            x = 111;
            y = 128;
            tongueoffsetX = 2; // 1
            tongueoffsetY = 7; // 7
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
                bean_x[i] = r.Next(40, 192);
                bean_anim_counter[i] = rnd.Next(0, 43);
                bean_rainbow_counter[i] = rnd.Next(0, 5);
                bean_type[i] = 0;
            }

            random_number_main();

            base.Initialize();



            /* restart game
            x = 111;
            y = 128;
            tongueoffsetX = 2; // 1
            tongueoffsetY = 7; // 7
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
                bean_x[i] = r.Next(40, 192);
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
            arial = Content.Load<SpriteFont>("font");
            smallfont = Content.Load<SpriteFont>("smallfont");
            pyororight = Content.Load<Texture2D>("pyoro_standing0");
            pyoroleft = Content.Load<Texture2D>("pyoro_standing1");
            pyorosquatright = Content.Load<Texture2D>("pyorosquatright");
            pyorosquatleft = Content.Load<Texture2D>("pyorosquatleft");
            pyoroopenright = Content.Load<Texture2D>("pyoroopenright");
            pyoroopenleft = Content.Load<Texture2D>("pyoroopenleft");
            pyorodeadleft = Content.Load<Texture2D>("pyorodeadleft");
            pyorodeadright = Content.Load<Texture2D>("pyorodeadright");
            pyoro = pyororight;
            background = loadPng("background");
            scoreLabel = loadPng("score");
            highScoreLabel = loadPng("highscore");
            scoreDigits = loadScoreDigits();
            bean_centre = Content.Load<Texture2D>("bean_centre");
            bean_left = Content.Load<Texture2D>("bean_left");
            bean_right = Content.Load<Texture2D>("bean_right");
            beanw_centre = Content.Load<Texture2D>("beanw_centre");
            beanw_left = Content.Load<Texture2D>("beanw_left");
            beanw_right = Content.Load<Texture2D>("beanw_right");
            beanb_centre = Content.Load<Texture2D>("beanb_centre");
            beanb_left = Content.Load<Texture2D>("beanb_left");
            beanb_right = Content.Load<Texture2D>("beanb_right");
            temp_bean_sprite = bean_centre;
            for (int i = 0; i < max_amount_of_beans; i++)
            {
                current_bean_sprite[i] = bean_centre;
            }
            frame = Content.Load<Texture2D>("frame");
            block = Content.Load<Texture2D>("block");
            collisionblock = Content.Load<Texture2D>("collisionblock");
            explosionSprite = Content.Load<Texture2D>("explosion");
            angelSprite = Content.Load<Texture2D>("angel");
            tongue = Content.Load<Texture2D>("tonguepart");
            tonguespriteright = Content.Load<Texture2D>("tonguespriteright");
            tonguespriteleft = Content.Load<Texture2D>("tonguespriteleft");
            tonguesprite = tonguespriteright;
            tonguecollision = Content.Load<Texture2D>("tonguecollision");
            select = Content.Load<Texture2D>("select");
            gameoversprite = Content.Load<Texture2D>("gameoversprite");
            scoresSprite = Content.Load<Texture2D>("scores");
            // game frame is (start x=40,y=8 . end x=199, y=151) (width = 160 height = 144 , 20x18 8px blocks)
            // TODO: use this.Content to load your game content here
        }

        Texture2D loadPng(string name)
        {
            using (Stream stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Assets", name + ".png")))
                return Texture2D.FromStream(GraphicsDevice, stream);
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

        void drawScore(int value, int left)
        {
            // Keep both counters six digits wide, including leading zeroes.
            int remaining = Math.Clamp(value, 0, 999999);
            for (int digit = 5; digit >= 0; digit--)
            {
                int number = remaining % 10;
                Rectangle source = new Rectangle(number * 8, 0, 8, 9);
                // Zero has an extra pixel of left padding in the supplied sheet.
                int padding = number == 0 ? 1 : 0;
                spriteBatch.Draw(scoreDigits, new Vector2(left + digit * 8 - padding, 7), source, Color.White);
                remaining /= 10;
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            background.Dispose();
            scoreLabel.Dispose();
            highScoreLabel.Dispose();
            scoreDigits.Dispose();
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
                    beanxrandom = 0x90 << 16;
                    beanxrandom = beanxrandom >> 16;
                    randnum3 = (0x6D * randnum3) + 0x3FD;
                    randnum3 = (randnum3 & 0x0000FFFF);
                    beanxrandom = beanxrandom * randnum3;
                    beanxrandom = beanxrandom >> 16;
                    beanxrandom = beanxrandom << 16;
                    beanxrandom = beanxrandom >> 16;
                    beanxrandom += 0x30;
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
                            bean_x[i] = currentbeanx; // r.Next(40, 192);
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
                    int blocktocheckagainstbean = (int)System.Math.Ceiling((bean_x[i] - 40) / 8);
                    if (bean_y[i] > 128 && blocks[blocktocheckagainstbean] == true && bean_active[i])
                    {
                        bean_active[i] = false;
                        blocks[blocktocheckagainstbean] = false;
                        bean_y[i] = -20;
                        // Explosion where the block disappears.
                        spawnExplosion(40 + blocktocheckagainstbean * 8, 144);
                    }
                    if (bean_y[i] > 180)
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
                if (Keyboard.GetState().IsKeyDown(Keys.Left) && !pyorodead)// && x > 40)
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
                        blocktocheck = (int)System.Math.Ceiling((x - speed - 40) / 8);
                        if (blocktocheck < 0)
                        {
                            blocktocheck = 0;
                        }
                        if (blocktocheck > 19)
                        {
                            blocktocheck = 19;
                        }
                        if (blocks[blocktocheck] == false)
                        {
                            x = ((blocktocheck) * 8) + 40;
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
                        int subtract = checkx - 40 - 2; // ((current_x_in_memory>>8)-0x28)
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
                        blocktocheck = (int)System.Math.Ceiling((x + speed - 40 + 1) / 8);
                        if (blocktocheck < 0)
                        {
                            blocktocheck = 0;
                        }
                        if (blocktocheck > 19)
                        {
                            blocktocheck = 19;
                        }
                        if (blocks[blocktocheck] == false)
                        {
                            x = ((blocktocheck - 1) * 8) + 40 - 1;
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
                                    tonguesprite = tonguespriteleft;
                                }
                                if (pyorosquat >= 2)
                                {
                                    pyoro = pyoroleft;
                                    tonguesprite = tonguespriteleft;
                                }
                                break;
                            case 1:
                                if (pyorosquat < 2)
                                {
                                    pyoro = pyorosquatright;
                                    tonguesprite = tonguespriteright;
                                }
                                if (pyorosquat >= 2)
                                {
                                    pyoro = pyororight;
                                    tonguesprite = tonguespriteright;
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
                        if ((float)System.Math.Round((decimal)tongueX + ((decimal)(tonguecount+(2 * speed)) * facingright)) < 202 && (float)System.Math.Round((decimal)tongueX + ((decimal)(tonguecount + (2 * speed)) * facingright)) > 37)
                        {
                            if (tongueY - tonguecount > 8)
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
                                                            rainbowClearQueue.Clear();
                                                            for (int j = 0; j < max_amount_of_beans; j++)
                                                            {
                                                                if (bean_active[j] == true)
                                                                    rainbowClearQueue.Add(j);
                                                            }
                                                            // Sort descending by y so the lowest bean clears first.
                                                            rainbowClearQueue.Sort((a, b) => bean_y[b].CompareTo(bean_y[a]));
                                                            rainbowClearTimer = 0;
                                                            for (int j = 0; j < rainbowbeantotal; j++)
                                                            {
                                                                block_recovery();
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
                if (x < 40)
                {
                    x = 40;
                }
                if (x > 183)
                {
                    x = 183;
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
                    int pyoroCol = (int)System.Math.Ceiling((x - 40) / 8);
                    if (pyoroCol < 0) pyoroCol = 0;
                    if (pyoroCol > blockamount - 1) pyoroCol = blockamount - 1;
                    // Search outward from Pyoro for the nearest missing block.
                    for (int d = 0; d < blockamount; d++)
                    {
                        int right = pyoroCol + d;
                        int left = pyoroCol - d;
                        if (right < blockamount && !blocks[right])
                        {
                            spawnAngel(right);
                            break;
                        }
                        if (left >= 0 && !blocks[left])
                        {
                            spawnAngel(left);
                            break;
                        }
                    }
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
                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    // Do whatever you want here
                    // Map mouse to native 240x160 coords relative to the
                    // centered scaled rect, then to a block column.
                    int nativeX = mouseState.X - rect.X;
                    int click = ((nativeX / gameSize - 40) / 8);
                    if (click > 19) click = 19;
                    if (click < 0) click = 0;
                    blocks[click] = false;
                }
                if (mouseState.RightButton == ButtonState.Pressed)
                {
                    // Do whatever you want here
                    int nativeX = mouseState.X - rect.X;
                    int click = ((nativeX / gameSize - 40) / 8);
                    if (click > 19) click = 19;
                    if (click < 0) click = 0;
                    blocks[click] = true;
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
                    if(y>160)
                    {
                        gameover = true;
                    }
                }



                if (Keyboard.GetState().IsKeyDown(Keys.R) && gameover) /// RESTART GAME ///
                {
                    x = 111;
                    y = 128;
                    tongueoffsetX = 2; // 1
                    tongueoffsetY = 7; // 7
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
                        bean_x[i] = r.Next(40, 192);
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

                    rightblockcount = 0;
                    leftblockcount = 0;
                    rightblocktorecover = 0;
                    leftblocktorecover = 0;
                    rightblockcheck = false;
                    leftblockcheck = false;
                    blockcheck = false;
                }



            }
            //test = ((((int)x << 8 + bigspeed) - 0x28) >> 3);
            updateScorePopups();
            updateExplosions();
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
            GraphicsDevice.Clear(new Color(0x21, 0x21, 0x4a));
            frameRate = 1 / (float)gameTime.ElapsedGameTime.TotalSeconds;
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(background, new Vector2(40, 8), Color.White);
            /*spriteBatch.DrawString(arial, string.Format("FPS {0:0.00}/s\nUPDATES: {1:0.00000}/s", frameRate, updates), new Vector2(40, 20), Color.White);
            spriteBatch.DrawString(arial, string.Format("X: {0}\nY: {1}\nSPEED: 0x{2:0.0000}", recall, spaceheld, speed), new Vector2(40, 50), Color.White);
            spriteBatch.DrawString(arial, string.Format("MouseX: {0}\nMosueY: {1}\nPyoroBoxX: {2}\nTongueX: {3} Space: {4} TC: {5}", mouseState.X/gameSize, mouseState.Y / gameSize, System.Math.Ceiling((x+speed - 40)/8), tongueY - (tonguecount), spaceheld, tonguecollide), new Vector2(40, 90), Color.White); // ((mouseState.X / gameSize - 40)/8) mouse on small rendertarget
            */

            //spriteBatch.Draw(current_bean_sprite, new Vector2(160,120), Color.White);

            if (!pyorodead)
            {
                spriteBatch.Draw(tongue, new Vector2((float)System.Math.Round((decimal)tongueX), tongueY), Color.White);
            }
            /*if(spaceheld==1)
            {*/
            int tonguefaceoffset = 0;
            if (facingright == 1)
            {
                tonguefaceoffset = 1;
            }
            if (facingright == -1)
            {
                tonguefaceoffset = 2;
            }
            int beantongueoffset = 0;
            if (facingright == 1)
            {
                beantongueoffset = 0;
            }
            if (facingright == -1)
            {
                beantongueoffset = 10;
            }
            if (tonguecount > 0 && !pyorodead)
            {
                spriteBatch.Draw(tonguesprite, new Vector2((float)System.Math.Round((decimal)tongueX - tongueoffsetX - tonguefaceoffset + ((decimal)(tonguecount + (2 * speed)) * facingright)), (float)System.Math.Round((tongueY - 5 - (tonguecount + (2 * speed))))), Color.White);
            }
            for (int j = 0; j < System.Math.Ceiling(tonguecount); j++)
            {
                if ((float)System.Math.Round((decimal)tongueX + (j * facingright)) < 199 && (float)System.Math.Round((decimal)tongueX + (j * facingright)) > 40)
                {
                    if (tongueY - j > 8 && !pyorodead)
                    {
                        spriteBatch.Draw(tongue, new Vector2((float)System.Math.Round((decimal)tongueX + (j * facingright)), (int)tongueY - j), Color.White);
                    }
                }

            }
            // Angels descending to restore blocks (drawn behind existing blocks)
            drawAngels(spriteBatch);

            for (int i = 0; i < blockamount; i++)
            {
                int blockx = 40 + (i * 8);
                int blocky = 144;

                if (blocks[i])
                {
                    spriteBatch.Draw(block, new Vector2(blockx, blocky), Color.White);
                }
            }
            // Explosions draw ON TOP of the blocks they mark.
            drawExplosions(spriteBatch);
            //}
            spriteBatch.Draw(pyoro, new Vector2((float)System.Math.Round((decimal)x), (float)System.Math.Round((decimal)y)), Color.White);
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
                if (tonguecollide && recall && tonguecount>0)
                {
                    spriteBatch.Draw(current_bean_sprite[caughtbean], new Vector2((float)Math.Round(tongueX- beantongueoffset + tonguecount * facingright), (float)Math.Round((decimal)(tongueY - tonguecount-14))), Color.White);
                }

            }
            
            if(gameover)
            {
                spriteBatch.Draw(gameoversprite, new Vector2(120-35, 80), Color.White);
                // Small centred hint below the game-over text.
                string retry = "Press R to Retry";
                Vector2 retrySize = smallfont.MeasureString(retry);
                spriteBatch.DrawString(smallfont, retry, new Vector2(120 - retrySize.X / 2f, 92), Color.White);
            }
            


            /*if (tonguecount > 0)
            {
                spriteBatch.Draw(tonguecollision, new Vector2((float)System.Math.Round((decimal)tongueX + ((decimal)(tonguecount + (2 * speed)) * facingright)), (float)(tongueY - (tonguecount + (2 * speed)))), Color.White);
            }*/
            spriteBatch.Draw(frame, new Vector2(0, 0), Color.White);
            spriteBatch.Draw(scoreLabel, new Vector2(44, 10), Color.White);
            drawScore(score, 64);
            spriteBatch.Draw(highScoreLabel, new Vector2(117, 10), Color.White);
            drawScore(highScore, 152);
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
            
            //cursor highlight (native coords, offset-aware)
            int hoverNativeX = mouseState.X - rect.X;
            int hoverNativeY = mouseState.Y - rect.Y;
            if (hoverNativeX >= 0 && hoverNativeY >= 0 && hoverNativeX < NATIVE_WIDTH && hoverNativeY < NATIVE_HEIGHT)
            {
                spriteBatch.Draw(select, new Vector2((float)System.Math.Floor(((decimal)hoverNativeX / gameSize) / 8) * 8, (float)System.Math.Floor(((decimal)hoverNativeY / gameSize) / 8) * 8), Color.Purple);
            }
            spriteBatch.End();

            // SET RENDERTARGET TO NOTHING
            GraphicsDevice.SetRenderTarget(null);
            // Clear the whole window as the border colour; the scaled game is
            // drawn centered by "rect" and any leftover space shows this fill.
            GraphicsDevice.Clear(new Color(0x21, 0x21, 0x4a)); // #21214a border

            // DRAW _nativeRenderTarget TO SCREEN at the integer scale
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            
            spriteBatch.Draw(_nativeRenderTarget, rect, Color.White);
            //spriteBatch.DrawString(arial, string.Format("X: {0}\nY: {1}\nSPEED: {2:0.00}", x, y, speed), new Vector2(40, 50), Color.White);
            spriteBatch.End();

            // Press Tab to save the current framebuffer as a PNG.
            if (Keyboard.GetState().IsKeyDown(Keys.Tab))
            {
                SaveScreenshot();
            }

            base.Draw(gameTime);
        }

        // Capture the current backbuffer and write it to a PNG file.
        void SaveScreenshot()
        {
            try
            {
                int w = GraphicsDevice.PresentationParameters.BackBufferWidth;
                int h = GraphicsDevice.PresentationParameters.BackBufferHeight;
                Color[] pixels = new Color[w * h];
                GraphicsDevice.GetBackBufferData(pixels);

                Texture2D tex = new Texture2D(GraphicsDevice, w, h);
                tex.SetData(pixels);

                string dir = Path.Combine(AppContext.BaseDirectory, "screenshots");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, "screenshot_" + screenshotCount + ".png");
                using (System.IO.FileStream fs = new System.IO.FileStream(file, System.IO.FileMode.Create))
                {
                    tex.SaveAsPng(fs, w, h);
                }
                tex.Dispose();
                screenshotCount++;
            }
            catch (Exception ex)
            {
                // Don't crash the game if a screenshot fails.
                System.Diagnostics.Debug.WriteLine("Screenshot failed: " + ex.Message);
            }
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
            public float x, y;      // native (240x160) centre position
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
