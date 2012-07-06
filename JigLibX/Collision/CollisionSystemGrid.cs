#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using JigLibX.Collision;
using JigLibX.Physics;
using JigLibX.Geometry;
using JigLibX.Math;
using System.Collections.ObjectModel;
#endregion

namespace JigLibX.Collision
{

    #region GridEntry
    /// <summary>
    /// Double linked list used to contain all the skins in a grid box
    /// </summary>
    internal class GridEntry
    {
        public CollisionSkin Skin;
        public GridEntry Previous;
        public GridEntry Next;
        public int GridIndex;

        public GridEntry()
        {
        }

        public GridEntry(CollisionSkin skin)
        {
            this.Skin = skin;
            this.Previous = this.Next = null;
        }

        /// <summary>
        /// Removes the entry by updating its neighbours. Also zaps the prev/next
        /// pointers in the entry, to help debugging
        /// </summary>
        /// <param name="entry"></param>
        public static void RemoveGridEntry(GridEntry entry)
        {
            // link the previous to the next (may be 0)
            entry.Previous.Next = entry.Next;
            // link the next (if it exists) to the previous.
            if (entry.Next != null)
                entry.Next.Previous = entry.Previous;
            // tidy up this entry
            entry.Previous = entry.Next = null;
            entry.GridIndex = -2;
        }


        /// <summary>
        /// Inserts an entry after prev, updating all links
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="prev"></param>
        public static void InsertGridEntryAfter(GridEntry entry, GridEntry prev)
        {
            GridEntry next = prev.Next;
            prev.Next = entry;
            entry.Previous = prev;
            entry.Next = next;
            if (next != null)
                next.Previous = entry;
            entry.GridIndex = prev.GridIndex;
        }

    }
    #endregion

    /// <summary>
    /// implements a collision system by dividing the world up into a wrapping
    /// grid with a certain configurable size. If objects are evenly distributed
    /// this will reduce the number of checks that need to be made.
    /// </summary>
    public class CollisionSystemGrid : CollisionSystem
    {
        private List<GridEntry> gridEntries;
        private List<CollisionSkin> skins = new List<CollisionSkin>();
        private List<AABox> gridBoxes;
        private List<GridEntry> tempGridLists;

        private GridEntry overflowEntries;

        private int nx, ny, nz;
        private float dx, dy, dz;
        private float sizeX, sizeY, sizeZ;

        // minimum of the grid deltas
        private float minDelta;

        private Stack<GridEntry> freeGrids;

        /// <summary>
        /// Initializes a new CollisionSystem which uses a grid to speed up collision detection.
        /// Use this system for larger scenes with many objects.
        /// </summary>
        /// <param name="nx">Number of GridEntries in X Direction.</param>
        /// <param name="ny">Number of GridEntries in Y Direction.</param>
        /// <param name="nz">Number of GridEntries in Z Direction.</param>
        /// <param name="dx">Size of a single GridEntry in X Direction.</param>
        /// <param name="dy">Size of a single GridEntry in Y Direction.</param>
        /// <param name="dz">Size of a single GridEntry in Z Direction.</param>
        public CollisionSystemGrid(int nx, int ny, int nz, float dx, float dy, float dz)
        {
            this.nx = nx; this.ny = ny; this.nz = nz;
            this.dx = dx; this.dy = dy; this.dz = dz;

            this.sizeX = nx * dx;
            this.sizeY = ny * dy;
            this.sizeZ = nz * dz;
            this.minDelta = System.Math.Min(System.Math.Min(dx, dy), dz);

            int numEntries = nx * ny * nz * 2; // we allocate twice as many as need for collision skins
            gridEntries = new List<GridEntry>(numEntries);
            gridBoxes = new List<AABox>(numEntries);

            tempGridLists = new List<GridEntry>(numEntries);

            freeGrids = new Stack<GridEntry>(numEntries);
            for (int i = 0; i < numEntries; ++i)
            {
                GridEntry entry = new GridEntry();
                entry.GridIndex = -2;
                freeGrids.Push(entry);
            }

            for (int i = 0; i < (nx * ny * nz); ++i)
            {
                GridEntry gridEntry = freeGrids.Pop();
                gridEntry.GridIndex = i;
                gridEntries.Add(gridEntry);
                gridBoxes.Add(null);
            }

            overflowEntries = freeGrids.Pop();
            overflowEntries.GridIndex = -1;

            for (int iX = 0; iX < nx; ++iX)
            {
                for (int iY = 0; iY < ny; ++iY)
                {
                    for (int iZ = 0; iZ < nz; ++iZ)
                    {
                        AABox box = new AABox();
                        box.AddPoint(new Vector3(iX * dx, iY * dy, iZ + dz));
                        box.AddPoint(new Vector3(iX * dx + dx, iY * dy + dy, iZ * dz + dz));

                        int index = CalcIndex(iX, iY, iZ);
                        gridBoxes[index] = box;
                    }
                }
            }
        }


