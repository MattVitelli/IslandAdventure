using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Core;
using Gaia.Physics;
using Gaia.Rendering;
using Gaia.SceneGraph;

namespace Gaia.Voxels
{
    public class VoxelCollision
    {
        Scene scene;

        const float CollisionDeleteTimeS = 10; //Ten seconds of idle collision before we delete the mesh
        float CollisionDeleteTime = CollisionDeleteTimeS;

        BoundingBox boundsWorldSpaceCollision;
        Transform transformation;
        VoxelGeometry geometry;

        public VoxelCollision(VoxelGeometry voxel, Transform transform, BoundingBox bounds, Scene scene)
        {
            geometry = voxel;
            transformation = transform;
            this.scene = scene;

            boundsWorldSpaceCollision = bounds;
            boundsWorldSpaceCollision.Min = bounds.Min * 1.5f;
            boundsWorldSpaceCollision.Max = bounds.Max * 1.5f;
            if (geometry.CanRender)
            {
                //GenerateCollisionMesh();
            }
        }

        /*
        public void UpdateCollision()
        {
            if (geometry.CanRender)
            {
                RigidBody[] bodies = PhysicsHelper.PhysicsBodiesVolume(boundsWorldSpaceCollision, scene.GetPhysicsEngine());
                if(CollisionMesh == null && bodies.Length > 0)
                {
                    GenerateCollisionMesh();
                    CollisionDeleteTime = CollisionDeleteTimeS;
                }
                else if (CollisionMesh != null)
                {
                    if (bodies.Length < 1)
                    {
                        CollisionDeleteTime -= Time.GameTime.ElapsedTime;
                        if (CollisionDeleteTime <= 0)
                        {
                            PhysicsSystem.CurrentPhysicsSystem.CollisionSystem.RemoveCollisionSkin(Collision);
                            Collision = null;
                            CollisionMesh = null;
                        }
                    }
                    else
                    {
                        CollisionDeleteTime = CollisionDeleteTimeS;
                    }
                }
            }
        }
        */

        /*
        void GenerateCollisionMesh()
        {
            List<JVector> jvertices = new List<JVector>(geometry.verts.Length);

            Matrix transform = transformation.GetTransform();
            for (int i = 0; i < geometry.verts.Length; i++)
            {
                Vector3 vertex = Vector3.Transform(new Vector3(geometry.verts[i].Position.X, geometry.verts[i].Position.Y, geometry.verts[i].Position.Z), transform);
                jvertices.Add(JitterConverter.ToJitterVector(vertex));
            }

            int triCount = 0;
            TriangleVertexIndices triIdx = new TriangleVertexIndices(0, 0, 0);
            List<TriangleVertexIndices> triColl = new List<TriangleVertexIndices>();
            for (int i = 0; i < geometry.ib.Length; i++)
            {
                switch (triCount)
                {
                    case 0:
                        triIdx.I2 = geometry.ib[i];
                        break;
                    case 1:
                        triIdx.I1 = geometry.ib[i];
                        break;
                    case 2:
                        triIdx.I0 = geometry.ib[i];
                        triCount = -1;
                        triColl.Add(triIdx);
                        triIdx = new TriangleVertexIndices(0, 0, 0);
                        break;
                }
                triCount++;
            }

            Octree octree = new Octree(jvertices, triColl);

            TriangleMeshShape tms = new TriangleMeshShape(octree);
            RigidBody body = new RigidBody(tms);
            body.IsStatic = true;

            scene.GetPhysicsEngine().AddBody(body);
        }
        */
    }
}
