#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
#endregion

namespace JigLibX.Collision
{

    #region public struct MaterialPairProperties
    public struct MaterialPairProperties
    {
        public float Restitution;
        public float StaticFriction;
        public float DynamicFriction;

        public MaterialPairProperties(float r, float sf, float df)
        {
            this.Restitution = r;
            this.DynamicFriction = df;
            this.StaticFriction = sf;
        }
    }
    #endregion

    #region public struct MaterialProperties
    public struct MaterialProperties
    {
        public float Elasticity;
        public float StaticRoughness;
        public float DynamicRoughness;

        public MaterialProperties(float e, float sr, float dr)
        {
            this.Elasticity = e;
            this.StaticRoughness = sr;
            this.DynamicRoughness = dr;
        }

        public static MaterialProperties Unset { get { return new MaterialProperties(); } }

    }
    #endregion

    /// <summary>
    /// This handles the properties of interactions between different
    /// materials.
    /// </summary>
    public class MaterialTable
    {

        /// <summary>
        /// Some default materials that get added automatically User
        /// materials should start at NumMaterialTypes, or else
        /// ignore this and over-ride everything. User-refined values can
        /// get used so should not assume the values come form this enum -
        /// use MaterialID
        /// </summary>
        public enum MaterialID
        {
            Unset, UserDefined, //< individual values should be used/calculated at run-time
            NotBouncySmooth, NotBouncyNormal, NotBouncyRough,
            NormalSmooth, NormalNormal, NormalRough,
            BouncySmooth, BouncyNormal, BouncyRough,
            Player,
            NumMaterialTypes
        }

        private Dictionary<int, MaterialProperties> materials =
            new Dictionary<int, MaterialProperties>();

        private Dictionary<int, MaterialPairProperties> materialPairs = new Dictionary<int, MaterialPairProperties>();

        /// <summary>
        /// On construction all the default Materials get added
        /// </summary>
        public MaterialTable()
        {
            Reset();
        }

        /// <summary>
        /// Clear everything except the default Materials
        /// </summary>
        public void Reset()
        {
            Clear();

            float normalBouncy = 0.3f; float normalRoughS = 0.5f; float normalRoughD = 0.43f;
            float roughRoughS = 0.65f; float roughRoughD = 0.5f;

            SetMaterialProperties((int)MaterialID.Unset, new MaterialProperties(0.0f, 0.0f, 0.0f));
            SetMaterialProperties((int)MaterialID.NotBouncySmooth, new MaterialProperties(0.0f, 0.0f, 0.0f));
            SetMaterialProperties((int)MaterialID.NotBouncyNormal, new MaterialProperties(0.0f, normalRoughS, normalRoughD));
            SetMaterialProperties((int)MaterialID.NotBouncyRough, new MaterialProperties(0.0f, roughRoughD, roughRoughD));
            SetMaterialProperties((int)MaterialID.NormalSmooth, new MaterialProperties(normalBouncy, 0.0f, 1.0f));
            SetMaterialProperties((int)MaterialID.NormalNormal, new MaterialProperties(normalBouncy, normalRoughS, normalRoughD));
            SetMaterialProperties((int)MaterialID.NormalRough, new MaterialProperties(normalBouncy, roughRoughS, roughRoughD));
            SetMaterialProperties((int)MaterialID.BouncySmooth, new MaterialProperties(1.0f, 0.0f, 0.0f));
            SetMaterialProperties((int)MaterialID.BouncyNormal, new MaterialProperties(1.0f, normalRoughS, normalRoughD));
            SetMaterialProperties((int)MaterialID.BouncyRough, new MaterialProperties(1.0f, roughRoughS, roughRoughD));
            SetMaterialProperties((int)MaterialID.Player, new MaterialProperties(0.0f, 1.0f, 1.0f));
        }

        /// <summary>
        /// Clear everything
        /// </summary>
        public void Clear()
        {
            materials.Clear();
            materialPairs.Clear();
        }

        /// <summary>
        /// This adds/overrides a material, and sets all the pairs for
        /// existing materials using some sensible heuristic
        /// </summary>
        /// <param name="id"></param>
        /// <param name="properties"></param>
        public void SetMaterialProperties(int id, MaterialProperties properties)
        {
            materials[id] = properties;

            foreach (KeyValuePair<int, MaterialProperties> it in materials)
            {
                int otherID = it.Key;
                MaterialProperties mat = it.Value;

                int key01 = otherID << 16 | id;
                int key10 = id << 16 | otherID;
                materialPairs[key01] = materialPairs[key10] =
                    new MaterialPairProperties(properties.Elasticity * mat.Elasticity,
                        properties.StaticRoughness * mat.StaticRoughness,
                        properties.DynamicRoughness * mat.DynamicRoughness);
            }
        }

        /// <summary>
        /// Returns properties of a material - defaults on inelastic
        /// frictionless.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public MaterialProperties GetMaterialProperties(int id)
        {
            return materials[id];
        }

        /// <summary>
        /// Gets the properties for a pair of materials. Same result even
        /// if the two ids are swapped
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id2"></param>
        /// <returns></returns>
        public MaterialPairProperties GetPairProperties(int id1, int id2)
        {
            int key = id1 << 16 | id2;
            return materialPairs[key];
        }

        /// <summary>
        /// This overrides the result for a single pair of materials. It's
        /// recommended that you add all materials first. Order of ids
        /// doesn't matter
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id2"></param>
        /// <param name="pairProperties"></param>
        public void SetMaterialPairProperties(int id1, int id2, MaterialPairProperties pairProperties)
        {
            int key01 = id1 << 16 | id2;
            int key10 = id2 << 16 | id1;
            materialPairs[key01] = materialPairs[key10] = pairProperties;
        }

    }

}
