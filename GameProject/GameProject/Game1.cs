using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameProject
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        // game objects. Using inheritance would make this
        // easier, but inheritance isn't a GDD 1200 topic
        Burger burger;
        List<TeddyBear> bears = new List<TeddyBear>();
        static List<Projectile> projectiles = new List<Projectile>();
        List<Explosion> explosions = new List<Explosion>();

        // projectile and explosion sprites. Saved so they don't have to
        // be loaded every time projectiles or explosions are created
        static Texture2D frenchFriesSprite;
        static Texture2D teddyBearProjectileSprite;
        static Texture2D explosionSpriteStrip;

        // scoring support
        int score = 0;
        string scoreString = GameConstants.ScorePrefix + 0;

        //Window Width and Window Height.        
        int windowWidth = GameConstants.WindowWidth;
        int windowHeight = GameConstants.WindowHeight;

        // health support
        string healthString = GameConstants.HealthPrefix + GameConstants.BurgerInitialHealth;
        bool burgerDead = false;

        // text display support
        SpriteFont font;

        // sound effects
        SoundEffect burgerDamage;
        SoundEffect burgerDeath;
        SoundEffect burgerShot;
        SoundEffect explosion;
        SoundEffect teddyBounce;
        SoundEffect teddyShot;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // set resolution
            graphics.PreferredBackBufferWidth = GameConstants.WindowWidth;
            graphics.PreferredBackBufferHeight = GameConstants.WindowHeight;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            RandomNumberGenerator.Initialize();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // load audio content
            burgerDamage = Content.Load<SoundEffect>(@"Audio\BurgerDamage");
            burgerDeath = Content.Load<SoundEffect>(@"Audio\BurgerDeath");
            burgerShot = Content.Load<SoundEffect>(@"Audio\BurgerShot");
            explosion = Content.Load<SoundEffect>(@"Audio\Explosion");
            teddyBounce = Content.Load<SoundEffect>(@"Audio\TeddyBounce");
            teddyShot = Content.Load<SoundEffect>(@"Audio\TeddyShot");

            // load sprite font
            font = Content.Load<SpriteFont>(@"Fonts\Arial20");

            // load projectile and explosion sprites
            frenchFriesSprite = Content.Load<Texture2D>(@"graphics\frenchfries");
            teddyBearProjectileSprite = Content.Load<Texture2D>(@"graphics\teddybearprojectile");
            explosionSpriteStrip = Content.Load<Texture2D>(@"graphics\explosion");

            // add initial game objects
            burger = new Burger(Content, @"graphics\burger", GameConstants.WindowWidth / 2, GameConstants.WindowHeight * 7 / 8, burgerShot);
            int bearCount = 0;
            while (bearCount != GameConstants.MaxBears)
            {
                SpawnBear();
                bearCount++;
            }

            // set initial health and score strings
            burger.Health = GameConstants.BurgerInitialHealth;
            healthString = GameConstants.HealthPrefix + burger.Health;
            scoreString = GameConstants.ScorePrefix + score;
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
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // get current mouse state and update burger            
            MouseState mouse = Mouse.GetState();            
            //KeyboardState keyboard = Keyboard.GetState();
            burger.Update(gameTime, mouse);
            // update other game objects            
            foreach (TeddyBear bear in bears)
            {
                bear.Update(gameTime);
            }
            foreach (Projectile projectile in projectiles)
            {
                projectile.Update(gameTime);
            }
            foreach (Explosion exploded in explosions)
            {
                exploded.Update(gameTime);
            }
            // check and resolve collisions between teddy bears            
            for (int i = 0; i < bears.Count; i++)
            {
                for (int j = i + 1; j < bears.Count; j++)
                {
                    if (bears[i].Active && bears[j].Active)
                    {
                        CollisionResolutionInfo resolutionInfo = CollisionUtils.CheckCollision(gameTime.ElapsedGameTime.Milliseconds, windowWidth, windowHeight, bears[i].Velocity, bears[i].CollisionRectangle, bears[j].Velocity, bears[j].CollisionRectangle);
                        if (resolutionInfo != null)
                        {
                            if (resolutionInfo.FirstOutOfBounds)
                            {
                                bears[i].Active = false;
                            }
                            else
                            {
                                bears[i].Velocity = resolutionInfo.FirstVelocity;
                                bears[i].DrawRectangle = resolutionInfo.FirstDrawRectangle;
                            }
                            if (resolutionInfo.SecondOutOfBounds)
                            {
                                bears[j].Active = false;
                            }
                            else
                            {
                                bears[j].Velocity = resolutionInfo.SecondVelocity;
                                bears[j].DrawRectangle = resolutionInfo.SecondDrawRectangle;
                            }
                        }
                    }
                }
            }
            // check and resolve collisions between burger and teddy bears            
            foreach (TeddyBear bearC in bears)
            {
                if (bearC.Active == true && bearC.CollisionRectangle.Intersects(burger.CollisionRectangle))
                {
                    //Scoring                    
                    score += GameConstants.BearPoints;
                    scoreString = GameConstants.ScorePrefix + score;
                    burger.Health -= GameConstants.BearDamage;
                    healthString = GameConstants.HealthPrefix + burger.Health;
                    bearC.Active = false;
                    Explosion bomba = new Explosion(explosionSpriteStrip, bearC.CollisionRectangle.X, bearC.CollisionRectangle.Y, explosion);
                    explosions.Add(bomba);
                    //burger sound effect.                    
                    burgerDamage.Play();
                    CheckBurgerKill();
                }
            }
            // check and resolve collisions between burger and projectiles            
            foreach (Projectile projectileC in projectiles)
            {
                if (projectileC.Active == true && projectileC.Type == ProjectileType.TeddyBear && projectileC.CollisionRectangle.Intersects(burger.CollisionRectangle))
                {
                    burger.Health -= GameConstants.TeddyBearProjectileDamage; healthString = GameConstants.HealthPrefix + burger.Health; projectileC.Active = false;
                    //burger sound effect.                    
                    burgerDamage.Play();
                    CheckBurgerKill();
                }
            }
            // check and resolve collisions between teddy bears and projectiles            
            foreach (TeddyBear bearB in bears)
            {
                foreach (Projectile projectileB in projectiles)
                {
                    if (projectileB.Type == ProjectileType.FrenchFries && projectileB.Active && bearB.Active && (bearB.CollisionRectangle.Intersects(projectileB.CollisionRectangle)))
                    {
                        //Scoring                        
                        score += GameConstants.BearPoints;
                        scoreString = GameConstants.ScorePrefix + score;
                        //Explosion occurs.                        
                        Explosion explode = new Explosion(explosionSpriteStrip, bearB.CollisionRectangle.X, bearB.CollisionRectangle.Y, explosion);
                        explosions.Add(explode);
                        bearB.Active = false;
                        projectileB.Active = false;
                    }
                }
            }
            // clean out inactive teddy bears and add new ones as necessary            
            for (int x = bears.Count - 1; x >= 0; x--)
            {
                if (!(bears[x].Active))
                {
                    bears.RemoveAt(x);
                    while (bears.Count != GameConstants.MaxBears)
                    {
                        SpawnBear();
                    }
                }
            }
            // clean out inactive projectiles            
            for (int y = projectiles.Count - 1; y >= 0; y--)
            {
                if (!(projectiles[y].Active))
                {
                    projectiles.RemoveAt(y);
                }
            }
            // clean out finished explosions            
            for (int z = explosions.Count - 1; z >= 0; z--)
            {
                if ((explosions[z].Finished))
                {
                    explosions.RemoveAt(z);
                }
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            // draw game objects            
            burger.Draw(spriteBatch);
            foreach (TeddyBear bear in bears)
            {
                bear.Draw(spriteBatch);
            }
            foreach (Projectile projectile in projectiles)
            {
                projectile.Draw(spriteBatch);
            }
            foreach (Explosion explosionB in explosions)
            {
                explosionB.Draw(spriteBatch);
            }
            // draw score and health            
            spriteBatch.DrawString(font, healthString, GameConstants.HealthLocation, Color.White);
            spriteBatch.DrawString(font, scoreString, GameConstants.ScoreLocation, Color.White);

            spriteBatch.End();

            base.Draw(gameTime);
        }

        #region Public methods

        /// <summary>
        /// Gets the projectile sprite for the given projectile type
        /// </summary>
        /// <param name="type">the projectile type</param>
        /// <returns>the projectile sprite for the type</returns>
        public static Texture2D GetProjectileSprite(ProjectileType type)
        {
            // replace with code to return correct projectile sprite based on projectile type            
            Texture2D currentSprite; if (type == ProjectileType.FrenchFries)
            {
                currentSprite = frenchFriesSprite;
            }
            else
            {
                currentSprite = teddyBearProjectileSprite;
            }
            return currentSprite;
        }
        /// <summary>        
		/// Adds the given projectile to the game        
		/// </summary>        
		/// <param name="projectile">the projectile to add</param>        
		public static void AddProjectile(Projectile projectile)
        {
            projectiles.Add(projectile);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Spawns a new teddy bear at a random location
        /// </summary>
        private void SpawnBear()
        {
            // generate random location            
            int randX = RandomNumberGenerator.Next(GameConstants.SpawnBorderSize);
            int randY = RandomNumberGenerator.Next(GameConstants.SpawnBorderSize);
            // generate random velocity            
            float randSpeed = GameConstants.MinBearSpeed + RandomNumberGenerator.NextFloat(GameConstants.BearSpeedRange);
            float randAngle = RandomNumberGenerator.NextFloat(2*(float)Math.PI);
            Vector2 randVelocity = new Vector2(randSpeed * (float)Math.Cos(randAngle), randSpeed * (float)Math.Sin(randAngle));
            // create new bear            
            TeddyBear newBear = new TeddyBear(Content, @"graphics\teddybear", randX, randY, randVelocity, teddyBounce, teddyShot);
            // make sure we don't spawn into a collision            
            List<Rectangle> CollisionRectangles = GetCollisionRectangles();
            while (CollisionUtils.IsCollisionFree(newBear.CollisionRectangle, CollisionRectangles) != true)
            {
                newBear.X = GetRandomLocation(GameConstants.SpawnBorderSize, graphics.PreferredBackBufferWidth - 2 * GameConstants.SpawnBorderSize);
                newBear.Y = GetRandomLocation(GameConstants.SpawnBorderSize, graphics.PreferredBackBufferHeight - 2 * GameConstants.SpawnBorderSize);
            }
            // add new bear to list            
            bears.Add(newBear);
        }

        /// <summary>
        /// Gets a random location using the given min and range
        /// </summary>
        /// <param name="min">the minimum</param>
        /// <param name="range">the range</param>
        /// <returns>the random location</returns>
        private int GetRandomLocation(int min, int range)
        {
            return min + RandomNumberGenerator.Next(range);
        }

        /// <summary>
        /// Gets a list of collision rectangles for all the objects in the game world
        /// </summary>
        /// <returns>the list of collision rectangles</returns>
        private List<Rectangle> GetCollisionRectangles()
        {
            List<Rectangle> collisionRectangles = new List<Rectangle>();
            collisionRectangles.Add(burger.CollisionRectangle);
            foreach (TeddyBear bear in bears)
            {
                collisionRectangles.Add(bear.CollisionRectangle);
            }
            foreach (Projectile projectile in projectiles)
            {
                collisionRectangles.Add(projectile.CollisionRectangle);
            }
            foreach (Explosion explosion in explosions)
            {
                collisionRectangles.Add(explosion.CollisionRectangle);
            }
            return collisionRectangles;
        }

        /// <summary>
        /// Checks to see if the burger has just been killed
        /// </summary>
        private void CheckBurgerKill()
        {
            if (burger.Health == 0 && burgerDead == false)
            {
                burgerDead = true;
                burgerDeath.Play();
            }
        }

        #endregion
    }
}