        public override ReadOnlyCollection<CollisionSkin> CollisionSkins
        {
            get { return skins.AsReadOnly(); }
        }

        private int CalcIndex(int i, int j, int k)
        {
            int I = i % nx;
            int J = j % ny;
            int K = k % nz;
            return I + nx * J + (nx + ny) * K;
        }

        private void CalcGridForSkin(out int i, out int j, out int k, CollisionSkin skin)
        {
            Vector3 sides = skin.WorldBoundingBox.Max - skin.WorldBoundingBox.Min;
            if ((sides.X > dx) || (sides.Y > dy) || (sides.Z > dz))
            {
                System.Diagnostics.Debug.WriteLine("CollisionSkin too big for gridding system - putting it into overflow list.");
                i = j = k = -1;
                return;
            }

            Vector3 min = skin.WorldBoundingBox.Min;

            min.X = JiggleMath.Wrap(min.X, 0.0f, sizeX);
            min.Y = JiggleMath.Wrap(min.Y, 0.0f, sizeY);
            min.Z = JiggleMath.Wrap(min.Z, 0.0f, sizeZ);

            i = (int)(min.X / dx) % nx;
            j = (int)(min.Y / dy) % ny;
            k = (int)(min.Z / dz) % nz;
        }

        public void CalcGridForSkin(out int i, out int j, out int k, out float fi,
            out float fj, out float fk, CollisionSkin skin)
        {
            Vector3 sides = skin.WorldBoundingBox.Max - skin.WorldBoundingBox.Min;
            if ((sides.X > dx) || (sides.Y > dy) || (sides.Z > dz))
            {
                System.Diagnostics.Debug.WriteLine("CollisionSkin too big for gridding system - putting it into overflow list.");

                i = j = k = -1;
                fi = fj = fk = 0.0f;
                return;
            }

            Vector3 min = skin.WorldBoundingBox.Min;

            min.X = JiggleMath.Wrap(min.X, 0.0f, sizeX);
            min.Y = JiggleMath.Wrap(min.Y, 0.0f, sizeY);
            min.Z = JiggleMath.Wrap(min.Z, 0.0f, sizeZ);

            fi = min.X / dx;
            fj = min.Y / dy;
            fk = min.Z / dz;

            i = (int)fi;
            j = (int)fj;
            k = (int)fk;

            if (i < 0) { i = 0; fi = 0.0f; }
            else if (i >= (int)nx) { i = 0; fi = 0.0f; }
            else fi -= (float)i;

            if (j < 0) { j = 0; fj = 0.0f; }
            else if (j >= (int)ny) { j = 0; fj = 0.0f; }
            else fj -= (float)j;

            if (k < 0) { k = 0; fk = 0.0f; }
            else if (k >= (int)nz) { k = 0; fk = 0.0f; }
            else fk -= (float)k;
        }

        private int CalcGridIndexForSkin(CollisionSkin skin)
        {
            int i, j, k;
            CalcGridForSkin(out i, out j, out k, skin);
            if (i == -1) return -1;
            return CalcIndex(i, j, k);
        }

        public override void AddCollisionSkin(CollisionSkin skin)
        {
            if (skins.Contains(skin))
                System.Diagnostics.Debug.WriteLine("Warning: tried to add skin to CollisionSkinGrid but it's already registered");
            else
                skins.Add(skin);

            skin.CollisionSystem = this;

            if (freeGrids.Count == 0)
            {
                freeGrids.Push(new GridEntry());
            }

            // also do the grid stuff - for now put it on the overflow list
            GridEntry entry = freeGrids.Pop();
            skin.ExternalData = entry;
            entry.Skin = skin;
            // add entry to the start of the list
            GridEntry.InsertGridEntryAfter(entry, overflowEntries);
            CollisionSkinMoved(skin);
        }

