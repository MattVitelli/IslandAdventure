using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Gaia.Core;
using Gaia.Resources;
namespace Gaia.SceneGraph.GameEntities
{
    public class Raptor : Actor
    {
        ViewModel model;
        DinosaurDatablock datablock;

        const string IDLE_NAME = "AllosaurusIdle";
        const string RUN_NAME = "AllosaurusRunN";
        const string WALK_NAME = "AlphaRaptorWalk";
        const string STEALTHIDLE_NAME = "AllosaurusIdle";
        const string ROAR_NAME = "AllosaurusBark";
        const string JUMPNAV_NAME = "AlphaRaptorJumpNav";
        const string ATTACK_NAME = "AllosaurusShove";
        const string MELEE_NAME = "AllosaurusShove";
        const string LEAPSTART_NAME = "AlphaRaptorLeapStart";
        const string LEAPIDLE_NAME = "AlphaRaptorLeapIdle";
        const string LEAPEND_NAME = "AlphaRaptorLeapEnd";
        const string LEAPATTACK_NAME = "AlphaRaptorLeapAttack";


        public enum RaptorState
        {
            Wander = 0,
            Stealth,
            Chase,
            Attack,
            Dead
        }

        const float DISTANCE_EPSILON = 1.0f;

        const int WANDER_MAX_MOVES = 3;
        const int WANDER_DISTANCE = 160;
        const float WANDER_DELAY_SECONDS = 4.0f;
        const float ATTACK_DELAY_SECONDS = 1.5f;
        const float SIGHT_DISTANCE = 120;
        const float ATTACK_DISTANCE = 5;
        const float MIN_ATTACK_DISTANCE = 3;

        int wanderMovesCount;
        Vector3 wanderPosition;
        Vector3 wanderStartPosition;
        float wanderDelayTime;

        Vector3 velocityVector = Vector3.Zero;
        const float speed =  9.5f;
        NormalTransform grounding = new NormalTransform();

        Actor enemy = null;

        RaptorState state;

