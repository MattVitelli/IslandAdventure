using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Voxels;

namespace Gaia.Rendering
{
    public class Cylinder
    {
        VertexBuffer vertexBuffer;
        IndexBuffer indexBuffer;

        VertexBuffer vertexBufferInstanced;
        IndexBuffer indexBufferInstanced;

        int vertexCount;
        int primitiveCount;

        public VertexBuffer GetVertexBuffer()
        {
            return vertexBuffer;
        }

        public IndexBuffer GetIndexBuffer()
        {
            return indexBuffer;
        }

        public VertexBuffer GetVertexBufferInstanced()
        {
            return vertexBufferInstanced;
        }

        public IndexBuffer GetIndexBufferInstanced()
        {
            return indexBufferInstanced;
        }

        public int GetVertexCount()
        {
            return vertexCount;
        }

        public int GetPrimitiveCount()
        {
            return primitiveCount;
        }

        public Cylinder(int numSides)
        {
            VertexPNTTI[] vertices = new VertexPNTTI[numSides * 2];
            int deltaTheta = 360 / numSides;
            float deltaTCX = 1.0f / (float)(numSides-1);

            for (int i = 0; i < numSides; i++)
            {
                float a = MathHelper.ToRadians(deltaTheta * i);
                Vector3 pos = new Vector3((float)Math.Cos(a), 0, (float)Math.Sin(a));
                int index = i * 2;
                vertices[index].Position = pos;
                vertices[index].Texcoord = new Vector2(i * deltaTCX, 0);
                //vertices[index].Normal +=
                vertices[index + 1].Position = pos + Vector3.Up;
                vertices[index + 1].Texcoord = new Vector2(i * deltaTCX, 1);
            }
            /*
            vertices[numSides * 2].Position = new Vector4(Vector3.Up * -1, 1.0f);
            vertices[numSides * 2 + 1].Position = new Vector4(Vector3.Up, 1.0f);
            */
            ushort[] indices = new ushort[6 * numSides];
            for (int i = 0; i < numSides; i++)
            {
                int index = i*6;
                int indexVert = i*2;
                indices[index] = (ushort)(indexVert + 1);
                indices[index + 1] = (ushort)indexVert;
                indices[index + 2] = (ushort)(indexVert + 2);
                indices[index + 3] = (ushort)(indexVert + 2);
                indices[index + 4] = (ushort)(indexVert + 3);
                indices[index + 5] = (ushort)(indexVert + 1);
                if (i == numSides - 1)
                {
                    indices[index + 2] = (ushort)0;
                    indices[index + 4] = (ushort)1;
                    indices[index + 3] = (ushort)0;
                }
            }

            vertexBuffer = new VertexBuffer(GFX.Device, vertices.Length * VertexPNTTI.SizeInBytes, BufferUsage.WriteOnly);
            vertexBuffer.SetData<VertexPNTTI>(vertices);

            vertexCount = vertices.Length;

            indexBuffer = new IndexBuffer(GFX.Device, sizeof(ushort) * indices.Length, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
            indexBuffer.SetData<ushort>(indices);

            primitiveCount = indices.Length / 3;

            CreateInstancedBuffers(vertices, indices);
        }

        void CreateInstancedBuffers(VertexPNTTI[] verts, ushort[] ib)
        {
            VertexPNTTI[] instVerts = new VertexPNTTI[verts.Length * GFXShaderConstants.NUM_INSTANCES];
            for (int i = 0; i < GFXShaderConstants.NUM_INSTANCES; i++)
            {
                for (int j = 0; j < verts.Length; j++)
                {
                    instVerts[i * verts.Length + j] = new VertexPNTTI(verts[j].Position, verts[j].Normal, verts[j].Texcoord, verts[j].Tangent, i);
                }
            }

            ushort[] instIB = new ushort[ib.Length * GFXShaderConstants.NUM_INSTANCES];
            for (int i = 0; i < GFXShaderConstants.NUM_INSTANCES; i++)
            {
                for (int j = 0; j < ib.Length; j++)
                {
                    instIB[i * ib.Length + j] = (ushort)(ib[j] + i * verts.Length);
                }
            }

            vertexBufferInstanced = new VertexBuffer(GFX.Device, instVerts.Length * VertexPNTTI.SizeInBytes, BufferUsage.None);
            vertexBufferInstanced.SetData<VertexPNTTI>(instVerts);

            indexBufferInstanced = new IndexBuffer(GFX.Device, sizeof(ushort) * instIB.Length, BufferUsage.None, IndexElementSize.SixteenBits);
            indexBufferInstanced.SetData<ushort>(instIB);
        }
    }