        public override bool RemoveCollisionSkin(CollisionSkin skin)
        {
            GridEntry entry = (GridEntry)skin.ExternalData;

            if (entry != null)
            {
                entry.Skin = null;
                freeGrids.Push(entry);
                GridEntry.RemoveGridEntry(entry);
                skin.ExternalData = null;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Warning - skin being deleted without a grid entry");
            }

            if (!skins.Contains(skin)) return false;
            skins.Remove(skin);
            return true;
        }

        public override void CollisionSkinMoved(CollisionSkin skin)
        {
            GridEntry entry = (GridEntry)skin.ExternalData;
            if (entry == null)
            {
                System.Diagnostics.Debug.WriteLine("Warning skin has grid entry null!");
                return;
            }

            int gridIndex = CalcGridIndexForSkin(skin);

            // see if it's moved grid
            if (gridIndex == entry.GridIndex)
                return;

            GridEntry start;
            if (gridIndex >= 0)
                start = gridEntries[gridIndex];
            else
                start = overflowEntries;

            GridEntry.RemoveGridEntry(entry);
            GridEntry.InsertGridEntryAfter(entry, start);
        }

        private void GetListsToCheck(List<GridEntry> entries, CollisionSkin skin)
        {
            entries.Clear();

            GridEntry entry = (GridEntry)skin.ExternalData;
            if (entry == null)
            {
                System.Diagnostics.Debug.WriteLine("Warning skin has grid entry null!");
                //TRACE("Warning = skin %s has grid entry 0!\n", skin);
                return;
            }

            // todo - work back from the mGridIndex rather than calculating it again...
            int i, j, k;
            float fi, fj, fk;
            CalcGridForSkin(out i, out j, out k, out fi, out fj, out fk, skin);

            if (i == -1)
            {
                // oh dear - add everything
                for (i = 0; i < gridEntries.Count; ++i)
                {
                    if (gridEntries[i].Next != null)
                    {
                        entries.Add(gridEntries[i]);
                    }
                }
                //entries = gridEntries;
                entries.Add(overflowEntries);
                return;
            }

            // always add the overflow
            entries.Add(overflowEntries);

            Vector3 delta = skin.WorldBoundingBox.Max - skin.WorldBoundingBox.Min;
            int maxI = 1, maxJ = 1, maxK = 1;
            if (fi + (delta.X / dx) < 1.0f)
                maxI = 0;
            if (fj + (delta.Y / dy) < 1.0f)
                maxJ = 0;
            if (fk + (delta.Z / dz) < 1.0f)
                maxK = 0;

            // now add the contents of all 18 grid boxes - their contents may extend beyond the bounds
            for (int di = -1; di <= maxI; ++di)
            {
                for (int dj = -1; dj <= maxJ; ++dj)
                {
                    for (int dk = -1; dk <= maxK; ++dk)
                    {
                        int thisIndex = CalcIndex(nx + i + di, ny + j + dj, nz + k + dk);
                        GridEntry start = gridEntries[thisIndex];
                        if (start.Next != null)
                            entries.Add(start);
                    }
                }
            }
        }

        private static bool CheckCollidables(CollisionSkin skin0, CollisionSkin skin1)
        {
            List<CollisionSkin> nonColl0 = skin0.NonCollidables;
            List<CollisionSkin> nonColl1 = skin1.NonCollidables;

            //most common case
            if (nonColl0.Count == 0 && nonColl1.Count == 0)
                return true;

            for (int i0 = nonColl0.Count; i0-- != 0; )
            {
                if (nonColl0[i0] == skin1)
                    return false;
            }

            for (int i1 = nonColl1.Count; i1-- != 0; )
            {
                if (nonColl1[i1] == skin0)
                    return false;
            }

            return true;
        }

