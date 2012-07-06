#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Collision;
using JigLibX.Geometry;
using JigLibX.Math;
using System.Collections.ObjectModel;
using System.Diagnostics;
#endregion

namespace JigLibX.Physics
{

    /// <summary>
    ///Looks after (but doesn't own) a collection of bodies and runs
    ///their updates. Doesn't deal with collision detection - it will
    ///get passes a collision detector to do that.
    ///In the vast majority of cases there will be only one physics system,
    ///and it is consequently very annoying if every object has to keep
    ///track of which physics system it's associated with. Therefore,
    ///PhysicsSystem supports a "singleton" style use, but it also lets
    ///the user change the "current" physics system (bad things will
    ///happen if you set it to zero whilst there are still physical objects!).
    ///If you want more than one physics system, then in your update loop set
    ///the physics system to the first one, run physics, then set it to
    ///the second one etc.
    ///Note that the physics system constructor and destructor will set the
    ///current physics system variable, so under normal circumstances you don't
    ///need to worry about this.
    /// </summary>
    public class PhysicsSystem
    {

        public enum Solver
        {
            Fast,
            Normal,
            Combined,
            Accumulated
        }

        #region private class Contact
        // A class can be handled much faster within lists.
        private class Contact
        {
            #region private struct BodyPair

            public struct BodyPair
            {
                public Body BodyA;
                public Body BodyB;
                public Vector3 RA;
                /// <summary>
                /// Only the bodies are used for the
                /// comparison-
                /// Note that bodyB is likely to be null.
                /// </summary>
                /// <param name="bodyA"></param>
                /// <param name="bodyB"></param>
                /// <param name="rA"></param>
                /// <param name="rB"></param>
                public BodyPair(Body bodyA, Body bodyB, ref Vector3 rA, ref Vector3 rB)
                {

                    // if (bodyA > bodyB) {mBodyA = bodyA; mBodyB = bodyB; mRA = rA;}
                    // else {mBodyA = bodyB; mBodyB = bodyA; mRA = rB;}
                    int skinId = -1;
                    if (bodyB != null) skinId = bodyB.ID;

                    if (bodyA.ID > skinId)
                    {
                        this.BodyA = bodyA; this.BodyB = bodyB; this.RA = rA;
                    }
                    else
                    {
                        this.BodyA = bodyB; this.BodyB = bodyA; this.RA = rB;
                    }
                }

                public BodyPair(Body bodyA, Body bodyB)
                {

                    // if (bodyA > bodyB) {mBodyA = bodyA; mBodyB = bodyB; mRA = rA;}
                    // else {mBodyA = bodyB; mBodyB = bodyA; mRA = rB;}
                    int skinId = -1;
                    if (bodyB != null) skinId = bodyB.ID;

                    if (bodyA.ID > skinId)
                    {
                        this.BodyA = bodyA; this.BodyB = bodyB;
                    }
                    else
                    {
                        this.BodyA = bodyB; this.BodyB = bodyA;
                    }

                    this.RA = Vector3.Zero;
                }
            }

            #endregion

            #region private struct CachedImpulse
            public struct CachedImpulse
            {

                public float NormalImpulse;
                public float NormalImpulseAux;
                public Vector3 FrictionImpulse;

                public CachedImpulse(float normalImpulse, float normalImpulseAux, ref Vector3 frictionImpulse)
                {
                    this.NormalImpulse = normalImpulse;
                    this.NormalImpulseAux = normalImpulseAux;
                    this.FrictionImpulse = frictionImpulse;
                }

            }

            #endregion

            public BodyPair Pair;
            public CachedImpulse Impulse;

        }
        #endregion

        private CollisionSystem collisionSystem;

        private List<Body> bodies = new List<Body>();
        private List<Body> activeBodies = new List<Body>();
        private List<CollisionInfo> collisions = new List<CollisionInfo>();
        private List<Controller> controllers = new List<Controller>();
        private List<Constraint> constraints = new List<Constraint>();

        private List<Contact> catchedContacts = new List<Contact>();

        public List<CollisionIsland> islands = new List<CollisionIsland>();

        private delegate void PreProcessCollisionFn(CollisionInfo collision, float dt);
        private delegate bool ProcessCollisionFn(CollisionInfo collision, float dt, bool firstContact);

        private PreProcessCollisionFn preProcessContactFn;
        private ProcessCollisionFn processContactFn;
        private PreProcessCollisionFn preProcessCollisionFn;
        private ProcessCollisionFn processCollisionFn;

        private const float maxVelMag = 0.5f;
        private const float maxShockVelMag = 0.05f;
        private const float minVelForProcessing = 0.001f;
        private const float penetrationShockRelaxtionTimestep = 10.0f;

        private float targetTime = 0.0f;
        private float oldTime = 0.0f;

        private int numCollisionIterations = 4;
        private int numContactIterations = 12;
        private int numPenetrationRelaxtionTimesteps = 3;

        private float allowedPenetration = 0.01f;

        private bool doShockStep = false;

        private float collToll = 0.05f;

        private Solver solverType = Solver.Combined;

        private Vector3 gravity;
        private float gravityMagnitude;
        private int gravityAxis;

        private bool freezingEnabled = false;

        private bool nullUpdate;

        private static PhysicsSystem currentPhysicsSystem;

        /// <summary>
        /// Initializes a new PhysicSystem and makes it the current one.
        /// </summary>
        public PhysicsSystem()
        {
            CurrentPhysicsSystem = this;
            Gravity = -10.0f * Vector3.Up;
        }

        /// <summary>
        /// Returns a readonly collection of all Bodies registered. To add or remove Bodies
        /// use AddBody or RemoveBody.
        /// </summary>
        public ReadOnlyCollection<Body> Bodies
        {
            get { return bodies.AsReadOnly(); }
        }

        /// <summary>
        /// Returns a readonly collection of all Constraints registered. To add or remove 
        /// Constraints use AddConstraint or RemoveConstraint.
        /// </summary>
        public ReadOnlyCollection<Constraint> Constraints
        {
            get { return constraints.AsReadOnly(); }
        }

        /// <summary>
        /// Returns a readonly collection of all Controllers registered. To add or remove 
        /// Controllers use AddController or RemoveController.
        /// </summary>
        public ReadOnlyCollection<Controller> Controllers
        {
            get { return controllers.AsReadOnly(); }
        }

        /// <summary>
        /// Adds a constraint to the simulation.
        /// </summary>
        /// <param name="constraint">Constraint which should be added.</param>
        public void AddConstraint(Constraint constraint)
        {
            if (!constraints.Contains(constraint))
                constraints.Add(constraint);
            else
                System.Diagnostics.Debug.WriteLine("Warning: tried to add constraint to physics but it's already registered");
        }

        /// <summary>
        /// Removes a constraint form the simulation.
        /// </summary>
        /// <param name="constraint">The constraint which should be removed.</param>
        /// <returns>True if the constraint was successfully removed.</returns>
        public bool RemoveConstraint(Constraint constraint)
        {
            if (!constraints.Contains(constraint)) return false;
            constraints.Remove(constraint);
            return true;
        }

        /// <summary>
        /// Add a controller to the simulation.
        /// </summary>
        /// <param name="controller">Controller which should be added.</param>
        public void AddController(Controller controller)
        {
            if (!controllers.Contains(controller))
                controllers.Add(controller);
            else
                System.Diagnostics.Debug.WriteLine("Warning: tried to add controller to physics but it's already registered");
        }

        /// <summary>
        /// Removes a Controller from the simulation.
        /// </summary>
        /// <param name="controller">The Controller which should be removed.</param>
        /// <returns>True if the Controller was successfully removed.</returns>
        public bool RemoveController(Controller controller)
        {
            if (!controllers.Contains(controller)) return false;
            controllers.Remove(controller);
            return true;
        }

        /// <summary>
        /// Adds the body to the Physic- and the CollisionSystem
        /// </summary>
        /// <param name="body">Body which should be added to the simulation.</param>
        public void AddBody(Body body)
        {
            if (!bodies.Contains(body))
                bodies.Add(body);
            else
                System.Diagnostics.Debug.WriteLine("Warning: tried to add body to physics but it's already registered");

            if ((collisionSystem != null) && (body.CollisionSkin != null))
                collisionSystem.AddCollisionSkin(body.CollisionSkin);
        }

        /// <summary>
        /// Removes the body from the Physic- and the CollisionSystem
        /// </summary>
        /// <param name="body">Body which should be removed form the simulation</param>
        /// <returns>True if the Body was successfully removed.</returns>
        public bool RemoveBody(Body body)
        {
            if ((collisionSystem != null) && (body.CollisionSkin != null))
                collisionSystem.RemoveCollisionSkin(body.CollisionSkin);

            if (!bodies.Contains(body))
                return false;

            bodies.Remove(body);
            return true;
        }

        private void FindAllActiveBodies()
        {
            activeBodies.Clear();
            int numBodies = bodies.Count;
            for (int i = 0; i < numBodies; ++i)
            {
                if (bodies[i].IsActive)
                    activeBodies.Add(bodies[i]);
            }
        }

        private int LessBodyX(Body body0, Body body1)
        {
            return (body1.Position.X > body0.Position.X) ? -1 : 1;
        }

        private int LessBodyY(Body body0, Body body1)
        {
            return (body1.Position.Y > body0.Position.Y) ? -1 : 1;
        }

        private int LessBodyZ(Body body0, Body body1)
        {
            return (body1.Position.Z > body0.Position.Z) ? -1 : 1;
        }