    public class ParticleGeometry
    {
        public VertexParticles[] particles = new VertexParticles[GFXShaderConstants.MAX_PARTICLES];

        float[] particleSizeBuffer = new float[GFXShaderConstants.MAX_PARTICLES];

        public void UpdateParticles(int textureSize)
        {
            Vector2 invRes = Vector2.One / (float)textureSize;
            for (int j = 0; j < textureSize; j++)
            {
                for (int i = 0; i < textureSize; i++)
                {
                    int index = i + j * textureSize;
                    particles[index].Index.X = invRes.X * i;
                    particles[index].Index.Y = invRes.Y * j;
                    particles[index].Index += 0.5f * invRes;
                }
            }
        }
    }

    public class ScreenAlignedQuad
    {
        VertexBuffer vertexBuffer;
        IndexBuffer indexBuffer;

        VertexBuffer vertexBufferInstanced;
        IndexBuffer indexBufferInstanced;

        IndexBuffer indexBufferInstancedDoubleSided;

        public ScreenAlignedQuad(bool faceUp)
        {
            
            VertexPositionTexture[] verts;
            if(faceUp)
            {
                verts = new VertexPositionTexture[]
                        {
                            new VertexPositionTexture(
                                new Vector3(1,0,-1),
                                new Vector2(1,1)),
                            new VertexPositionTexture(
                                new Vector3(-1,0,-1),
                                new Vector2(0,1)),
                            new VertexPositionTexture(
                                new Vector3(-1,0,1),
                                new Vector2(0,0)),
                            new VertexPositionTexture(
                                new Vector3(1,0,1),
                                new Vector2(1,0))
                        };
            }
            else
            {
                verts = new VertexPositionTexture[]
                        {
                            new VertexPositionTexture(
                                new Vector3(1,-1,0),
                                new Vector2(1,1)),
                            new VertexPositionTexture(
                                new Vector3(-1,-1,0),
                                new Vector2(0,1)),
                            new VertexPositionTexture(
                                new Vector3(-1,1,0),
                                new Vector2(0,0)),
                            new VertexPositionTexture(
                                new Vector3(1,1,0),
                                new Vector2(1,0))
                        };
            }

            vertexBuffer = new VertexBuffer(GFX.Device, verts.Length * VertexPositionTexture.SizeInBytes, BufferUsage.WriteOnly);
            vertexBuffer.SetData<VertexPositionTexture>(verts);

            short[] ib = new short[] { 0, 1, 2, 2, 3, 0 };

            indexBuffer = new IndexBuffer(GFX.Device, sizeof(short) * ib.Length, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
            indexBuffer.SetData<short>(ib);

            CreateInstancedBuffers(verts, ib);
        }

        ~ScreenAlignedQuad()
        {
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
        }

        public VertexBuffer GetInstanceVertexBuffer()
        {
            return vertexBufferInstanced;
        }

        public IndexBuffer GetInstanceIndexBuffer()
        {
            return indexBufferInstanced;
        }

        public IndexBuffer GetInstanceIndexBufferDoubleSided()
        {
            return indexBufferInstancedDoubleSided;
        }

        void CreateInstancedBuffers(VertexPositionTexture[] verts, short[] ib)
        {
            VertexPTI[] instVerts = new VertexPTI[verts.Length * GFXShaderConstants.NUM_INSTANCES];
            for (int i = 0; i < GFXShaderConstants.NUM_INSTANCES; i++)
            {
                for (int j = 0; j < verts.Length; j++)
                {
                    instVerts[i * verts.Length + j] = new VertexPTI(verts[j].Position, verts[j].TextureCoordinate, i);
                }
            }

            ushort[] instIB = new ushort[ib.Length * GFXShaderConstants.NUM_INSTANCES];
            for (int i = 0; i < GFXShaderConstants.NUM_INSTANCES; i++)
            {
                for(int j = 0; j < ib.Length; j++)
                {
                    instIB[i * ib.Length + j] = (ushort)(ib[j] + i * verts.Length);
                }
            }

            //Our double-sided index buffer
            ushort[] instIBDouble = new ushort[ib.Length * 2 * GFXShaderConstants.NUM_INSTANCES];
            Array.Copy(instIB, instIBDouble, instIB.Length);
            for (int i = 0; i < instIB.Length; i++)
            {
                instIBDouble[i+instIB.Length] = instIBDouble[instIB.Length - 1 - i];
            }

            vertexBufferInstanced = new VertexBuffer(GFX.Device, instVerts.Length * VertexPTI.SizeInBytes, BufferUsage.None);
            vertexBufferInstanced.SetData<VertexPTI>(instVerts);

            indexBufferInstanced = new IndexBuffer(GFX.Device, sizeof(ushort) * instIB.Length, BufferUsage.None, IndexElementSize.SixteenBits);
            indexBufferInstanced.SetData<ushort>(instIB);

            indexBufferInstancedDoubleSided = new IndexBuffer(GFX.Device, sizeof(ushort) * instIBDouble.Length, BufferUsage.None, IndexElementSize.SixteenBits);
            indexBufferInstancedDoubleSided.SetData<ushort>(instIBDouble);
        }


        public void Render()
        {
            GFX.Device.VertexDeclaration = GFXVertexDeclarations.PTDec;
            GFX.Device.Indices = indexBuffer;
            GFX.Device.Vertices[0].SetSource(vertexBuffer, 0, VertexPositionTexture.SizeInBytes);
            GFX.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
        }
    }

