using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gaia.Rendering.Geometry
{
    public class ProjectiveOcean
    {
        int subdivisions = 20;
        VertexBuffer vertexBuffer;
        IndexBuffer indexBuffer;

        int primitiveCount;
        int vertexCount;

        public ProjectiveOcean(int subdivisionLevel)
        {
            subdivisions = subdivisionLevel;

            int subdivOne = subdivisions + 1;
            vertexCount = subdivOne * subdivOne;
            VertexPositionTexture[] verts = new VertexPositionTexture[vertexCount];

            Vector3 deltaCrd = Vector3.One * 2.0f / (float)subdivisions;
            Vector3 origin = Vector3.One * -1.0f;
            origin.Y = 0.0f;
            Vector2 originTC = new Vector2(0, 1);
            Vector2 deltaTC = (new Vector2(1, 0) - new Vector2(0, 1)) / (float)subdivisions;

            for (int i = 0; i < subdivOne; i++)
            {
                for (int j = 0; j < subdivOne; j++)
                {
                    int index = i + j * subdivOne;
                    verts[index] = new VertexPositionTexture(
                        origin + new Vector3(deltaCrd.X * i, 0, deltaCrd.Y * j),
                        originTC + new Vector2(deltaTC.X * i, deltaTC.Y * j));
                }
            }

            primitiveCount = 2 * subdivisions * subdivisions;

            ushort[] ib = new ushort[3 * primitiveCount];
            for (int i = 0; i < subdivisions; i++)
            {
                for (int j = 0; j < subdivisions; j++)
                {
                    int vertIndex = i + j * subdivOne;
                    int index = (i + j * subdivisions) * 6;
                    ib[index] = (ushort)(vertIndex + subdivOne); 
                    ib[index + 1] = (ushort)(vertIndex);
                    ib[index + 2] = (ushort)(vertIndex + 1);
                    ib[index + 3] = (ushort)(vertIndex + 1);
                    ib[index + 4] = (ushort)(vertIndex + 1 + subdivOne);
                    ib[index + 5] = (ushort)(vertIndex + subdivOne); 
                }
            }

            vertexBuffer = new VertexBuffer(GFX.Device, verts.Length * VertexPositionTexture.SizeInBytes, BufferUsage.WriteOnly);
            vertexBuffer.SetData<VertexPositionTexture>(verts);

            indexBuffer = new IndexBuffer(GFX.Device, sizeof(ushort) * ib.Length, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
            indexBuffer.SetData<ushort>(ib);
        }

        ~ProjectiveOcean()
        {
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
        }

        public void Render()
        {
            GFX.Device.VertexDeclaration = GFXVertexDeclarations.PTDec;
            GFX.Device.Indices = indexBuffer;
            GFX.Device.Vertices[0].SetSource(vertexBuffer, 0, VertexPositionTexture.SizeInBytes);
            GFX.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertexCount, 0, primitiveCount);
        }
    }
}