        private void DoShockStep(float dt)
        {
            int numBodies = bodies.Count;

            if (System.Math.Abs(gravity.X) > System.Math.Abs(gravity.Y) && System.Math.Abs(gravity.X) > System.Math.Abs(gravity.Z))
                bodies.Sort(LessBodyX);
            else if (System.Math.Abs(gravity.Y) > System.Math.Abs(gravity.Z) && System.Math.Abs(gravity.Y) > System.Math.Abs(gravity.X))
                bodies.Sort(LessBodyY);
            else if (System.Math.Abs(gravity.Z) > System.Math.Abs(gravity.X) && System.Math.Abs(gravity.Z) > System.Math.Abs(gravity.Y))
                bodies.Sort(LessBodyZ);

            bool gotOne = true;
            int loops = 0;

            while (gotOne)
            {
                gotOne = false;
                ++loops;
                for (int ibody = 0; ibody < numBodies; ++ibody)
                {
                    Body body = bodies[ibody];
                    if (!body.Immovable && body.DoShockProcessing)
                    {
                        CollisionSkin skin = body.CollisionSkin;

                        if (skin != null)
                        {
                            List<CollisionInfo> colls = skin.Collisions;
                            int numColls = colls.Count;

                            if ((0 == numColls) || (!body.IsActive))
                            {
                                body.InternalSetImmovable();
                            }
                            else
                            {
                                bool setImmovable = false;
                                // process every collision on body that is between it and
                                // another immovable... then make it immovable (temporarily).
                                for (int i = 0; i < numColls; ++i)
                                {
                                    CollisionInfo info = colls[i];
                                    // only if this collision is against an immovable object then
                                    // process it
                                    if (((info.SkinInfo.Skin0 == body.CollisionSkin) &&
                                        ((info.SkinInfo.Skin1.Owner == null) ||
                                        (info.SkinInfo.Skin1.Owner.Immovable))) ||
                                        ((info.SkinInfo.Skin1 == body.CollisionSkin) &&
                                        ((info.SkinInfo.Skin0.Owner == null) ||
                                        (info.SkinInfo.Skin0.Owner.Immovable))))
                                    {
                                        // need to recalc denominator since immovable set
                                        preProcessCollisionFn(info, dt);
                                        ProcessCollisionsForShock(info, dt);
                                        setImmovable = true;
                                    }
                                }
                                if (setImmovable)
                                {
                                    body.InternalSetImmovable();
                                    gotOne = true;
                                }
                            }
                        }
                        else
                        {
                            // no skin - help early out next loop
                            body.InternalSetImmovable();
                        }
                    }
                }
            }

            for (int i = 0; i < numBodies; ++i)
                bodies[i].InternalRestoreImmovable();
        }

        private void GetAllExternalForces(float dt)
        {
            int numBodies = bodies.Count;
            for (int i = 0; i < numBodies; ++i)
                bodies[i].AddExternalForces(dt);

            int numControllers = controllers.Count;
            for (int i = 0; i < numControllers; ++i)
                controllers[i].UpdateController(dt);
        }

        private void UpdateAllVelocities(float dt)
        {
            int numBodies = bodies.Count;
            for (int i = 0; i < numBodies; ++i)
            {
                if (bodies[i].IsActive || bodies[i].VelChanged)
                    bodies[i].UpdateVelocity(dt);
            }
        }

        private void UpdateAllPositions(float dt)
        {
            int numBodies = activeBodies.Count;
            for (int i = 0; i < numBodies; ++i)
                activeBodies[i].UpdatePositionWithAux(dt);
        }

        private void CopyAllCurrentStatesToOld()
        {
            int numBodies = bodies.Count;
            for (int i = 0; i < numBodies; ++i)
            {
                if (bodies[i].IsActive || bodies[i].VelChanged)
                    bodies[i].CopyCurrentStateToOld();
            }
        }

        Random rand = new Random();
        private void DetectAllCollisions(float dt)
        {
            if (collisionSystem == null)
                return;

            int numBodies = bodies.Count;
            int numColls = collisions.Count;
            int numActiveBodies = activeBodies.Count;

            int i;

            for (i = 0; i < numActiveBodies; ++i)
                activeBodies[i].StoreState();

            UpdateAllVelocities(dt);
            UpdateAllPositions(dt);

            for (i = 0; i < numColls; ++i)
                CollisionInfo.FreeCollisionInfo(collisions[i]);

            collisions.Clear();

            for (i = 0; i < numBodies; ++i)
            {
                if (bodies[i].CollisionSkin != null)
                    bodies[i].CollisionSkin.Collisions.Clear();
            }

            BasicCollisionFunctor collisionFunctor = new BasicCollisionFunctor(collisions);
            collisionSystem.DetectAllCollisions(activeBodies, collisionFunctor, null, collToll);

            int index; CollisionInfo collInfo;
            for (i = 1; i < collisions.Count; i++)
            {
                index = rand.Next(i + 1);
                collInfo = collisions[i];
                collisions[i] = collisions[index];
                collisions[index] = collInfo;
            }

            for (i = 0; i < numActiveBodies; ++i)
                activeBodies[i].RestoreState();

        }

        private void NotifyAllPostPhysics(float dt)
        {
            int numBodies = bodies.Count;
            for (int i = 0; i < numBodies; ++i)
                bodies[i].PostPhysics(dt);
        }

        private void LimitAllVelocities()
        {
            int numActiveBodies = activeBodies.Count;
            for (int i = 0; i < numActiveBodies; ++i)
            {
                activeBodies[i].LimitVel();
                activeBodies[i].LimitAngVel();
            }
        }

        private bool ProcessCollision(CollisionInfo collision, float dt, bool firstContact)
        {
            collision.Satisfied = true;

            Body body0 = collision.SkinInfo.Skin0.Owner;
            Body body1 = collision.SkinInfo.Skin1.Owner;

            Vector3 N = collision.DirToBody0;

            bool gotOne = false;

            for (int pos = 0; pos < collision.NumCollPts; ++pos)
            {
                CollPointInfo ptInfo = collision.PointInfo[pos];
                float normalVel;

                if (body1 != null)
                {
                    Vector3 v0, v1;
                    body0.GetVelocity(ref ptInfo.info.R0, out v0);
                    body1.GetVelocity(ref ptInfo.info.R1, out v1);
                    Vector3.Subtract(ref v0, ref v1, out v0);
                    Vector3.Dot(ref v0, ref N, out normalVel);
                }
                else
                {
                    Vector3 v0;
                    body0.GetVelocity(ref ptInfo.info.R0, out v0);
                    Vector3.Dot(ref v0, ref N, out normalVel);
                }

                if (normalVel > ptInfo.MinSeparationVel)
                    continue;

                float finalNormalVel = -collision.MatPairProperties.Restitution * normalVel;

                if (finalNormalVel < minVelForProcessing)
                {
                    // could be zero elasticity in collision, or could be zero
                    // elasticity in contact - don't care.  relax towards 0
                    // penetration
                    finalNormalVel = ptInfo.MinSeparationVel;
                }

                float deltaVel = finalNormalVel - normalVel;

                if (deltaVel <= minVelForProcessing)
                    continue;

                if (ptInfo.Denominator < JiggleMath.Epsilon)
                {
                    ptInfo.Denominator = JiggleMath.Epsilon;
                }

                float normalImpulse = deltaVel / ptInfo.Denominator;

                // prepare our return value
                gotOne = true;
                Vector3 impulse;
                Vector3.Multiply(ref N, normalImpulse, out impulse);

                body0.ApplyBodyWorldImpulse(ref impulse, ref ptInfo.info.R0);

                if (body1 != null)
                    body1.ApplyNegativeBodyWorldImpulse(ref impulse, ref ptInfo.info.R1);

                // For friction, work out the impulse in the opposite direction to
                // the tangential velocity that would be required to bring this
                // point to a halt. Apply the minimum of this impulse magnitude,
                // and the one obtained from the normal impulse. This prevents
                // reversing the velocity direction.
                //
                // recalculate the velocity since it's changed.

                Vector3 vrNew;
                body0.GetVelocity(ref ptInfo.info.R0, out vrNew);

                if (body1 != null)
                {
                    Vector3 v1;
                    body1.GetVelocity(ref ptInfo.info.R1, out v1);
                    Vector3.Subtract(ref vrNew, ref v1, out vrNew);
                    //vrNew -= body1.GetVelocity(ptInfo.R1);
                }

                //Vector3 tangentVel = vrNew - Vector3.Dot(vrNew, N) * N;
                Vector3 tangentVel; float f1;
                Vector3.Dot(ref vrNew, ref N, out f1);
                Vector3.Multiply(ref N, f1, out tangentVel);
                Vector3.Subtract(ref vrNew, ref tangentVel, out tangentVel);

                float tangentSpeed = tangentVel.Length();

                if (tangentSpeed > minVelForProcessing)
                {
                    Vector3 T = -tangentVel / tangentSpeed;

                    // calculate an "inelastic collision" to zero the relative vel
                    float denominator = 0.0f;
                    if (!body0.Immovable)
                    {
                        #region INLINE: denominator = body0.InvMass + Vector3.Dot(T, Vector3.Cross(body0.WorldInvInertia * (Vector3.Cross(ptInfo.R0, T)), ptInfo.R0));
                        float num0 = (ptInfo.R0.Y * T.Z) - (ptInfo.R0.Z * T.Y);
                        float num1 = (ptInfo.R0.Z * T.X) - (ptInfo.R0.X * T.Z);
                        float num2 = (ptInfo.R0.X * T.Y) - (ptInfo.R0.Y * T.X);

                        float num3 = (((num0 * body0.worldInvInertia.M11) + (num1 * body0.worldInvInertia.M21)) + (num2 * body0.worldInvInertia.M31));
                        float num4 = (((num0 * body0.worldInvInertia.M12) + (num1 * body0.worldInvInertia.M22)) + (num2 * body0.worldInvInertia.M32));
                        float num5 = (((num0 * body0.worldInvInertia.M13) + (num1 * body0.worldInvInertia.M23)) + (num2 * body0.worldInvInertia.M33));

                        num0 = (num4 * ptInfo.R0.Z) - (num5 * ptInfo.R0.Y);
                        num1 = (num5 * ptInfo.R0.X) - (num3 * ptInfo.R0.Z);
                        num2 = (num3 * ptInfo.R0.Y) - (num4 * ptInfo.R0.X);

                        denominator = body0.InverseMass + ((num0 * T.X) + (num1 * T.Y) + (num2 * T.Z));
                        #endregion
                    }

                    if ((body1 != null) && (!body1.Immovable))
                    {
                        #region INLINE: denominator += body1.InvMass + Vector3.Dot(T, Vector3.Cross(body1.WorldInvInertia * (Vector3.Cross(ptInfo.R1, T)), ptInfo.R1));
                        float num0 = (ptInfo.R1.Y * T.Z) - (ptInfo.R1.Z * T.Y);
                        float num1 = (ptInfo.R1.Z * T.X) - (ptInfo.R1.X * T.Z);
                        float num2 = (ptInfo.R1.X * T.Y) - (ptInfo.R1.Y * T.X);

                        float num3 = (((num0 * body1.worldInvInertia.M11) + (num1 * body1.worldInvInertia.M21)) + (num2 * body1.worldInvInertia.M31));
                        float num4 = (((num0 * body1.worldInvInertia.M12) + (num1 * body1.worldInvInertia.M22)) + (num2 * body1.worldInvInertia.M32));
                        float num5 = (((num0 * body1.worldInvInertia.M13) + (num1 * body1.worldInvInertia.M23)) + (num2 * body1.worldInvInertia.M33));

                        num0 = (num4 * ptInfo.R1.Z) - (num5 * ptInfo.R1.Y);
                        num1 = (num5 * ptInfo.R1.X) - (num3 * ptInfo.R1.Z);
                        num2 = (num3 * ptInfo.R1.Y) - (num4 * ptInfo.R1.X);

                        denominator += body1.InverseMass + ((num0 * T.X) + (num1 * T.Y) + (num2 * T.Z));
                        #endregion
                    }

                    if (denominator > JiggleMath.Epsilon)
                    {
                        float impulseToReserve = tangentSpeed / denominator;

                        float impulseFromNormalImpulse =
                            collision.MatPairProperties.StaticFriction * normalImpulse;
                        float frictionImpulse;

                        if (impulseToReserve < impulseFromNormalImpulse)
                            frictionImpulse = impulseToReserve;
                        else
                            frictionImpulse = collision.MatPairProperties.DynamicFriction * normalImpulse;

                        T *= frictionImpulse;
                        body0.ApplyBodyWorldImpulse(ref T, ref ptInfo.info.R0);
                        if (body1 != null)
                            body1.ApplyNegativeBodyWorldImpulse(ref T, ref ptInfo.info.R1);

                    }

                } // end of friction

            }

            if (gotOne)
            {
                body0.SetConstraintsAndCollisionsUnsatisfied();
                if (body1 != null)
                    body1.SetConstraintsAndCollisionsUnsatisfied();
            }

            return gotOne;

        }