        public override void DetectCollisions(Body body, CollisionFunctor collisionFunctor, CollisionSkinPredicate2 collisionPredicate, float collTolerance)
        {
            if (!body.IsActive)
                return;

            CollDetectInfo info = new CollDetectInfo();

            info.Skin0 = body.CollisionSkin;
            if (info.Skin0 == null)
                return;

            int bodyPrimitives = info.Skin0.NumPrimitives;
            int numSkins = skins.Count;

            for (int skin = 0; skin < numSkins; ++skin)
            {
                info.Skin1 = skins[skin];
                if ((info.Skin0 != info.Skin1) && CheckCollidables(info.Skin0, info.Skin1))
                {
                    int primitives = info.Skin1.NumPrimitives;

                    for (info.IndexPrim0 = 0; info.IndexPrim0 < bodyPrimitives; ++info.IndexPrim0)
                    {
                        for (info.IndexPrim1 = 0; info.IndexPrim1 < primitives; ++info.IndexPrim1)
                        {
                            DetectFunctor f =
                                GetCollDetectFunctor(info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0).Type,
                                info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1).Type);
                            if (f != null)
                                f.CollDetect(info, collTolerance, collisionFunctor);
                        }
                    }
                }
            }
        }

        public override void DetectAllCollisions(List<Body> bodies, CollisionFunctor collisionFunctor, CollisionSkinPredicate2 collisionPredicate, float collTolerance)
        {
            int numBodies = bodies.Count;

            CollDetectInfo info = new CollDetectInfo();

            for (int iBody = 0; iBody < numBodies; ++iBody)
            {
                Body body = bodies[iBody];
                if (!body.IsActive)
                    continue;

                info.Skin0 = body.CollisionSkin;
                if (info.Skin0 == null)
                    continue;

                tempGridLists.Clear();
                GetListsToCheck(tempGridLists, info.Skin0);

                for (int iList = tempGridLists.Count; iList-- != 0; )
                {
                    // first one is a placeholder;
                    GridEntry entry = tempGridLists[iList];
                    for (entry = entry.Next; entry != null; entry = entry.Next)
                    {
                        info.Skin1 = entry.Skin;
                        if (info.Skin1 == info.Skin0)
                            continue;

                        // CHANGE
                        if (info.Skin1 == null)
                            continue;

                        bool skinSleeping = true;

                        if ((info.Skin1.Owner != null) && (info.Skin1.Owner.IsActive))
                            skinSleeping = false;

                        // only do one per pair
                        if ((skinSleeping == false) && (info.Skin1.ID < info.Skin0.ID))
                             continue;

                        if ((collisionPredicate != null) && (!collisionPredicate.ConsiderSkinPair(info.Skin0, info.Skin1)))
                            continue;

                        // basic bbox test
                        if (BoundingBoxHelper.OverlapTest(ref info.Skin1.WorldBoundingBox,
                            ref info.Skin0.WorldBoundingBox, collTolerance))
                        {
                            if (CheckCollidables(info.Skin0, info.Skin1))
                            {
                                int bodyPrimitives = info.Skin0.NumPrimitives;
                                int primitves = info.Skin1.NumPrimitives;

                                for (info.IndexPrim0 = 0; info.IndexPrim0 < bodyPrimitives; ++info.IndexPrim0)
                                {
                                    for (info.IndexPrim1 = 0; info.IndexPrim1 < primitves; ++info.IndexPrim1)
                                    {
                                        DetectFunctor f = GetCollDetectFunctor(info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0).Type,
                                            info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1).Type);
                                        if (f != null)
                                            f.CollDetect(info, collTolerance, collisionFunctor);
                                    }
                                }
                            } // check collidables
                        } // overlapt test
                    }// loop over entries
                } // loop over lists
            } // loop over bodies
        }

        public override bool SegmentIntersect(out float fracOut, out CollisionSkin skinOut, out Vector3 posOut, out Vector3 normalOut, Segment seg, CollisionSkinPredicate1 collisionPredicate)
        {
            int numSkins = skins.Count;
            BoundingBox segBox = BoundingBoxHelper.InitialBox;
            BoundingBoxHelper.AddSegment(seg, ref segBox);

            //initialise the outputs
            fracOut = float.MaxValue;
            skinOut = null;
            posOut = normalOut = Vector3.Zero;

            // working vars
            float frac;
            Vector3 pos;
            Vector3 normal;

            for (int iskin = 0; iskin < numSkins; ++iskin)
            {
                CollisionSkin skin = skins[iskin];
                if ((collisionPredicate == null) ||
                    collisionPredicate.ConsiderSkin(skin))
                {
                    // basic bbox test
                    if (BoundingBoxHelper.OverlapTest(ref skin.WorldBoundingBox, ref segBox))
                    {
                        if (skin.SegmentIntersect(out frac, out pos, out normal, seg))
                        {
                            if (frac < fracOut)
                            {
                                posOut = pos;
                                normalOut = normal;
                                skinOut = skin;
                                fracOut = frac;
                            }
                        }

                    }
                }
            }

            if (fracOut > 1.0f) return false;
            fracOut = MathHelper.Clamp(fracOut, 0.0f, 1.0f);
            return true;
        }
    }
}