        public Raptor(DinosaurDatablock datablock)
        {
            this.datablock = datablock;
            model = new ViewModel(datablock.MeshName);

            grounding.SetScale(Vector3.One * 0.09f);
            grounding.SetRotation(new Vector3(-MathHelper.PiOver2, MathHelper.Pi, 0));
            BoundingBox bounds = model.GetMeshBounds();
            Vector3 midPoint = (bounds.Max + bounds.Min) * 0.5f;
            grounding.SetPosition(Vector3.Up * (midPoint.Y - bounds.Min.Y) * grounding.GetScale() * 1.15f);

            model.SetCustomMatrix(grounding.GetTransform());
            team = datablock.Team;
        }

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);
            model.SetTransform(this.Transformation);
        }

        void UpdateAnimation()
        {
            float vel = velocityVector.Length();
            if (vel < 0.01f)
            {
                model.GetAnimationLayer().AddAnimation(datablock.Animations[(int)DinosaurAnimations.Idle][0]);
                model.GetAnimationLayer().AddAnimation(IDLE_NAME);//.SetAnimationLayer(IDLE_NAME, 1.0f);
            }
            else
            {
                //model.SetAnimationLayer(IDLE_NAME, 0.0f);
                float walkWeight = MathHelper.Clamp((3.5f - vel) / 3.5f, 0.0f, 1.0f);
                float runWeight = 1.0f - walkWeight;
                //model.SetAnimationLayer(WALK_NAME, walkWeight);
                model.GetAnimationLayer().AddAnimation(RUN_NAME);//.SetAnimationLayer(RUN_NAME, 1.0f);
                //model.SetAnimationLayer(ATTACK_NAME, 0.0f);
            }
            if (state == RaptorState.Attack)
            {
                //model.SetAnimationLayer(RUN_NAME, 0.0f);
                model.GetAnimationLayer().AddAnimation(MELEE_NAME, true);
                //model.SetAnimationLayer(MELEE_NAME, 1.0f);
            }
            //grounding.SetForwardVector(Vector3.Normalize(velocityVector));
            if (velocityVector.Length() > 0.01f)
                grounding.SetForwardVector(Vector3.Normalize(velocityVector));
            grounding.ConformToNormal(body.GetContactNormal());

            model.SetCustomMatrix(grounding.GetTransform());
            model.OnUpdate();
        }

        private void Wander()
        {
            // Calculate wander vector on X, Z axis
            Vector3 wanderVector = wanderPosition - Transformation.GetPosition();
            wanderVector.Y = 0;
            float wanderVectorLength = wanderVector.Length();

            // Reached the destination position
            if (wanderVectorLength < DISTANCE_EPSILON)
            {
                Random rand = new Random();
                // Generate new random position
                if (wanderMovesCount < WANDER_MAX_MOVES)
                {
                    wanderPosition = Transformation.GetPosition() +
                        WANDER_DISTANCE * (2.0f * new Vector3((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble()) - Vector3.One);

                    wanderMovesCount++;
                }
                // Go back to the start position
                else
                {
                    wanderPosition = wanderStartPosition;
                    wanderMovesCount = 0;
                }

                // Next time wander
                wanderDelayTime = WANDER_DELAY_SECONDS +
                    WANDER_DELAY_SECONDS * (float)rand.NextDouble();

                velocityVector = Vector3.Zero;
            }

            wanderDelayTime -= Time.GameTime.ElapsedTime;

            // Wait for the next action time
            if (wanderDelayTime <= 0.0f)
            {
                Move(Vector3.Normalize(wanderVector));
            }
        }

        void Move(Vector3 moveDir)
        {
            Vector3 forwardVec = this.Transformation.GetTransform().Forward;
            Vector3 strafeVec = this.Transformation.GetTransform().Right;
            
            float radianAngle = (float)Math.Acos(Vector3.Dot(forwardVec, moveDir));//Y * moveDir.Y);
            Vector3 rot = Transformation.GetRotation();
            if (radianAngle >= 0.075f)
            {
                if (Vector3.Dot(strafeVec, moveDir) < 0)
                    rot.Y += radianAngle * 0.02f;
                else
                    rot.Y -= radianAngle * 0.02f;
            }
            //Transformation.SetRotation(rot);
            velocityVector = moveDir * speed;
        }

        void AcquireEnemy()
        {
            enemy = null;
            float minDist = float.PositiveInfinity;
            for (int i = 0; i < scene.Actors.Count; i++)
            {
                Actor currActor = scene.Actors[i];
                if (currActor.GetTeam() != this.GetTeam() && !currActor.IsDead())
                {
                    float dist = Vector3.DistanceSquared(currActor.Transformation.GetPosition(), this.Transformation.GetPosition());
                    if (dist < minDist)
                    {
                        enemy = currActor;
                        minDist = dist;
                    }
                }
            }
        }

        void PerformBehavior()
        {
            if (this.IsDead())
            {
                return;
            }

            if (enemy == null || enemy.IsDead())
            {
                AcquireEnemy();
            }
            float distanceToTarget = float.PositiveInfinity;
            Vector3 targetVec = Vector3.Forward;

            if (enemy != null)
            {

                targetVec = enemy.Transformation.GetPosition() - this.Transformation.GetPosition();
                distanceToTarget = targetVec.Length();
                targetVec *= 1.0f / distanceToTarget; //Normalize the vector
            }

            switch (state)
            {
                case RaptorState.Wander:
                    if (distanceToTarget < SIGHT_DISTANCE)
                        // Change state
                        state = RaptorState.Chase;
                    else
                        Wander();
                    break;

                case RaptorState.Chase:
                    if (distanceToTarget <= ATTACK_DISTANCE)
                    {
                        // Change state
                        state = RaptorState.Attack;
                        wanderDelayTime = 0;
                    }
                    if (distanceToTarget > SIGHT_DISTANCE * 1.35f)
                        state = RaptorState.Wander;
                    else if (distanceToTarget > MIN_ATTACK_DISTANCE)
                    {
                        Move(targetVec);
                    }
                    else
                    {
                        Move(-targetVec);
                    }
                    break;

                case RaptorState.Attack:
                    if (distanceToTarget > ATTACK_DISTANCE * 1.5f)// || distanceToTarget < MIN_ATTACK_DISTANCE)
                    {
                        state = RaptorState.Chase;
                    }
                    else
                    {
                        Move(targetVec);
                        state = RaptorState.Attack;
                        //Attack
                    }
                    break;

                default:
                    break;
            }
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            state = RaptorState.Dead;
            velocityVector = Vector3.Zero;
        }

        protected override void ResetState()
        {
            base.ResetState();
            wanderMovesCount = 0;
            // Unit configurations
            enemy = null;

            wanderPosition = Transformation.GetPosition();
            wanderStartPosition = wanderPosition;
            state = RaptorState.Wander;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            PerformBehavior();
            UpdateAnimation();
            body.DesiredVelocity = velocityVector;
        }

        public override void OnRender(Gaia.Rendering.RenderViews.RenderView view)
        {
            base.OnRender(view);
            model.OnRender(view, true);
        }
    }
}
