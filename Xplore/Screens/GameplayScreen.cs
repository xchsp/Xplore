using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Xplore.Screens
{
    public class GameplayScreen : Screen
    {
        private readonly Random _rand = new Random();
        private readonly List<Enemy> _enemies = new List<Enemy>();
        private readonly List<Boulder> _boulders = new List<Boulder>(); 
        private MouseState _previousMouseState;
        private KeyboardState _previousKeyboardState;
        private MouseState _mouseState;
        private readonly Player _player;
        private SpatialGrid _spatialGrid;
        private const int MaxEnemyCount = 100;
        private const int MaxBoulderCount = 50;
         
        private Dictionary<string,ICollisionEntity> _collisionEntities = new Dictionary<string, ICollisionEntity>(); 

        private const int GameSize = 8000;
        private Rectangle _gameBounds = new Rectangle(-(GameSize / 2), -(GameSize / 2), GameSize, GameSize);
        private KeyboardState _keyboardState;
        private Rectangle _backgroundRect => new Rectangle(_gameBounds.X-(_gameBounds.Width/2),_gameBounds.Y-(_gameBounds.Height/2),_gameBounds.Width*2,_gameBounds.Height*2);
        public override void LoadContent()
        {

        }

        public override void UnloadContent()
        {

        }

        private void EnemyShipDestroyed(object ship, EventArgs eventArgs)
        {
            _collisionEntities.Remove(((Enemy) ship).Guid.ToString());
             _enemies.Remove((Enemy)ship);
        }

        public void SpawnEnemies()
        {
            //spawn an emeny if the count of total enemies is less than maxEnemyCount
            while (_enemies.Count < MaxEnemyCount)
            {
                float radius = (float)_rand.Next(_gameBounds.Width / 2, _gameBounds.Width) / 2;
                float angle = (float)_rand.NextDouble() * MathHelper.TwoPi;
                float x = _player.Center.X + radius * (float)Math.Cos(angle);
                float y = _player.Center.Y + radius * (float)Math.Sin(angle);

                var enemy = new Enemy(ContentProvider.EnemyShips[_rand.Next(ContentProvider.EnemyShips.Count)],
                    new Vector2(x, y),
                    ShipType.NpcEnemy);
                enemy.Wander();
                _collisionEntities.Add(enemy.Guid.ToString(),enemy);
                enemy.Destroyed += EnemyShipDestroyed;
                _enemies.Add(enemy);
            }


        }

        public void SpawnBoulders()
        {
            while (_boulders.Count < MaxBoulderCount)
            {
                float radius = (float)_rand.Next(_gameBounds.Width / 2, _gameBounds.Width) / 2;
                float angle = (float)_rand.NextDouble() * MathHelper.TwoPi;
                float x = _player.Center.X + radius * (float)Math.Cos(angle);
                float y = _player.Center.Y + radius * (float)Math.Sin(angle);

                var boulder = new Boulder(ContentProvider.Boulder,
                    new Vector2(x, y));
                _collisionEntities.Add(boulder.Guid.ToString(), boulder);
                _boulders.Add(boulder);
            }
        }

        public void DespawnSprites()
        {
            var boulders = _boulders.ToArray();
            var enemies = _enemies.ToArray();
            foreach (var boulder in boulders)
            {
                if (IsSpriteOutSideGameBounds(boulder))
                {
                    _boulders.Remove(boulder);
                    _collisionEntities.Remove(boulder.Guid.ToString());
                }
            }
            foreach (var enemy in enemies)
            {
                if (IsSpriteOutSideGameBounds(enemy))
                {
                    //we need to cleanup the ship current particles
                    enemy.CleanupParticles();
                    _collisionEntities.Remove(enemy.Guid.ToString());
                    _enemies.Remove(enemy);
                }
            }

        }

        public bool IsSpriteOutSideGameBounds(Sprite sprite)
        {
            return sprite.Position.X < _gameBounds.X || sprite.Position.X > _gameBounds.X + _gameBounds.Width || sprite.Position.Y < _gameBounds.Y || sprite.Position.Y > _gameBounds.Y + _gameBounds.Height;
        }


        public void ApplyMouseWheelZoom()
        {
            //zoom in/out
            if (_mouseState.ScrollWheelValue > _previousMouseState.ScrollWheelValue)
            {
                Camera.Zoom += 0.05f;
            }
            if (_mouseState.ScrollWheelValue < _previousMouseState.ScrollWheelValue)
            {
                Camera.Zoom -= 0.05f;
            }
        }

        public void UpdateGameBounds()
        {

            _gameBounds = new Rectangle((int)(_player.Position.X - GameSize/2f), (int)(_player.Position.Y - GameSize/2f),GameSize,GameSize);
            
        }

        //TODO there is an issue where either collision entities or sprites are not being correctly removed from the dictionary of objects requiring updates
        public override void Update(GameTime gameTime)
        {
            UpdateGameBounds();
            _spatialGrid = new SpatialGrid(_gameBounds,200);
            _keyboardState = Keyboard.GetState();
            _mouseState = Mouse.GetState();
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                GameManager.PauseGame();

            if (_keyboardState.IsKeyDown(Keys.K) && !_previousKeyboardState.IsKeyDown(Keys.K))
            {
                GameManager.ToggleDebug();
            }

            ApplyMouseWheelZoom();
            Camera.Location = new Vector2(_player.Position.X, _player.Position.Y);
            UpdateLazersCollsions();
            SpawnEnemies();
            SpawnBoulders();
            DespawnSprites();
            _player.Update(gameTime);

            var enemies = _enemies.ToArray();
            for (int i = 0; i < enemies.Length; i++)
            {
                var fleeDistance = 1000f;
                if ((_player.Center - enemies[i].Center).Length() < fleeDistance)
                {
                    enemies[i].Flee(_player.Center, fleeDistance);
                }
                enemies[i].Update(gameTime);
                
            }

            foreach (var boulder in _boulders)
            {
                boulder.Update(gameTime);
            }

            CheckAndResolveCollisions(gameTime);


            _previousMouseState = _mouseState;
            _previousKeyboardState = _keyboardState;
        }

        private IEnumerable<Ship> GetAllShips()
        {
            var ships = new List<Ship>();
            ships.AddRange(_enemies);
            ships.Add(_player);
            return ships;
        }

        private void UpdateLazersCollsions()
        {
            foreach (var ship in GetAllShips())
            {
                foreach (var lazerBullet in ship.LazerBullets)
                {
                    var guid = lazerBullet.Guid.ToString();
                    if (!_collisionEntities.ContainsKey(guid))
                    {
                        _collisionEntities.Add(guid, lazerBullet);
                    }
                }
            }
            var collisionEntityArray = _collisionEntities.ToArray();
            for (int i = 0; i < collisionEntityArray.Length; i++)
            {
                if (!collisionEntityArray[i].Value.Active)
                {
                    _collisionEntities.Remove(collisionEntityArray[i].Key);
                }
            }

            
        }

        /// <summary>
        /// we need to add a broad and narrow phase instead of checking O(n^2)
        /// </summary>
        private void CheckAndResolveCollisions(GameTime gameTime)
        {
            
            var loopCount = 0;
            foreach (var collisionEntity in _collisionEntities.Values)
            {
                _spatialGrid.Insert(collisionEntity.BoundingCircle.SourceRectangle,collisionEntity);
            }

            foreach (var gridBox in _spatialGrid.GetCollsionGrid().Values)
            {
                List<string> checkedCollisions = new List<string>();
                if (gridBox.Count > 1)
                {
                    foreach (var collisionEntity in gridBox)
                    {
                        


                        foreach (var entity in gridBox)
                        {

                            var collisionIds =
                                    new string[] { collisionEntity.Guid.ToString(), entity.Guid.ToString() }.OrderBy(
                                        a => a);
                            var value = string.Join("", collisionIds);

                            //if we have already performed a collision check for these two objects skip check logic
                            //also if the compare objects are the same don't bother checking...
                            if (checkedCollisions.Contains(value) || entity == collisionEntity)
                            {
                                continue;
                            }
                            loopCount++;
                            Vector2 collsionVector;
                            if (CollisionHelper.IsCircleColliding(entity.BoundingCircle,collisionEntity.BoundingCircle,out collsionVector))
                            {
                                if (collisionEntity.CollisionsWith == CollisionType.Lazer ||
                                    entity.CollisionsWith == CollisionType.Lazer)
                                {
                                    
                                }
                                else
                                {
                                    _collisionEntities[entity.Guid.ToString()].ResolveSphereCollision(collsionVector);
                                }

                                var damage = 0;
                                
                                switch (entity.CollisionsWith)
                                {
                                    case CollisionType.None:
                                        damage = 0;
                                        break;
                                    case CollisionType.Ship:
                                        damage = 1;
                                        break;
                                    case CollisionType.Boulder:
                                        damage = 3;
                                        break;
                                    case CollisionType.Lazer:
                                        damage = 4;
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                                _collisionEntities[collisionEntity.Guid.ToString()].ApplyCollisionDamage(gameTime, damage);
                                _collisionEntities[entity.Guid.ToString()].ApplyCollisionDamage(gameTime,0);

                                //entity is colliding with collision entity so add it to the computed list of collisions
                                
                            }
                            checkedCollisions.Add(value);
                        }
                        
                    }
                }
            }

            Debug.WriteLine(loopCount);
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            var backgroundRect = _backgroundRect;
            spriteBatch.Draw(ContentProvider.Background, new Vector2(backgroundRect.X, backgroundRect.Y), backgroundRect, Color.White, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
            _player.Draw(spriteBatch);
            //_spatialGrid.RenderGrid(spriteBatch);
            foreach (var enemy in _enemies)
            {
                enemy.Draw(spriteBatch);
            }
            foreach (var boulder in _boulders)
            {
                boulder.Draw(spriteBatch);
            }
        }

        public GameplayScreen(bool active, Main game) : base(active, game)
        {
            UserInterface = false;
            ScreenType = ScreenType.Gameplay;
            Camera.Bounds = Game.GraphicsDevice.Viewport.Bounds;
            _player = new Player(ContentProvider.Ship, new Vector2(0, 0), ShipType.Player);
            _collisionEntities.Add(_player.Guid.ToString(), _player);
        }
    }
}