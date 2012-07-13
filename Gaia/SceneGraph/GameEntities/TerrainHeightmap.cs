using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Core;
using Gaia.Rendering;
using Gaia.Resources;
using Gaia.Voxels;

using JigLibX.Collision;
using JigLibX.Geometry;
using JigLibX.Utils;

namespace Gaia.SceneGraph.GameEntities
{
    public class TerrainHeightmap : Terrain
    {
        TerrainPatch[] patches;
        BoundingBox[] patchBounds;

        Material terrainMaterial;
        RenderTarget2D[] blendMaps;
        TerrainClimate climate;
        TerrainRenderElement[] patchElements;

        ClutterPlacement clutter;


        Texture2D NormalTangentTexure;
        public TextureResource NormalTangentMap;

        string heightmapFileName;

        int patchWidth = 16;
        Vector2 heightRange;
        float[] heightValues;
        int width;
        int depth;

        int numPatchesX;
        int numPatchesZ;

        CollisionSkin collision;

        BoundingFrustum frustum = new BoundingFrustum(Matrix.Identity);

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
            BuildTerrain();
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
            Matrix transform = Transformation.GetTransform();
            Vector3 center = Vector3.Transform(Vector3.Zero, transform);
            Vector2 invRes = (Vector2.One / new Vector2(width, depth)) * 2.0f - Vector2.One;
            Vector3 gridSize = Vector3.Transform(new Vector3(invRes.X, 0, invRes.Y), transform);
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    Vector3 transformed = Vector3.Transform(new Vector3(0, heightValues[x + z * width], 0), transform);
                    field.SetAt(x, z, transformed.Y);
                }
            }

            collision.AddPrimitive(new Heightmap(field, width/2, depth/2, gridSize.X, gridSize.Z), new MaterialProperties(0.1f, 0.02f, 0.9f));
            scene.GetPhysicsEngine().CollisionSystem.AddCollisionSkin(collision);
        }

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);

            clutter = new ClutterPlacement(this, scene.MainCamera);

            Matrix transform = this.Transformation.GetTransform();
            for (int i = 0; i < patches.Length; i++)
            {
                patchBounds[i] = MathUtils.TransformBounds(patches[i].GetBounds(), transform);
                patches[i].BuildTriangleGraph(transform);
                patches[i].GetRenderElement().Transform = new Matrix[1] { transform };
            }

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

            Vector3 randPos = new Vector3((float)randX / (float)width, 0, (float)randZ / (float)depth) * 2.0f - Vector3.One;
            randPos.Y = GetHeightValue(randX, randZ);
            position = Vector3.Transform(randPos, this.Transformation.GetTransform());
            ComputeVertexNormal(randX, randZ, out normal);
        }

        public override bool GetTrianglesInRegion(Random rand, out List<Gaia.Voxels.TriangleGraph> availableTriangles, BoundingBox region)
        {
            BoundingBox regionWorld = region;
            region.Min = Vector3.Transform(region.Min, this.Transformation.GetObjectSpace());
            region.Max = Vector3.Transform(region.Max, this.Transformation.GetObjectSpace());
            region.Min = Vector3.Clamp(region.Min, Vector3.One*-1.0f, Vector3.One);
            region.Max = Vector3.Clamp(region.Max, Vector3.One*-1.0f, Vector3.One);

            availableTriangles = null;

            if (region.Min == region.Max)
                return false;

            float halfWidth = numPatchesX * 0.5f;
            float halfDepth = numPatchesZ * 0.5f;
            int startX = (int)MathHelper.Clamp((region.Min.X + 1.0f) * halfWidth, 0, numPatchesX - 1);
            int startZ = (int)MathHelper.Clamp((region.Min.Z + 1.0f) * halfDepth, 0, numPatchesZ - 1);
            int endX = (int)MathHelper.Clamp((region.Max.X + 1.0f) * halfWidth, 0, numPatchesX - 1);
            int endZ = (int)MathHelper.Clamp((region.Max.Z + 1.0f) * halfDepth, 0, numPatchesZ - 1);

            availableTriangles = new List<Gaia.Voxels.TriangleGraph>();

            for (int x = startX; x <= endX; x++)
            {
                for (int z = startZ; z <= endZ; z++)
                {
                    int index = x + z * numPatchesX;
                    patches[index].GetTrianglesInRegion(availableTriangles);
                }
            }

            return (availableTriangles.Count > 0);
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

            center = new Vector3((float)x, GetHeightValue(x, z), (float)z);

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
                p1 = new Vector3(x - 1, GetHeightValue(x - 1, z), z);
                p2 = new Vector3(x, GetHeightValue(x, z - 1), z - 1);

                v1 = p1 - center;
                v2 = p2 - center;

                tmpNormal = Vector3.Cross(v2, v1);
                avgNormal += tmpNormal;

                ++avgCount;
            }

            if (spaceAbove && spaceRight)
            {
                p1 = new Vector3(x, GetHeightValue(x, z - 1), z - 1);
                p2 = new Vector3(x + 1, GetHeightValue(x + 1, z), z);

                v1 = p1 - center;
                v2 = p2 - center;

                tmpNormal = Vector3.Cross(v2, v1);
                avgNormal += tmpNormal;

                ++avgCount;
            }

            if (spaceBelow && spaceRight)
            {
                p1 = new Vector3(x + 1, GetHeightValue(x + 1, z), z);
                p2 = new Vector3(x, GetHeightValue(x, z + 1), z + 1);

                v1 = p1 - center;
                v2 = p2 - center;

                tmpNormal = Vector3.Cross(v2, v1);
                avgNormal += tmpNormal;

                ++avgCount;
            }

            if (spaceBelow && spaceLeft)
            {
                p1 = new Vector3(x, GetHeightValue(x, z + 1), z + 1);
                p2 = new Vector3(x - 1, GetHeightValue(x - 1, z), z);

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
            center = new Vector3((float)x, GetHeightValue(x, z), (float)z);

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
                p1 = new Vector3(x - 1, GetHeightValue(x - 1, z), z);
                p2 = new Vector3(x, GetHeightValue(x, z - 1), z - 1);

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
                p1 = new Vector3(x, GetHeightValue(x, z - 1), z - 1);
                p2 = new Vector3(x + 1, GetHeightValue(x + 1, z), z);

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
                p1 = new Vector3(x + 1, GetHeightValue(x + 1, z), z);
                p2 = new Vector3(x, GetHeightValue(x, z + 1), z + 1);

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
                p1 = new Vector3(x, GetHeightValue(x, z + 1), z + 1);
                p2 = new Vector3(x - 1, GetHeightValue(x - 1, z), z);

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

            BuildBlendMap(tex);

            BuildNormalTangentMap();
        }

        void BuildNormalTangentMap()
        {
            NormalTangentTexure = new Texture2D(GFX.Device, width, depth, 1, TextureUsage.None, SurfaceFormat.Vector4);

            Vector4[] normTanData = new Vector4[width * depth];

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < depth; j++)
                {
                    Vector3 normal = Vector3.Right;
                    ComputeVertexNormal(i, j, out normal);
                    normTanData[i + j * width] = new Vector4(normal, 1.0f);
                }
            }

            NormalTangentTexure.SetData<Vector4>(normTanData);
            NormalTangentMap = new TextureResource();
            NormalTangentMap.SetTexture(TextureResourceType.Texture2D, NormalTangentTexure);
        }

        void NormalizeBlendWeights()
        {
            Color[] blendWeightData = new Color[width*depth];
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

                blendMaps[i].GetTexture().Save("Test" + i + "_BEFORE.dds", ImageFileFormat.Dds);

                GFX.Device.SetRenderTarget(0, blendMaps[i]);
                GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / new Vector2(width, depth));
                GFX.Device.Textures[0] = texture;
                GFXPrimitives.Quad.Render();
                GFX.Device.SetRenderTarget(0, null);
                GFX.Device.Textures[0] = null;

                blendMaps[i].GetTexture().Save("Test" + i + "_AFTER.dds", ImageFileFormat.Dds);
                
            }
            /*
            Texture2D tempBlendTexture = new Texture2D(GFX.Device, width, depth, 1, TextureUsage.None, SurfaceFormat.Color);
            
            Texture2D weightTexture = new Texture2D(GFX.Device, width, depth, 1, TextureUsage.None, SurfaceFormat.Single);
            weightTexture.SetData<float>(blendMapWeights);

            weightTexture.Save("Test.dds", ImageFileFormat.Dds);

            Shader normalizeWeightsShader = ResourceManager.Inst.GetShader("FixBlendWeights");
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / new Vector2(width, depth));
            GFX.Device.SamplerStates[0].MagFilter = TextureFilter.None;
            GFX.Device.SamplerStates[0].MinFilter = TextureFilter.None;
            GFX.Device.SamplerStates[0].MipFilter = TextureFilter.None;

            GFX.Device.Textures[0] = weightTexture;
            for (int i = 0; i < blendMaps.Length; i++)
            {
                blendMaps[i].GetTexture().GetData<Color>(blendWeightData);
                tempBlendTexture.SetData<Color>(blendWeightData);
                GFX.Device.Textures[1] = tempBlendTexture; ;
                blendMaps[i].GetTexture().Save("Test" + i + "_BEFORE.dds", ImageFileFormat.Dds);
                GFX.Device.SetRenderTarget(0, blendMaps[i]);
                GFXPrimitives.Quad.Render();
                GFX.Device.SetRenderTarget(0, null);
                blendMaps[i].GetTexture().Save("Test" + i + "_AFTER.dds", ImageFileFormat.Dds);
                GFX.Device.Textures[1] = null;
            }
            */
        }

        void BuildBlendMap(Texture2D heightmap)
        {
            climate = ResourceManager.Inst.GetTerrainClimate("TestTerrain");
            //Texture2D heightmap = new Texture2D(GFX.Device, width, depth, 1, TextureUsage.None, SurfaceFormat.Single);
            //heightmap.SetData<float>(heightValues);

            Shader gradientShader = ResourceManager.Inst.GetShader("GradientHeightmap");
            gradientShader.SetupShader();
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / new Vector2(width, depth));
            GFX.Device.SetPixelShaderConstant(0, Vector2.One / new Vector2(width, depth));
            GFX.Device.Textures[0] = heightmap;

            RenderTarget2D rtGradient = new RenderTarget2D(GFX.Device, width, depth, 1, SurfaceFormat.Single);
            GFX.Device.SetRenderTarget(0, rtGradient);
            GFXPrimitives.Quad.Render();
            GFX.Device.SetRenderTarget(0, null);
            GFX.Device.Textures[0] = rtGradient.GetTexture();
            RenderTarget2D rtCurvature = new RenderTarget2D(GFX.Device, width, depth, 1, SurfaceFormat.Single);
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

                blendMaps[i] = new RenderTarget2D(GFX.Device, width, depth, 1, SurfaceFormat.Color);
                GFX.Device.SetRenderTarget(0, blendMaps[i]);
                GFXPrimitives.Quad.Render();
                GFX.Device.SetRenderTarget(0, null);
            }

            NormalizeBlendWeights();
        }

        void BuildTerrain()
        {
            terrainMaterial = ResourceManager.Inst.GetMaterial("NULL");

            numPatchesX = width / patchWidth;
            numPatchesZ = depth / patchWidth;
            // Create the terrain patches.
            patches = new TerrainPatch[numPatchesX * numPatchesZ];
            patchBounds = new BoundingBox[patches.Length];
            patchElements = new TerrainRenderElement[patches.Length];
            
            Texture2D[] blendTextures = new Texture2D[blendMaps.Length];
            for(int i = 0; i < blendTextures.Length; i++)
                blendTextures[i] = blendMaps[i].GetTexture();
            for (int x = 0; x < numPatchesX; x++)
            {
                for (int z = 0; z < numPatchesZ; z++)
                {
                    int index = x + z * numPatchesX;
                    patches[index] = new TerrainPatch(this, patchWidth, patchWidth, x * (patchWidth - 1), z * (patchWidth - 1));
                    patchBounds[index] = MathUtils.TransformBounds(patches[index].GetBounds(), Transformation.GetTransform());
                    patchElements[index] = new TerrainRenderElement();
                    patchElements[index].BlendMaps = blendTextures;
                    patchElements[index].Elements = new RenderElement[1] { patches[index].GetRenderElement() };
                }
            }
        }

        public override void OnUpdate()
        {
            clutter.OnUpdate();
            base.OnUpdate();
        }

        public override void OnRender(Gaia.Rendering.RenderViews.RenderView view)
        {
            clutter.OnRender(view);
            frustum.Matrix = this.Transformation.GetTransform() * view.GetViewProjection();
            for(int i = 0; i < patches.Length; i++)
            {
                if (frustum.Contains(patches[i].GetBounds()) != ContainmentType.Disjoint)
                {
                    if(view.GetRenderType() == Gaia.Rendering.RenderViews.RenderViewType.MAIN)
                    {
                        TerrainElementManager terrMgr = (TerrainElementManager)view.GetRenderElementManager(RenderPass.Terrain);
                        terrMgr.AddElement(climate, patchElements[i]);
                    }
                    else
                    {
                        RenderElement element = patches[i].GetRenderElement();
                        view.AddElement(terrainMaterial, element);
                    }
                }
            }
            base.OnRender(view);
        }

    }

    class TerrainPatch
    {
        BoundingBox Bounds;

        TerrainHeightmap parent;

        Vector3 center;

        VertexPNTT[] geometry;

        ushort[] indices;

        VertexBuffer vertexBuffer;

        IndexBuffer indexBuffer;

        int width;

        int depth;

        int offsetX;

        int offsetZ;

        RenderElement renderElement;

        public RenderElement GetRenderElement()
        {
            return renderElement;
        }

        public BoundingBox GetBounds()
        {
            return this.Bounds;
        }

        TriangleGraph[] triangleGraph;

        public TerrainPatch(TerrainHeightmap terrain, int width, int depth, int offsetX, int offsetZ)
        {
            parent = terrain;

            Bounds = new BoundingBox();

            this.width = width;
            this.depth = depth;

            this.offsetX = offsetX;
            this.offsetZ = offsetZ;

            float invWidth = 1.0f / (float)terrain.GetWidth();
            float invDepth = 1.0f / (float)terrain.GetDepth();
            Bounds.Min.X = offsetX * invWidth * 2 - 1;
            Bounds.Min.Z = offsetZ * invDepth * 2 - 1;

            Bounds.Max.X = (offsetX + width) * invWidth * 2 - 1;
            Bounds.Max.Z = (offsetZ + depth) * invDepth * 2 - 1;

            center = (Bounds.Max + Bounds.Min) / 2;

            BuildVertexBuffer();

            BuildIndexBuffer();

            int primitiveCount = 2 * (width - 1) * (depth - 1);

            renderElement = new RenderElement();
            renderElement.IndexBuffer = indexBuffer;
            renderElement.VertexBuffer = vertexBuffer;
            renderElement.VertexCount = geometry.Length;
            renderElement.VertexDec = GFXVertexDeclarations.PNTTDec;
            renderElement.VertexStride = VertexPNTT.SizeInBytes;
            renderElement.StartVertex = 0;
            renderElement.PrimitiveCount = primitiveCount;
            renderElement.IsAnimated = false;
        }

        /// <summary>
        /// Build the vertex buffer as well as the bounding box.
        /// </summary>
        /// <param name="heightmap"></param>
        private void BuildVertexBuffer()
        {
            int index = 0;

            Vector3 position;
            Vector3 normal;
            Vector3 tangent;
            Vector2 texcoord;

            Bounds.Min.Y = float.MaxValue;

            Bounds.Max.Y = float.MinValue;

            geometry = new VertexPNTT[width * depth];

            float invX = 1.0f / (float)parent.GetWidth();
            float invZ = 1.0f / (float)parent.GetDepth();

            float invX2 = 2.0f * invX;
            float invZ2 = 2.0f * invZ;

            for (int z = offsetZ; z < offsetZ + depth; z++)
            {
                for (int x = offsetX; x < offsetX + width; x++)
                {
                    float height = parent.GetHeightValue(x, z);

                    Bounds.Min.Y = Math.Min(Bounds.Min.Y, height);

                    Bounds.Max.Y = Math.Max(Bounds.Max.Y, height);

                    position = new Vector3((float)x * invX2 - 1.0f, height, (float)z * invZ2 - 1.0f);

                    parent.ComputeVertexNormal(x, z, out normal);

                    parent.ComputeVertexTangent(x, z, out tangent);

                    texcoord = new Vector2(x * invX, z * invZ);

                    geometry[index] = new VertexPNTT(position, normal, texcoord, tangent);

                    index++;
                }
            }

            vertexBuffer = new VertexBuffer(GFX.Device, VertexPNTT.SizeInBytes * geometry.Length, BufferUsage.WriteOnly);

            vertexBuffer.SetData<VertexPNTT>(geometry);
        }

        /// <summary>
        /// Build the index buffer.
        /// </summary>
        private void BuildIndexBuffer()
        {
            int widthMinusOne = width - 1;
            int depthMinusOne = depth - 1;
            int primitiveCount = 2 * widthMinusOne * depthMinusOne;

            indices = new ushort[3 * primitiveCount];
            for (int i = 0; i < widthMinusOne; i++)
            {
                for (int j = 0; j < depthMinusOne; j++)
                {
                    int vertIndex = i + j * width;
                    int index = (i + j * widthMinusOne) * 6;
                    indices[index] = (ushort)(vertIndex + width);
                    indices[index + 1] = (ushort)(vertIndex);
                    indices[index + 2] = (ushort)(vertIndex + 1);
                    indices[index + 3] = (ushort)(vertIndex + 1);
                    indices[index + 4] = (ushort)(vertIndex + 1 + width);
                    indices[index + 5] = (ushort)(vertIndex + width);
                }
            }

            indexBuffer = new IndexBuffer(GFX.Device, sizeof(ushort) * indices.Length, BufferUsage.None, IndexElementSize.SixteenBits);

            indexBuffer.SetData<ushort>(indices);
        }

        static int TriangleCompareFunction(TriangleGraph elementA, TriangleGraph elementB, int axis)
        {
            float valueA = (axis == 0) ? elementA.Centroid.X : (axis == 1) ? elementA.Centroid.Y : elementA.Centroid.Z;

            float valueB = (axis == 0) ? elementB.Centroid.X : (axis == 1) ? elementB.Centroid.Y : elementB.Centroid.Z;

            if (valueA < valueB)
                return -1;
            if (valueA > valueB)
                return 1;

            return 0;
        }

        public void BuildTriangleGraph(Matrix parentTransform)
        {
            int primitiveCount = this.renderElement.PrimitiveCount;
            triangleGraph = new TriangleGraph[primitiveCount];
            ushort[] indices = new ushort[primitiveCount * 3];
            indexBuffer.GetData<ushort>(indices);
            for (int i = 0; i < primitiveCount; i++)
            {
                int index = i * 3;
                ushort idx0 = indices[index];
                ushort idx1 = indices[index+1];
                ushort idx2 = indices[index+2];
                Vector3 p0 = Vector3.Transform(new Vector3(geometry[idx0].Position.X, geometry[idx0].Position.Y, geometry[idx0].Position.Z), parentTransform);
                Vector3 p1 = Vector3.Transform(new Vector3(geometry[idx1].Position.X, geometry[idx1].Position.Y, geometry[idx1].Position.Z), parentTransform);
                Vector3 p2 = Vector3.Transform(new Vector3(geometry[idx2].Position.X, geometry[idx2].Position.Y, geometry[idx2].Position.Z), parentTransform);
                triangleGraph[i] = new TriangleGraph(idx0, idx1, idx2, p0, p1, p2);
            }
        }

        public void GetTrianglesInRegion(List<TriangleGraph> availableTriangles)
        {
            availableTriangles.AddRange(triangleGraph);
        }
    }
}
