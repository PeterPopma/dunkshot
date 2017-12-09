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
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        private VideoCapture videoCapture;
        enum GameState { PLAYING, FINISHED, REQUESTRESTART, SHOWHIGHSCORES, COUNTDOWN };
        GameState gameState = GameState.SHOWHIGHSCORES;
        Video video;
        Microsoft.Xna.Framework.Media.VideoPlayer videoPlayer;
        bool playingVideoWinner = false;
        Texture2D textureBackgroundHighscores;
        Texture2D textureFont;
        Texture2D textureFont2;
        Texture2D textureGeenArduino;
        Texture2D texturePhoto;
        Texture2D textureStartbutton;
        Texture2D textureArrow;
        Texture2D textureCountDown;
        Texture2D[] textureMatrix;
        private SoundEffect soundEffectPoing;
        private SoundEffect soundEffectCountDown;
        int GameTimeMilliSeconds;
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
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferHeight = 1080;
            graphics.PreferredBackBufferWidth = 1920;
            graphics.IsFullScreen = false;      // note: when using windows controls we can't use this option to go fullscreen.
            graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
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
            highscore.GameTimeMilliSeconds = Convert.ToInt32(srFile.ReadLine());
            highscore.Date = Convert.ToDateTime(srFile.ReadLine());
            highscore.Name = srFile.ReadLine();

            return highscore;
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
            srFile.WriteLine(highscore.GameTimeMilliSeconds);
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
            texturePhoto = videoCapture.getFrameRectangle(new Microsoft.Xna.Framework.Rectangle(20, 200, 400, 150));
        }

        private void UpdateHighscores()
        {
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
                // insert new score in highscore lists
                if (highscoresDay.Count < 10 || GameTimeMilliSeconds < highscoresDay[highscoresDay.Count - 1].GameTimeMilliSeconds)
                {
                    if (highscoresDay.Count == 10)
                    {
                        highscoresDay.RemoveAt(highscoresDay.Count - 1);
                    }
                    highscoresDay.Add(new Highscore(DateTime.Now, GameTimeMilliSeconds, texturePhoto));
                    highscoresDay.Sort((x, y) => x.GameTimeMilliSeconds.CompareTo(y.GameTimeMilliSeconds));
                }

                if (highscoresMonth.Count < 10 || GameTimeMilliSeconds < highscoresMonth[highscoresMonth.Count - 1].GameTimeMilliSeconds)
                {
                    if (highscoresMonth.Count == 10)
                    {
                        highscoresMonth.RemoveAt(highscoresMonth.Count - 1);
                    }
                    highscoresMonth.Add(new Highscore(DateTime.Now, GameTimeMilliSeconds, texturePhoto));
                    highscoresMonth.Sort((x, y) => x.GameTimeMilliSeconds.CompareTo(y.GameTimeMilliSeconds));
                }

                if (highscoresAll.Count < 10 || GameTimeMilliSeconds < highscoresAll[highscoresAll.Count - 1].GameTimeMilliSeconds)
                {
                    if (highscoresAll.Count == 10)
                    {
                        highscoresAll.RemoveAt(highscoresAll.Count - 1);
                    }
                    highscoresAll.Add(new Highscore(DateTime.Now, GameTimeMilliSeconds, texturePhoto));
                    highscoresAll.Sort((x, y) => x.GameTimeMilliSeconds.CompareTo(y.GameTimeMilliSeconds));
                }
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

            soundEffectPoing = Content.Load<SoundEffect>("poing");
            soundEffectCountDown = Content.Load<SoundEffect>("counting");
            video = Content.Load<Video>("crowd");
            videoPlayer = new Microsoft.Xna.Framework.Media.VideoPlayer();

            gameState = GameState.SHOWHIGHSCORES;
        }

        void InitGame()
        {
            gameState = GameState.PLAYING;
            GameTimeMilliSeconds = 0;
            requestRestartTime = 0;
            scoreYPosition = 0;
            scoreListType = 0;
            scoreListElapsed = 0;
            scoreYPosition = 0;
            score = 0;
            MakePhoto();
        }

        void AddScore()
        {
            score++;
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
                AddScore();
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
                    gameState = GameState.COUNTDOWN;
                    //                    InitGame();
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

            if (playingVideoWinner)
            {
                if (videoPlayer.State == MediaState.Stopped)
                {
                    playingVideoWinner = false;
                }
            }

            if (gameState.Equals(GameState.PLAYING))
            {
                UpdateGame(gameTime.ElapsedGameTime);
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
                if (scoreListElapsed > 20000)
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
                    if(countdownLetter<0)
                    {
                        countdownLetter = 5;
                        InitGame();
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

            base.Update(gameTime);
        }

        string GameTimeToString(int GameTimeMilliSeconds)
        {
            string seconds = ((GameTimeMilliSeconds / 10000) % 6).ToString() + ((GameTimeMilliSeconds / 1000) % 10).ToString();
            string minutes = ((GameTimeMilliSeconds / 600000) % 10).ToString() + ((GameTimeMilliSeconds / 60000) % 10).ToString();
            string millisecs = (GameTimeMilliSeconds % 1000).ToString();
            if (millisecs.Length == 2)
                millisecs = "0" + millisecs;
            if (millisecs.Length == 1)
                millisecs = "00" + millisecs;

            return minutes + ":" + seconds + "." + millisecs;
        }

        void DrawScore()
        {
            int score1 = score % 10;
            int score10 = (score/10) % 10;
            int score100 = score / 100;

            SpriteBatch.Draw(textureMatrix[score100], new Rectangle(600, 400, textureMatrix[score100].Width * 2, textureMatrix[score100].Height * 2), new Rectangle(0, 0, textureMatrix[score100].Width, textureMatrix[score100].Height), Microsoft.Xna.Framework.Color.Red);
            SpriteBatch.Draw(textureMatrix[score10], new Rectangle(800, 400, textureMatrix[score10].Width * 2, textureMatrix[score10].Height * 2), new Rectangle(0, 0, textureMatrix[score10].Width, textureMatrix[score10].Height), Microsoft.Xna.Framework.Color.Red);
            SpriteBatch.Draw(textureMatrix[score1], new Rectangle(1000, 400, textureMatrix[score1].Width * 2, textureMatrix[score1].Height * 2), new Rectangle(0, 0, textureMatrix[score1].Width, textureMatrix[score1].Height), Microsoft.Xna.Framework.Color.Red);
        }

        void DrawGame()
        {

//            if (videoPlayerBackground.State != MediaState.Stopped)
//            {
//                SpriteBatch.Draw(videoPlayerBackground.GetTexture(), /*new Microsoft.Xna.Framework.Rectangle(0, 0, 1920, 320)*/ new Vector2(0,4), new Microsoft.Xna.Framework.Rectangle(0, 0, videoPlayerBackground.GetTexture().Width, videoPlayerBackground.GetTexture().Height), Microsoft.Xna.Framework.Color.White);
//            }
          
            int seconds = (GameTimeMilliSeconds / 1000) % 10;
            int seconds10 = (GameTimeMilliSeconds / 10000) % 6;
            int minutes = (GameTimeMilliSeconds / 60000) % 10;
            int minutes10 = (GameTimeMilliSeconds / 600000) % 10;
            SpriteBatch.Draw(textureMatrix[minutes10], new Vector2(1499, 26), Microsoft.Xna.Framework.Color.Yellow);
            SpriteBatch.Draw(textureMatrix[minutes], new Vector2(1499 + 96, 26), Microsoft.Xna.Framework.Color.Yellow);
            SpriteBatch.Draw(textureMatrix[seconds10], new Vector2(1499 + 225, 26), Microsoft.Xna.Framework.Color.Yellow);
            SpriteBatch.Draw(textureMatrix[seconds], new Vector2(1499 + 321, 26), Microsoft.Xna.Framework.Color.Yellow);

            if (gameState.Equals(GameState.PLAYING))
            {
                DrawScore();
            }

            if (playingVideoWinner)
            {
                SpriteBatch.Draw(videoPlayer.GetTexture(), new Microsoft.Xna.Framework.Rectangle(0, 650, 960, 500), new Microsoft.Xna.Framework.Rectangle(0, 0, videoPlayer.GetTexture().Width, videoPlayer.GetTexture().Height), Microsoft.Xna.Framework.Color.White, 0.0f, new Vector2(0, 0), SpriteEffects.FlipHorizontally, 0.0f);
                SpriteBatch.Draw(videoPlayer.GetTexture(), new Microsoft.Xna.Framework.Rectangle(960, 650, 960, 500), Microsoft.Xna.Framework.Color.White);
            }

            if (gameState.Equals(GameState.FINISHED))
            {
                fontNormal.Print(SpriteBatch, "Spel afgelopen", 420, 40, true);
                fontNormal.Print(SpriteBatch, "Tijd; " + GameTimeToString(GameTimeMilliSeconds), 50, 440, true);
            }

            if (gameState.Equals(GameState.REQUESTRESTART))
            {
                fontNormal.Print(SpriteBatch, "Nieuw spel starten?", 200, 250, true);
                fontNormal.Print(SpriteBatch, "Druk nogmaals op start", 50, 460, true);
            }
        }

        void DrawHighscores(GameTime gameTime)
        {
            SpriteBatch.Draw(textureBackgroundHighscores, new Vector2(0, 0), Microsoft.Xna.Framework.Color.White);

            if (scoreListType == 0)
            {
                for (int k=0; k<highscoresDay.Count; k++)
                {
                    fontScore.Print(SpriteBatch, (k+1).ToString(), 60, k * 180 + 1080 - scoreYPosition, false);
                    SpriteBatch.Draw(highscoresDay[k].Photo, new Vector2(160, k * 180 + 1040 - scoreYPosition), Microsoft.Xna.Framework.Color.White);
                    fontScore.Print(SpriteBatch, GameTimeToString(highscoresDay[k].GameTimeMilliSeconds), 380, k * 180 + 1080 - scoreYPosition, false);
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
                    fontScore.Print(SpriteBatch, GameTimeToString(highscoresMonth[k].GameTimeMilliSeconds), 380, k * 180 + 1080 - scoreYPosition, false);
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
                    fontScore.Print(SpriteBatch, GameTimeToString(highscoresAll[k].GameTimeMilliSeconds), 380, k * 180 + 1080 - scoreYPosition, false);
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
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);

            BlendState blend = new BlendState();
            blend.AlphaSourceBlend = Blend.Zero;
            blend.AlphaDestinationBlend = Blend.SourceColor;
            blend.ColorSourceBlend = Blend.Zero;
            blend.ColorDestinationBlend = Blend.SourceColor;

            SpriteBatch.Begin();

            if(gameState.Equals(GameState.SHOWHIGHSCORES))
            {
                DrawHighscores(gameTime);
            }
            else
            {
                DrawGame();
                if (gameState.Equals(GameState.COUNTDOWN))
                {
                    DrawCountDown();
                }
                if (playingVideoWinner)
                {
                    int pos = (int)(gameTime.TotalGameTime.TotalMilliseconds / 5 % 255);
                    if (pos > 128)
                    {
                        pos = 255 - pos;
                    }
                }
            }

            if (!serialPort.IsOpen && gameTime.TotalGameTime.TotalSeconds%2>1)
            {
                SpriteBatch.Draw(textureGeenArduino, new Vector2(700, 50), new Microsoft.Xna.Framework.Rectangle(0, 0, textureGeenArduino.Width, textureGeenArduino.Height), Microsoft.Xna.Framework.Color.White);
            }

            SpriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