        private bool ProcessCollisionFast(CollisionInfo collision, float dt, bool firstContact)
        {
            collision.Satisfied = true;

            Body body0 = collision.SkinInfo.Skin0.Owner;
            Body body1 = collision.SkinInfo.Skin1.Owner;

            Vector3 N = collision.DirToBody0;

            bool gotOne = false;
            for (int pos = collision.NumCollPts; pos-- != 0; )
            {
                CollPointInfo ptInfo = collision.PointInfo[pos];

                float normalVel;
                if (body1 != null)
                {
                    //normalVel = Vector3.Dot(body0.GetVelocity(ptInfo.R0) - body1.GetVelocity(ptInfo.R1), collision.DirToBody0);
                    Vector3 v0, v1;
                    body0.GetVelocity(ref ptInfo.info.R0, out v0);
                    body1.GetVelocity(ref ptInfo.info.R1, out v1);
                    Vector3.Subtract(ref v0, ref v1, out v0);
                    normalVel = Vector3.Dot(v0, N);
                }
                else
                {
                    Vector3 v0;
                    body0.GetVelocity(ref ptInfo.info.R0, out v0);
                    normalVel = Vector3.Dot(v0, N);
                }

                if (normalVel > ptInfo.MinSeparationVel)
                    continue;

                float finalNormalVel = -collision.MatPairProperties.Restitution * normalVel;

                if (finalNormalVel < minVelForProcessing)
                {
                    // could be zero elasticity in collision, or could be zero
                    // elasticity in contact - don't care.  relax towards 0
                    // penetration
                    finalNormalVel = ptInfo.MinSeparationVel;
                }

                float deltaVel = finalNormalVel - normalVel;

                if (deltaVel < minVelForProcessing)
                    continue;

                float normalImpulse = deltaVel / ptInfo.Denominator;

                // prepare our return value
                gotOne = true;
                Vector3 impulse = normalImpulse * N;

                body0.ApplyBodyWorldImpulse(ref impulse, ref ptInfo.info.R0);
                if (body1 != null)
                    body1.ApplyNegativeBodyWorldImpulse(ref impulse, ref ptInfo.info.R1);

                // recalculate the velocity since it's changed.
                Vector3 vrNew = body0.GetVelocity(ptInfo.info.R0);
                if (body1 != null)
                    vrNew -= body1.GetVelocity(ptInfo.info.R1);

                Vector3 tangentVel = vrNew - Vector3.Dot(vrNew, N) * N;
                float tangentSpeed = tangentVel.Length();

                if (tangentSpeed > minVelForProcessing)
                {
                    Vector3 T = -tangentVel / tangentSpeed;

                    // calculate the "inelastic collision" to zeor the relative vel
                    float denominator = 0.0f;
                    if (!body0.Immovable)
                    {
                        //denominator = body0.InvMass +
                        //    Vector3.Dot(T, Vector3.Cross(Vector3.Transform(Vector3.Cross(ptInfo.R0, T), body0.WorldInvInertia), ptInfo.R0));
                        Vector3 v1; float f2;
                        Vector3.Cross(ref ptInfo.info.R0, ref T, out v1);
                        Vector3.Transform(ref v1, ref body0.worldInvInertia, out v1);
                        Vector3.Cross(ref v1, ref ptInfo.info.R0, out v1);
                        Vector3.Dot(ref T, ref v1, out f2);
                        denominator = body0.InverseMass + f2;
                    }

                    if ((body1 != null) && (!body1.Immovable))
                    {
                        //denominator += body1.InvMass +
                        //    Vector3.Dot(T, Vector3.Cross(Vector3.Transform(Vector3.Cross(ptInfo.R1, T), body1.WorldInvInertia), ptInfo.R1));
                        Vector3 v1; float f2;
                        Vector3.Cross(ref ptInfo.info.R1, ref T, out v1);
                        Vector3.Transform(ref v1, ref body1.worldInvInertia, out v1);
                        Vector3.Cross(ref v1, ref ptInfo.info.R1, out v1);
                        Vector3.Dot(ref T, ref v1, out f2);
                        denominator += body1.InverseMass + f2;
                    }

                    if (denominator > JiggleMath.Epsilon)
                    {
                        float impulseToReverse = tangentSpeed / denominator;
                        T *= impulseToReverse;
                        body0.ApplyBodyWorldImpulse(ref T, ref ptInfo.info.R0);
                        if (body1 != null)
                            body1.ApplyNegativeBodyWorldImpulse(ref T, ref ptInfo.info.R1);
                    }
                } // end of friction
            }

            if (gotOne)
            {
                body0.SetConstraintsAndCollisionsUnsatisfied();
                if (body1 != null)
                    body1.SetConstraintsAndCollisionsUnsatisfied();
            }

            return gotOne;

        }

