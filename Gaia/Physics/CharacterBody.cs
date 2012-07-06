using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using JigLibX.Physics;
using JigLibX.Collision;
using JigLibX.Geometry;
using JigLibX.Math;

namespace Gaia.Physics
{
    class ASkinPredicate : CollisionSkinPredicate1
    {
        public override bool ConsiderSkin(CollisionSkin skin0)
        {
            if (!(skin0.Owner is CharacterBody))
                return true;
            else
                return false;
        }
    }

    public class CharacterBody : Body
    {
        public CharacterBody()
            : base()
        {
        }

        float jumpForce = 16;
        public Vector3 DesiredVelocity { get; set; }

        private bool doJump = false;

        public void Jump(float _jumpForce)
        {
            doJump = true;
            jumpForce = _jumpForce;
        }

        public override void AddExternalForces(float dt)
        {
            ClearForces();

            if (doJump)
            {
                foreach (CollisionInfo info in CollisionSkin.Collisions)
                {
                    Vector3 N = info.DirToBody0;
                    if (this == info.SkinInfo.Skin1.Owner)
                        Vector3.Negate(ref N, out N);

                    if (Vector3.Dot(N, Orientation.Up) > 0.7f)
                    {
                        Vector3 vel = Velocity; vel.Y = jumpForce;
                        Velocity = vel;
                        break;
                    }
                }
            }

            foreach (CollisionInfo info in CollisionSkin.Collisions)
            {
                Vector3 N = info.DirToBody0;
                if (this == info.SkinInfo.Skin1.Owner)
                    Vector3.Negate(ref N, out N);
            }

            Vector3 deltaVel = DesiredVelocity - Velocity;

            bool running = true;

            if (DesiredVelocity.LengthSquared() < JiggleMath.Epsilon) running = false;
            else deltaVel.Normalize();

            deltaVel.Y = 0.0f;

            // start fast, slow down slower
            if (running) deltaVel *= 10.0f;
            else deltaVel *= 2.0f;

            float forceFactor = 1000.0f;
            AddBodyForce(deltaVel * Mass * dt * forceFactor);

            doJump = false;
            AddGravityToExternalForce();
        }

    }
}
