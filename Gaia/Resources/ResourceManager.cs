using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.Xna.Framework;

namespace Gaia.Resources
{
    public enum ResourceTypes
    {
        Shader = 0,
        Texture,
        Material,
        Particle,
        Mesh,
        Animation,
        SoundEffect,
        Team,
        TerrainClimate,
        TerrainClimateVoxels,
        DinosaurDatablock,
        WeaponDatablock,

        Count
    }

    public class ResourceManager
    {
        public static ResourceManager Inst = null;

        SortedList<string, IResource>[] resources;

        string[] ResourceTypeTokens;
        Type[] ResourceTypeDefs;

        public ResourceManager()
        {
            Inst = this;
            resources = new SortedList<string, IResource>[(int)ResourceTypes.Count];
            for (int i = 0; i < resources.Length; i++)
            {
                resources[i] = new SortedList<string, IResource>();
            }
            RegisterResourceTypes();
        }

        void RegisterResourceTypes()
        {
            ResourceTypeDefs = new Type[(int)ResourceTypes.Count];
            ResourceTypeTokens = new string[(int)ResourceTypes.Count];

            ResourceTypeDefs[(int)ResourceTypes.Animation] = typeof(AnimationSequence);
            ResourceTypeTokens[(int)ResourceTypes.Animation] = "Animation";

            ResourceTypeDefs[(int)ResourceTypes.Material] = typeof(Material);
            ResourceTypeTokens[(int)ResourceTypes.Material] = "Material";

            ResourceTypeDefs[(int)ResourceTypes.Mesh] = typeof(Mesh);
            ResourceTypeTokens[(int)ResourceTypes.Mesh] = "Mesh";

            ResourceTypeDefs[(int)ResourceTypes.Particle] = typeof(ParticleEffect);
            ResourceTypeTokens[(int)ResourceTypes.Particle] = "ParticleEffect";

            ResourceTypeDefs[(int)ResourceTypes.Shader] = typeof(Shader);
            ResourceTypeTokens[(int)ResourceTypes.Shader] = "Shader";

            ResourceTypeDefs[(int)ResourceTypes.SoundEffect] = typeof(SoundEffect);
            ResourceTypeTokens[(int)ResourceTypes.SoundEffect] = "SoundEffect";

            ResourceTypeDefs[(int)ResourceTypes.Texture] = typeof(TextureResource);
            ResourceTypeTokens[(int)ResourceTypes.Texture] = "Texture";

            ResourceTypeDefs[(int)ResourceTypes.Team] = typeof(TeamResource);
            ResourceTypeTokens[(int)ResourceTypes.Team] = "Team";

            ResourceTypeDefs[(int)ResourceTypes.TerrainClimate] = typeof(TerrainClimate);
            ResourceTypeTokens[(int)ResourceTypes.TerrainClimate] = "TerrainClimate";

            ResourceTypeDefs[(int)ResourceTypes.TerrainClimateVoxels] = typeof(TerrainClimateVoxels);
            ResourceTypeTokens[(int)ResourceTypes.TerrainClimateVoxels] = "TerrainClimateVoxels";

            ResourceTypeDefs[(int)ResourceTypes.DinosaurDatablock] = typeof(DinosaurDatablock);
            ResourceTypeTokens[(int)ResourceTypes.DinosaurDatablock] = "DinosaurDatablock";

            ResourceTypeDefs[(int)ResourceTypes.WeaponDatablock] = typeof(WeaponDatablock);
            ResourceTypeTokens[(int)ResourceTypes.WeaponDatablock] = "WeaponDatablock";
        }

        public void LoadResources()
        {
            string[] filePaths = Directory.GetFiles("./", "*.res", SearchOption.AllDirectories);
            Queue<XmlNodeList>[] nodesToProcess = new Queue<XmlNodeList>[(int)ResourceTypes.Count];
            for (int i = 0; i < filePaths.Length; i++)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filePaths[i]);
                for (int j = 0; j < ResourceTypeTokens.Length; j++)
                {
                    if (nodesToProcess[j] == null)
                        nodesToProcess[j] = new Queue<XmlNodeList>();
                    XmlNodeList list = xmlDoc.GetElementsByTagName(ResourceTypeTokens[j]);
                    nodesToProcess[j].Enqueue(list);
                }
            }
            for (int i = 0; i < nodesToProcess.Length; i++)
            {
                while (nodesToProcess[i] != null && nodesToProcess[i].Count > 0)
                {
                    XmlNodeList nodeCollection = nodesToProcess[i].Dequeue();
                    foreach (XmlNode node in nodeCollection)
                    {
                        IResource res = (IResource)ResourceTypeDefs[i].GetConstructors()[0].Invoke(null);
                        res.LoadFromXML(node);
                        if (resources[i].ContainsKey(res.Name))
                        {
                            res.Destroy();
                            res = null;
                            return;
                        }
                        resources[i].Add(res.Name, res);
                    }
                }
            }
        }

        IResource GetResource(string key, ResourceTypes type)
        {
            if (resources[(int)type].ContainsKey(key))
                return resources[(int)type][key];
            return null;
        }

        public Material GetMaterial(string key)
        {
            return (Material)GetResource(key, ResourceTypes.Material);
        }

        public TextureResource GetTexture(string key)
        {
            return (TextureResource)GetResource(key, ResourceTypes.Texture);
        }

        public Shader GetShader(string key)
        {
            return (Shader)GetResource(key, ResourceTypes.Shader);
        }

        public SoundEffect GetSoundEffect(string key)
        {
            return (SoundEffect)GetResource(key, ResourceTypes.SoundEffect);
        }

        public ParticleEffect GetParticleEffect(string key)
        {
            return (ParticleEffect)GetResource(key, ResourceTypes.Particle);
        }

        public Mesh GetMesh(string key)
        {
            return (Mesh)GetResource(key, ResourceTypes.Mesh);
        }

        public AnimationSequence GetAnimation(string key)
        {
            return (AnimationSequence)GetResource(key, ResourceTypes.Animation);
        }

        public TeamResource GetTeam(string key)
        {
            return (TeamResource)GetResource(key, ResourceTypes.Team);
        }

        public TerrainClimate GetTerrainClimate(string key)
        {
            return (TerrainClimate)GetResource(key, ResourceTypes.TerrainClimate);
        }

        public TerrainClimateVoxels GetTerrainClimateVoxels(string key)
        {
            return (TerrainClimateVoxels)GetResource(key, ResourceTypes.TerrainClimateVoxels);
        }

        public DinosaurDatablock GetDinosaurDatablock(string key)
        {
            return (DinosaurDatablock)GetResource(key, ResourceTypes.DinosaurDatablock);
        }

        public WeaponDatablock GetWeaponDatablock(string key)
        {
            return (WeaponDatablock)GetResource(key, ResourceTypes.WeaponDatablock);
        }

        public DinosaurDatablock[] GetDinosaurDatablocks()
        {
            DinosaurDatablock[] datablocks = new DinosaurDatablock[resources[(int)ResourceTypes.DinosaurDatablock].Count];
            resources[(int)ResourceTypes.DinosaurDatablock].Values.CopyTo(datablocks, 0);
            return datablocks;
        }

        public WeaponDatablock[] GetWeaponDatablocks()
        {
            WeaponDatablock[] datablocks = new WeaponDatablock[resources[(int)ResourceTypes.WeaponDatablock].Count];
            resources[(int)ResourceTypes.WeaponDatablock].Values.CopyTo(datablocks, 0);
            return datablocks;
        }
    }
}