        private bool ProcessCollisionAccumulated(CollisionInfo collision, float dt, bool firstContact)
        {
            collision.Satisfied = true;

            Body body0 = collision.SkinInfo.Skin0.Owner;
            Body body1 = collision.SkinInfo.Skin1.Owner;

            Vector3 N = collision.DirToBody0;

            bool gotOne = false;

            for (int pos = collision.NumCollPts; pos-- != 0; )
            {
                CollPointInfo ptInfo = collision.PointInfo[pos];
                float normalImpulse;

                // first the real impulse
                {
                    float normalVel;
                    if (body1 != null)
                    {
                        Vector3 v0, v1;
                        body0.GetVelocity(ref ptInfo.info.R0, out v0);
                        body1.GetVelocity(ref ptInfo.info.R1, out v1);
                        Vector3.Subtract(ref v0, ref v1, out v0);
                        Vector3.Dot(ref v0, ref N, out normalVel);
                        //normalVel = Vector3.Dot(v0, N);
                    }
                    else
                    {
                        Vector3 v0;
                        body0.GetVelocity(ref ptInfo.info.R0, out v0);
                        //normalVel = Vector3.Dot(v0, N);
                        Vector3.Dot(ref v0, ref N, out normalVel);
                    }

                    // result in zero...
                    float deltaVel = -normalVel;

                    // ...except that the impulse reduction to achieve the desired separation must be done
                    // here - not with aux - because aux would suck objects together
                    if (ptInfo.MinSeparationVel < 0.0f)
                        deltaVel += ptInfo.MinSeparationVel;

                    if (System.Math.Abs(deltaVel) > minVelForProcessing)
                    {
                        normalImpulse = deltaVel / ptInfo.Denominator;

                        float origAccumulatedNormalImpulse = ptInfo.AccumulatedNormalImpulse;
                        ptInfo.AccumulatedNormalImpulse = MathHelper.Max(ptInfo.AccumulatedNormalImpulse + normalImpulse, 0.0f);
                        float actualImpulse = ptInfo.AccumulatedNormalImpulse - origAccumulatedNormalImpulse;

                        //Vector3 impulse = Vector3.Multiply(N, actualImpulse);
                        Vector3 impulse;
                        Vector3.Multiply(ref N, actualImpulse, out impulse);

                        body0.ApplyBodyWorldImpulse(ref impulse, ref ptInfo.info.R0);

                        if (body1 != null)
                            body1.ApplyNegativeBodyWorldImpulse(ref impulse, ref ptInfo.info.R1);

                        // prepare our return value
                        gotOne = true;

                    }


                }

                // now the correction impulse
                bool doCorrection = true;

                if (doCorrection)
                {
                    float normalVel;
                    if (body1 != null)
                    {
                        Vector3 v0, v1;
                        body0.GetVelocityAux(ref ptInfo.info.R0, out v0);
                        body1.GetVelocityAux(ref ptInfo.info.R1, out v1);
                        Vector3.Subtract(ref v0, ref v1, out v0);
                        Vector3.Dot(ref v0, ref N, out normalVel);
                        //normalVel = Vector3.Dot(v0, N);
                        //normalVel = Vector3.Dot(body0.GetVelocityAux(ptInfo.R0) - body1.GetVelocityAux(ptInfo.R1), collision.DirToBody0);
                    }
                    else
                    {
                        Vector3 v0;
                        body0.GetVelocityAux(ref ptInfo.info.R0, out v0);
                        //normalVel = Vector3.Dot(v0, N);
                        Vector3.Dot(ref v0, ref N, out normalVel);
                    }

                    float deltaVel = -normalVel;
                    // only try to separate objects
                    if (ptInfo.MinSeparationVel > 0.0f)
                        deltaVel += ptInfo.MinSeparationVel;

                    if (System.Math.Abs(deltaVel) > minVelForProcessing)
                    {
                        normalImpulse = deltaVel / ptInfo.Denominator;

                        float origAccumulatedNormalImpulse = ptInfo.AccumulatedNormalImpulseAux;
                        ptInfo.AccumulatedNormalImpulseAux = System.Math.Max(ptInfo.AccumulatedNormalImpulseAux + normalImpulse, 0.0f);
                        float actualImpulse = ptInfo.AccumulatedNormalImpulseAux - origAccumulatedNormalImpulse;

                        //Vector3 impulse = actualImpulse * collision.DirToBody0;
                        //Vector3 impulse = Vector3.Multiply(N, actualImpulse);
                        Vector3 impulse;
                        Vector3.Multiply(ref N, actualImpulse, out impulse);

                        body0.ApplyBodyWorldImpulseAux(ref impulse, ref ptInfo.info.R0);
                        if (body1 != null)
                            body1.ApplyNegativeBodyWorldImpulseAux(ref impulse,ref ptInfo.info.R1);
                        //prepare our return value
                        gotOne = true;

                    }

                }

                // For friction, work out the impulse in the opposite direction to
                // the tangential velocity that would be required to bring this
                // point to a halt. Apply the minimum of this impulse magnitude,
                // and the one obtained from the normal impulse. This prevents
                // reversing the velocity direction.
                //
                // recalculate the velocity since it's changed.
                if (ptInfo.AccumulatedNormalImpulse > 0.0f)
                {
                    Vector3 vrNew = body0.GetVelocity(ptInfo.info.R0);
                    if (body1 != null)
                    {
                        Vector3 pt1Vel;
                        body1.GetVelocity(ref ptInfo.info.R1, out pt1Vel);
                        Vector3.Subtract(ref vrNew, ref pt1Vel, out vrNew);

                        //vrNew = Vector3.Subtract(vrNew, body1.GetVelocity(ptInfo.R1));
                    }
                    //vrNew -= body1.GetVelocity(ptInfo.R1);

                    //Vector3 tangentVel = vrNew - Vector3.Dot(vrNew, N) * N;
                    Vector3 tangentVel; float f1;
                    Vector3.Dot(ref vrNew, ref N, out f1);
                    Vector3.Multiply(ref N, f1, out tangentVel);
                    Vector3.Subtract(ref vrNew, ref tangentVel, out tangentVel);

                    float tangentSpeed = tangentVel.Length();

                    if (tangentSpeed > minVelForProcessing)
                    {
                        //Vector3 T = -tangentVel / tangentSpeed;
                        Vector3 T;
                        Vector3.Divide(ref tangentVel, -tangentSpeed, out T);

                        // calculate an "inelastic collision" to zeor the relative vel
                        float denominator = 0.0f;
                        if (!body0.Immovable)
                        {
                            #region INLINE: denominator = body0.InvMass + Vector3.Dot(T, Vector3.Cross(body0.WorldInvInertia * (Vector3.Cross(ptInfo.R0, T)), ptInfo.R0));
                            float num0 = ptInfo.R0.Y * T.Z - ptInfo.R0.Z * T.Y;
                            float num1 = ptInfo.R0.Z * T.X - ptInfo.R0.X * T.Z;
                            float num2 = ptInfo.R0.X * T.Y - ptInfo.R0.Y * T.X;

                            float num3 = (((num0 * body0.worldInvInertia.M11) + (num1 * body0.worldInvInertia.M21)) + (num2 * body0.worldInvInertia.M31));
                            float num4 = (((num0 * body0.worldInvInertia.M12) + (num1 * body0.worldInvInertia.M22)) + (num2 * body0.worldInvInertia.M32));
                            float num5 = (((num0 * body0.worldInvInertia.M13) + (num1 * body0.worldInvInertia.M23)) + (num2 * body0.worldInvInertia.M33));

                            num0 = num4 * ptInfo.R0.Z - num5 * ptInfo.R0.Y;
                            num1 = num5 * ptInfo.R0.X - num3 * ptInfo.R0.Z;
                            num2 = num3 * ptInfo.R0.Y - num4 * ptInfo.R0.X;

                            denominator = body0.InverseMass + ((num0 * T.X) + (num1 * T.Y) + (num2 * T.Z));
                            #endregion
                        }

                        if ((body1 != null) && (!body1.Immovable))
                        {
                            #region INLINE: denominator += body1.InvMass + Vector3.Dot(T, Vector3.Cross(body1.WorldInvInertia * (Vector3.Cross(ptInfo.R1, T)), ptInfo.R1));
                            float num0 = ptInfo.R1.Y * T.Z - ptInfo.R1.Z * T.Y;
                            float num1 = ptInfo.R1.Z * T.X - ptInfo.R1.X * T.Z;
                            float num2 = ptInfo.R1.X * T.Y - ptInfo.R1.Y * T.X;

                            float num3 = (((num0 * body1.worldInvInertia.M11) + (num1 * body1.worldInvInertia.M21)) + (num2 * body1.worldInvInertia.M31));
                            float num4 = (((num0 * body1.worldInvInertia.M12) + (num1 * body1.worldInvInertia.M22)) + (num2 * body1.worldInvInertia.M32));
                            float num5 = (((num0 * body1.worldInvInertia.M13) + (num1 * body1.worldInvInertia.M23)) + (num2 * body1.worldInvInertia.M33));

                            num0 = num4 * ptInfo.R1.Z - num5 * ptInfo.R1.Y;
                            num1 = num5 * ptInfo.R1.X - num3 * ptInfo.R1.Z;
                            num2 = num3 * ptInfo.R1.Y - num4 * ptInfo.R1.X;

                            denominator += body1.InverseMass + ((num0 * T.X) + (num1 * T.Y) + (num2 * T.Z));
                            #endregion
                        }

                        if (denominator > JiggleMath.Epsilon)
                        {
                            float impulseToReverse = tangentSpeed / denominator;
                            //Vector3 frictionImpulseVec = T * impulseToReverse;
                            Vector3 frictionImpulseVec;
                            Vector3.Multiply(ref T, impulseToReverse, out frictionImpulseVec);

                            Vector3 origAccumulatedFrictionImpulse = ptInfo.AccumulatedFrictionImpulse;
                            
                            //ptInfo.AccumulatedFrictionImpulse += frictionImpulseVec;
                            Vector3.Add(ref ptInfo.AccumulatedFrictionImpulse, ref frictionImpulseVec, out ptInfo.AccumulatedFrictionImpulse);

                            float AFIMag = ptInfo.AccumulatedFrictionImpulse.Length();
                            float maxAllowedAFIMAg = collision.MatPairProperties.StaticFriction * ptInfo.AccumulatedNormalImpulse;

                            if (AFIMag > JiggleMath.Epsilon && AFIMag > maxAllowedAFIMAg)
                            {
                                //ptInfo.AccumulatedFrictionImpulse *= maxAllowedAFIMAg / AFIMag;
                                Vector3.Multiply(ref ptInfo.AccumulatedFrictionImpulse, maxAllowedAFIMAg / AFIMag,
                                    out ptInfo.AccumulatedFrictionImpulse);
                            }

                            //Vector3 actualFrictionImpulse = ptInfo.AccumulatedFrictionImpulse - origAccumulatedFrictionImpulse;
                            Vector3 actualFrictionImpulse;
                            Vector3.Subtract(ref ptInfo.AccumulatedFrictionImpulse, ref origAccumulatedFrictionImpulse,
                                out actualFrictionImpulse);

                            body0.ApplyBodyWorldImpulse(ref actualFrictionImpulse, ref ptInfo.info.R0);
                            if (body1 != null)
                                body1.ApplyNegativeBodyWorldImpulse(ref actualFrictionImpulse, ref ptInfo.info.R1);

                        }
                    } // end of friction

                }
            }

            if (gotOne)
            {
                body0.SetConstraintsAndCollisionsUnsatisfied();
                if (body1 != null)
                    body1.SetConstraintsAndCollisionsUnsatisfied();
            }

            return gotOne;

        }

