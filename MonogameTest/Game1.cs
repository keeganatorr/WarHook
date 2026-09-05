using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
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

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            Window.AllowUserResizing = true;
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
        private Texture2D heightmap;

        private Texture2D gameoversprite;

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
                        blocks[rightblocktorecover] = true;
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
                        blocks[leftblocktorecover] = true;
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
                        blocks[rightblocktorecover] = true;
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
                    blocks[leftblocktorecover] = true;
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
       
        void random_number_main()
        {
            randnummain = (0x6D * randnummain) + 0x3FD;
            randnummain = (randnummain & 0x0000FFFF);
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
            graphics.PreferredBackBufferWidth = 960; // 960
            graphics.PreferredBackBufferHeight = 640; // 640
            rect = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            graphics.ApplyChanges();
            x = 100;
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
            x = 100;
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
            heightmap = Content.Load<Texture2D>("heightmap");
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
            tongue = Content.Load<Texture2D>("tonguepart");
            tonguespriteright = Content.Load<Texture2D>("tonguespriteright");
            tonguespriteleft = Content.Load<Texture2D>("tonguespriteleft");
            tonguesprite = tonguespriteright;
            tonguecollision = Content.Load<Texture2D>("tonguecollision");
            select = Content.Load<Texture2D>("select");
            gameoversprite = Content.Load<Texture2D>("gameoversprite");
            // game frame is (start x=40,y=8 . end x=199, y=151) (width = 160 height = 144 , 20x18 8px blocks)
            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
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
                                                                            score += 10;
                                                                    }
                                                                    else
                                                                    {
                                                                        score += 50;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    score += 100;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                score += 300;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            score += 1000;
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
                                                            for (int j = 0; j < max_amount_of_beans; j++)
                                                            {
                                                                if (bean_active[j] == true)
                                                                {
                                                                    bean_active[j] = false;
                                                                    score += 50;
                                                                }
                                                            }
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
                    int click = ((mouseState.X / gameSize - 40) / 8);
                    if (click > 19) click = 19;
                    if (click < 0) click = 0;
                    blocks[click] = false;
                }
                if (mouseState.RightButton == ButtonState.Pressed)
                {
                    // Do whatever you want here
                    int click = ((mouseState.X / gameSize - 40) / 8);
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



                if (Keyboard.GetState().IsKeyDown(Keys.Space) && gameover) /// RESTART GAME ///
                {
                    x = 100;
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
            GraphicsDevice.Clear(Color.CornflowerBlue);
            frameRate = 1 / (float)gameTime.ElapsedGameTime.TotalSeconds;
            spriteBatch.Begin();
            spriteBatch.Draw(heightmap, new Vector2(0, 0), Color.White);
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
            for (int i = 0; i < blockamount; i++)
            {
                int blockx = 40 + (i * 8);
                int blocky = 144;

                if (blocks[i])
                {
                    spriteBatch.Draw(block, new Vector2(blockx, blocky), Color.White);
                }
            }
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
            }
            


            /*if (tonguecount > 0)
            {
                spriteBatch.Draw(tonguecollision, new Vector2((float)System.Math.Round((decimal)tongueX + ((decimal)(tonguecount + (2 * speed)) * facingright)), (float)(tongueY - (tonguecount + (2 * speed)))), Color.White);
            }*/
            spriteBatch.Draw(frame, new Vector2(0, 0), Color.White);
            spriteBatch.DrawString(arial, string.Format("score: {0}", score), new Vector2(50, 10), Color.White);
            //spriteBatch.DrawString(arial, string.Format("tonguecollide {0}\nrecall {1}\ntonguecount {2}\n{3}", tonguecollide, recall, tonguecount,dissappearcounter), new Vector2(0, 0), Color.White);
            //spriteBatch.DrawString(arial, string.Format("smlspeed: 0x{0:X2}\nbigspeed: 0x{1:X2}", smallspeed, bigspeed), new Vector2(150, 10), Color.White);
            //spriteBatch.DrawString(arial, string.Format("max_time: 0x{0:X2}\ntmpmax: 0x{1:X2}\nrandnum: 0x{2:X2}\ntime_until_new_bean: 0x{3:X2}\nscore: {4}\nbigspeed: {5:X2}\nbeanspeed: {6:X2}\nnew_bean_number_debug: {7}", max_time, tmpmax, randnum, time_until_new_bean, score, bigspeed, beanspeed, new_bean_number_debug), new Vector2(50, 10), Color.White);
            //spriteBatch.DrawString(arial, string.Format(" rightblockcount {0} \n leftblockcount {1} \n rightblocktorecover {2} \n leftblocktorecover {3}", rightblockcount, leftblockcount, rightblocktorecover, leftblocktorecover), new Vector2(50, 10), Color.White);
            
            spriteBatch.Draw(select, new Vector2((float)System.Math.Floor(((decimal)mouseState.X/gameSize)/8)*8, (float)System.Math.Floor(((decimal)mouseState.Y / gameSize)/8)*8), Color.Purple);
            spriteBatch.End();

            // SET RENDERTARGET TO NOTHING
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Beige);

            // DRAW _nativeRenderTarget TO SCREEN
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            
            spriteBatch.Draw(_nativeRenderTarget, rect, Color.White);
            //spriteBatch.DrawString(arial, string.Format("X: {0}\nY: {1}\nSPEED: {2:0.00}", x, y, speed), new Vector2(40, 50), Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
