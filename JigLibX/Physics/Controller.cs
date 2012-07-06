#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
#endregion

namespace JigLibX.Physics
{

    /// <summary>
    /// This can get updated at the same time as Body.AddExternalForces so that forces
    /// can be added independant of individual bodies - e.g. joints between pairs of bodies.
    /// </summary>
    public abstract class Controller
    {

        private bool controllerEnabled = false;

        /// <summary>
        /// Register with the physics system.
        /// </summary>
        public void EnableController()
        {
            if (PhysicsSystem.CurrentPhysicsSystem == null) return;
            if (controllerEnabled) return;

            controllerEnabled = true;
            
            PhysicsSystem.CurrentPhysicsSystem.AddController(this);
        }

        /// <summary>
        /// Deregister from the physics system.
        /// </summary>
        public void DisableController()
        {
            if (PhysicsSystem.CurrentPhysicsSystem == null) return;
            if (!controllerEnabled) return;

            controllerEnabled = false;
            PhysicsSystem.CurrentPhysicsSystem.RemoveController(this);
        }

        /// <summary>
        /// Are we registered with the physics system?
        /// </summary>
        public bool IsControllerEnabled
        {
            get { return controllerEnabled; }
        }

        /// <summary>
        /// implement this to apply whatever forces are needed to the
        /// objects this controls
        /// </summary>
        /// <param name="dt"></param>
        public abstract void UpdateController(float dt);

    }
}