        private unsafe bool ProcessCollisionCombined(CollisionInfo collision, float dt, bool firstContact)
        {
            collision.Satisfied = true;

            Body body0 = collision.SkinInfo.Skin0.Owner;
            Body body1 = collision.SkinInfo.Skin1.Owner;

            Vector3 N = collision.DirToBody0;

            // the individual impulses in the same order as
            // collision->mPointInfo - for friction
            float totalImpulse = 0.0f;
            int pos;

            Vector3 avPos = Vector3.Zero;
            float avMinSeparationVel = 0.0f;

            // the fastest possible way to allocate short living arrays of primitive types
            float* impulses = stackalloc float[CollisionInfo.MaxCollisionPoints];

            for (pos = collision.NumCollPts; pos-- != 0; )
            {
                CollPointInfo ptInfo = collision.PointInfo[pos];
                impulses[pos] = 0.0f;

                float normalVel;
                if (body1 != null)
                    normalVel = Vector3.Dot(body0.GetVelocity(ptInfo.R0) -
                        body1.GetVelocity(ptInfo.R1), N);
                else
                    normalVel = Vector3.Dot(body0.GetVelocity(ptInfo.R0), N);

                if (normalVel > ptInfo.MinSeparationVel)
                    continue;

                float finalNormalVel = -collision.MatPairProperties.Restitution * normalVel;

                if (finalNormalVel < minVelForProcessing)
                {
                    // could be zero elasticity in collision, or could be zero
                    // elasticity in contact - don't care.  relax towards 0
                    // penetration
                    finalNormalVel = ptInfo.MinSeparationVel;
                }

                float deltaVel = finalNormalVel - normalVel;
                if (deltaVel < minVelForProcessing)
                    continue;

                float normalImpulse = deltaVel / ptInfo.Denominator;

                impulses[pos] = normalImpulse;
                totalImpulse += normalImpulse;

                avPos = avPos + normalImpulse * ptInfo.Position;
                avMinSeparationVel += ptInfo.MinSeparationVel * normalImpulse;
            }

            if (totalImpulse <= JiggleMath.Epsilon)
                return false;

            float scale = 1.0f / totalImpulse;

            // apply all these impulses (as well as subsequently applying an
            // impulse at an averaged position)
            for (pos = collision.NumCollPts; pos-- != 0; )
            {
                if (impulses[pos] > JiggleMath.Epsilon)
                {
                    CollPointInfo ptInfo = collision.PointInfo[pos];
                    float sc = impulses[pos] * scale;

                    Vector3 impulse;
                    Vector3.Multiply(ref N, impulses[pos] * sc, out impulse);

                    body0.ApplyBodyWorldImpulse(ref impulse, ref ptInfo.info.R0);

                    if (body1 != null)
                        body1.ApplyNegativeBodyWorldImpulse(ref impulse, ref ptInfo.info.R1);
                }
            }
            Vector3.Multiply(ref avPos, scale, out avPos);
            avMinSeparationVel *= scale;

            // now calculate the single impulse to be applied at avPos
            Vector3 R0, R1 = Vector3.Zero;
            R0 = avPos - body0.Position;
            Vector3 Vr = body0.GetVelocity(R0);
            if (body1 != null)
            {
                R1 = avPos - body1.Position;
                Vr -= body1.GetVelocity(R1);
            }

            float normalVel2 = Vector3.Dot(Vr, N);

            float normalImpulse2 = 0.0f;

            if (normalVel2 < avMinSeparationVel)
            {
                // coefficient of restitution
                float finalNormalVel = -collision.MatPairProperties.Restitution * normalVel2;

                if (finalNormalVel < minVelForProcessing)
                {
                    // must be a contact - could be zero elasticity in collision, or
                    // could be zero elasticity in contact - don't care.  relax
                    // towards 0 penetration
                    finalNormalVel = avMinSeparationVel;
                }

                float deltaVel = finalNormalVel - normalVel2;

                if (deltaVel > minVelForProcessing)
                {
                    float denominator = 0.0f;
                    if (!body0.Immovable)
                        denominator = body0.InverseMass +
                            Vector3.Dot(N, Vector3.Cross(Vector3.Transform(Vector3.Cross(R0, N), body0.WorldInvInertia), R0));
                    if ((body1 != null) && (!body1.Immovable))
                        denominator += body1.InverseMass +
                            Vector3.Dot(N, Vector3.Cross(Vector3.Transform(Vector3.Cross(R1, N), body1.WorldInvInertia), R1));
                    if (denominator < JiggleMath.Epsilon)
                        denominator = JiggleMath.Epsilon;

                    normalImpulse2 = deltaVel / denominator;
                    Vector3 impulse = normalImpulse2 * N;

                    body0.ApplyWorldImpulse(impulse, avPos);
                    if (body1 != null)
                        body1.ApplyNegativeWorldImpulse(impulse, avPos);
                }
            }



            // Now do friction point by point
            for (pos = collision.NumCollPts; pos-- != 0; )
            {
                // For friction, work out the impulse in the opposite direction to
                // the tangential velocity that would be required to bring this
                // point to a halt. Apply the minimum of this impulse magnitude,
                // and the one obtained from the normal impulse. This prevents
                // reversing the velocity direction.
                //
                // However, recalculate the velocity since it's changed.
                CollPointInfo ptInfo = collision.PointInfo[pos];

                Vector3 vrNew = (body1 != null) ?
                    (body0.GetVelocity(ptInfo.R0) - body1.GetVelocity(ptInfo.R1)) :
                    (body0.GetVelocity(ptInfo.R0));

                Vector3 T = vrNew - Vector3.Dot(vrNew, N) * N;
                float tangentSpeed = T.Length();
                if (tangentSpeed > minVelForProcessing)
                {
                    T /= -tangentSpeed;

                    float sc = impulses[pos] * scale;
                    float ptNormalImpulse = sc * (normalImpulse2 + impulses[pos]);

                    // calculate an "inelastic collision" to zero the relative vel
                    float denominator = 0.0f;


                    if (!body0.Immovable)
                    {
                        denominator = body0.InverseMass +
                            Vector3.Dot(T, Vector3.Cross(Vector3.Transform(Vector3.Cross(ptInfo.R0, T), body0.WorldInvInertia), ptInfo.R0));
                    }

                    if ((body1 != null) && (!body1.Immovable))
                    {
                        denominator += body1.InverseMass +
                            Vector3.Dot(T, Vector3.Cross(Vector3.Transform(Vector3.Cross(ptInfo.R1, T), body1.WorldInvInertia), ptInfo.R1));
                    }

                    if (denominator > JiggleMath.Epsilon)
                    {
                        float impulseToReverse = tangentSpeed / denominator;
                        float impulseFromNormalImpulse =
                            collision.MatPairProperties.StaticFriction * ptNormalImpulse;
                        float frictionImpulse;

                        if (impulseToReverse < impulseFromNormalImpulse)
                            frictionImpulse = impulseToReverse;
                        else
                            frictionImpulse = collision.MatPairProperties.DynamicFriction * ptNormalImpulse;

                        T *= frictionImpulse;
                        body0.ApplyBodyWorldImpulse(T, ptInfo.R0);
                        if (body1 != null)
                            body1.ApplyNegativeBodyWorldImpulse(ref T, ref ptInfo.info.R1);
                    }
                }
            } // end of friction

            body0.SetConstraintsAndCollisionsUnsatisfied();
            if (body1 != null)
                body1.SetConstraintsAndCollisionsUnsatisfied();

            return true;
        }

        private bool ProcessCollisionsForShock(CollisionInfo collision, float dt)
        {
            collision.Satisfied = true;
            Vector3 N = collision.DirToBody0;
            // Changed here. N.X = N.Y = 0.0f;
            N.X = N.Z = 0.0f;
            JiggleMath.NormalizeSafe(ref N);
            int iterations = 5;
            int pos;
            float timescale = penetrationShockRelaxtionTimestep * dt;
            for (pos = 0; pos < collision.NumCollPts; ++pos)
            {
                CollPointInfo ptInfo = collision.PointInfo[pos];
            }

            // since this is shock, body 0 OR body1 can be immovable. Also, if
            // immovable make the constraint against a non-moving object
            Body body0 = collision.SkinInfo.Skin0.Owner;
            Body body1 = collision.SkinInfo.Skin1.Owner;
            if (body0.Immovable)
                body0 = null;
            if ((body1 != null) && body1.Immovable)
                body1 = null;

            if (body0 == null && body1 == null)
                return false;

            for (int iteration = 0; iteration < iterations; ++iteration)
            {
                for (pos = 0; pos < collision.NumCollPts; ++pos)
                {
                    CollPointInfo ptInfo = collision.PointInfo[pos];
                    float normalVel = 0.0f;
                    if (body0 != null)
                        normalVel = Vector3.Dot(body0.GetVelocity(ptInfo.R0), N) + Vector3.Dot(body0.GetVelocityAux(ptInfo.R0), N);
                    if (body1 != null)
                        normalVel -= Vector3.Dot(body1.GetVelocity(ptInfo.R1), N) + Vector3.Dot(body1.GetVelocityAux(ptInfo.R1), N);

                    float finalNormalVel = (ptInfo.InitialPenetration - allowedPenetration) / timescale;

                    if (finalNormalVel < 0.0f)
                        continue;

                    float impulse = (finalNormalVel - normalVel) / ptInfo.Denominator;

                    float orig = ptInfo.AccumulatedNormalImpulseAux;
                    ptInfo.AccumulatedNormalImpulseAux = System.Math.Max(ptInfo.AccumulatedNormalImpulseAux + impulse, 0.0f);
                    Vector3 actualImpulse = (ptInfo.AccumulatedNormalImpulseAux - orig) * N;

                    if (body0 != null)
                        body0.ApplyBodyWorldImpulseAux(ref actualImpulse,ref ptInfo.info.R0);
                    if (body1 != null)
                        body1.ApplyNegativeBodyWorldImpulseAux(ref actualImpulse,ref ptInfo.info.R1);

                }
            }

            if (body0 != null)
                body0.SetConstraintsAndCollisionsUnsatisfied();
            if (body1 != null)
                body1.SetConstraintsAndCollisionsUnsatisfied();

            return true;
        }

