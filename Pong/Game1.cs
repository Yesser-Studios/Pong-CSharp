﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Pong
{
    public class Game1 : Game
    {
        private Point gameResolution = new(480, 360);

        private RenderTarget2D _renderTarget;
        private Rectangle _renderTargetDest;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private GameObject ball;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.IsFullScreen = true;
            _graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            ball = new(
                Content.Load<Texture2D>("Ball"),
                new(gameResolution.X / 2, gameResolution.Y / 2),
                2f,
                _spriteBatch);

            _renderTarget = new(GraphicsDevice, gameResolution.X, gameResolution.Y);
            _renderTargetDest = GetRenderTargetDestination(gameResolution, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(_renderTarget);

            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            ball.Draw();
            _spriteBatch.End();

            base.Draw(gameTime);

            GraphicsDevice.SetRenderTarget(null);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_renderTarget, _renderTargetDest, Color.White);
            _spriteBatch.End();
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
    }
}