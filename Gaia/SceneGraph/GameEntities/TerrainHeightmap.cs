using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Core;
using Gaia.Rendering;
using Gaia.Resources;
using Gaia.Voxels;
using Gaia.TerrainHelper;

using JigLibX.Collision;
using JigLibX.Geometry;
using JigLibX.Utils;

namespace Gaia.SceneGraph.GameEntities
{
    public class TerrainHeightmap : Terrain
    {
        Material terrainMaterial;
        RenderTarget2D[] blendMaps;
        Texture2D normalTexture;
        TerrainClimate climate;
        TerrainRenderElement terrainElement;
        ClutterPlacement clutter;

        string heightmapFileName;
        Vector2 heightRange;
        public float[] heightValues;
        public int width;
        public int depth;

        CollisionSkin collision;

        int L = 6;              // levels
        public int N = 63;             // size of a level
        Clipmap[] clips;

        public float MaximumHeight;

        public int GetWidth()
        {
            return width;
        }

        public int GetDepth()
        {
            return depth;
        }

        public TerrainClimate GetClimate()
        {
            return climate;
        }

        public override void OnSave(System.Xml.XmlWriter writer)
        {
            base.OnSave(writer);

            writer.WriteStartAttribute("hmfilename");
            writer.WriteValue(heightmapFileName);
            writer.WriteEndAttribute();

            writer.WriteStartAttribute("heightrange");
            writer.WriteValue(ParseUtils.WriteVector2(heightRange));
            writer.WriteEndAttribute();
        }

        public override void OnLoad(System.Xml.XmlNode node)
        {
            base.OnLoad(node);

            heightmapFileName = node.Attributes["hmfilename"].Value;
            heightRange = ParseUtils.ParseVector2(node.Attributes["heightrange"].Value);
            LoadHeightFromTexture();
            BuildTerrain();
        }

        public TerrainHeightmap(string heightmapFilename, float minHeight, float maxHeight)
        {
            this.heightmapFileName = heightmapFilename;
            heightRange = new Vector2(minHeight, maxHeight);
            LoadHeightFromTexture();
        }

        public Texture2D[] GetBlendMaps()
        {
            Texture2D[] blendTextures = new Texture2D[blendMaps.Length];
            for(int i = 0; i < blendMaps.Length; i++)
                blendTextures[i] = blendMaps[i].GetTexture();
            return blendTextures;
        }

        void CreateCollisionMesh()
        {
            collision = new CollisionSkin(null);
            Array2D field = new Array2D(width, depth);
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    field.SetAt(x, z, heightValues[x + z * width]);
                }
            }