        private void PreProcessCollision(CollisionInfo collision, float dt)
        {
            Body body0 = collision.SkinInfo.Skin0.Owner;
            Body body1 = collision.SkinInfo.Skin1.Owner;

            // make as not satisfied
            collision.Satisfied = false;

            //always calc the following
            Vector3 N = collision.DirToBody0;
            float timescale = numPenetrationRelaxtionTimesteps * dt;

            for (int pos = 0; pos < collision.NumCollPts; ++pos)
            {
                CollPointInfo ptInfo = collision.PointInfo[pos];
                // some things we only calculate if there are bodies, and they are
                // movable
                if (body0.Immovable)
                    ptInfo.Denominator = 0.0f;
                else
                {
                    #region INLINE: ptInfo.Denominator = body0.InvMass + Vector3.Dot(N, Vector3.Cross(body0.WorldInvInertia * (Vector3.Cross(ptInfo.R0, N)), ptInfo.R0));
                    float num0 = (ptInfo.R0.Y * N.Z) - (ptInfo.R0.Z * N.Y);
                    float num1 = (ptInfo.R0.Z * N.X) - (ptInfo.R0.X * N.Z);
                    float num2 = (ptInfo.R0.X * N.Y) - (ptInfo.R0.Y * N.X);

                    float num3 = (((num0 * body0.worldInvInertia.M11) + (num1 * body0.worldInvInertia.M21)) + (num2 * body0.worldInvInertia.M31));
                    float num4 = (((num0 * body0.worldInvInertia.M12) + (num1 * body0.worldInvInertia.M22)) + (num2 * body0.worldInvInertia.M32));
                    float num5 = (((num0 * body0.worldInvInertia.M13) + (num1 * body0.worldInvInertia.M23)) + (num2 * body0.worldInvInertia.M33));

                    num0 = (num4 * ptInfo.R0.Z) - (num5 * ptInfo.R0.Y);
                    num1 = (num5 * ptInfo.R0.X) - (num3 * ptInfo.R0.Z);
                    num2 = (num3 * ptInfo.R0.Y) - (num4 * ptInfo.R0.X);

                    ptInfo.Denominator = body0.InverseMass + ((num0 * N.X) + (num1 * N.Y) + (num2 * N.Z));
                    #endregion
                }

                if ((body1 != null) && !body1.Immovable)
                {
                    #region INLINE: ptInfo.Denominator += body1.InvMass + Vector3.Dot(N, Vector3.Cross(body1.WorldInvInertia * (Vector3.Cross(ptInfo.R1, N)), ptInf1.R0));
                    float num0 = (ptInfo.R1.Y * N.Z) - (ptInfo.R1.Z * N.Y);
                    float num1 = (ptInfo.R1.Z * N.X) - (ptInfo.R1.X * N.Z);
                    float num2 = (ptInfo.R1.X * N.Y) - (ptInfo.R1.Y * N.X);

                    float num3 = (((num0 * body1.worldInvInertia.M11) + (num1 * body1.worldInvInertia.M21)) + (num2 * body1.worldInvInertia.M31));
                    float num4 = (((num0 * body1.worldInvInertia.M12) + (num1 * body1.worldInvInertia.M22)) + (num2 * body1.worldInvInertia.M32));
                    float num5 = (((num0 * body1.worldInvInertia.M13) + (num1 * body1.worldInvInertia.M23)) + (num2 * body1.worldInvInertia.M33));

                    num0 = (num4 * ptInfo.R1.Z) - (num5 * ptInfo.R1.Y);
                    num1 = (num5 * ptInfo.R1.X) - (num3 * ptInfo.R1.Z);
                    num2 = (num3 * ptInfo.R1.Y) - (num4 * ptInfo.R1.X);

                    ptInfo.Denominator += body1.InverseMass + ((num0 * N.X) + (num1 * N.Y) + (num2 * N.Z));
                    #endregion
                }

                if (ptInfo.Denominator < JiggleMath.Epsilon)
                    ptInfo.Denominator = JiggleMath.Epsilon;

                // calculate the world position
                //Vector3.Add(ref body0.oldTransform.Position, ref ptInfo.R0, out ptInfo.Position);
                //ptInfo.Position = body0.OldPosition + ptInfo.R0;
                Vector3.Add(ref body0.oldTransform.Position, ref ptInfo.info.R0, out ptInfo.Position);

                // per-point penetration resolution
                if (ptInfo.InitialPenetration > allowedPenetration)
                {
                    ptInfo.MinSeparationVel = (ptInfo.InitialPenetration - allowedPenetration) / timescale;
                }
                else
                {
                    float approachScale = -0.1f * (ptInfo.InitialPenetration - allowedPenetration) / (JiggleMath.Epsilon + allowedPenetration);
                    approachScale = MathHelper.Clamp(approachScale, JiggleMath.Epsilon, 1.0f);
                    ptInfo.MinSeparationVel = approachScale * (ptInfo.InitialPenetration - allowedPenetration) / MathHelper.Max(dt, JiggleMath.Epsilon);
                }

                if (ptInfo.MinSeparationVel > maxVelMag)
                    ptInfo.MinSeparationVel = maxVelMag;
            }
        }

        private int MoreCollPtPenetration(CollPointInfo info1, CollPointInfo info2)
        {
            if (info1 == null && info2 == null) return 0;
            if (info1 == null) return 1;
            if (info2 == null) return -1;

            if (info1.InitialPenetration == info2.InitialPenetration) return 0;
            return (info1.InitialPenetration < info2.InitialPenetration) ? 1 : -1;
        }

        private void PreProcessCollisionFast(CollisionInfo collision, float dt)
        {
            Body body0 = collision.SkinInfo.Skin0.Owner;
            Body body1 = collision.SkinInfo.Skin1.Owner;

            // make as not satisfied
            collision.Satisfied = false;

            // always calc the following
            Vector3 N = collision.DirToBody0;
            float timescale = numPenetrationRelaxtionTimesteps * dt;

            const int keep = 3;
            if (collision.NumCollPts > keep)
            {
                Array.Sort( collision.PointInfo, MoreCollPtPenetration);
                collision.NumCollPts = keep;
            }

            for (int pos = 0; pos < collision.NumCollPts; ++pos)
            {
                CollPointInfo ptInfo = collision.PointInfo[pos];
                // some things we only calculate if there are bodies, and they are
                // movable
                if (body0.Immovable)
                    ptInfo.Denominator = 0.0f;
                else
                {
                    Vector3 cross; float res;
                    Vector3.Cross(ref ptInfo.info.R0, ref N, out cross);
                    Vector3.Transform(ref cross, ref body0.worldInvInertia, out cross);
                    Vector3.Cross(ref cross, ref ptInfo.info.R0, out cross);
                    Vector3.Dot(ref N, ref cross, out res);
                    ptInfo.Denominator = body0.InverseMass + res;
                }

                if ((body1 != null) && !body1.Immovable)
                {
                    //ptInfo.Denominator += body1.InvMass +
                    //  Vector3.Dot(N, Vector3.Cross(Vector3.Transform(Vector3.Cross(ptInfo.R1, N), body1.WorldInvInertia), ptInfo.R1));
                    Vector3 cross; float res;
                    Vector3.Cross(ref ptInfo.info.R1, ref N, out cross);
                    Vector3.Transform(ref cross, ref body1.worldInvInertia, out cross);
                    Vector3.Cross(ref cross, ref ptInfo.info.R1, out cross);
                    Vector3.Dot(ref N, ref cross, out res);
                    ptInfo.Denominator += body1.InverseMass + res;
                }

                if (ptInfo.Denominator < JiggleMath.Epsilon)
                    ptInfo.Denominator = JiggleMath.Epsilon;

                // calculate the world position
                Vector3.Add(ref body0.oldTransform.Position, ref ptInfo.info.R0, out ptInfo.Position);

                // per-point penetration resolution
                if (ptInfo.InitialPenetration > allowedPenetration)
                {
                    ptInfo.MinSeparationVel = (ptInfo.InitialPenetration - allowedPenetration) / timescale;
                }
                else
                {
                    float approachScale = -0.1f * (ptInfo.InitialPenetration - allowedPenetration) / (JiggleMath.Epsilon + allowedPenetration);
                    approachScale = MathHelper.Clamp(approachScale, JiggleMath.Epsilon, 1.0f);
                    ptInfo.MinSeparationVel = approachScale * (ptInfo.InitialPenetration - allowedPenetration) / MathHelper.Max(dt, JiggleMath.Epsilon);
                }
                if (ptInfo.MinSeparationVel > maxVelMag)
                    ptInfo.MinSeparationVel = maxVelMag;

            }

        }

