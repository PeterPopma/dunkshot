using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;

namespace Dunkshot
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game : Microsoft.Xna.Framework.Game
    {
        const int MAX_BALLS = 12;
        const int BRIDGE_AXIS_X = 1646 - 48;
        const int BRIDGE_AXIS_Y = 704 - 48;

        GraphicsDeviceManager graphicsDeviceManager;
        SpriteBatch spriteBatch;
        SmokePlumeParticleSystem smokePlume;
        ParticleManager<ParticleState> particleManager;

        private VideoCapture videoCapture;
        enum GameState { PLAYING, END_SEQUENCE, END_SEQUENCE2, END_SEQUENCE3, FINISHED, REQUESTRESTART, SHOWHIGHSCORES, COUNTDOWN, GAMEOVER };
        GameState gameState = GameState.SHOWHIGHSCORES;
        Texture2D textureBackground;
        Texture2D textureBackgroundHighscores;
        Texture2D textureFont;
        Texture2D textureFont2;
        Texture2D textureBall;
        Texture2D textureLandrover;
        Texture2D textureBridge;
        Texture2D textureFilledBridge;
        Texture2D textureGeenArduino;
        Texture2D texturePhoto;
        Texture2D textureStartbutton;
        Texture2D textureArrow;
        Texture2D textureCountDown;
        Texture2D textureSmoke;
        Texture2D textureLaser;
        Texture2D textureShattered;
        Texture2D textureSadman;
        Texture2D textureLight;
        Texture2D textureNewHighscore;
        Texture2D textureFinished;
        Texture2D textureGameover;
        Texture2D textureGameoverMask;
        List<ShatteredPart> shattersCar;
        List<ShatteredPart> shatters = new List<ShatteredPart>();
        Texture2D[] textureMatrix;
        private SoundEffect soundEffectDriveAway;
        private SoundEffect soundEffectCountDown;
        private SoundEffect soundEffectIdea;
        private SoundEffect soundEffectTing;
        private SoundEffect soundEffectFallingCrash;
        int GameTimeMilliSeconds;
        GameTime gameTime;
        static SerialPort serialPort;
        List<Highscore> highscoresDay = new List<Highscore>();
        List<Highscore> highscoresMonth = new List<Highscore>();
        List<Highscore> highscoresAll = new List<Highscore>();
        double requestRestartTime;
        bool enterPressed;
        bool pressedA;
        Font fontNormal;
        Font fontScore;
        int checkPortOpenElapsed;
        int scoreYPosition;
        int scoreListType;
        int scoreListElapsed;
        int showWinningElapsed = 0;
        float countdownPhase = 1.0f;
        int countdownLetter = 5;
        static Random rand = new Random();
        bool receivedResetSignal = false;
        int score;
        float bridgePosition;
        float carUpDownPosition;
        float carPositionX;
        float carPositionY;
        float carRotation;
        int numberOfBalls;
        List<Ball> balls = new List<Ball>();
        RenderTarget2D renderTargetFireworks;
        RenderTarget2D renderTargetGameover;
        int timeLimitSeconds;
        int level;
        int timerScoreAdd;
        bool isNewHighscore;
        float highscoreSize;
        bool highscoreSizeUp;
        float finishedSize;
        bool finishedSizeUp;
        float gameoverWobble;
        bool gameoverWobbleUp;

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndIntertAfter, int X, int Y, int cx, int cy, int uFlags);
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int Which);

        public SpriteBatch SpriteBatch
        {
            get
            {
                return spriteBatch;
            }

            set
            {
                spriteBatch = value;
            }
        }

        public Game()
        {
            graphicsDeviceManager = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphicsDeviceManager.PreferredBackBufferHeight = 1080;
            graphicsDeviceManager.PreferredBackBufferWidth = 1920;
            graphicsDeviceManager.IsFullScreen = false;      // note: when using windows controls we can't use this option to go fullscreen.
            graphicsDeviceManager.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
            /*
            Form.FromHandle(Window.Handle).FindForm().WindowState = FormWindowState.Maximized;
            Form.FromHandle(Window.Handle).FindForm().FormBorderStyle = FormBorderStyle.None;
            Form.FromHandle(Window.Handle).FindForm().TopMost = true;
            SetWindowPos(Window.Handle, IntPtr.Zero, 0, 0, GetSystemMetrics(0), GetSystemMetrics(1), 64);            
            */
        }

        Highscore ReadHighscore(StreamReader srFile)
        {
            Highscore highscore = new Highscore();
            highscore.Score = Convert.ToInt32(srFile.ReadLine());
            highscore.Date = Convert.ToDateTime(srFile.ReadLine());
            highscore.Name = srFile.ReadLine();

            return highscore;
        }

        private void CreateFilledBridge()
        {
            textureFilledBridge = Content.Load<Texture2D>("bridge");
            Color[] destinationData = new Color[textureFilledBridge.Width * textureFilledBridge.Height];
            textureFilledBridge.GetData<Color>(destinationData, 0, destinationData.Length);

            Texture2D textureSource = TakeScreenshot();
            Color[] sourceData = new Color[textureSource.Width * textureSource.Height];
            textureSource.GetData<Color>(sourceData, 0, sourceData.Length);

            for (int y = 2; y <= 562; y++)
            {
                for (int x = 31; x <= 110; x++)
                {
                    destinationData[x + textureFilledBridge.Width * y].R = sourceData[x + textureFilledBridge.Width * y].R;
                    destinationData[x + textureFilledBridge.Width * y].G = sourceData[x + textureFilledBridge.Width * y].G;
                    destinationData[x + textureFilledBridge.Width * y].B = sourceData[x + textureFilledBridge.Width * y].B;
                    destinationData[x + textureFilledBridge.Width * y].A = 255;
                }
            }
        }

        public Texture2D TakeScreenshot()
        {
            int w, h;
            w = GraphicsDevice.PresentationParameters.BackBufferWidth;
            h = GraphicsDevice.PresentationParameters.BackBufferHeight;
            RenderTarget2D screenshot;
            screenshot = new RenderTarget2D(GraphicsDevice, w, h, false, SurfaceFormat.Bgra32, DepthFormat.None);
            GraphicsDevice.SetRenderTarget(screenshot);
            Draw(new GameTime());
            GraphicsDevice.Present();
            GraphicsDevice.SetRenderTarget(null);
            return screenshot;
        }

        private void SaveTextureAsPng(Texture2D texture, string filename)
        {
            byte[] textureData = new byte[4 * texture.Width * texture.Height];
            texture.GetData<byte>(textureData);

            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(
                           texture.Width, texture.Height,
                           System.Drawing.Imaging.PixelFormat.Format32bppArgb
                         );

            System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(
                           new System.Drawing.Rectangle(0, 0, texture.Width, texture.Height),
                           System.Drawing.Imaging.ImageLockMode.WriteOnly,
                           System.Drawing.Imaging.PixelFormat.Format32bppArgb
                         );

            IntPtr safePtr = bmpData.Scan0;
            System.Runtime.InteropServices.Marshal.Copy(textureData, 0, safePtr, textureData.Length);
            bmp.UnlockBits(bmpData);

            bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
        }


        private void LoadHighscorePictures()
        {
            for (int k = 0; k < highscoresDay.Count; k++)
            {
                highscoresDay[k].Photo = Texture2D.FromStream(GraphicsDevice, new FileStream("D" + k + ".png", FileMode.Open));
            }
            for (int k = 0; k < highscoresMonth.Count; k++)
            {
                highscoresMonth[k].Photo = Texture2D.FromStream(GraphicsDevice, new FileStream("M" + k + ".png", FileMode.Open));
            }
            for (int k = 0; k < highscoresAll.Count; k++)
            {
                highscoresAll[k].Photo = Texture2D.FromStream(GraphicsDevice, new FileStream("A" + k + ".png", FileMode.Open));
            }
        }

        private void SaveHighscorePictures()
        {
            for(int k=0; k<highscoresDay.Count; k++)
            {
                SaveTextureAsPng(highscoresDay[k].Photo, "D" + k + ".png");
            }
            for (int k = 0; k < highscoresMonth.Count; k++)
            {
                SaveTextureAsPng(highscoresMonth[k].Photo, "M" + k + ".png");
            }
            for (int k = 0; k < highscoresAll.Count; k++)
            {
                SaveTextureAsPng(highscoresAll[k].Photo, "A" + k + ".png");
            }
        }

        private void LoadHighscores()
        {
            int numScores = 0;
            highscoresDay.Clear();
            highscoresMonth.Clear();
            highscoresAll.Clear();
            try
            {
                using (StreamReader srFile = new StreamReader("Highscores"))
                {
                    numScores = Convert.ToInt32(srFile.ReadLine());
                    for (int k=0; k<numScores; k++)
                    {
                        Highscore highscore = ReadHighscore(srFile);
                        highscoresDay.Add(highscore);
                    }
                    numScores = Convert.ToInt32(srFile.ReadLine());
                    for (int k = 0; k < numScores; k++)
                    {
                        Highscore highscore = ReadHighscore(srFile);
                        highscoresMonth.Add(highscore);
                    }
                    numScores = Convert.ToInt32(srFile.ReadLine());
                    for (int k = 0; k < numScores; k++)
                    {
                        Highscore highscore = ReadHighscore(srFile);
                        highscoresAll.Add(highscore);
                    }
                }
            } catch (FileNotFoundException)
            {

            }
            LoadHighscorePictures();
        }

        void WriteHighscore(StreamWriter srFile, Highscore highscore)
        {
            srFile.WriteLine(highscore.Score);
            srFile.WriteLine(highscore.Date);
            srFile.WriteLine(highscore.Name);
        }

        private void SaveHighscores()
        {
            using (StreamWriter srFile = new StreamWriter("Highscores"))
            {
                srFile.WriteLine(highscoresDay.Count);
                foreach (Highscore highscore in highscoresDay)
                {
                    WriteHighscore(srFile, highscore);
                }
                srFile.WriteLine(highscoresMonth.Count);
                foreach (Highscore highscore in highscoresMonth)
                {
                    WriteHighscore(srFile, highscore);
                }
                srFile.WriteLine(highscoresAll.Count);
                foreach (Highscore highscore in highscoresAll)
                {
                    WriteHighscore(srFile, highscore);
                }
            }
            SaveHighscorePictures();
        }

        void MakePhoto()
        {
            texturePhoto = videoCapture.getFrameRectangle(new Microsoft.Xna.Framework.Rectangle(20, 200, 200, 150));
        }

        private void UpdateHighscores()
        {
            isNewHighscore = false;

            // remove scores older than one month
            DateTime sampleDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            foreach (Highscore highscore in highscoresMonth.ToList())
            {
                if (highscore.Date < sampleDate)
                {
                    highscoresMonth.Remove(highscore);
                }
            }
            // remove scores older than one day
            sampleDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            foreach (Highscore highscore in highscoresDay.ToList())
            {
                if (highscore.Date < sampleDate)
                {
                    highscoresDay.Remove(highscore);
                }
            }

            // insert new score in highscore lists
            if (highscoresDay.Count < 10 || score > highscoresDay[highscoresDay.Count - 1].Score)
            {
                isNewHighscore = true;

                if (highscoresDay.Count == 10)
                {
                    highscoresDay.RemoveAt(highscoresDay.Count - 1);
                }
                highscoresDay.Add(new Highscore(DateTime.Now, score, texturePhoto));
                highscoresDay.Sort((x, y) => x.Score.CompareTo(y.Score));
            }

            if (highscoresMonth.Count < 10 || score > highscoresMonth[highscoresMonth.Count - 1].Score)
            {
                if (highscoresMonth.Count == 10)
                {
                    highscoresMonth.RemoveAt(highscoresMonth.Count - 1);
                }
                highscoresMonth.Add(new Highscore(DateTime.Now, score, texturePhoto));
                highscoresMonth.Sort((x, y) => x.Score.CompareTo(y.Score));
            }

            if (highscoresAll.Count < 10 || score > highscoresAll[highscoresAll.Count - 1].Score)
            {
                if (highscoresAll.Count == 10)
                {
                    highscoresAll.RemoveAt(highscoresAll.Count - 1);
                }
                highscoresAll.Add(new Highscore(DateTime.Now, score, texturePhoto));
                highscoresAll.Sort((x, y) => x.Score.CompareTo(y.Score));
            }
        }

        private void CreateFirework(Vector2 Position)
        {
            float extraLeftRight = (float)(rand.Next(-3000,3000)/200.0f);
            int colortype = rand.Next(0, 4);
            for (int i = 0; i < 600; i++)
            {
                Microsoft.Xna.Framework.Color color;
                if (colortype == 0)
                {
                    color = new Microsoft.Xna.Framework.Color(rand.Next(0, 100), rand.Next(0, 100), 255, 255);
                }
                else if (colortype == 1)
                {
                    color = new Microsoft.Xna.Framework.Color(255, rand.Next(0, 80), rand.Next(0, 80), 255);
                }
                else if (colortype == 2)
                {
                    color = new Microsoft.Xna.Framework.Color(rand.Next(0, 100), 255, rand.Next(0, 100), 255);
                }
                else
                {
                    color = new Microsoft.Xna.Framework.Color(255, 255, rand.Next(0, 100), 255);
                }

                float speed = 4f * (1f - 1 / rand.NextFloat(1f, 10f));
                var state = new ParticleState()
                {
                    Velocity = rand.NextVector2(speed*2, speed),
                    Type = ParticleType.None,
                    LengthMultiplier = 1
                };
                state.Velocity.Y -= 30;
                state.Velocity.X += extraLeftRight;
                float scale = 1.80f - (0.40f * i%4);
                particleManager.CreateParticle(textureLaser, Position, color, 130, scale, state);
            }
        }

        /*
        private void LoadSettings()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Peter Popma\\RollerBall");
            if (key != null)
            {
                RecordScoreDay = Convert.ToInt32(key.GetValue("RecordScoreDay", 0));

            }
        }

        private void SaveSettings()
        {
            // Create or get existing subkey
            RegistryKey key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Peter Popma\\RollerBall");

            key.SetValue("RecordScoreDay", RecordScoreDay);
        }
        */

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            serialPort = new SerialPort();
            Console.WriteLine("Available Ports:");
            foreach (string s in SerialPort.GetPortNames())
            {
                Console.WriteLine("   {0}", s);
                if(!s.Equals("COM1"))
                {
                    serialPort.PortName = s;
                }
            }
            Console.WriteLine("Using port: {0}", serialPort.PortName);

            // Set the read/write timeouts
            serialPort.ReadTimeout = SerialPort.InfiniteTimeout;
            serialPort.WriteTimeout = SerialPort.InfiniteTimeout;
            serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_Datareceived);
            try
            {
                serialPort.Open();
            } catch (IOException)
            {
                Console.WriteLine("USB port not available.");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("USB port used (by arduino program?)");
            }

            // we'll see lots of these effects at once; this is ok
            // because they have a fairly small number of particles per effect.
            smokePlume = new SmokePlumeParticleSystem(this, 20);    // max 20 smokes at once
            Components.Add(smokePlume);

            // Initialize particle system for Fireworks
            // TODO: combine particle systems of Fireworks and Smoke into one.
            particleManager = new ParticleManager<ParticleState>(1024 * 20, ParticleState.UpdateParticle, graphicsDeviceManager);

            base.Initialize();
        }

        public void serialPort_Datareceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            int bytes = serialPort.BytesToRead;
            try
            {
                while (bytes > 0)     // if there is data in the buffer
                {
                    bytes--;
                    int data = serialPort.ReadByte();
                    switch (data)
                    {
                        case 65:
                            AddScore();
                            break;
                        case 80:        // Reset game
                            receivedResetSignal = true;
                            break;
                    }
                }
            } catch (IOException)
            {

            }
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            LoadHighscores();
            videoCapture = new VideoCapture(GraphicsDevice);

            // Create a new SpriteBatch, which can be used to draw textures.
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            textureBackgroundHighscores = Content.Load<Texture2D>("scorebackground");
            textureFont = Content.Load<Texture2D>("Font");
            textureFont2 = Content.Load<Texture2D>("Font2");
            textureGeenArduino = Content.Load<Texture2D>("geenarduino");
            textureStartbutton = Content.Load<Texture2D>("startbutton");
            textureArrow = Content.Load<Texture2D>("arrow");
            textureCountDown = Content.Load<Texture2D>("countdown");
            textureBackground = Content.Load<Texture2D>("canyon");
            textureBall = Content.Load<Texture2D>("ball");
            textureBridge = Content.Load<Texture2D>("bridge");
            textureLandrover = Content.Load<Texture2D>("landrover");
            textureSmoke = Content.Load<Texture2D>("smoke");
            textureLaser = Content.Load<Texture2D>("laser");
            textureSadman = Content.Load<Texture2D>("sadman");
            textureShattered = Content.Load<Texture2D>("shattered");
            textureNewHighscore = Content.Load<Texture2D>("newhigh");
            textureLight = Content.Load<Texture2D>("light");
            textureFinished = Content.Load<Texture2D>("finish");
            textureGameover = Content.Load<Texture2D>("gameover");
            textureGameoverMask = Content.Load<Texture2D>("gameovermask");
            shattersCar = Shattered.CreateShatteredParts(GraphicsDevice, textureLandrover, textureShattered);

            textureMatrix = new Texture2D[10];
            for (int k = 0; k < 10; k++)
            {
                textureMatrix[k] = Content.Load<Texture2D>("matrix"+k);
            }

            fontScore = new Dunkshot.Font();
            int[] fontExtraYOffset = { 4, 9, 7, 7, 7, 5, 5 };
            fontScore.Adjust(fontExtraYOffset, 0, 0);
            fontScore.Initialize(textureFont2);

            fontNormal = new Dunkshot.Font();
            fontNormal.Initialize(textureFont);

            soundEffectIdea = Content.Load<SoundEffect>("idea");
            soundEffectTing = Content.Load<SoundEffect>("ting");
            soundEffectCountDown = Content.Load<SoundEffect>("counting");
            soundEffectDriveAway = Content.Load<SoundEffect>("driveaway");
            soundEffectFallingCrash = Content.Load<SoundEffect>("fallingcrash");

            renderTargetFireworks = new RenderTarget2D(
                        graphicsDeviceManager.GraphicsDevice,
                        graphicsDeviceManager.GraphicsDevice.PresentationParameters.BackBufferWidth,
                        graphicsDeviceManager.GraphicsDevice.PresentationParameters.BackBufferHeight,
                        false,
                        graphicsDeviceManager.GraphicsDevice.PresentationParameters.BackBufferFormat,
                        DepthFormat.Depth24,
                        0,
                        RenderTargetUsage.DiscardContents);

            renderTargetGameover = new RenderTarget2D(
                        graphicsDeviceManager.GraphicsDevice,
                        textureGameover.Width,
                        textureGameover.Height,
                        false,
                        graphicsDeviceManager.GraphicsDevice.PresentationParameters.BackBufferFormat,
                        DepthFormat.Depth24,
                        0,
                        RenderTargetUsage.DiscardContents);

            gameState = GameState.SHOWHIGHSCORES;
        }

        void ExpodeCar(Vector2 position)
        {
            foreach (ShatteredPart shatter in shattersCar)
            {
                int xToCenter = (textureLandrover.Width / 2) - (shatter.XOffset + (shatter.Texture.Width / 2));
                int yToCenter = (textureLandrover.Height / 2) - (shatter.YOffset + (shatter.Texture.Height / 2));
                ShatteredPart newShatter = new ShatteredPart();
                newShatter.X = (int)position.X + shatter.XOffset;
                newShatter.Y = (int)position.Y + shatter.YOffset;
                newShatter.XSpeed = rand.NextFloat(3.0f, 8.0f) - xToCenter / 20.0f;
                newShatter.YSpeed = -15.0f;
                newShatter.Texture = shatter.Texture;
                newShatter.RotationSpeed = rand.NextFloat(-0.25f, 0.25f);
                shatters.Add(newShatter);
            }
        }

        private void UpdateShatters()
        {
            for (int i = 0; i < shatters.Count; i++)
            {
                ShatteredPart shatter = shatters[i];
                if (shatter.Y < 1080)
                {
                    shatter.Angle += shatter.RotationSpeed;
                    shatter.YSpeed += 0.4f;
                    shatter.Y += shatter.YSpeed;
                    shatter.X += shatter.XSpeed;
                }
                else
                {
                    shatters.RemoveAt(i);
                }
            }
        }

        void AddFireworks()
        {
            int k = rand.Next(40);
            if (k < 4)
            {
                CreateFirework(new Vector2(rand.Next(1920), 580 + rand.Next(200)));
            }
        }

        void InitGame()
        {
            score = 0;
            level = 1;
            highscoreSize = 1;
            finishedSize = 0.9f;
            MakePhoto();
            InitLevel();
        }

        void InitLevel()
        {
            GameTimeMilliSeconds = 0;
            timeLimitSeconds = 50 - 10*level;
            requestRestartTime = 0;
            scoreYPosition = 0;
            scoreListType = 0;
            scoreListElapsed = 0;
            scoreYPosition = 0;
            carPositionX = -190;
            carPositionY = 575;
            carRotation = 0;
            numberOfBalls = 0;
            timerScoreAdd = 0;
            bridgePosition = 0;
            countdownPhase = 1;
            balls.Clear();
            particleManager.Clear();
        }

        void AddScore()
        {
            if (numberOfBalls < MAX_BALLS)
            {
                soundEffectTing.Play();
                score += level;
                numberOfBalls++;
                balls.Add(new Ball(1598, -50));
                if (numberOfBalls == MAX_BALLS)
                {
                    gameState = GameState.END_SEQUENCE;
                }
            }
        }

        private void UpdateBalls()
        {
            for (int k = 0; k < balls.Count; k++)
            {
                if (balls[k].Y < 580 - k * (565 / MAX_BALLS))
                {
                    balls[k].YOrigin = balls[k].Y += 8;
                }
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            videoCapture.Dispose();
            SaveHighscores();
        }

        private void UpdateGame(TimeSpan elapsedTime)
        {
            UpdateBalls();

            carPositionX = -188 + ((GameTimeMilliSeconds / 1000.0f) / timeLimitSeconds * 1078);
            if (carPositionX > 890)
            {
                gameState = GameState.GAMEOVER;
                soundEffectFallingCrash.Play();
                UpdateHighscores();
            }

            // The time since Update was called last.
            GameTimeMilliSeconds += Convert.ToInt32(elapsedTime.TotalMilliseconds);
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (!serialPort.IsOpen)     // keep trying to open the port
            {
                checkPortOpenElapsed += gameTime.ElapsedGameTime.Milliseconds;
                if (checkPortOpenElapsed > 5000)       // check every 5 seconds
                {
                    checkPortOpenElapsed = 0;
                    try
                    {
                        serialPort.Open();
                    }
                    catch (IOException)
                    {
                        Console.WriteLine("USB port not available.");
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine("USB port used (by arduino program?)");
                    }
                }
            }

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == Microsoft.Xna.Framework.Input.ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
                Exit();

            if (enterPressed && Keyboard.GetState().IsKeyUp(Microsoft.Xna.Framework.Input.Keys.Enter))
            {
                enterPressed = false;
            }

            if (!pressedA && Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A))
            {
                pressedA = true;
                if (gameState.Equals(GameState.PLAYING))
                {
                    AddScore();
                }
            }
            if (pressedA && Keyboard.GetState().IsKeyUp(Microsoft.Xna.Framework.Input.Keys.A))
            {
                pressedA = false;
            }

            if (receivedResetSignal || (!enterPressed && Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Enter)))
            {
                enterPressed = true;
                receivedResetSignal = false;
                if (gameState.Equals(GameState.REQUESTRESTART))
                {
                    InitGame();
                }
                else if(gameState.Equals(GameState.SHOWHIGHSCORES))
                {
                    InitGame();
                    gameState = GameState.COUNTDOWN;
                    soundEffectCountDown.Play();
                }
                else if (gameState.Equals(GameState.PLAYING))
                {
                    gameState = GameState.REQUESTRESTART;
                    requestRestartTime = gameTime.TotalGameTime.TotalMilliseconds;
                }
                else if (gameState.Equals(GameState.FINISHED))
                {
                    gameState = GameState.SHOWHIGHSCORES;
                }
            }

            if (gameState.Equals(GameState.PLAYING))
            {
                UpdateGame(gameTime.ElapsedGameTime);
            }
            if (gameState.Equals(GameState.END_SEQUENCE))
            {
                UpdateBalls();
                if (balls[balls.Count - 1].Y >= 580 - (balls.Count - 1) * (565 / MAX_BALLS))
                {
                    if (bridgePosition == 0 || bridgePosition > -0.5f * (float)Math.PI)
                    {
                        bridgePosition -= 0.04f;
                        foreach(Ball ball in balls)
                        {
                            float length = BRIDGE_AXIS_Y - ball.YOrigin;
                            ball.X = (float)(BRIDGE_AXIS_X + Math.Sin(bridgePosition) * length);
                            ball.Y = (float)(BRIDGE_AXIS_Y - Math.Cos(bridgePosition) * length);
                        }
                    }
                    else
                    {
                        soundEffectDriveAway.Play();
                        gameState = GameState.END_SEQUENCE2;
                    }
                }
            }
            if (gameState.Equals(GameState.END_SEQUENCE2))
            {
                particleManager.Update();
                AddFireworks();
                carPositionX += 10;
                if (carPositionX > 1090 && carPositionX <= 1430)
                {
                    carPositionY = 595;
                }
                if (carPositionX>1430)
                {
                    carPositionY = 575 + (float)Math.Sin( (1430 - carPositionX)*0.004f)*110.0f;
                }
                if(carPositionX>2100)
                {
                    gameState = GameState.END_SEQUENCE3;
                }
            }
            if (gameState.Equals(GameState.END_SEQUENCE3))
            {
                particleManager.Update();
                AddFireworks();
                timerScoreAdd++;
                if(timerScoreAdd>10)
                {
                    int timeSeconds = timeLimitSeconds - GameTimeMilliSeconds / 1000;
                    if (timeSeconds > 0) {
                        timerScoreAdd = 0;
                        timeLimitSeconds--;
                        score += level;
                        soundEffectIdea.Play();
                    }
                    else if (timerScoreAdd > 100)
                    {
                        timerScoreAdd = 0;
                        if (level<4)
                        {
                            level++;
                            InitLevel();
                            soundEffectCountDown.Play();
                            gameState = GameState.COUNTDOWN;
                        }
                        else
                        {
                            UpdateHighscores();
                            gameState = GameState.FINISHED;
                        }
                    }
                }
            }
            if (gameState.Equals(GameState.REQUESTRESTART))
            {
                if(gameTime.TotalGameTime.TotalMilliseconds-requestRestartTime>4000)
                {
                    // Cancel the request to restart
                    gameState = GameState.PLAYING;

                }
            }
            if (gameState.Equals(GameState.SHOWHIGHSCORES))
            {
                scoreYPosition += 2;
                scoreListElapsed += gameTime.ElapsedGameTime.Milliseconds;
                int secondsShowHighscore = highscoresDay.Count;
                if (scoreListType==1)
                {
                    secondsShowHighscore = highscoresMonth.Count;
                }
                if (scoreListType==2)
                {
                    secondsShowHighscore = highscoresAll.Count;
                }
                if (scoreListElapsed > 10000 + secondsShowHighscore*1000)
                {
                    scoreListElapsed = 0;
                    scoreYPosition = 0;
                    scoreListType++;
                    if (scoreListType > 2)
                    {
                        scoreListType = 0;
                    }
                }
            }
            if (gameState.Equals(GameState.COUNTDOWN))
            {
                countdownPhase *= 1.08f;
                if(countdownPhase>100)
                {
                    countdownPhase = 1;
                    countdownLetter--;
                    if(countdownLetter<1)
                    {
                        countdownLetter = 5;
                        gameState = GameState.PLAYING;
                    }
                }
            }
            if (gameState.Equals(GameState.FINISHED))
            {
                showWinningElapsed += gameTime.ElapsedGameTime.Milliseconds;
                if(showWinningElapsed>20000)
                {
                    showWinningElapsed=0;
                    gameState = GameState.SHOWHIGHSCORES;
                }
            }
            if (gameState.Equals(GameState.GAMEOVER))
            {
                if (carPositionY < 1200)
                {
                    carPositionX += 7;
                    if (carPositionX > 1100)
                    {
                        carPositionY += (carPositionX - 1100) * .04f;
                        carRotation += 0.04f;
                    }
                }
                else if (carPositionY < 1999)
                {
                    carPositionY = 1999;
                    ExpodeCar(new Vector2(1200, 1000));
                }
                else if (carPositionY < 2300)
                {
                    carPositionY+=2;
                }
                else
                {
                    showWinningElapsed += gameTime.ElapsedGameTime.Milliseconds;
                    if (showWinningElapsed > 20000)
                    {
                        showWinningElapsed = 0;
                        gameState = GameState.SHOWHIGHSCORES;
                    }
                }

                UpdateShatters();
            }

            base.Update(gameTime);
        }

        void DrawScore()
        {
            int score1 = score % 10;
            int score10 = (score/10) % 10;
            int score100 = score / 100;

            SpriteBatch.Draw(textureMatrix[score100], new Rectangle(20, 16, textureMatrix[score100].Width * 2, textureMatrix[score100].Height * 2), new Rectangle(0, 0, textureMatrix[score100].Width, textureMatrix[score100].Height), Microsoft.Xna.Framework.Color.White);
            SpriteBatch.Draw(textureMatrix[score10], new Rectangle(210, 16, textureMatrix[score10].Width * 2, textureMatrix[score10].Height * 2), new Rectangle(0, 0, textureMatrix[score10].Width, textureMatrix[score10].Height), Microsoft.Xna.Framework.Color.White);
            SpriteBatch.Draw(textureMatrix[score1], new Rectangle(400, 16, textureMatrix[score1].Width * 2, textureMatrix[score1].Height * 2), new Rectangle(0, 0, textureMatrix[score1].Width, textureMatrix[score1].Height), Microsoft.Xna.Framework.Color.White);
        }

        void DrawGameTime()
        {
            int timeSeconds = timeLimitSeconds - GameTimeMilliSeconds / 1000;
            if (timeSeconds<0)
            {
                timeSeconds = 0;
            }
            int seconds = (timeSeconds) % 10;
            int seconds10 = (timeSeconds / 10) % 6;
            int minutes = (timeSeconds / 60) % 10;
            int minutes10 = (timeSeconds / 600) % 10;
            SpriteBatch.Draw(textureMatrix[minutes10], new Vector2(688, 32), Microsoft.Xna.Framework.Color.Yellow);
            SpriteBatch.Draw(textureMatrix[minutes], new Vector2(688 + 96, 32), Microsoft.Xna.Framework.Color.Yellow);
            SpriteBatch.Draw(textureMatrix[seconds10], new Vector2(688 + 225, 32), Microsoft.Xna.Framework.Color.Yellow);
            SpriteBatch.Draw(textureMatrix[seconds], new Vector2(688 + 321, 32), Microsoft.Xna.Framework.Color.Yellow);
        }

        BlendState BlendStateMultiply = new BlendState()
        {
            AlphaSourceBlend = Blend.DestinationAlpha,
            AlphaDestinationBlend = Blend.Zero,
            AlphaBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.Zero,
            ColorBlendFunction = BlendFunction.Add
        };

        BlendState BlendStatePeter = new BlendState()
        {
            ColorSourceBlend = Blend.One,
            AlphaSourceBlend = Blend.Zero,
            ColorDestinationBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
            ColorBlendFunction = BlendFunction.Add,
            AlphaBlendFunction = BlendFunction.Add
        };

        public void DrawPrepareGameover(RenderTarget2D renderTarget, GameTime gameTime)
        {
            // Set the render target
            graphicsDeviceManager.GraphicsDevice.SetRenderTarget(renderTarget);

            Color color = new Color(0, 0, 0, 0);
            graphicsDeviceManager.GraphicsDevice.Clear(color);
                       
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendStatePeter);

            // Spots
            spriteBatch.Draw(textureLight, new Vector2((float)(textureGameover.Width / 2 + (Math.Sin(gameTime.TotalGameTime.TotalMilliseconds / 1000f) * 150)), (float)(textureNewHighscore.Height / 2 + (Math.Cos(gameTime.TotalGameTime.TotalMilliseconds / 1000f) * 150))), null, new Microsoft.Xna.Framework.Color(255, 255, 255), 0, new Vector2(textureLight.Width/2, textureLight.Height/2), 2.2f, SpriteEffects.None, 0);
            spriteBatch.Draw(textureLight, new Vector2((float)(textureGameover.Width / 2 + (Math.Sin(gameTime.TotalGameTime.TotalMilliseconds / 700f) * 400.0f)), (float)(textureNewHighscore.Height / 2 - (Math.Cos(gameTime.TotalGameTime.TotalMilliseconds / 700f) * 400.0f))), null, new Microsoft.Xna.Framework.Color(255, 255, 255), 0, new Vector2(textureLight.Width / 2, textureLight.Height / 2), 3.2f, SpriteEffects.None, 0);

            spriteBatch.End();
            
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendStateMultiply);
            spriteBatch.Draw(textureGameoverMask, new Vector2(0, 0), null, new Microsoft.Xna.Framework.Color(255, 255, 255));
            spriteBatch.End();           

            // Drop the render target
            graphicsDeviceManager.GraphicsDevice.SetRenderTarget(null);
        }

        void DrawGame()
        {
            SpriteBatch.Draw(textureBackground, new Vector2(0, 0), new Rectangle(0, 0, textureBackground.Width, textureBackground.Height), Microsoft.Xna.Framework.Color.White);
            DrawBalls();
            SpriteBatch.Draw(textureBridge, new Rectangle(1640, 690, textureBridge.Width, textureBridge.Height), new Rectangle(0, 0, textureBridge.Width, textureBridge.Height), Microsoft.Xna.Framework.Color.White, bridgePosition, new Vector2(74,621), SpriteEffects.None, 0f);

            DrawScore();
            DrawGameTime();

            smokePlume.DrawSmoke();
            SpriteBatch.Draw(textureLandrover, new Rectangle((int)carPositionX, (int)(carPositionY + carUpDownPosition*.02f), textureLandrover.Width, textureLandrover.Height), new Rectangle(0, 0, textureLandrover.Width, textureLandrover.Height), Color.White, carRotation, new Vector2(190,75), SpriteEffects.None, 0f);

            if (GameTimeMilliSeconds % 200 == 0)
            {
                smokePlume.AddParticles(new Vector2(carPositionX - 50, carPositionY + 50), Color.White);
            }

            int adjust = GameTimeMilliSeconds % 400;
            if (adjust<200)
            {
                carUpDownPosition = adjust;
            }
            else
            {
                carUpDownPosition = 400 - adjust;
            }

            if (gameState.Equals(GameState.PLAYING) || gameState.Equals(GameState.END_SEQUENCE) || gameState.Equals(GameState.END_SEQUENCE2) || gameState.Equals(GameState.END_SEQUENCE3))
            {
                // first particles were drawn Additive on a black background, now add alphablending to real background
                particleManager.Draw(SpriteBatch, renderTargetFireworks);
            }

            if (gameState.Equals(GameState.REQUESTRESTART))
            {
                fontNormal.Print(SpriteBatch, "Nieuw spel starten?", 200, 250, true);
                fontNormal.Print(SpriteBatch, "Druk nogmaals op start", 50, 460, true);
            }

            if (gameState.Equals(GameState.COUNTDOWN))
            {
                DrawCountDown();
            }
            if (gameState.Equals(GameState.GAMEOVER))
            {
                foreach (ShatteredPart shatter in shatters.ToList())
                {
                    SpriteBatch.Draw(shatter.Texture, new Vector2(shatter.X, shatter.Y), new Microsoft.Xna.Framework.Rectangle(0, 0, shatter.Texture.Width, shatter.Texture.Height), Microsoft.Xna.Framework.Color.White, shatter.Angle, new Vector2(0, 0), 1.0f, SpriteEffects.None, 0f);
                }

                if (carPositionY > 1999)
                {
                    SpriteBatch.Draw(textureSadman, new Vector2(1300, 1080 - carPositionY + 2080), new Microsoft.Xna.Framework.Rectangle(0, 0, textureSadman.Width, textureSadman.Height), Microsoft.Xna.Framework.Color.White);
                }

                if (showWinningElapsed > 0)
                {
                    DrawGameOver();
                }
            }
            if (gameState.Equals(GameState.FINISHED))
            {
                DrawGameOver();
            }
        }

        void DrawGameOver()
        {
            if (gameoverWobbleUp)
            {
                gameoverWobble += 0.002f;
                if (gameoverWobble > .2f)
                {
                    gameoverWobbleUp = false;
                }
            }
            else
            {
                gameoverWobble -= 0.002f;
                if (gameoverWobble < -.2f)
                {
                    gameoverWobbleUp = true;
                }
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            SpriteBatch.Draw(textureGameover, new Rectangle(1050, 450, textureGameover.Width, textureGameover.Height), new Rectangle(0, 0, textureGameover.Width, textureGameover.Height), Color.White, gameoverWobble, new Vector2(textureGameover.Width / 2, textureGameover.Height / 2), SpriteEffects.None, 0f);
            spriteBatch.End();
            spriteBatch.Begin();
            SpriteBatch.Draw(renderTargetGameover, new Rectangle(1050, 450, textureGameover.Width, textureGameover.Height), new Rectangle(0, 0, textureGameover.Width, textureGameover.Height), Color.White, gameoverWobble, new Vector2(textureGameover.Width / 2, textureGameover.Height / 2), SpriteEffects.None, 0f);

            if (isNewHighscore)
            {
                if (highscoreSizeUp)
                {
                    highscoreSize += 0.002f;
                    if(highscoreSize>1)
                    {
                        highscoreSizeUp = false;
                    }
                }
                else
                {
                    highscoreSize -= 0.002f;
                    if (highscoreSize < 0.9)
                    {
                        highscoreSizeUp = true;
                    }
                }

                SpriteBatch.Draw(textureNewHighscore, new Rectangle(1650, 700, (int)(textureNewHighscore.Width * highscoreSize), (int)(textureNewHighscore.Height * highscoreSize)), new Rectangle(0, 0, textureNewHighscore.Width, textureNewHighscore.Height), Color.White, -gameoverWobble/2, new Vector2(textureNewHighscore.Width / 2, textureNewHighscore.Height / 2), SpriteEffects.None, 0f);
            }

            if (gameState.Equals(GameState.FINISHED))
            {
                if (finishedSizeUp)
                {
                    finishedSize += 0.001f;
                    if (finishedSize > 1)
                    {
                        finishedSizeUp = false;
                    }
                }
                else
                {
                    finishedSize -= 0.001f;
                    if (finishedSize < 0.9)
                    {
                        finishedSizeUp = true;
                    }
                }

                SpriteBatch.Draw(textureFinished, new Rectangle(350, 650, (int)(textureFinished.Width * finishedSize), (int)(textureFinished.Height * finishedSize)), new Rectangle(0, 0, textureFinished.Width, textureFinished.Height), Color.White, -gameoverWobble/2, new Vector2(textureFinished.Width / 2, textureFinished.Height / 2), SpriteEffects.None, 0f);
            }

        }

        void DrawHighscores()
        {
            SpriteBatch.Draw(textureBackgroundHighscores, new Vector2(0, 0), Microsoft.Xna.Framework.Color.White);

            if (scoreListType == 0)
            {
                for (int k=0; k<highscoresDay.Count; k++)
                {
                    fontScore.Print(SpriteBatch, (k+1).ToString(), 60, k * 180 + 1080 - scoreYPosition, false);
                    SpriteBatch.Draw(highscoresDay[k].Photo, new Vector2(160, k * 180 + 1040 - scoreYPosition), Microsoft.Xna.Framework.Color.White);
                    fontScore.Print(SpriteBatch, highscoresDay[k].Score.ToString(), 420, k * 180 + 1080 - scoreYPosition, false);
                    fontScore.Print(SpriteBatch, highscoresDay[k].Date.ToString("HH:mm:ss"), 830, k * 180 + 1080 - scoreYPosition, false);
                }
                SpriteBatch.Draw(textureBackgroundHighscores, new Vector2(0, 0), new Microsoft.Xna.Framework.Rectangle(0, 0, 1600, 280), Microsoft.Xna.Framework.Color.White);
                fontNormal.Print(SpriteBatch, "Topscores van vandaag", 70, 50, true);
            }
            else if (scoreListType == 1)
            {
                for (int k = 0; k < highscoresMonth.Count; k++)
                {
                    fontScore.Print(SpriteBatch, (k + 1).ToString(), 60, k * 180 + 1080 - scoreYPosition, false);
                    SpriteBatch.Draw(highscoresMonth[k].Photo, new Vector2(160, k * 180 + 1040 - scoreYPosition), Microsoft.Xna.Framework.Color.White);
                    fontScore.Print(SpriteBatch, highscoresMonth[k].Score.ToString(), 420, k * 180 + 1080 - scoreYPosition, false);
                    fontScore.Print(SpriteBatch, highscoresMonth[k].Date.ToString("dd/MM/yy"), 830, k * 180 + 1080 - scoreYPosition, false);
                }
                SpriteBatch.Draw(textureBackgroundHighscores, new Vector2(0, 0), new Microsoft.Xna.Framework.Rectangle(0, 0, 1600, 280), Microsoft.Xna.Framework.Color.White);
                fontNormal.Print(SpriteBatch, "Topscores van de maand", 10, 50, true);
            }
            else
            {
                for (int k = 0; k < highscoresAll.Count; k++)
                {
                    fontScore.Print(SpriteBatch, (k + 1).ToString(), 60, k * 180 + 1080 - scoreYPosition, false);
                    SpriteBatch.Draw(highscoresAll[k].Photo, new Vector2(160, k * 180 + 1040 - scoreYPosition), Microsoft.Xna.Framework.Color.White);
                    fontScore.Print(SpriteBatch, highscoresAll[k].Score.ToString(), 420, k * 180 + 1080 - scoreYPosition, false);
                    fontScore.Print(SpriteBatch, highscoresAll[k].Date.ToString("dd/MM/yy"), 830, k * 180 + 1080 - scoreYPosition, false);
                }
                SpriteBatch.Draw(textureBackgroundHighscores, new Vector2(0, 0), new Microsoft.Xna.Framework.Rectangle(0, 0, 1600, 280), Microsoft.Xna.Framework.Color.White);
                fontNormal.Print(SpriteBatch, "Topscores aller tijden", 150, 50, true);
            }

            int alpha = (int)(gameTime.TotalGameTime.TotalMilliseconds / 2 % 610);
            if (alpha > 355)
            {
                alpha = 610 - alpha;
            }
            if (alpha > 255)
            {
                alpha = 255;
            }
            Microsoft.Xna.Framework.Color color = new Microsoft.Xna.Framework.Color(255, 255, 255, alpha);
            SpriteBatch.End();
            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            SpriteBatch.Draw(textureStartbutton, new Vector2(1400, 650), new Microsoft.Xna.Framework.Rectangle(0, 0, textureStartbutton.Width, 328), color);
            SpriteBatch.Draw(textureStartbutton, new Vector2(1400, 970), new Microsoft.Xna.Framework.Rectangle(0, 328, textureStartbutton.Width, textureStartbutton.Height), color/*Microsoft.Xna.Framework.Color.White*/);
            SpriteBatch.End();
            SpriteBatch.Begin();
            SpriteBatch.Draw(textureArrow, new Vector2(1520, 520 - (int)(0.5*alpha)), new Microsoft.Xna.Framework.Rectangle(0, 0, textureArrow.Width, textureArrow.Height), Microsoft.Xna.Framework.Color.White);

        }

        private void DrawBalls()
        {
            foreach (Ball ball in balls)
            {
                SpriteBatch.Draw(textureBall, new Vector2(ball.X,ball.Y), new Microsoft.Xna.Framework.Rectangle(0, 0, textureBall.Width, textureBall.Height), Microsoft.Xna.Framework.Color.White);
            }
        }

        void DrawCountDown()
        {
            int alpha = (int)(255 - 4*countdownPhase);
            if(alpha<0)
            {
                alpha = 0;
            }
            Microsoft.Xna.Framework.Color color = new Microsoft.Xna.Framework.Color(255, 255, 255, alpha);
            if (countdownLetter==5)
            {
                // ratio 2:3
                SpriteBatch.Draw(textureCountDown, new Microsoft.Xna.Framework.Rectangle((int)(860 - 100 * countdownPhase), (int)(390 - 150 * countdownPhase), (int)(200+200*countdownPhase), (int)(300+300*countdownPhase)), new Microsoft.Xna.Framework.Rectangle(0, 0, 214, 319), color);
            }
            if (countdownLetter == 4)
            {
                // ratio 2:3
                SpriteBatch.Draw(textureCountDown, new Microsoft.Xna.Framework.Rectangle((int)(860 - 100 * countdownPhase), (int)(390 - 150 * countdownPhase), (int)(200 + 200 * countdownPhase), (int)(300 + 300 * countdownPhase)), new Microsoft.Xna.Framework.Rectangle(214, 0, 266, 319), color);
            }
            if (countdownLetter == 3)
            {
                // ratio 2:3
                SpriteBatch.Draw(textureCountDown, new Microsoft.Xna.Framework.Rectangle((int)(860 - 100 * countdownPhase), (int)(390 - 150 * countdownPhase), (int)(200 + 200 * countdownPhase), (int)(300 + 300 * countdownPhase)), new Microsoft.Xna.Framework.Rectangle(480, 0, 222, 319), color);
            }
            if (countdownLetter == 2)
            {
                // ratio 2:3
                SpriteBatch.Draw(textureCountDown, new Microsoft.Xna.Framework.Rectangle((int)(860 - 100 * countdownPhase), (int)(390 - 150 * countdownPhase), (int)(200 + 200 * countdownPhase), (int)(300 + 300 * countdownPhase)), new Microsoft.Xna.Framework.Rectangle(702, 0, 232, 319), color);
            }
            if (countdownLetter == 1)
            {
                // ratio 2:3
                SpriteBatch.Draw(textureCountDown, new Microsoft.Xna.Framework.Rectangle((int)(800 - 100 * countdownPhase), (int)(390 - 150 * countdownPhase), (int)(200 + 200 * countdownPhase), (int)(300 + 300 * countdownPhase)), new Microsoft.Xna.Framework.Rectangle(936, 0, 178, 319), color);
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            this.gameTime = gameTime;
            particleManager.DrawPrepare(SpriteBatch, renderTargetFireworks);
            DrawPrepareGameover(renderTargetGameover, gameTime);

            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);

            BlendState blend = new BlendState();
            blend.AlphaSourceBlend = Blend.Zero;
            blend.AlphaDestinationBlend = Blend.SourceColor;
            blend.ColorSourceBlend = Blend.Zero;
            blend.ColorDestinationBlend = Blend.SourceColor;

            base.Draw(gameTime);

            SpriteBatch.Begin();

            if (gameState.Equals(GameState.SHOWHIGHSCORES))
            {
                DrawHighscores();
            }
            else
            {
                DrawGame();
            }

            if (!serialPort.IsOpen && gameTime.TotalGameTime.TotalSeconds%2>1)
            {
                SpriteBatch.Draw(textureGeenArduino, new Vector2(700, 50), new Microsoft.Xna.Framework.Rectangle(0, 0, textureGeenArduino.Width, textureGeenArduino.Height), Microsoft.Xna.Framework.Color.White);
            }

            SpriteBatch.End();
        }
    }
}