    public class RenderCube
    {
        VertexBuffer vertexBuffer;
        IndexBuffer indexBuffer;

        public RenderCube()
        {
            VertexPositionTexture[] verts = new VertexPositionTexture[]
                        {
                            new VertexPositionTexture(
                                new Vector3(1,-1,1),
                                new Vector2(1,1)),
                            new VertexPositionTexture(
                                new Vector3(-1,-1,1),
                                new Vector2(0,1)),
                            new VertexPositionTexture(
                                new Vector3(-1,1,1),
                                new Vector2(0,0)),
                            new VertexPositionTexture(
                                new Vector3(1,1,1),
                                new Vector2(1,0)),
                            new VertexPositionTexture(
                                new Vector3(1,-1,-1),
                                new Vector2(1,1)),
                            new VertexPositionTexture(
                                new Vector3(-1,-1,-1),
                                new Vector2(0,1)),
                            new VertexPositionTexture(
                                new Vector3(-1,1,-1),
                                new Vector2(0,0)),
                            new VertexPositionTexture(
                                new Vector3(1,1,-1),
                                new Vector2(1,0))
                        };

            vertexBuffer = new VertexBuffer(GFX.Device, verts.Length * VertexPositionTexture.SizeInBytes, BufferUsage.WriteOnly);
            vertexBuffer.SetData<VertexPositionTexture>(verts);

            short[] ib = new short[] { 0, 1, 2, 2, 3, 0, 6, 5, 4, 4, 7, 6, 
                                   3, 2, 6, 6, 7, 3, 5, 1, 0, 0, 4, 5, 
                                   6, 2, 1, 1, 5, 6, 0, 3, 7, 7, 4, 0};

            indexBuffer = new IndexBuffer(GFX.Device, sizeof(short) * ib.Length, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
            indexBuffer.SetData<short>(ib);

        }

        ~RenderCube()
        {
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
        }

        public void Render()
        {
            GFX.Device.VertexDeclaration = GFXVertexDeclarations.PTDec;
            GFX.Device.Indices = indexBuffer;
            GFX.Device.Vertices[0].SetSource(vertexBuffer, 0, VertexPositionTexture.SizeInBytes);
            GFX.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 8, 0, 12);
        }
    }