        private void PreProcessCollisionAccumulated(CollisionInfo collision, float dt)
        {
            Body body0 = collision.SkinInfo.Skin0.Owner;
            Body body1 = collision.SkinInfo.Skin1.Owner;

            // make as not satisfied
            collision.Satisfied = false;

            // always calc the following
            Vector3 N = collision.DirToBody0;
            float timescale = numPenetrationRelaxtionTimesteps * dt;

            for (int pos = 0; pos < collision.NumCollPts; ++pos)
            {
                CollPointInfo ptInfo = collision.PointInfo[pos];

                // some things we only calculate if there are bodies, and they are
                // movable
                if (body0.Immovable)
                    ptInfo.Denominator = 0.0f;
                else
                {
                    #region INLINE: ptInfo.Denominator = body0.InvMass + Vector3.Dot(N, Vector3.Cross(body0.WorldInvInertia * (Vector3.Cross(ptInfo.R0, N)), ptInfo.R0));
                    float num0 = ptInfo.R0.Y * N.Z - ptInfo.R0.Z * N.Y;
                    float num1 = ptInfo.R0.Z * N.X - ptInfo.R0.X * N.Z;
                    float num2 = ptInfo.R0.X * N.Y - ptInfo.R0.Y * N.X;

                    float num3 = (((num0 * body0.worldInvInertia.M11) + (num1 * body0.worldInvInertia.M21)) + (num2 * body0.worldInvInertia.M31));
                    float num4 = (((num0 * body0.worldInvInertia.M12) + (num1 * body0.worldInvInertia.M22)) + (num2 * body0.worldInvInertia.M32));
                    float num5 = (((num0 * body0.worldInvInertia.M13) + (num1 * body0.worldInvInertia.M23)) + (num2 * body0.worldInvInertia.M33));

                    num0 = num4 * ptInfo.R0.Z - num5 * ptInfo.R0.Y;
                    num1 = num5 * ptInfo.R0.X - num3 * ptInfo.R0.Z;
                    num2 = num3 * ptInfo.R0.Y - num4 * ptInfo.R0.X;

                    ptInfo.Denominator = body0.InverseMass + ((num0 * N.X) + (num1 * N.Y) + (num2 * N.Z));
                    #endregion
                }

                if ((body1 != null) && !body1.Immovable)
                {
                    #region INLINE: ptInfo.Denominator += body1.InvMass + Vector3.Dot(N, Vector3.Cross(body1.WorldInvInertia * (Vector3.Cross(ptInfo.R1, N)), ptInf1.R0));
                    float num0 = ptInfo.R1.Y * N.Z - ptInfo.R1.Z * N.Y;
                    float num1 = ptInfo.R1.Z * N.X - ptInfo.R1.X * N.Z;
                    float num2 = ptInfo.R1.X * N.Y - ptInfo.R1.Y * N.X;

                    float num3 = (((num0 * body1.worldInvInertia.M11) + (num1 * body1.worldInvInertia.M21)) + (num2 * body1.worldInvInertia.M31));
                    float num4 = (((num0 * body1.worldInvInertia.M12) + (num1 * body1.worldInvInertia.M22)) + (num2 * body1.worldInvInertia.M32));
                    float num5 = (((num0 * body1.worldInvInertia.M13) + (num1 * body1.worldInvInertia.M23)) + (num2 * body1.worldInvInertia.M33));

                    num0 = num4 * ptInfo.R1.Z - num5 * ptInfo.R1.Y;
                    num1 = num5 * ptInfo.R1.X - num3 * ptInfo.R1.Z;
                    num2 = num3 * ptInfo.R1.Y - num4 * ptInfo.R1.X;

                    ptInfo.Denominator += body1.InverseMass + ((num0 * N.X) + (num1 * N.Y) + (num2 * N.Z));
                    #endregion
                }


                if (ptInfo.Denominator < JiggleMath.Epsilon)
                    ptInfo.Denominator = JiggleMath.Epsilon;

                // calculate the world position
                Vector3.Add(ref body0.oldTransform.Position, ref ptInfo.info.R0, out ptInfo.Position);
                //ptInfo.Position = body0.OldPosition + ptInfo.R0;

                // per-point penetetration resolution
                if (ptInfo.InitialPenetration > allowedPenetration)
                {
                    ptInfo.MinSeparationVel = (ptInfo.InitialPenetration - allowedPenetration) / timescale;
                }
                else
                {
                    float approachScale = -0.1f * (ptInfo.InitialPenetration - allowedPenetration) / (JiggleMath.Epsilon + allowedPenetration);
                    approachScale = MathHelper.Clamp(approachScale, JiggleMath.Epsilon, 1.0f);
                    ptInfo.MinSeparationVel = approachScale * (ptInfo.InitialPenetration - allowedPenetration) / MathHelper.Max(dt, JiggleMath.Epsilon);
                }

                ptInfo.AccumulatedNormalImpulse = 0.0f;
                ptInfo.AccumulatedNormalImpulseAux = 0.0f;
                ptInfo.AccumulatedFrictionImpulse = Vector3.Zero;

                /// todo take this value from config or derive from the geometry (but don't reference the body in the cache as it
                /// may be deleted)

                float minDist = 0.2f;
                float bestDistSq = minDist * minDist;

                Contact.BodyPair bp = new Contact.BodyPair(body0, body1);
                int count = catchedContacts.Count;

                for (int i = 0; i < count; i++)
                {
                    if (!(bp.BodyA == catchedContacts[i].Pair.BodyA && bp.BodyB == catchedContacts[i].Pair.BodyB))
                        continue;

                    //float distSq = (catchedContacts[i].Pair.BodyA == collision.SkinInfo.Skin0.Owner) ?
                    //    Distance.PointPointDistanceSq(catchedContacts[i].Pair.RA, ptInfo.R0) :
                    //    Distance.PointPointDistanceSq(catchedContacts[i].Pair.RA, ptInfo.R1);

                    float distSq;
                    if (catchedContacts[i].Pair.BodyA == collision.SkinInfo.Skin0.Owner)
                    {
                        float num3 = catchedContacts[i].Pair.RA.X - ptInfo.R0.X;
                        float num2 = catchedContacts[i].Pair.RA.Y - ptInfo.R0.Y;
                        float num0 = catchedContacts[i].Pair.RA.Z - ptInfo.R0.Z;
                        distSq = ((num3 * num3) + (num2 * num2)) + (num0 * num0);
                    }
                    //Distance.PointPointDistanceSq(ref catchedContacts[i].Pair.RA, ref ptInfo.R0, out distSq);
                    else
                    {
                        //Distance.PointPointDistanceSq(ref catchedContacts[i].Pair.RA, ref ptInfo.R1, out distSq);
                        float num3 = catchedContacts[i].Pair.RA.X - ptInfo.R1.X;
                        float num2 = catchedContacts[i].Pair.RA.Y - ptInfo.R1.Y;
                        float num0 = catchedContacts[i].Pair.RA.Z - ptInfo.R1.Z;
                        distSq = ((num3 * num3) + (num2 * num2)) + (num0 * num0);
                    }

                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;

                        ptInfo.AccumulatedNormalImpulse = catchedContacts[i].Impulse.NormalImpulse;
                        ptInfo.AccumulatedNormalImpulseAux = catchedContacts[i].Impulse.NormalImpulseAux;
                        ptInfo.AccumulatedFrictionImpulse = catchedContacts[i].Impulse.FrictionImpulse;

                        if (catchedContacts[i].Pair.BodyA != collision.SkinInfo.Skin0.Owner)
                            ptInfo.AccumulatedFrictionImpulse *= -1;

                    }
                }

                float oldScale = 1.0f;
                ptInfo.AccumulatedNormalImpulse *= oldScale;
                ptInfo.AccumulatedFrictionImpulse *= oldScale;
                ptInfo.AccumulatedNormalImpulseAux *= oldScale;

                if (ptInfo.AccumulatedNormalImpulse != 0.0f)
                {
                    //Vector3 impulse = N * ptInfo.AccumulatedNormalImpulse;
                    Vector3 impulse;
                    Vector3.Multiply(ref N, ptInfo.AccumulatedNormalImpulse, out impulse);
                    //impulse += ptInfo.AccumulatedFrictionImpulse;
                    Vector3.Add(ref impulse, ref ptInfo.AccumulatedFrictionImpulse, out impulse);
                    body0.ApplyBodyWorldImpulse(ref impulse, ref ptInfo.info.R0);
                    if (body1 != null)
                        body1.ApplyNegativeBodyWorldImpulse(ref impulse, ref ptInfo.info.R1);
                }
                if (ptInfo.AccumulatedNormalImpulseAux != 0.0f)
                {
                    //Vector3 impulse = N * ptInfo.AccumulatedNormalImpulseAux;
                    Vector3 impulse;
                    Vector3.Multiply(ref N, ptInfo.AccumulatedNormalImpulseAux, out impulse);
                    body0.ApplyBodyWorldImpulseAux(ref impulse,ref ptInfo.info.R0);
                    if (body1 != null)
                        body1.ApplyNegativeBodyWorldImpulseAux(ref impulse,ref ptInfo.info.R1);
                }
            }
        }

        private void SetCollisionFns()
        {
            switch (solverType)
            {
                case Solver.Fast:
                    preProcessCollisionFn = preProcessContactFn = PreProcessCollisionFast;
                    processCollisionFn = processContactFn = ProcessCollisionFast;
                    break;
                case Solver.Normal:
                    preProcessCollisionFn = preProcessContactFn = PreProcessCollision;
                    processCollisionFn = processContactFn = ProcessCollision;
                    break;
                case Solver.Combined:
                    preProcessCollisionFn = preProcessContactFn = PreProcessCollision;
                    processCollisionFn = processContactFn = ProcessCollisionCombined;
                    break;
                case Solver.Accumulated:
                    preProcessCollisionFn = PreProcessCollision;
                    processCollisionFn = ProcessCollision;
                    preProcessContactFn = PreProcessCollisionAccumulated;
                    processContactFn = ProcessCollisionAccumulated;
                    break;
            }
        }

