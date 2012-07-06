using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using JigLibX.Physics;
using JigLibX.Collision;
using JigLibX.Geometry;
using JigLibX.Math;

namespace Gaia.Physics
{
    public struct State
    {
        public Vector3 position;
        public Vector3 velocity;

        public State(Vector3 pos, Vector3 vel)
        {
            position = pos;
            velocity = vel;
        }
    };

    public struct Derivative
    {
        public Vector3 dx;
        public Vector3 dv;
    };

    public static class PhysicsHelper
    {

        /*
        public static RigidBody PhysicsBodiesVolume(BoundingBox bounds, World physicsEngine)
        {
            IEnumerator<RigidBody> rigidBodies = physicsEngine.RigidBodies.GetEnumerator();
            while(rigidBodies
            physicsEngine.RigidBodies.GetEnumerator().Current 
        }
        */

        public static Vector3 SetMass(float mass, Body body, CollisionSkin collision)
        {
            PrimitiveProperties primitiveProperties =
                new PrimitiveProperties(PrimitiveProperties.MassDistributionEnum.Solid, PrimitiveProperties.MassTypeEnum.Density, mass);

            float junk;
            Vector3 com;
            Matrix it, itCoM;

            collision.GetMassProperties(primitiveProperties, out junk, out com, out it, out itCoM);
            body.BodyInertia = itCoM;
            body.Mass = junk;

            return com;
        }

        public static Derivative Evaluate(State initial, Vector3 acceleration, float dt, Derivative d)
        {
            State state;
            state.position = initial.position + d.dx * dt;
            state.velocity = initial.velocity + d.dv * dt;
            Derivative output;
            output.dx = state.velocity;
            output.dv = acceleration;
            return output;
        }

        public static State Integrate(State oldState, Vector3 accel, float timeDT)
        {
            Derivative orig;
            orig.dx = Vector3.Zero;
            orig.dv = Vector3.Zero;
            Derivative a = Evaluate(oldState, accel, 0, orig);
            Derivative b = Evaluate(oldState, accel, timeDT * 0.5f, a);
            Derivative c = Evaluate(oldState, accel, timeDT * 0.5f, b);
            Derivative d = Evaluate(oldState, accel, timeDT, c);

            Vector3 dxdt = 1.0f / 6.0f * (a.dx + 2.0f * (b.dx + c.dx) + d.dx);
            Vector3 dvdt = 1.0f / 6.0f * (a.dv + 2.0f * (b.dv + c.dv) + d.dv);

            State newState;
            newState.position = oldState.position + dxdt * timeDT;
            newState.velocity = oldState.velocity + dvdt * timeDT;
            return newState;
        }

        public static State GainsFromFrequencies(Vector3 frequency, Vector3 dampingRatio, float mass)
        {
            State gainsState = new State();
            gainsState.position = mass * frequency * frequency;
            gainsState.velocity = dampingRatio * 2 * mass * frequency;
            return gainsState;
        }

        public static State GainsFromFrequencies(Vector3 frequency, Vector3 dampingRatio)
        {
            return GainsFromFrequencies(frequency, dampingRatio, 1.0f);
        }

        public static State PIDMotionPlan(State oldState, State goalState, Vector3 acceleration, State gainsState, float timeDT)
        {
            Vector3 pidAccel = acceleration - gainsState.velocity * (oldState.velocity - goalState.velocity) - gainsState.position * (oldState.position - goalState.position);
            return Integrate(oldState, pidAccel, timeDT);
        }
    }
}