    public class LightCube
    {
        VertexBuffer vertexBuffer;
        IndexBuffer indexBuffer;

        public LightCube()
        {
            VertexPosition[] verts = new VertexPosition[]
                        {
                            new VertexPosition(
                                new Vector3(1,-1,1)),
                            new VertexPosition(
                                new Vector3(-1,-1,1)),
                            new VertexPosition(
                                new Vector3(-1,1,1)),
                            new VertexPosition(
                                new Vector3(1,1,1)),
                            new VertexPosition(
                                new Vector3(1,-1,-1)),
                            new VertexPosition(
                                new Vector3(-1,-1,-1)),
                            new VertexPosition(
                                new Vector3(-1,1,-1)),
                            new VertexPosition(
                                new Vector3(1,1,-1))
                        };

            vertexBuffer = new VertexBuffer(GFX.Device, verts.Length * VertexPosition.SizeInBytes, BufferUsage.WriteOnly);
            vertexBuffer.SetData<VertexPosition>(verts);

            short[] ib = new short[] { 0, 1, 2, 2, 3, 0, 6, 5, 4, 4, 7, 6, 
                                   3, 2, 6, 6, 7, 3, 5, 1, 0, 0, 4, 5, 
                                   6, 2, 1, 1, 5, 6, 0, 3, 7, 7, 4, 0};
            indexBuffer = new IndexBuffer(GFX.Device, sizeof(short) * ib.Length, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
            indexBuffer.SetData<short>(ib);
        }

        ~LightCube()
        {
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
        }

        public void Render()
        {
            GFX.Device.VertexDeclaration = GFXVertexDeclarations.PDec;
            GFX.Device.Indices = indexBuffer;
            GFX.Device.Vertices[0].SetSource(vertexBuffer, 0, VertexPosition.SizeInBytes);
            GFX.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 8, 0, 12);
        }
    }

    public class Sphere
    {
        public VoxelGeometry Geometry;

        public Sphere()
        {
            GenerateGeometry();
        }

        void GenerateGeometry()
        {
            byte[] DensityField;
            int DensityFieldSize = 17;
            byte IsoValue = 127;

            DensityField = new byte[DensityFieldSize * DensityFieldSize * DensityFieldSize];
            Vector3 center = Vector3.One * DensityFieldSize * 0.5f;
            Vector3 minPos = center;
            Vector3 maxPos = center;

            float radius = DensityFieldSize / 2;

            for (int x = 0; x < DensityFieldSize; x++)
            {
                for (int y = 1; y < (DensityFieldSize - 1); y++)
                {
                    for (int z = 0; z < DensityFieldSize; z++)
                    {
                        Vector3 pos = new Vector3(x, y, z);

                        float density = MathHelper.Clamp(1.0f-(pos-center).Length()/radius, 0, 1);
                        if (density > 0.0f)
                        {
                            pos = (pos / DensityFieldSize) * 2.0f - Vector3.One;
                            minPos = Vector3.Min(pos, minPos);
                            maxPos = Vector3.Max(pos, maxPos);
                        }
                        DensityField[x + (y + z * DensityFieldSize) * DensityFieldSize] = (byte)(density * 255.0f);
                    }
                }
            }

            Geometry = new VoxelGeometry();
            Vector3 ratio = 2.0f * Vector3.One / (float)(DensityFieldSize - 1);
            Geometry.GenerateGeometry(ref DensityField, IsoValue, DensityFieldSize, DensityFieldSize, DensityFieldSize, DensityFieldSize - 1, DensityFieldSize - 1, DensityFieldSize - 1, 0, 0, 0, ratio, Matrix.Identity);
        }
    }

