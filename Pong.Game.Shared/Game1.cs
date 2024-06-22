using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Pong.Game
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        #region Variables
        private const float BallSpeed = 5f;

        private readonly Random _rnd = new Random();
        private readonly PlatformSpecific _platformSpecific;

        private readonly Point _gameResolution = new Point(960, 720);

        private bool _playWithBot = false;
        private bool _botButtonDown = false;
        private float _botTargetY;
        private const float BotDeadzone = 0.25f;

        private bool _gameStarted = false;
        private bool _showStartMessage = true;
        private bool _roundStarted = false;
        private bool _gameEnded = false;

        private const int PadXOffset = 10;
        private const float PadScale = 0.5f;
        private const float PadSpeed = 7.5f;
        private ScreenSide _winningPlayer = ScreenSide.Center;

        private const float GamepadDeadzone = 0.1f;
        private const float GamepadSensitivity = 1.1f;

        private RenderTarget2D _renderTarget;
        private Rectangle _renderTargetDest;
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;

        private Ball _ball;
        private GameObject _leftPad;
        private GameObject _rightPad;

        private Texture2D _ballTex;

        private int _leftScore,
            _rightScore = 0;

        /// <summary>
        /// Whether left/right stopped after landing a goal.
        /// </summary>
        private bool _leftStopped,
            _rightStopped = false;

        #endregion

        #region Initialization
        public Game1(PlatformSpecific platformSpecific)
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;

            _platformSpecific = platformSpecific;
            
            _botTargetY = _gameResolution.Y / 2f;
        }
        
        public Game1()
        {
            Console.WriteLine("Constructing Game1...");
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;
            Console.WriteLine("Constructed Game1");
        }

        protected override void Initialize()
        {
            Console.WriteLine("Initializing...");
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.IsFullScreen = true;
            
#if DEBUG
            // Make debug window borderless to allow breakpoints to redirect to IDE.
            _graphics.HardwareModeSwitch = false;
#endif
            
            _graphics.ApplyChanges();

            Console.WriteLine("Applied changes to graphics");

            base.Initialize();

            Console.WriteLine("base.Initialize run");
        }

        protected void Restart()
        {
            _roundStarted = false;
            _gameEnded = false;
            _showStartMessage = true;
            _winningPlayer = ScreenSide.Center;
            _leftScore = 0;
            _rightScore = 0;
            GenerateBall();
        }

        private void GenerateBall()
        {
            // Generates a random Y velocity.
            Vector2 ballVelocity = new Vector2(_rnd.Next(0, 2) == 1 ? -1 : 1, GenerateRandomBallYVelocity());

            _ball = new Ball(
                _ballTex,
                new Vector2(_gameResolution.X / 2, _gameResolution.Y / 2),
                1f,
                _spriteBatch,
                _gameResolution.X,
                _gameResolution.Y,
                ballVelocity * BallSpeed,
                _leftPad,
                _rightPad);

            _ball.OnBallBouncedEvent += OnBallBounced;
        }

        protected override void LoadContent()
        {
            Console.WriteLine("Loading content...");
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            Console.WriteLine("Spritebatch created");

            _leftPad = new GameObject(
                Content.Load<Texture2D>("Pad"),
                new Vector2(PadXOffset, _gameResolution.Y / 2),
                PadScale,
                _spriteBatch,
                _gameResolution.X,
                _gameResolution.Y);

            Console.WriteLine("Created left pad");
            
            _rightPad = new GameObject(
                Content.Load<Texture2D>("Pad"),
                new Vector2(_gameResolution.X - PadXOffset, _gameResolution.Y / 2),
                PadScale,
                _spriteBatch,
                _gameResolution.X,
                _gameResolution.Y);

            Console.WriteLine("Created right pad");

            _ballTex = Content.Load<Texture2D>("Ball");

            Console.WriteLine("Loaded ball texture");

            GenerateBall();

            Console.WriteLine("Generated ball");

            _font = Content.Load<SpriteFont>("GameFont");

            Console.WriteLine("Loaded font");

            _renderTarget = new RenderTarget2D(
                GraphicsDevice,
                _gameResolution.X,
                _gameResolution.Y);

            Console.WriteLine("Created rendertarget");

            _renderTargetDest = GetRenderTargetDestination(
                _gameResolution,
                _graphics.PreferredBackBufferWidth,
                _graphics.PreferredBackBufferHeight);

            Console.WriteLine("Set RenderTarget destination");
        }

        protected float GenerateRandomBallYVelocity()
        {
            float output = _rnd.Next(1, 9) * 0.1f;
            return output * (_rnd.Next(0, 2) == 1 ? -1 : 1);
        }

        #endregion

        #region Scoring
        protected void Scored(ScreenSide side)
        {
            switch (side)
            {
                case ScreenSide.Left:
                    _rightScore++;
                    _roundStarted = false;
                    _leftStopped = false;
                    _rightStopped = false;

                    CheckWin();
                    if (!_gameEnded)
                        GenerateBall();
                    break;
                case ScreenSide.Right:
                    _leftScore++;
                    _roundStarted = false;
                    _leftStopped = false;
                    _rightStopped = false;

                    CheckWin();
                    if (!_gameEnded)
                        GenerateBall();
                    break;
                default:
                    return;
            }
        }
        protected void CheckWin()
        {
            if (_leftScore >= 10)
            {
                _gameEnded = true;
                _winningPlayer = ScreenSide.Left;
            }
            else if (_rightScore  >= 10)
            {
                _gameEnded = true;
                _winningPlayer = ScreenSide.Right;
            }
        }
        #endregion

        protected void OnBallBounced(ScreenSide side)
        {
            if (side == ScreenSide.Left)
                SimulateBall();
        }
        
        /// <summary>
        /// Simulates ball movement from the left side to the right side and returns the position at which it hit the right pad level. <para />
        /// The position is then stored in <see cref="_botTargetY"/>.
        /// </summary>
        protected void SimulateBall()
        {
            if (!_playWithBot)
                return;

            var dummyBall = new GameObject(_ball.Texture, _ball.Position, _ball.Scale, null,
                _gameResolution.X, _gameResolution.Y)
            {
                Velocity = _ball.Velocity
            };

            var lastTouched = ScreenSide.Center;
            
            while (dummyBall.Position.X + (_ball.Width / 2) < _rightPad.Position.X - (_rightPad.Width / 2))
            {
                dummyBall.Position += dummyBall.Velocity;
                var touchSide = dummyBall.CheckOOS();

                var newVelocity = dummyBall.Velocity;

                switch (touchSide)
                {
                    case ScreenSide.Bottom:
                        newVelocity.Y = -MathF.Abs(dummyBall.Velocity.Y)
                                        - (lastTouched != ScreenSide.Bottom
                                            ? Ball.BOUNCE_SPEED_UP
                                            : 0);
                        lastTouched = ScreenSide.Bottom;
                        break;
                    case ScreenSide.Top:
                        newVelocity.Y = MathF.Abs(newVelocity.Y)
                                        + (lastTouched != ScreenSide.Top
                                            ? Ball.BOUNCE_SPEED_UP
                                            : 0);
                        lastTouched = ScreenSide.Top;
                        break;
                }

                if (dummyBall.Position.X - (dummyBall.Width / 2) <=
                    _leftPad.Position.X + (_leftPad.Width / 2))
                {
                    newVelocity.X = MathF.Abs(newVelocity.X) +
                                    (lastTouched != ScreenSide.Left ? Ball.BOUNCE_SPEED_UP : 0);
                    lastTouched = ScreenSide.Left;
                }

                dummyBall.Velocity = newVelocity;
            }

            _botTargetY = dummyBall.Position.Y;
        }

        #region Update and Drawing
        protected override void Update(GameTime gameTime)
        {
            Console.WriteLine("Updating");
            KeyboardState keyboard = Keyboard.GetState();
            Console.WriteLine("Got keyboard state");
            GamePadState plr1GamepadState = GamePad.GetState(PlayerIndex.One);
            GamePadState plr2GamepadState = GamePad.GetState(PlayerIndex.Two);
            Console.WriteLine("Got gamepad state");

            bool leftUsedKeyboard = false;
            bool rightUsedKeyboard = false;
            bool leftUsedStick = false;
            bool rightUsedStick = false;
            bool leftUsedDPad = false;
            bool rightUsedDPad = false;

            #region Exit, Restart and Bot controls

            Console.WriteLine("Checking exit");
            if (keyboard.IsKeyDown(Keys.Escape))
            {
                Console.WriteLine("Exiting...");
                Exit();
            }

            Console.WriteLine("Checking restart");
            if (keyboard.IsKeyDown(Keys.R)
                || plr1GamepadState.IsButtonDown(Buttons.Start)
                || plr2GamepadState.IsButtonDown(Buttons.Start))
                Restart();

            Console.WriteLine("Checking bot button");
            if ((!_botButtonDown && keyboard.IsKeyDown(Keys.B))
                || plr1GamepadState.IsButtonDown(Buttons.Y)
                || plr2GamepadState.IsButtonDown(Buttons.Y))
            {
                _playWithBot = !_playWithBot;
                _botButtonDown = true;
            }

            if (keyboard.IsKeyUp(Keys.B)
                && plr1GamepadState.IsButtonUp(Buttons.Y)
                && plr2GamepadState.IsButtonUp(Buttons.Y))
                _botButtonDown = false;
            
            #endregion
            
            #region Keyboard controls

            Console.WriteLine("left up");
            // Left Up
            if (keyboard.IsKeyDown(Keys.W))
            {
                _leftPad.MoveNoOOS(0, -PadSpeed);
                leftUsedKeyboard = true;
            }

            Console.WriteLine("left down");
            // Left Down
            if (keyboard.IsKeyDown(Keys.S))
            {
                _leftPad.MoveNoOOS(0, PadSpeed);
                leftUsedKeyboard = true;
            }

            Console.WriteLine("right up");
            // Right Up
            if (keyboard.IsKeyDown(Keys.Up) && !_playWithBot)
            {
                _rightPad.MoveNoOOS(0, -PadSpeed);
                rightUsedKeyboard = true;
            }

            Console.WriteLine("right down");
            // Right Down
            if (keyboard.IsKeyDown(Keys.Down) && !_playWithBot)
            {
                _rightPad.MoveNoOOS(0, PadSpeed);
                rightUsedKeyboard = true;
            }

            #endregion

            #region Gamepad controls

            Console.WriteLine("left thumb");
            // Left Thumbstick
            if ((plr1GamepadState.ThumbSticks.Left.Y > GamepadDeadzone 
                 || plr1GamepadState.ThumbSticks.Left.Y < -GamepadDeadzone)
                && !leftUsedKeyboard)
            {
                _leftPad.MoveNoOOS(0, -plr1GamepadState.ThumbSticks.Left.Y * PadSpeed * GamepadSensitivity);
                leftUsedStick = true;
            }

            Console.WriteLine("right thumb");
            // Right Thumbstick
            if ((plr2GamepadState.ThumbSticks.Left.Y > GamepadDeadzone 
                 || plr2GamepadState.ThumbSticks.Left.Y < -GamepadDeadzone) 
                && !rightUsedKeyboard && !_playWithBot)
            {
                _rightPad.MoveNoOOS(0, -plr2GamepadState.ThumbSticks.Left.Y * PadSpeed * GamepadSensitivity);
                rightUsedStick = true;
            }

            Console.WriteLine("left dpad up");
            // Left DPad Up
            if (plr1GamepadState.DPad.Up == ButtonState.Pressed
                && !leftUsedKeyboard && !leftUsedStick)
            {
                _leftPad.MoveNoOOS(0, -PadSpeed);
                leftUsedDPad = true;
            }

            Console.WriteLine("left dpad down");
            // Left DPad Down
            if (plr1GamepadState.DPad.Down == ButtonState.Pressed
                && !leftUsedKeyboard && !leftUsedStick)
            {
                _leftPad.MoveNoOOS(0, PadSpeed);
                leftUsedDPad = true;
            }

            Console.WriteLine("right dpad up");
            // Right DPad Up
            if (plr2GamepadState.DPad.Up == ButtonState.Pressed
                && !rightUsedKeyboard && !rightUsedStick
                && !_playWithBot)
            {
                _rightPad.MoveNoOOS(0, -PadSpeed);
                rightUsedDPad = true;
            }

            Console.WriteLine("right dpad down");
            // Right DPad down
            if (plr2GamepadState.DPad.Down == ButtonState.Pressed
                && !rightUsedKeyboard && !rightUsedStick
                && !_playWithBot)
            {
                _rightPad.MoveNoOOS(0, PadSpeed);
                rightUsedDPad = true;
            }
            
            #endregion

            #region Bot Movement

            Console.WriteLine("bot moov");
            if (_playWithBot && _roundStarted)
            {
                Console.WriteLine("bot mooving");
                if (_botTargetY > _rightPad.Y + (_rightPad.Height * BotDeadzone)
                    || _botTargetY < _rightPad.Y - (_rightPad.Height * BotDeadzone))
                    _rightPad.MoveNoOOS(0, _rightPad.Y < _botTargetY ? PadSpeed : -PadSpeed);
                Console.WriteLine("bot mooved");
            }

            #endregion

            Console.WriteLine("set moved");
            bool moved = leftUsedKeyboard || rightUsedKeyboard
                || leftUsedStick || rightUsedStick
                || leftUsedDPad || rightUsedDPad;

            Console.WriteLine("set leftMoved");
            bool leftMoved = leftUsedKeyboard
                || leftUsedStick
                || leftUsedDPad;

            Console.WriteLine("set rightMoved");
            bool rightMoved = rightUsedKeyboard
                || rightUsedStick
                || rightUsedDPad;

            Console.WriteLine("start round");
            if ((leftMoved && _leftStopped) || (rightMoved && _rightStopped))
            {
                Console.WriteLine("sim ball");
                if (!_roundStarted && _ball.Velocity.X > 0)
                    SimulateBall();
                
                _roundStarted = true;
                _showStartMessage = false;
                _gameStarted = true;
            }

            Console.WriteLine("set left stopped");
            if (!leftMoved)
                _leftStopped = true;

            Console.WriteLine("set right stopped");
            if (!rightMoved)
                _rightStopped = true;

            Console.WriteLine("moov ball");
            if (_roundStarted)
                _ball.MoveByVelocity();

            Console.WriteLine("check score");
            if (_roundStarted)
            {
                ScreenSide scored = _ball.CheckScored();
                if (scored != ScreenSide.Center)
                    Scored(scored);
            }

            Console.WriteLine("call base update");
            base.Update(gameTime);

            Console.WriteLine("call platform specific update");
            _platformSpecific?.UpdateFinished(_leftScore, _rightScore, _gameStarted);
            Console.WriteLine("end update");
        }

        private void WriteStatusText(string text)
        {
            Console.WriteLine("Writing status...");
            Vector2 textMiddlePoint = _font.MeasureString(text) / 2;
            // Places text in center of the screen
            Vector2 position = new Vector2(_graphics.PreferredBackBufferWidth / 2, _graphics.PreferredBackBufferHeight / 10);
            _spriteBatch.DrawString(_font, text, position, Color.White, 0, textMiddlePoint, 1.0f, SpriteEffects.None, 0.5f);
            Console.WriteLine("Status written");
        }

        protected override void Draw(GameTime gameTime)
        {
            Console.WriteLine("Draw called");
            GraphicsDevice.SetRenderTarget(_renderTarget);
            Console.WriteLine("Render target set");

            GraphicsDevice.Clear(Color.Black);

            Console.WriteLine("Drawing elements...");
            _spriteBatch.Begin(samplerState: SamplerState.PointWrap);
            Console.WriteLine("SpriteBatch begun");
            _ball.Draw();
            Console.WriteLine("ball drawn");
            _leftPad.Draw();
            Console.WriteLine("leftpad drawn");
            _rightPad.Draw();
            Console.WriteLine("Right pad drawn");

            _spriteBatch.End();
            Console.WriteLine("spritebatch ended");

            base.Draw(gameTime);
            Console.WriteLine("Called base.draw");

            GraphicsDevice.SetRenderTarget(null);
            Console.WriteLine("Render target disabled");
            GraphicsDevice.Clear(new Color(30, 30, 30));
            Console.WriteLine("Screen cleared");

            _spriteBatch.Begin(samplerState: SamplerState.PointWrap);
            Console.WriteLine("basic spritebatch begun");

            _spriteBatch.Draw(_renderTarget, _renderTargetDest, Color.White);
            Console.WriteLine("rendertarget drawn");

            #region Text
            if (!_gameEnded)
                WriteStatusText(_showStartMessage ?
                    "Left: W-S/Gamepad 1 stick/Gamepad 1 Dpad Up-Down"
                        + "\n" + (_playWithBot ? "Right: Played by bot" : "Right: Up-Down Arrow/Gamepad 2 stick/Gamepad 2 Dpad Up-Down")
                        + "\nMove to start"
                        + "\nPress B/Gamepad Y button to play with " + (_playWithBot ? "another person" : "bot")
                    : $"{_leftScore} - {_rightScore}");
            else
                WriteStatusText((_winningPlayer == ScreenSide.Left ?
                    _playWithBot ? "You won!" : "Left player won!"
                    : _playWithBot ? "You lose..." : "Right player won!")
                        + "\nPress Esc to quit. (on PC only)"
                        + "\nPress R on keyboard or Menu button on gamepad to restart.");
            #endregion

            _spriteBatch.End();
            Console.WriteLine("spritebatch ended");
        }

        Rectangle GetRenderTargetDestination(Point resolution, int preferredBackBufferWidth, int preferredBackBufferHeight)
        {
            float resolutionRatio = (float)resolution.X / resolution.Y;
            float screenRatio;
            Point bounds = new Point(preferredBackBufferWidth, preferredBackBufferHeight);
            screenRatio = (float)bounds.X / bounds.Y;
            float scale;
            Rectangle rectangle = new Rectangle();

            if (resolutionRatio < screenRatio)
                scale = (float)bounds.Y / resolution.Y;
            else if (resolutionRatio > screenRatio)
                scale = (float)bounds.X / resolution.X;
            else
            {
                // Resolution and window/screen share aspect ratio
                rectangle.Size = bounds;
                return rectangle;
            }
            rectangle.Width = (int)(resolution.X * scale);
            rectangle.Height = (int)(resolution.Y * scale);
            return CenterRectangle(new Rectangle(Point.Zero, bounds), rectangle);
        }

        static Rectangle CenterRectangle(Rectangle outerRectangle, Rectangle innerRectangle)
        {
            Point delta = outerRectangle.Center - innerRectangle.Center;
            innerRectangle.Offset(delta);
            return innerRectangle;
        }

        #endregion
    }
}