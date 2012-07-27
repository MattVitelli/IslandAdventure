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

        /*
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
        */

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

        float animationDelay = 0;

        Vector3 velocityVector = Vector3.Zero;
        const float speed =  7.5f;
        NormalTransform grounding = new NormalTransform();

        Actor enemy = null;

        RaptorState state;

        public Raptor(DinosaurDatablock datablock)
        {
            this.datablock = datablock;
            model = new ViewModel(datablock.MeshName);
            model.GetAnimationLayer().SetActiveAnimation(datablock.GetAnimation(DinosaurAnimationsSimple.Idle), true);

            grounding.SetScale(datablock.Scale);
            grounding.SetRotation(datablock.Rotation);
            grounding.SetPosition(datablock.Position);

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
            if (IsDead())
                return;

            float vel = velocityVector.Length();
            if (vel < 0.015f)
            {
                model.GetAnimationLayer().SetActiveAnimation(datablock.GetAnimation(DinosaurAnimationsSimple.Idle), false);
            }
            else
            {
                //model.SetAnimationLayer(IDLE_NAME, 0.0f);
                float walkWeight = MathHelper.Clamp(1 - vel / 3.5f, 0.0f, 1.0f);
                float runWeight = 1.0f - walkWeight;
                if (walkWeight > 0.5f)
                    model.GetAnimationLayer().SetActiveAnimation(datablock.GetAnimation(DinosaurAnimationsSimple.Walk), false);
                else
                    model.GetAnimationLayer().SetActiveAnimation(datablock.GetAnimation(DinosaurAnimationsSimple.Run), false);
            }
            if (state == RaptorState.Attack)
            {
                if (animationDelay <= 0.0f)
                {
                    string attackAnim = datablock.GetAnimation(DinosaurAnimationsSimple.Attack);
                    model.GetAnimationLayer().AddAnimation(attackAnim, true);
                    animationDelay = ResourceManager.Inst.GetAnimation(attackAnim).EndTime;
                }
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

            float radianAngle = (float)Math.Acos(Vector3.Dot(forwardVec, moveDir));
            Vector3 rot = Transformation.GetRotation();
            if (Math.Abs(radianAngle) >= 0.075f)
            {
                radianAngle = MathHelper.Clamp(radianAngle, -1, 1);
                if (Vector3.Dot(strafeVec, moveDir) < 0)
                {
                    rot.Y += radianAngle * 0.02f;
                    //model.GetAnimationLayer().AddAnimation(datablock.GetAnimation(DinosaurAnimationsSimple.TurnLeft), true);
                }
                else
                {
                    rot.Y -= radianAngle * 0.02f;
                    //model.GetAnimationLayer().AddAnimation(datablock.GetAnimation(DinosaurAnimationsSimple.TurnRight), true);
                }
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
            animationDelay -= Time.GameTime.ElapsedTime;

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
                        if(distanceToTarget > ATTACK_DISTANCE)
                            Move(targetVec);
                        state = RaptorState.Attack;
                        if (animationDelay <= 0.0f)
                        {
                            enemy.ApplyDamage(datablock.Damage);
                        }
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
