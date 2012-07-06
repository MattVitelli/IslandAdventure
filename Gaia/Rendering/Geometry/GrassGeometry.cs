using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gaia.Rendering.Geometry
{
    public class GrassGeometry
    {
        RenderElement[] grassGeometries;

        public GrassGeometry()
        {
            grassGeometries = new RenderElement[3];
            grassGeometries[0] = CreateGrass2(8, 200, 3);
            grassGeometries[1] = CreateGrass2(4, 15, 3);
            grassGeometries[2] = CreateGrass2(2, 10, 3);
        }

        public RenderElement GetHighDetail()
        {
            return grassGeometries[0];
        }

        RenderElement CreateGrass2(int numSubdivisions, int numElements, int numSides)
        {
            List<VertexPN> grassVerts = new List<VertexPN>();
            List<ushort> grassIB = new List<ushort>();
            Random randomGen = new Random();
            float grassRadiusMin = 0.01f;
            float grassRadiusMax = 0.075f;
            for (int k = 0; k < numElements; k++)
            {
                VertexPN[] vertices = new VertexPN[numSides * (numSubdivisions + 1)];
                float deltaTheta = 360.0f / (float)numSides;

                Vector3 randomPos = new Vector3((float)(randomGen.NextDouble()), 0, (float)(randomGen.NextDouble()));
                float randomRadius = MathHelper.Lerp(grassRadiusMin, grassRadiusMax, (float)randomGen.NextDouble());

                for (int i = 0; i < numSides; i++)
                {
                    float a = MathHelper.ToRadians(deltaTheta * i);
                    float deltaHeight = 1.0f / (float)numSubdivisions;
                    Vector3 pos = new Vector3((float)Math.Cos(a) * randomRadius, 0, (float)Math.Sin(a) * randomRadius) + randomPos;
                    for (int j = 0; j <= numSubdivisions; j++)
                    {
                        pos.Y = deltaHeight * j;
                        int index = i * (numSubdivisions + 1);
                        vertices[index + j].Position = pos;
                    }
                }
                
                ushort[] indices = new ushort[6 * numSides * numSubdivisions];
                for (int j = 0; j < numSubdivisions; j++)
                {
                    for (int i = 0; i < numSides; i++)
                    {
                        int index = (i + j * numSides) * 6;
                        int indexVert = i * (numSubdivisions+1) + j + grassVerts.Count;
                        indices[index] = (ushort)(indexVert + 1);
                        indices[index + 1] = (ushort)indexVert;
                        indices[index + 2] = (ushort)(indexVert + (numSubdivisions + 1));
                        indices[index + 3] = (ushort)(indexVert + (numSubdivisions + 1));
                        indices[index + 4] = (ushort)(indexVert + (numSubdivisions + 1)+1);
                        indices[index + 5] = (ushort)(indexVert + 1);
                        if (i == numSides - 1)
                        {
                            indices[index + 2] = (ushort)(j + grassVerts.Count);
                            indices[index + 4] = (ushort)(j + 1 + grassVerts.Count);
                            indices[index + 3] = (ushort)(j + grassVerts.Count);
                        }
                    }
                }
                grassVerts.AddRange(vertices);
                grassIB.AddRange(indices);
            }

            RenderElement element = new RenderElement();

            element.VertexBuffer = new VertexBuffer(GFX.Device, grassVerts.Count * VertexPN.SizeInBytes, BufferUsage.WriteOnly);
            element.VertexBuffer.SetData<VertexPN>(grassVerts.ToArray());

            element.StartVertex = 0;
            element.VertexStride = VertexPN.SizeInBytes;
            element.VertexDec = GFXVertexDeclarations.PNDec;
            element.VertexCount = grassVerts.Count;

            element.IndexBuffer = new IndexBuffer(GFX.Device, sizeof(ushort) * grassIB.Count, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
            element.IndexBuffer.SetData<ushort>(grassIB.ToArray());

            element.PrimitiveCount = grassIB.Count / 3;

            return element;
        }

        RenderElement CreateGrass(int numSubdivisions, int numElements, int numSides)
        {
            VertexPN[] vertices = new VertexPN[numSides * (numSubdivisions + 1) * numElements];
            float deltaTheta = 360.0f / (float)numSides;
            float deltaHeight = 1.0f / (float)numSubdivisions;
            Random randomGen = new Random();

            float grassRadius = 0.08f;

            for (int k = 0; k < numElements; k++)
            {
                float randomX = (float)(randomGen.NextDouble()*2.0-1.0);
                float randomZ = (float)(randomGen.NextDouble()*2.0-1.0);
                Vector3 randomCenter = new Vector3(randomX, 0.0f, randomZ);
                for (int i = 0; i < numSides; i++)
                {
                    float a = MathHelper.ToRadians(deltaTheta * i);
                    Vector3 pos = new Vector3((float)Math.Cos(a) * grassRadius, 0, (float)Math.Sin(a) * grassRadius);
                    pos = pos + randomCenter;

                    int index = (k * numSides + i) * (numSubdivisions + 1);
                    for (int j = 0; j <= numSubdivisions; j++)
                    {
                        vertices[index + j].Position = pos + Vector3.Up * deltaHeight * j;
                        if (j == numSubdivisions)
                        {
                            pos.X = randomCenter.X;
                            pos.Z = randomCenter.Z;
                        }

                    }
                }
            }

            ushort[] indices = new ushort[3 * numSides * (numSubdivisions + 1) * numElements];
            for (int k = 0; k < numElements; k++)
            {
                for (int i = 0; i < numSides; i++)
                {
                    for (int j = 0; j <= numSubdivisions; j++)
                    {
                        int vertStride = numSubdivisions + 1;
                        int index = (k * numSides + i) * 3 * (numSubdivisions + 1);
                        int indexVert = i * (numSubdivisions+1) + j;
                        indices[index] = (ushort)(indexVert + 1);
                        indices[index + 1] = (ushort)indexVert;
                        indices[index + 2] = (ushort)(indexVert + vertStride);
                        indices[index + 3] = (ushort)(indexVert + vertStride);
                        indices[index + 4] = (ushort)(indexVert + vertStride + 1);
                        indices[index + 5] = (ushort)(indexVert + 1);
                        if (i == numSides - 1)
                        {
                            indices[index + 2] = (ushort)j;
                            indices[index + 4] = (ushort)(j+1);
                            indices[index + 3] = (ushort)j;
                        }
                    }
                }
            }
            RenderElement element = new RenderElement();

            element.VertexBuffer = new VertexBuffer(GFX.Device, vertices.Length * VertexPN.SizeInBytes, BufferUsage.WriteOnly);
            element.VertexBuffer.SetData<VertexPN>(vertices);

            element.StartVertex = 0;
            element.VertexStride = VertexPN.SizeInBytes;
            element.VertexDec = GFXVertexDeclarations.PNDec;
            element.VertexCount = vertices.Length;

            element.IndexBuffer = new IndexBuffer(GFX.Device, sizeof(ushort) * indices.Length, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
            element.IndexBuffer.SetData<ushort>(indices);

            element.PrimitiveCount = indices.Length / 3;

            return element;
        }
    }
}
