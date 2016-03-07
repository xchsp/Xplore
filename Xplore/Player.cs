using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Xplore
{

    public class Player : Ship, IShip
    {

        public Player(Texture2D texture, Vector2 position, Rectangle screenBounds) : base(texture, position, screenBounds)
        {
            DirectionVector = new Vector2(0, -1);
            DirectionGoalVector = DirectionVector;
        }

        public override void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();

            VelocityGoal = Vector2.Zero;
            var vector = Camera.GetWorldPosition(new Vector2(mouseState.X, mouseState.Y));

            DirectionGoalVector.X = vector.X - (BoundingBox.Center.X - BoundingBox.Width / 2f);
            DirectionGoalVector.Y = vector.Y - (BoundingBox.Center.Y - BoundingBox.Height / 2f);
            DirectionGoalVector.Normalize();
            rotation = (float)DirectionVector.GetRotationFromVector();

            if (mouseState.LeftButton == ButtonState.Pressed && gameTime.TotalGameTime.TotalMilliseconds > LastFire + 200)
            {
                LastFire = gameTime.TotalGameTime.TotalMilliseconds;
                Fire();
            }

            if (keyboardState.IsKeyDown(Keys.W))
            {
                //CreateExhaustParticles();
                VelocityGoal = (DirectionVector) * Speed;
            }

            if (keyboardState.IsKeyDown(Keys.S))
            {
                VelocityGoal += (DirectionVector) * -Speed;
            }
            if (keyboardState.IsKeyDown(Keys.D))
            {
                VelocityGoal += new Vector2(-DirectionVector.Y, DirectionVector.X) * Speed;
            }
            else if (keyboardState.IsKeyDown(Keys.A))
            {
                VelocityGoal += new Vector2(DirectionVector.Y, -DirectionVector.X) * Speed;
            }

            DirectionVector = Vector2.Lerp(DirectionGoalVector, DirectionVector, RotationSpeed);
            Velocity = Vector2.Lerp(VelocityGoal, Velocity, 0.99f);
            //scale = Vector2.Lerp(scaleGoal,scale,0.995f);

            base.Update(gameTime);
        }

        
    }



    public static class Vector2DEx
    {
        public static double GetRotationFromVector(this Vector2 unitLengthVector)
        {
            return (float)Math.Atan2(unitLengthVector.X, -unitLengthVector.Y);
        }

        public static Vector2 RotateAboutOrigin(this Vector2 point, Vector2 origin, float rotation)
        {
            return Vector2.Transform(point,
                Matrix.CreateTranslation(-origin.X, -origin.Y, 0f) *
                Matrix.CreateRotationZ(rotation) *
                Matrix.CreateTranslation(origin.X, origin.Y, 0f)
                );
        }

    }

}