        private void HandleAllConstraints(float dt, int iter, bool forceInelastic)
        {
            int i;
            int origNumCollisions = collisions.Count;
            int numConstraints = constraints.Count;

            // prepare all the constraints
            for (i = 0; i < numConstraints; ++i)
            {
                constraints[i].PreApply(dt);
            }

            // prepare all the collisions 
            if (forceInelastic)
            {
                for (i = 0; i < origNumCollisions; ++i)
                {
                    preProcessContactFn(collisions[i], dt);
                    collisions[i].MatPairProperties.Restitution = 0.0f;
                    collisions[i].Satisfied = false;
                }
            }
            else
            {
                // prepare for the collisions
                for (i = 0; i < origNumCollisions; ++i)
                    preProcessCollisionFn(collisions[i], dt);
            }

            // iterate over the collisions
            bool dir = true;

            for (int step = 0; step < iter; ++step)
            {
                bool gotOne = true;
                // step 6
                int numCollisions = collisions.Count;

                dir = !dir;

                for (i = (dir) ? 0 : numCollisions - 1;
                    i >= 0 && i < numCollisions; i +=
                    (dir) ? (1) : (-1))
                {
                    if (!collisions[i].Satisfied)
                    {
                        if (forceInelastic)
                            gotOne |= processContactFn(collisions[i], dt, step == 0);
                        else
                            gotOne |= processCollisionFn(collisions[i], dt, step == 0);
                    }
                }

                for (i = 0; i < numConstraints; ++i)
                {
                    if (!constraints[i].Satisfied)
                        gotOne |= constraints[i].Apply(dt);
                }

                numCollisions = collisions.Count;

                if (forceInelastic)
                {
                    for (i = origNumCollisions; i < numCollisions; ++i)
                    {
                        collisions[i].MatPairProperties.Restitution = 0.0f;
                        collisions[i].Satisfied = false;
                        preProcessContactFn(collisions[i], dt);
                    }
                }
                else
                {
                    for (i = origNumCollisions; i < numCollisions; ++i)
                    {
                        preProcessCollisionFn(collisions[i], dt);
                    }
                }

                origNumCollisions = numCollisions;

                if (!gotOne)
                    break;

            }
        }

        /// <summary>
        /// Gets the current PhysicSystem.
        /// </summary>
        public static PhysicsSystem CurrentPhysicsSystem
        {
            get { return currentPhysicsSystem; }
            set { currentPhysicsSystem = value; }
        }


        /// <summary>
        /// If there is to be any collision detection, this physics system
        /// needs to know how to collide objects. In the absence of a
        /// collision system, no collisions will occur (surprise
        /// surprise).
        /// </summary>
        public CollisionSystem CollisionSystem
        {
            get { return collisionSystem; }
            set { collisionSystem = value; }
        }

        private void NotifyAllPrePhysics(float dt)
        {
            int numBodies = bodies.Count;
            for (int i = 0; i < numBodies; ++i)
                bodies[i].PrePhysics(dt);
        }


        private void BuildIslands()
        {
            foreach (CollisionIsland c in islands)
            {
                freeCollisionIslands.Push(c);
            }
            islands.Clear();

            foreach (Body body in bodies) body.foundIsland = false;

            foreach (Body body in bodies)
            {
                if (!body.foundIsland)
                {
                    if (freeCollisionIslands.Count == 0)
                    {
                        freeCollisionIslands.Push(new CollisionIsland());
                    }
                    CollisionIsland island = freeCollisionIslands.Pop();
                    island.Clear();
                    FindConnected(body, island);
                    islands.Add(island);
                }
            }
        }

        private void DampAllActiveBodies()
        {
            int numBodies = activeBodies.Count;
            for (int i = 0; i < numBodies; ++i)
                activeBodies[i].DampForDeactivation();
        }

        /// <summary>
        /// Finds recursivly all bodies which are touching each other and
        /// adds them to a list.
        /// </summary>
        /// <param name="body">The body with which to start</param>
        /// <param name="island">The island contains the bodies which are in contact with
        /// each other</param>
        private void FindConnected(Body body, CollisionIsland island)
        {
            if (body.foundIsland) return;
            body.foundIsland = true;

            island.Add(body);

            foreach (CollisionInfo collision in body.CollisionSkin.Collisions)
            {
                if (collision.SkinInfo.Skin1.Owner != null)
                {
                    if (collision.SkinInfo.Skin1.Owner == body)
                        FindConnected(collision.SkinInfo.Skin0.Owner, island);
                    else
                        FindConnected(collision.SkinInfo.Skin1.Owner, island);
                }
            }
        }

     
        /// <summary>
        /// Integrates the system forwards by dt - the caller is
        /// responsible for making sure that repeated calls to this use
        /// the same dt (if desired)
        /// </summary>
        /// <param name="dt"></param>
        public void Integrate(float dt)
        {

            oldTime = targetTime;
            targetTime += dt;

            SetCollisionFns();

            NotifyAllPrePhysics(dt);

            FindAllActiveBodies();

            CopyAllCurrentStatesToOld();

            GetAllExternalForces(dt);

            DetectAllCollisions(dt);

            HandleAllConstraints(dt, numCollisionIterations, false);

            UpdateAllVelocities(dt);

            HandleAllConstraints(dt, numContactIterations, true);

            // do a shock step to help stacking
            if (doShockStep) DoShockStep(dt);

            if (freezingEnabled && (collisions.Count != 0))
            {
                BuildIslands();

                for (int i = 0; i < bodies.Count; i++)
                    bodies[i].UpdateDeactivation(dt);

                for (int i = 0; i < islands.Count; i++)
                {
                    if (islands[i].WantsDeactivation(dt)) islands[i].Deactivate();
                    else islands[i].Activate();
                }

                // TODO: Think about a good damping algorithm.
                DampAllActiveBodies();
            }

            LimitAllVelocities();

            UpdateAllPositions(dt);

            NotifyAllPostPhysics(dt);

            if (solverType == Solver.Accumulated)
                UpdateContactCache();

            if (nullUpdate)
            {
                for (int i = 0; i < activeBodies.Count; ++i)
                    activeBodies[i].RestoreState();
            }

        }

        /// <summary>
        /// Get the physics idea of the time we're advancing towards.
        /// </summary>
        public float TargetTime
        {
            get { return targetTime; }
        }

        /// <summary>
        /// Gets the physics idea of the time we've left behind
        /// </summary>
        public float OldTime
        {
            get { return oldTime; }
        }

        public void ResetTime(float time)
        {
            this.targetTime = this.oldTime = time;
        }

        /// <summary>
        /// Number of iterations done in the collision step of the solver.
        /// </summary>
        public int NumCollisionIterations
        {
            set { numCollisionIterations = value; }
            get { return numCollisionIterations; }
        }

        /// <summary>
        /// Number of iterations done in the contact step of the solver.
        /// </summary>
        public int NumContactIterations
        {
            set { numContactIterations = value; }
            get { return numContactIterations; }
        }

        public int NumPenetrationRelaxtionTimesteps
        {
            set { this.numPenetrationRelaxtionTimesteps = value; }
            get { return this.numPenetrationRelaxtionTimesteps; }
        }

        public float AllowedPenetration
        {
            set { this.allowedPenetration = value; }
            get { return this.allowedPenetration; }
        }

        public bool IsShockStepEnabled
        {
            set { this.doShockStep = value; }
            get { return this.doShockStep; }
        }

        public float CollisionTollerance
        {
            get { return collToll; }
            set { collToll = value; }
        }

        public Solver SolverType
        {
            get { return solverType; }
            set { solverType = value; }
        }

        /// <summary>
        /// if nullUpdate then all updates will use dt = 0 (for debugging/profiling)
        /// </summary>
        public bool NullUpdate
        {
            set { nullUpdate = value; }
            get { return nullUpdate; }
        }

        public Vector3 Gravity
        {
            get { return this.gravity; }
            set
            {
                // Have a look here
                this.gravity = value;
                this.gravityMagnitude = value.Length();

                if (value.X == value.Y && value.Y == value.Z)
                    gravityAxis = -1;

                gravityAxis = 0;

                if (System.Math.Abs(value.Y) > System.Math.Abs(value.X))
                    gravityAxis = 1;

                float[] gravity = new float[3] { value.X, value.Y, value.Z };

                if (System.Math.Abs(value.Z) > System.Math.Abs(gravity[gravityAxis]))
                    gravityAxis = 2;
            }
        }

        public float GravityMagnitude
        {
            get { return this.gravityMagnitude; }
        }

        public int MainGravityAxis
        {
            get { return this.gravityAxis; }
        }

        public bool EnableFreezing
        {
            set
            {
                this.freezingEnabled = value;

                if (!freezingEnabled)
                {
                    int numBodies = bodies.Count;
                    for (int i = 0; i < numBodies; ++i)
                        bodies[i].SetActive();
                }

            }
        }

        public bool IsFreezingEnabled
        {
            get { return freezingEnabled; }
        }

        public List<CollisionInfo> Collisions
        {
            get { return collisions; }
        }

        private static Stack<Contact> freeContacts = new Stack<Contact>(128);
        private static Stack<CollisionIsland> freeCollisionIslands = new Stack<CollisionIsland>(64);

        static PhysicsSystem()
        {
            for (int i = 0; i < 128; ++i)
            {
                freeContacts.Push(new Contact());
            }
            for (int i = 0; i < 64; ++i)
            {
                freeCollisionIslands.Push(new CollisionIsland());
            }
        }
        private void UpdateContactCache()
        {
            foreach (Contact c in catchedContacts)
            {
                freeContacts.Push(c);
            }
            catchedContacts.Clear();

            for (int i = collisions.Count; i-- != 0; )
            {
                CollisionInfo collInfo = collisions[i];
                for (int pos = 0; pos < collInfo.NumCollPts; ++pos)
                {
                    CollPointInfo ptInfo = collInfo.PointInfo[pos];

                    int skinId1 = -1;
                    if (collInfo.SkinInfo.Skin1.Owner != null) skinId1 = collInfo.SkinInfo.Skin1.Owner.ID;


                    Vector3 fricImpulse = (collInfo.SkinInfo.Skin0.Owner.ID > skinId1) ?
                        ptInfo.AccumulatedFrictionImpulse : -ptInfo.AccumulatedFrictionImpulse;

                    if (freeContacts.Count == 0)
                    {
                        freeContacts.Push(new Contact());
                    }
                
                    Contact contact = freeContacts.Pop();
                    contact.Impulse = new Contact.CachedImpulse(ptInfo.AccumulatedNormalImpulse, ptInfo.AccumulatedNormalImpulseAux, ref fricImpulse);
                    contact.Pair = new Contact.BodyPair(collInfo.SkinInfo.Skin0.Owner, collInfo.SkinInfo.Skin1.Owner, ref ptInfo.info.R0, ref ptInfo.info.R1);

                    catchedContacts.Add(contact);
                }
            }
        }
    }
}