            collision.AddPrimitive(new Heightmap(field, width*0.5f, depth*0.5f, 1, 1), new MaterialProperties(0.1f, 0.02f, 0.9f));
            scene.GetPhysicsEngine().CollisionSystem.AddCollisionSkin(collision);
        }

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);
            BuildTerrain();
            clutter = new ClutterPlacement(this, scene.MainCamera);
            CreateCollisionMesh();
        }

        public override void OnDestroy()
        {
            scene.GetPhysicsEngine().CollisionSystem.RemoveCollisionSkin(collision);
            base.OnDestroy();
        }

        public override void GenerateRandomTransform(Random rand, out Vector3 position, out Vector3 normal)
        {
            int randX = rand.Next(0, width);
            int randZ = rand.Next(0, depth);
            position = new Vector3(randX, GetHeightValue(randX, randZ), randZ);
            ComputeVertexNormal(randX, randZ, out normal);
        }

        public override bool GetTrianglesInRegion(Random rand, out List<Gaia.Voxels.TriangleGraph> availableTriangles, BoundingBox region)
        {
            availableTriangles = null;
            return false;
        }

        public float GetHeightValue(int x, int y)
        {
            x = (int)MathHelper.Clamp(x, 0, width-1);
            y = (int)MathHelper.Clamp(y, 0, depth-1);
            int index = x + y * width;
            float height = heightValues[index];
            float weight = 1;
            if (y - 1 >= 0)
            {
                weight++;
                height += heightValues[index - width];
            }
            if (x - 1 >= 0)
            {
                weight++;
                height += heightValues[index - 1];
            }
            if (y + 1 < depth)
            {
                weight++;
                height += heightValues[index + width];
            }
            if (x + 1 < width)
            {
                weight++;
                height += heightValues[index + 1];
            }
            return height / weight;
        }

        public void ComputeVertexNormal(int x, int z, out Vector3 normal)
        {
            Vector3 center;
            Vector3 p1;
            Vector3 p2;
            Vector3 avgNormal = Vector3.Zero;

            int avgCount = 0;

            bool spaceAbove = false;
            bool spaceBelow = false;
            bool spaceLeft = false;
            bool spaceRight = false;

            Vector3 tmpNormal;
            Vector3 v1;
            Vector3 v2;

            Vector3 invScale = new Vector3(1.0f / (float)width, 1.0f, 1.0f / (float)depth);
            center = new Vector3(x, GetHeightValue(x, z), z);// *invScale;

            if (x > 0)
            {
                spaceLeft = true;
            }

            if (x < width - 1)
            {
                spaceRight = true;
            }

            if (z > 0)
            {
                spaceAbove = true;
            }

            if (z < depth - 1)
            {
                spaceBelow = true;
            }

            if (spaceAbove && spaceLeft)
            {
                p1 = new Vector3(x - 1, GetHeightValue(x - 1, z), z);// *invScale;
                p2 = new Vector3(x, GetHeightValue(x, z - 1), z - 1);// *invScale;

                v1 = p1 - center;
                v2 = p2 - center;

                tmpNormal = Vector3.Cross(v2, v1);
                avgNormal += tmpNormal;

                ++avgCount;
            }

            if (spaceAbove && spaceRight)
            {
                p1 = new Vector3(x, GetHeightValue(x, z - 1), z - 1);// *invScale;
                p2 = new Vector3(x + 1, GetHeightValue(x + 1, z), z);// *invScale;

                v1 = p1 - center;
                v2 = p2 - center;

                tmpNormal = Vector3.Cross(v2, v1);
                avgNormal += tmpNormal;

                ++avgCount;
            }

            if (spaceBelow && spaceRight)
            {
                p1 = new Vector3(x + 1, GetHeightValue(x + 1, z), z);// *invScale;
                p2 = new Vector3(x, GetHeightValue(x, z + 1), z + 1);// *invScale;

                v1 = p1 - center;
                v2 = p2 - center;

                tmpNormal = Vector3.Cross(v2, v1);
                avgNormal += tmpNormal;

                ++avgCount;
            }

            if (spaceBelow && spaceLeft)
            {
                p1 = new Vector3(x, GetHeightValue(x, z + 1), z + 1);// *invScale;
                p2 = new Vector3(x - 1, GetHeightValue(x - 1, z), z);// *invScale;

                v1 = p1 - center;
                v2 = p2 - center;

                tmpNormal = Vector3.Cross(v2, v1);
                avgNormal += tmpNormal;

                ++avgCount;
            }

            normal = avgNormal / avgCount;
            normal.Normalize();
        }

        public void ComputeVertexTangent(int x, int z, out Vector3 tangent)
        {
            Vector3 center;
            Vector3 p1;
            Vector3 p2;
            Vector3 avgNormal = Vector3.Zero;
            Vector3 normal;

            int avgCount = 0;

            bool spaceAbove = false;
            bool spaceBelow = false;
            bool spaceLeft = false;
            bool spaceRight = false;

            Vector3 tmpNormal;
            Vector3 avgTangent = Vector3.Zero;
            Vector3 v1;
            Vector3 v2;
            Vector2 s;
            Vector2 w;
            Vector2 centerUV = new Vector2((float)x, (float)z);
            Vector3 invScale = new Vector3(1.0f / (float)width, 1.0f, 1.0f / (float)depth);
            center = new Vector3((float)x, GetHeightValue(x, z), (float)z) * invScale;

            if (x > 0)
            {
                spaceLeft = true;
            }

            if (x < width - 1)
            {
                spaceRight = true;
            }

            if (z > 0)
            {
                spaceAbove = true;
            }

            if (z < depth - 1)
            {
                spaceBelow = true;
            }

            if (spaceAbove && spaceLeft)
            {
                p1 = new Vector3(x - 1, GetHeightValue(x - 1, z), z) * invScale;
                p2 = new Vector3(x, GetHeightValue(x, z - 1), z - 1) * invScale;

                v1 = p1 - center;
                v2 = p2 - center;
                w = new Vector2(x - 1, z) - centerUV;
                s = new Vector2(x, z - 1) - centerUV;
                float r = 1.0F / (s.X * w.Y - s.Y * w.X);
                Vector3 sdir = new Vector3((w.Y * v2.X - s.Y * v1.X) * r, (w.Y * v2.Y - s.Y * v1.Y) * r,
                (w.Y * v2.Z - s.Y * v1.Z) * r);
                avgTangent += sdir;
                tmpNormal = Vector3.Cross(v2, v1);
                avgNormal += tmpNormal;


                ++avgCount;
            }

            if (spaceAbove && spaceRight)
            {
                p1 = new Vector3(x, GetHeightValue(x, z - 1), z - 1) * invScale;
                p2 = new Vector3(x + 1, GetHeightValue(x + 1, z), z) * invScale;

                v1 = p1 - center;
                v2 = p2 - center;
                w = new Vector2(x, z - 1) - centerUV;
                s = new Vector2(x + 1, z) - centerUV;
                float r = 1.0F / (s.X * w.Y - s.Y * w.X);
                Vector3 sdir = new Vector3((w.Y * v2.X - s.Y * v1.X) * r, (w.Y * v2.Y - s.Y * v1.Y) * r,
                (w.Y * v2.Z - s.Y * v1.Z) * r);
                avgTangent += sdir;

                tmpNormal = Vector3.Cross(v2, v1);
                avgNormal += tmpNormal;

                ++avgCount;
            }

            if (spaceBelow && spaceRight)
            {
                p1 = new Vector3(x + 1, GetHeightValue(x + 1, z), z) * invScale;
                p2 = new Vector3(x, GetHeightValue(x, z + 1), z + 1) * invScale;

                v1 = p1 - center;
                v2 = p2 - center;
                w = new Vector2(x + 1, z) - centerUV;
                s = new Vector2(x, z + 1) - centerUV;
                float r = 1.0F / (s.X * w.Y - s.Y * w.X);
                Vector3 sdir = new Vector3((w.Y * v2.X - s.Y * v1.X) * r, (w.Y * v2.Y - s.Y * v1.Y) * r,
                (w.Y * v2.Z - s.Y * v1.Z) * r);
                avgTangent += sdir;

                tmpNormal = Vector3.Cross(v2, v1);
                avgNormal += tmpNormal;

                ++avgCount;
            }

            if (spaceBelow && spaceLeft)
            {
                p1 = new Vector3(x, GetHeightValue(x, z + 1), z + 1) * invScale;
                p2 = new Vector3(x - 1, GetHeightValue(x - 1, z), z) * invScale;

                v1 = p1 - center;
                v2 = p2 - center;
                w = new Vector2(x, z + 1) - centerUV;
                s = new Vector2(x - 1, z) - centerUV;
                float r = 1.0F / (s.X * w.Y - s.Y * w.X);
                Vector3 sdir = new Vector3((w.Y * v2.X - s.Y * v1.X) * r, (w.Y * v2.Y - s.Y * v1.Y) * r,
                (w.Y * v2.Z - s.Y * v1.Z) * r);
                avgTangent += sdir;

                tmpNormal = Vector3.Cross(v2, v1);
                avgNormal += tmpNormal;

                ++avgCount;
            }
            avgTangent /= avgCount;
            normal = avgNormal / avgCount;
            normal.Normalize();
            tangent = (avgTangent - normal * Vector3.Dot(normal, avgTangent));
            tangent.Normalize();

        }

        void LoadHeightFromTexture()
        {
            Texture2D tex = Texture2D.FromFile(GFX.Device, heightmapFileName);
            width = tex.Width;
            depth = tex.Height;
            heightValues = new float[width * depth];
            
            byte[] data = new byte[width * depth];
            if (tex.Format != SurfaceFormat.Luminance8)
            {
                Color[] dataColor = new Color[tex.Width * tex.Height];
                tex.GetData<Color>(dataColor, 0, dataColor.Length);
                for (int i = 0; i < dataColor.Length; i++)
                    data[i] = dataColor[i].R;
            } 
            else 
            {
                tex.GetData<byte>(data);
            }
            float scale = (heightRange.Y - heightRange.X);
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int index = x + width * z;
                    heightValues[index] = (float)((data[index] / 255.0f) * scale) + heightRange.X;
                }
            }

            MaximumHeight = scale + heightRange.X;

            Shader basic2DShader = ResourceManager.Inst.GetShader("Generic2D");
            basic2DShader.SetupShader();
            GFX.Device.Textures[0] = tex;
            GFX.Device.SetVertexShaderConstant(0, Vector2.One / new Vector2(width - 1, depth - 1));
            GFX.Inst.SetTextureFilter(0, TextureFilter.Linear);
            RenderTarget2D heightmapSmaller = new RenderTarget2D(GFX.Device, width - 1, depth - 1, 1, SurfaceFormat.Color);
            DepthStencilBuffer dsOld = GFX.Device.DepthStencilBuffer;
            GFX.Device.DepthStencilBuffer = GFX.Inst.dsBufferLarge;
            GFX.Device.SetRenderTarget(0, heightmapSmaller);
            GFXPrimitives.Quad.Render();
            GFX.Device.SetRenderTarget(0, null);
            GFX.Device.DepthStencilBuffer = dsOld;

            BuildBlendMap(heightmapSmaller.GetTexture());
            GenerateNormalMap();
        }

        void NormalizeBlendWeights()
        {
            int width = blendMaps[0].Width;
            int depth = blendMaps[0].Height;
            Color[] blendWeightData = new Color[width * depth];
            float[] blendMapWeights = new float[blendWeightData.Length];
            for (int i = 0; i < blendMapWeights.Length; i++)
                blendMapWeights[i] = 0;

            for (int i = 0; i < blendMaps.Length; i++)
            {
                blendMaps[i].GetTexture().GetData<Color>(blendWeightData);
                for (int j = 0; j < blendWeightData.Length; j++)
                {
                    Vector4 currWeights = blendWeightData[j].ToVector4();
                    blendMapWeights[j] += Vector4.Dot(Vector4.One, currWeights);
                }
            }

            for (int i = 0; i < blendMapWeights.Length; i++)
                blendMapWeights[i] = 1.0f / blendMapWeights[i];

            Texture2D texture = new Texture2D(GFX.Device, width, depth, 1, TextureUsage.None, SurfaceFormat.Color);

            Shader basic2DShader = ResourceManager.Inst.GetShader("Generic2D");
            basic2DShader.SetupShader();
            for (int i = 0; i < blendMaps.Length; i++)
            {
                blendMaps[i].GetTexture().GetData<Color>(blendWeightData);

                for (int j = 0; j < blendWeightData.Length; j++)
                    blendWeightData[j] = new Color(blendWeightData[j].ToVector4() * blendMapWeights[j]);
                texture.SetData<Color>(blendWeightData);

                GFX.Device.SetRenderTarget(0, blendMaps[i]);
                GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / new Vector2(width, depth));
                GFX.Device.Textures[0] = texture;
                GFXPrimitives.Quad.Render();
                GFX.Device.SetRenderTarget(0, null);
                GFX.Device.Textures[0] = null;
            }
        }

        void BuildBlendMap(Texture2D heightmap)
        {
            climate = ResourceManager.Inst.GetTerrainClimate("TestTerrain");

            DepthStencilBuffer dsOld = GFX.Device.DepthStencilBuffer;
            GFX.Device.DepthStencilBuffer = GFX.Inst.dsBufferLarge;

            Shader gradientShader = ResourceManager.Inst.GetShader("GradientHeightmap");
            gradientShader.SetupShader();
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / new Vector2(heightmap.Width, heightmap.Height));
            GFX.Device.SetPixelShaderConstant(0, Vector2.One / new Vector2(heightmap.Width, heightmap.Height));
            GFX.Device.Textures[0] = heightmap;

            RenderTarget2D rtGradient = new RenderTarget2D(GFX.Device, heightmap.Width, heightmap.Height, 1, SurfaceFormat.Single);
            GFX.Device.SetRenderTarget(0, rtGradient);
            GFXPrimitives.Quad.Render();
            GFX.Device.SetRenderTarget(0, null);
            GFX.Device.Textures[0] = rtGradient.GetTexture();
            RenderTarget2D rtCurvature = new RenderTarget2D(GFX.Device, heightmap.Width, heightmap.Height, 1, SurfaceFormat.Single);
            GFX.Device.SetRenderTarget(0, rtCurvature);
            GFXPrimitives.Quad.Render();
            GFX.Device.SetRenderTarget(0, null);


            Shader blendShader = ResourceManager.Inst.GetShader("BlendHeightmap");
            blendShader.SetupShader();
            GFX.Device.Textures[0] = heightmap;
            GFX.Device.Textures[1] = rtGradient.GetTexture();
            GFX.Device.Textures[2] = rtCurvature.GetTexture();

            Vector4[] climateParams = new Vector4[4];
            Vector4[] climateParams2 = new Vector4[4];

            int numBlendMapsNeeded = climate.blendZones.Length / 4;
            blendMaps = new RenderTarget2D[numBlendMapsNeeded];

            for (int i = 0; i < numBlendMapsNeeded; i++)
            {
                int offset = i * 4;
                for (int j = 0; j < 4; j++)
                {
                    int climateIndex = j + offset;
                    climateParams[j] = new Vector4(climate.heightCoeffs[climateIndex], climate.gradientCoeffs[climateIndex], climate.curvatureCoeffs[climateIndex], climate.baseScores[climateIndex]);
                    climateParams2[j] = Vector4.One * climate.blendZones[climateIndex];
                }

                GFX.Device.SetPixelShaderConstant(0, climateParams);
                GFX.Device.SetPixelShaderConstant(4, climateParams2);

                blendMaps[i] = new RenderTarget2D(GFX.Device, heightmap.Width, heightmap.Height, 1, SurfaceFormat.Color);
                GFX.Device.SetRenderTarget(0, blendMaps[i]);
                GFXPrimitives.Quad.Render();
                GFX.Device.SetRenderTarget(0, null);
            }

            NormalizeBlendWeights();
            GFX.Device.DepthStencilBuffer = dsOld;
        }

        void BuildTerrain()
        {
            terrainMaterial = ResourceManager.Inst.GetMaterial("NULL");
            
            Texture2D[] blendTextures = new Texture2D[blendMaps.Length];
            for(int i = 0; i < blendTextures.Length; i++)
                blendTextures[i] = blendMaps[i].GetTexture();

            clips = new Clipmap[L];
            for (int i = 0; i < L; i++)
                clips[i] = new Clipmap(i, this);

            terrainElement = new TerrainRenderElement();
            terrainElement.clips = clips;
            terrainElement.N = this.N;
            terrainElement.BlendMaps = blendTextures;
            terrainElement.NormalMap = normalTexture;
        }

        void GenerateNormalMap()
        {
            Texture2D heightmap = new Texture2D(GFX.Device, width, depth, 1, TextureUsage.None, SurfaceFormat.Single);
            heightmap.SetData<float>(heightValues);

            Vector2 invRes = Vector2.One / new Vector2(heightmap.Width, heightmap.Height);
            Shader normalTangentShader = ResourceManager.Inst.GetShader("NormalTangentHeightmap");
            normalTangentShader.SetupShader();
            GFX.Device.SetVertexShaderConstant(0, invRes);
            GFX.Device.SetPixelShaderConstant(0, invRes);
            GFX.Device.SetPixelShaderConstant(1, Vector4.One / MaximumHeight);
            GFX.Device.Textures[0] = heightmap;
            GFX.Inst.SetTextureFilter(0, TextureFilter.Linear);

            RenderTarget2D normTanTarget = new RenderTarget2D(GFX.Device, heightmap.Width, heightmap.Height, 1, SurfaceFormat.Vector4);
            DepthStencilBuffer dsOld = GFX.Device.DepthStencilBuffer;
            DepthStencilBuffer dsNew = new DepthStencilBuffer(GFX.Device, heightmap.Width, heightmap.Height, dsOld.Format);
            GFX.Device.DepthStencilBuffer = dsNew;
            GFX.Device.SetRenderTarget(0, normTanTarget);
            GFXPrimitives.Quad.Render();
            GFX.Device.SetRenderTarget(0, null);
            GFX.Device.DepthStencilBuffer = dsOld;

            normalTexture = normTanTarget.GetTexture();
        }

        public override void OnUpdate()
        {
            clutter.OnUpdate();

            terrainElement.CameraPos = (this.scene.MainCamera.GetPosition());
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].UpdateVertices(terrainElement.CameraPos);
                if (i == 0)
                {
                    // Level 0 has no nested level, so pass null as parameter.
                    clips[i].UpdateIndices(null);
                }
                else
                {
                    // All other levels i have the level i-1 nested in.
                    clips[i].UpdateIndices(clips[i - 1]);
                }
            }
            
            base.OnUpdate();
        }

        public override void OnRender(Gaia.Rendering.RenderViews.RenderView view)
        {
            clutter.OnRender(view);
            TerrainElementManager terrMgr = (TerrainElementManager)view.GetRenderElementManager(RenderPass.Terrain);
            if(terrMgr != null)
                terrMgr.AddElement(climate, terrainElement);

            base.OnRender(view);
        }

    }
}