    public class Gem
    {
        public VoxelGeometry Geometry;

        public Gem()
        {
            GenerateGeometry();
        }

        void GenerateGeometry()
        {
            byte[] DensityField;
            int DensityFieldSize = 17;
            byte IsoValue = 127;
            float radiusMax = (DensityFieldSize / 2);
            float radiusMin = (DensityFieldSize / 16);
            Vector3 cylinderCenter = Vector3.One * DensityFieldSize * 0.5f;
            Vector3 minPos, maxPos;
            minPos = maxPos = cylinderCenter;
            DensityField = new byte[DensityFieldSize * DensityFieldSize * DensityFieldSize];

            for (int x = 0; x < DensityFieldSize; x++)
            {
                for (int y = 1; y < (DensityFieldSize - 1); y++)
                {
                    for (int z = 0; z < DensityFieldSize; z++)
                    {
                        Vector3 pos = new Vector3(x, y, z);

                        float offset = Math.Abs(pos.Y - cylinderCenter.Y);
                        pos.Y = cylinderCenter.Y;
                        float radius = MathHelper.Lerp(radiusMax, radiusMin, offset / cylinderCenter.Y);
                        float density = Math.Max(1.0f - (pos - cylinderCenter).Length() / (radius), 0.0f);
                        if (density > 0.0f)
                        {
                            pos = (pos / DensityFieldSize) * 2.0f - Vector3.One;
                            minPos = Vector3.Min(pos, minPos);
                            maxPos = Vector3.Max(pos, maxPos);
                        }
                        DensityField[x + (y + z * DensityFieldSize) * DensityFieldSize] = (byte)(density * 255.0f);
                    }
                }
            }

            Geometry = new VoxelGeometry();
            Vector3 ratio = Vector3.One * 2.0f / (float)(DensityFieldSize - 1);
            Geometry.GenerateGeometry(ref DensityField, IsoValue, DensityFieldSize, DensityFieldSize, DensityFieldSize, DensityFieldSize - 1, DensityFieldSize - 1, DensityFieldSize - 1, 0, 0, 0, ratio, Matrix.Identity);
        }
    }

    public static class GFXPrimitives
    {
        public static ParticleGeometry Particle;
        public static ScreenAlignedQuad Quad;
        public static ScreenAlignedQuad Decal;
        public static RenderCube CubePT;
        public static LightCube Cube;
        public static Cylinder CylinderGeometry;
        public static Sphere SphereGeometry;
        public static Gem GemGeometry;

        public static RenderElement CreateBillboardElement()
        {
            RenderElement element;
            element = new RenderElement();
            element.IndexBuffer = GFXPrimitives.Quad.GetInstanceIndexBuffer();
            element.VertexBuffer = GFXPrimitives.Quad.GetInstanceVertexBuffer();
            element.VertexCount = 4;
            element.VertexDec = GFXVertexDeclarations.PTIDec;
            element.VertexStride = VertexPTI.SizeInBytes;
            element.StartVertex = 0;
            element.IsAnimated = false;
            element.PrimitiveCount = 2;
            return element;
        }

        public static void Initialize()
        {
            Quad = new ScreenAlignedQuad(false);
            Decal = new ScreenAlignedQuad(true);
            Cube = new LightCube();
            CubePT = new RenderCube();
            Particle = new ParticleGeometry();
            CylinderGeometry = new Cylinder(12);
            SphereGeometry = new Sphere();
            GemGeometry = new Gem();
        }
    }
}
