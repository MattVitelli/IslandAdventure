using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Core;
using Gaia.Rendering;
using Gaia.Rendering.RenderViews;

using JigLibX.Geometry;

namespace Gaia.Resources
{
    class Imposter
    {
        public RenderTarget2D BaseMap;
        public RenderTarget2D NormalMap;

        public Material ImposterMaterial;

        public RenderElement Element;

        public Imposter()
        {
            Element = GFXPrimitives.CreateBillboardElement();
            ImposterMaterial = new Material();
        }
    }

    public class Mesh : IResource
    {
        string name;

        public string Name
        {
            get { return name; }
        }

        ModelPart[] parts;
        AnimationNode[] nodes;
        SortedList<string, List<ModelPart> > LODS = new SortedList<string, List<ModelPart> >();
        SortedList<string, AnimationNode> namesToNodes = new SortedList<string, AnimationNode>();
        SortedList<string, Matrix> inverseMatrices = new SortedList<string, Matrix>();
        AnimationNode[] rootNodes;
        BoundingBox meshBounds;
        TriangleMesh collisionMesh;
        int vertexCount;
        VertexBuffer vertexBuffer;
        VertexBuffer vertexBufferInstanced;

        Imposter imposterGeometry = null;

        public bool Rendered = true;
        
        public TriangleMesh GetCollisionMesh()
        {
            return collisionMesh;
        }
        
        public BoundingBox GetBounds()
        {
            return meshBounds;
        }

        class ModelPart
        {
            public RenderElement renderElement;
            public RenderElement renderElementInstanced;
            public string name;
            public Material material;
            public BoundingBox bounds;
            public SortedList<RenderView, List<Matrix>> cachedTransforms = new SortedList<RenderView, List<Matrix>>();
        }

        struct ModelVertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 TexCoord;
            public Vector3 Tangent;
            public int BoneIndex;
            public int Weight;

            public void AddNormal(Vector3 normal)
            {
                Normal += normal;
                Weight++;
            }

            public void NormalizeWeights()
            {
                Normal /= Weight;
                TexCoord /= Weight;
                Tangent /= Weight;
                Weight = 1;
            }

            public void AddTangent(ModelVertex v1, ModelVertex v2)
            {
                Vector3 d0 = v1.Position - Position;
                Vector3 d1 = v2.Position - Position;

                Vector2 s = v1.TexCoord - TexCoord;
                Vector2 t = v2.TexCoord - TexCoord;

                float r = 1.0F / (t.X * s.Y - t.Y * s.X);
                Tangent += new Vector3(s.Y * d1.X - t.Y * d0.X, s.Y * d1.Y - t.Y * d0.Y, s.Y * d1.Z - t.Y * d0.Z) * r;
                Weight++;
            }
        }

        struct Triangle
        {
            public ushort vertex0;
            public ushort vertex1;
            public ushort vertex2;
        }

        public AnimationNode[] GetRootNodes(out SortedList<string, AnimationNode> nodeCollection)
        {
            
            nodeCollection = new SortedList<string, AnimationNode>();

            if (rootNodes == null)
                return null;

            AnimationNode[] dupNodes = new AnimationNode[rootNodes.Length];
            for (int i = 0; i < rootNodes.Length; i++)
                dupNodes[i] = rootNodes[i].RecursiveCopy(nodeCollection);

            return dupNodes;
        }

        void CheckMissingMaterials(string filename, int[] materialIndices, string[] materialNames)
        {
            SortedList<int, byte> missingIndicesList = new SortedList<int, byte>();
            for (int i = 0; i < materialIndices.Length; i++)
            {
                if (ResourceManager.Inst.GetMaterial(materialNames[materialIndices[i]]) == null && !missingIndicesList.ContainsKey(materialIndices[i]))
                    missingIndicesList.Add(materialIndices[i], 1);
            }

            if(missingIndicesList.Count == 0)
                return;

            string newFileName = filename.Substring(0, filename.Length - 4) + "_MISSING.txt";

            using (FileStream fs = new FileStream(newFileName, FileMode.Create))
            {
                using (StreamWriter wr = new StreamWriter(fs))
                {
                    for (int i = 0; i < missingIndicesList.Count; i++)
                        wr.WriteLine(materialNames[missingIndicesList.Keys[i]]);
                }
            }
        }

        void LoadMS3D(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                using (BinaryReader br = new BinaryReader(fs, System.Text.Encoding.Default))
                {
                    br.ReadChars(10);
                    br.ReadInt32();
                    vertexCount = br.ReadUInt16();
                    ModelVertex[] vertices = new ModelVertex[vertexCount];

                    Vector3 minVert = Vector3.One * float.PositiveInfinity;
                    Vector3 maxVert = Vector3.One * float.NegativeInfinity;
                    for (int i = 0; i < vertexCount; i++)
                    {
                        br.ReadByte();
                        vertices[i].Position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                        minVert = Vector3.Min(minVert, vertices[i].Position);
                        maxVert = Vector3.Max(maxVert, vertices[i].Position);
                        vertices[i].BoneIndex = (int)br.ReadChar();
                        if (vertices[i].BoneIndex >= 255)
                            vertices[i].BoneIndex = 0;
                        vertices[i].Weight = 1;
                        br.ReadByte();
                    }

                    ushort triangleCount = br.ReadUInt16();

                    Triangle[] triList = new Triangle[triangleCount];
                    for (int i = 0; i < triangleCount; i++)
                    {
                        br.ReadUInt16(); //flag

                        //Indices
                        ushort v0 = br.ReadUInt16();
                        ushort v1 = br.ReadUInt16();
                        ushort v2 = br.ReadUInt16();
                        triList[i].vertex0 = v0;
                        triList[i].vertex1 = v1;
                        triList[i].vertex2 = v2;

                        //Vertex 0 Normal
                        vertices[v0].Normal += new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

                        //Vertex 1 Normal
                        vertices[v1].Normal += new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

                        //Vertex 2 Normal
                        vertices[v2].Normal += new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

                        //U
                        vertices[v0].TexCoord.X = br.ReadSingle();
                        vertices[v1].TexCoord.X = br.ReadSingle();
                        vertices[v2].TexCoord.X = br.ReadSingle();

                        //V
                        vertices[v0].TexCoord.Y = br.ReadSingle();
                        vertices[v1].TexCoord.Y = br.ReadSingle();
                        vertices[v2].TexCoord.Y = br.ReadSingle();

                        vertices[v0].Weight++;
                        vertices[v1].Weight++;
                        vertices[v2].Weight++;

                        //Smoothing
                        br.ReadByte();

                        //Group index
                        br.ReadByte();
                    }

                    for (int i = 0; i < vertexCount; i++)
                    {
                        vertices[i].Normal /= vertices[i].Weight;
                        vertices[i].Weight = 1;
                    }

                    for (int i = 0; i < triangleCount; i++)
                    {
                        vertices[triList[i].vertex0].AddTangent(vertices[triList[i].vertex1], vertices[triList[i].vertex2]);
                        vertices[triList[i].vertex1].AddTangent(vertices[triList[i].vertex0], vertices[triList[i].vertex2]);
                        vertices[triList[i].vertex2].AddTangent(vertices[triList[i].vertex0], vertices[triList[i].vertex1]);
                    }

                    VertexPNTTI[] verts = new VertexPNTTI[vertexCount];
                    for (int i = 0; i < vertexCount; i++)
                    {
                        Vector3 N = vertices[i].Normal;
                        N.Normalize();
                        Vector3 tangent = vertices[i].Tangent / vertices[i].Weight;
                        tangent = (tangent - N * Vector3.Dot(N, tangent));
                        tangent.Normalize();
                        vertices[i].Tangent = tangent;
                        vertices[i].Weight = 1;
                        verts[i] = new VertexPNTTI(vertices[i].Position, vertices[i].Normal, vertices[i].TexCoord, vertices[i].Tangent, vertices[i].BoneIndex);
                    }
                    vertexBuffer = new VertexBuffer(GFX.Device, vertexCount * VertexPNTTI.SizeInBytes, BufferUsage.None);
                    vertexBuffer.SetData<VertexPNTTI>(verts);

                    ushort groupCount = br.ReadUInt16();
                    parts = new ModelPart[groupCount];
                    int[] matIndices = new int[groupCount];
                    for (int i = 0; i < groupCount; i++)
                    {
                        br.ReadByte();
                        parts[i] = new ModelPart();
                        parts[i].name = new string(br.ReadChars(32)); //Group Name
                        parts[i].name = parts[i].name.Replace("\0",string.Empty);
                        ushort numTriangles = br.ReadUInt16(); //numTriangles

                        parts[i].renderElement = new RenderElement();
                        parts[i].renderElement.PrimitiveCount = numTriangles;
                        parts[i].renderElement.StartVertex = 0;
                        parts[i].renderElement.VertexBuffer = vertexBuffer;
                        parts[i].renderElement.VertexCount = vertexCount;
                        parts[i].renderElement.VertexStride = VertexPNTTI.SizeInBytes;
                        parts[i].renderElement.VertexDec = GFXVertexDeclarations.PNTTIDec;
                        parts[i].bounds.Max = Vector3.Zero;
                        parts[i].bounds.Min = Vector3.Zero;

                        bool useIntIndices = (numTriangles >= ushort.MaxValue);
                        IndexElementSize size = (useIntIndices) ? IndexElementSize.ThirtyTwoBits : IndexElementSize.SixteenBits;
                        int stride = (useIntIndices) ? sizeof(uint) : sizeof(ushort);

                        parts[i].renderElement.IndexBuffer = new IndexBuffer(GFX.Device, stride * numTriangles * 3, BufferUsage.None, size);

                        List<ushort> ushortIndices = new List<ushort>();
                        List<uint> uintIndices = new List<uint>();
                        for (int l = 0; l < numTriangles; l++)
                        {
                            ushort t = br.ReadUInt16(); //triangle index
                            if (useIntIndices)
                            {
                                uintIndices.Add((uint)triList[t].vertex2);
                                uintIndices.Add((uint)triList[t].vertex1);
                                uintIndices.Add((uint)triList[t].vertex0);

                            }
                            else
                            {
                                ushortIndices.Add((ushort)triList[t].vertex2);
                                ushortIndices.Add((ushort)triList[t].vertex1);
                                ushortIndices.Add((ushort)triList[t].vertex0);
                            }
                            parts[i].bounds.Max = Vector3.Max(parts[i].bounds.Max, vertices[triList[t].vertex0].Position);
                            parts[i].bounds.Max = Vector3.Max(parts[i].bounds.Max, vertices[triList[t].vertex1].Position);
                            parts[i].bounds.Max = Vector3.Max(parts[i].bounds.Max, vertices[triList[t].vertex2].Position);

                            parts[i].bounds.Min = Vector3.Min(parts[i].bounds.Min, vertices[triList[t].vertex0].Position);
                            parts[i].bounds.Min = Vector3.Min(parts[i].bounds.Min, vertices[triList[t].vertex1].Position);
                            parts[i].bounds.Min = Vector3.Min(parts[i].bounds.Min, vertices[triList[t].vertex2].Position);
                        }
                        if (useIntIndices)
                            parts[i].renderElement.IndexBuffer.SetData<uint>(uintIndices.ToArray());
                        else
                            parts[i].renderElement.IndexBuffer.SetData<ushort>(ushortIndices.ToArray());

                        matIndices[i] = (int)br.ReadChar(); //Material index
                    }

                    meshBounds = new BoundingBox(parts[0].bounds.Min, parts[0].bounds.Max);
                    for (int i = 1; i < parts.Length; i++)
                    {
                        meshBounds.Min = Vector3.Min(parts[i].bounds.Min, meshBounds.Min);
                        meshBounds.Max = Vector3.Max(parts[i].bounds.Max, meshBounds.Max);
                    }

                    ushort MaterialCount = br.ReadUInt16();
                    string[] materialNames = new string[MaterialCount];
                    for (int i = 0; i < MaterialCount; i++)
                    {

                        materialNames[i] = new string(br.ReadChars(32));
                        materialNames[i] = materialNames[i].Replace("\0", string.Empty);
                        for (int l = 0; l < 4; l++)
                            br.ReadSingle();
                        for (int l = 0; l < 4; l++)
                            br.ReadSingle();
                        for (int l = 0; l < 4; l++)
                            br.ReadSingle();
                        for (int l = 0; l < 4; l++)
                            br.ReadSingle();

                        br.ReadSingle();
                        br.ReadSingle();
                        br.ReadChar();
                        br.ReadChars(128);
                        br.ReadChars(128);
                    }

                    CheckMissingMaterials(filename, matIndices, materialNames);

                    for (int i = 0; i < groupCount; i++)
                    {
                        int matIndex = matIndices[i];
                        if (matIndex < 255)
                            parts[i].material = ResourceManager.Inst.GetMaterial(materialNames[matIndex]);

                        if (parts[i].material == null)
                            parts[i].material = ResourceManager.Inst.GetMaterial("NULL");
                    }
                    
                    br.ReadSingle();//FPS
                    br.ReadSingle();//current time
                    br.ReadInt32(); //Total frames

                    ushort boneCount = br.ReadUInt16();
                    nodes = new AnimationNode[boneCount];
                    if (boneCount > 0)
                    {
                        List<string> nodeParentNames = new List<string>();

                        for (int i = 0; i < boneCount; i++)
                        {
                            br.ReadByte(); //flag

                            string name = new string(br.ReadChars(32));
                            string parentName = new string(br.ReadChars(32));
                            nodeParentNames.Add(parentName);

                            Vector3 rotation = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

                            Vector3 position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

                            nodes[i] = new AnimationNode(name, position, rotation);
                            namesToNodes.Add(name, nodes[i]);

                            ushort keyRotCount = br.ReadUInt16(); //Key frame rot count
                            ushort keyPosCount = br.ReadUInt16(); //Key frame pos count
                            for (int j = 0; j < keyRotCount; j++)
                            {
                                for (int k = 0; k < 4; k++)
                                    br.ReadSingle();
                                /*
                                bones[i].rotationFrames[l].time = br.ReadSingle() * fps; //time
                                //rotation
                                bones[i].rotationFrames[l].Displacement = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                                bones[i].rotationFrames[l].refBone = i;*/
                            }

                            for (int j = 0; j < keyPosCount; j++)
                            {
                                for (int k = 0; k < 4; k++)
                                    br.ReadSingle();
                                /*
                                bones[i].translationFrames[l].time = br.ReadSingle() * fps; //time
                                //position
                                Vector3 disp = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                                bones[i].translationFrames[l].Displacement = disp;
                                bones[i].translationFrames[l].refBone = i;*/
                            }
                        }

                        List<AnimationNode> rootNodeList = new List<AnimationNode>();
                        for (int i = 0; i < nodes.Length; i++)
                        {
                            if (namesToNodes.ContainsKey(nodeParentNames[i]))
                            {
                                AnimationNode node = namesToNodes[nodeParentNames[i]];
                                node.children.Add(nodes[i]);
                            }
                            else
                                rootNodeList.Add(nodes[i]);
                        }
                        rootNodes = rootNodeList.ToArray();
                        for (int i = 0; i < rootNodes.Length; i++)
                        {
                            Matrix identityMat = Matrix.Identity;
                            rootNodes[i].ApplyTransform(ref identityMat);
                        }

                        Matrix[] inverseMats = new Matrix[nodes.Length];
                        for(int i = 0; i < inverseMats.Length; i++)
                            inverseMats[i] = Matrix.Invert(nodes[i].Transform);
                        
                        for(int i = 0; i < verts.Length; i++)
                        {
                            verts[i].Position = Vector3.Transform(verts[i].Position, inverseMats[(int)verts[i].Index]);
                        }
                        vertexBuffer.SetData<VertexPNTTI>(verts);
                        
                        for (int i = 0; i < nodes.Length; i++)
                        {
                            Vector3 Rotation = nodes[i].Rotation;
                            Matrix transform = Matrix.CreateRotationX(Rotation.X) * Matrix.CreateRotationY(Rotation.Y) * Matrix.CreateRotationZ(Rotation.Z);
                            transform.Translation = nodes[i].Translation;
                            nodes[i].Transform = transform;
                        }
                        
                    }
                }
            }
        }

        void ModifyMesh()
        {
            List<ModelPart> meshes = new List<ModelPart>();
            ModelPart collisionMesh = null;
            for (int i = 0; i < parts.Length; i++)
            {
                string[] meshName = parts[i].name.Split(':');
                if (meshName.Length == 1)
                {
                    if (meshName[0] == "COLLISION")
                        collisionMesh = parts[i];
                    else
                        meshes.Add(parts[i]);                        
                }
                else
                {
                    if (!LODS.ContainsKey(meshName[0]))
                        LODS.Add(meshName[0], new List<ModelPart>());
                    int currLODValue = int.Parse(meshName[1].Substring(3));
                    parts[i].name = currLODValue.ToString();
                    bool addedPart = false;
                    for(int j = 0; j < LODS[meshName[0]].Count; j++)
                    {
                        int LODValue = int.Parse(LODS[meshName[0]][j].name);
                        if(currLODValue < LODValue)
                        {
                            LODS[meshName[0]].Insert(j, parts[i]);
                            addedPart = true;
                            break;
                        }
                    }
                    if (!addedPart)
                        LODS[meshName[0]].Add(parts[i]);
                }
            }

            if (collisionMesh != null)
                CreateCollisionMesh(collisionMesh);
            parts = meshes.ToArray();

        }

        void CreateCollisionMesh(ModelPart collisionMesh)
        {
            SortedList<ushort, ushort> renamedVertexIndices = new SortedList<ushort, ushort>();
            SortedList<ushort, ushort> renamedVertexIndicesCollision = new SortedList<ushort, ushort>();
            RenderElement collisionElem = collisionMesh.renderElement;
            List<Vector3> collisionVerts = new List<Vector3>();
            List<TriangleVertexIndices> collisionIndices = new List<TriangleVertexIndices>(collisionElem.PrimitiveCount);
            VertexPNTTI[] vertexData = new VertexPNTTI[vertexCount];
            vertexBuffer.GetData<VertexPNTTI>(vertexData);
            ushort[] indexDataCollision = new ushort[collisionElem.PrimitiveCount * 3];
            collisionElem.IndexBuffer.GetData<ushort>(indexDataCollision);

            for (int i = 0; i < collisionElem.PrimitiveCount; i++)
            {
                int index = i * 3;
                for(int j = 0; j < 3; j++)
                {
                    ushort currIdx = indexDataCollision[index + j];
                    if(!renamedVertexIndicesCollision.ContainsKey(currIdx))
                    {
                        renamedVertexIndicesCollision.Add(currIdx, (ushort)collisionVerts.Count);
                        Vector3 pos;
                        pos.X = vertexData[currIdx].Position.X;
                        pos.Y = vertexData[currIdx].Position.Y;
                        pos.Z = vertexData[currIdx].Position.Z;
                        collisionVerts.Add(pos);
                    }
                }
                ushort idx0 = indexDataCollision[index];
                ushort idx1 = indexDataCollision[index + 1];
                ushort idx2 = indexDataCollision[index + 2];
                collisionIndices.Add(new TriangleVertexIndices(renamedVertexIndicesCollision[idx2], renamedVertexIndicesCollision[idx1], renamedVertexIndicesCollision[idx0]));
            }

            this.collisionMesh = new TriangleMesh(collisionVerts, collisionIndices);

            List<VertexPNTTI> newVertexData = new List<VertexPNTTI>();
            for (int i = 0; i < parts.Length; i++)
            {
                RenderElement currElem = parts[i].renderElement;
                ushort[] indexData = new ushort[currElem.PrimitiveCount * 3];
                currElem.IndexBuffer.GetData<ushort>(indexData);
                for (int j = 0; j < indexData.Length; j++)
                {
                    if (!renamedVertexIndices.ContainsKey(indexData[j]))
                    {
                        renamedVertexIndices.Add(indexData[j], (ushort)newVertexData.Count);
                        newVertexData.Add(vertexData[indexData[j]]);
                    }
                    indexData[j] = renamedVertexIndices[indexData[j]];
                }
                currElem.IndexBuffer.SetData<ushort>(indexData);
            }
            vertexBuffer.Dispose();

            vertexCount = newVertexData.Count;
            vertexBuffer = new VertexBuffer(GFX.Device, VertexPNTTI.SizeInBytes * newVertexData.Count, BufferUsage.None);
            vertexBuffer.SetData<VertexPNTTI>(newVertexData.ToArray());
            for (int i = 0; i < parts.Length; i++)
                parts[i].renderElement.VertexBuffer = vertexBuffer;
        }

        /*
        void CreateCollisionMesh()
        {
            List<TriangleVertexIndices> indices = new List<TriangleVertexIndices>();
            List<Vector3> vertices = new List<Vector3>(vertexCount);

            VertexPNTTI[] verts = new VertexPNTTI[vertexCount];
            vertexBuffer.GetData<VertexPNTTI>(verts);
            
            for (int i = 0; i < verts.Length; i++)
            {
                vertices.Add(JitterConverter.ToJitter(Vector4.Transform(verts[i].Position, nodes[(int)verts[i].Index].Transform)));
            }

            for (int i = 0; i < parts.Length; i++)
            {
                RenderElement srcElem = parts[i].renderElement;
                ushort[] indexData = new ushort[srcElem.PrimitiveCount * 3];
                srcElem.IndexBuffer.GetData<ushort>(indexData);
                TriangleVertexIndices tvi;
                for (int j = 0; j < indexData.Length; ++j)
                {
                    tvi.I0 = indexData[j * 3 + 0];
                    tvi.I1 = indexData[j * 3 + 1];
                    tvi.I2 = indexData[j * 3 + 2];
                    indices.Add(tvi);
                }
            }

            collisionMesh = new Octree(vertices, indices);
        }
        */

        public void RenderNoLOD(Matrix transform, RenderView view)
        {
            BoundingFrustum frustum = new BoundingFrustum(transform * view.GetViewProjection());

            for (int i = 0; i < parts.Length; i++)
            {
                if (frustum.Contains(parts[i].bounds) != ContainmentType.Disjoint)
                {
                    RenderElement element = parts[i].renderElement;
                    element.Transform = new Matrix[1] { transform };
                    view.AddElement(parts[i].material, element);
                }
            }
        }

        public void RenderPostSceneQuery()
        {
            for (int i = 0; i < parts.Length; i++)
            {
                for (int j = 0; j < parts[i].cachedTransforms.Count; j++)
                {
                    RenderView view = parts[i].cachedTransforms.Keys[j];
                    RenderElement tempElem = parts[i].renderElementInstanced;
                    tempElem.Transform = parts[i].cachedTransforms[view].ToArray();
                    if (tempElem.Transform.Length > 1)
                    {
                        int m = 0;
                    }
                    view.AddElement(parts[i].material, tempElem);
                    parts[i].cachedTransforms[view].Clear();
                }
            }
        }

        public void Render(Matrix transform, RenderView view)
        {
            if (Rendered)
                GFX.Inst.AddMeshToRender(this);

            BoundingFrustum frustum = view.GetFrustum();
            Matrix oldMat = frustum.Matrix;
            frustum.Matrix = transform * view.GetViewProjection();
            /*
            if (imposterGeometry != null)
            {
                RenderElement srcElem = imposterGeometry.Element;
                RenderElement element = new RenderElement();
                element.IndexBuffer = srcElem.IndexBuffer;
                element.PrimitiveCount = srcElem.PrimitiveCount;
                element.StartVertex = srcElem.StartVertex;
                element.VertexBuffer = srcElem.VertexBuffer;
                element.VertexCount = srcElem.VertexCount;
                element.VertexDec = srcElem.VertexDec;
                element.VertexStride = srcElem.VertexStride;
                element.Transform = new Matrix[1] { transform };
                view.AddElement(imposterGeometry.ImposterMaterial, element);
            }
            */
            //else
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    if (frustum.Contains(parts[i].bounds) != ContainmentType.Disjoint)
                    {
                        if (!parts[i].cachedTransforms.ContainsKey(view))
                            parts[i].cachedTransforms.Add(view, new List<Matrix>());
                        parts[i].cachedTransforms[view].Add(transform);
                        /*
                        RenderElement srcElem = parts[i].renderElement;
                        RenderElement element = srcElem;
                        element.Transform = new Matrix[1] { transform };
                        view.AddElement(parts[i].material, element);
                        */
                    }
                }
            }
            frustum.Matrix = oldMat;
        }

        public void Render(Matrix transform, SortedList<string, AnimationNode> animNodes, RenderView view)
        {
            BoundingFrustum frustum = view.GetFrustum();
            Matrix oldMat = frustum.Matrix;
            frustum.Matrix = transform * view.GetViewProjection();
            Matrix[] transforms = new Matrix[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
                transforms[i] = animNodes[nodes[i].Name].Transform;
            for (int i = 0; i < parts.Length; i++)
            {
                if (frustum.Contains(parts[i].bounds) != ContainmentType.Disjoint)
                {
                    RenderElement srcElem = parts[i].renderElement;
                    RenderElement element = new RenderElement();
                    element.IndexBuffer = srcElem.IndexBuffer;
                    element.PrimitiveCount = srcElem.PrimitiveCount;
                    element.StartVertex = srcElem.StartVertex;
                    element.VertexBuffer = srcElem.VertexBuffer;
                    element.VertexCount = srcElem.VertexCount;
                    element.VertexDec = srcElem.VertexDec;
                    element.VertexStride = srcElem.VertexStride;
                    element.Transform = transforms;
                    element.IsAnimated = true;
                    view.AddElement(parts[i].material, element);
                }
            }
            frustum.Matrix = oldMat;
        }

        void CreateInstanceData()
        {
            VertexPNTTI[] vertData = new VertexPNTTI[vertexCount];
            vertexBuffer.GetData<VertexPNTTI>(vertData);
            VertexPNTTI[] instVerts = new VertexPNTTI[vertexCount * GFXShaderConstants.NUM_INSTANCES];
            for (int i = 0; i < GFXShaderConstants.NUM_INSTANCES; i++)
            {
                for (int j = 0; j < vertexCount; j++)
                {
                    
                    int index = i * vertexCount + j;
                    instVerts[index] = vertData[j];
                    instVerts[index].Index = i;
                }
            }

            vertexBufferInstanced = new VertexBuffer(GFX.Device, instVerts.Length * VertexPNTTI.SizeInBytes, BufferUsage.None);
            vertexBufferInstanced.SetData<VertexPNTTI>(instVerts);

            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].renderElementInstanced = parts[i].renderElement;
                IndexElementSize elementSize = parts[i].renderElement.IndexBuffer.IndexElementSize;
                IndexBuffer indexBufferInstanced;

                if (instVerts.Length > ushort.MaxValue)
                    elementSize = IndexElementSize.ThirtyTwoBits;
                if (elementSize == IndexElementSize.SixteenBits)
                {
                    ushort[] indexData = new ushort[parts[i].renderElement.PrimitiveCount * 3];
                    parts[i].renderElement.IndexBuffer.GetData<ushort>(indexData);

                    ushort[] instIB = new ushort[indexData.Length * GFXShaderConstants.NUM_INSTANCES];
                    for (int k = 0; k < GFXShaderConstants.NUM_INSTANCES; k++)
                    {
                        for (int j = 0; j < indexData.Length; j++)
                        {
                            int newIndex = indexData[j] + k * vertexCount;
                            if (newIndex > ushort.MaxValue)
                                Console.WriteLine("This is very very bad!");
                            instIB[k * indexData.Length + j] = (ushort)newIndex;
                        }
                    }

                    indexBufferInstanced = new IndexBuffer(GFX.Device, sizeof(ushort) * instIB.Length, BufferUsage.None, elementSize);

                    indexBufferInstanced.SetData<ushort>(instIB);
                }
                else
                {
                    ushort[] indexData = new ushort[parts[i].renderElement.PrimitiveCount * 3];
                    parts[i].renderElement.IndexBuffer.GetData<ushort>(indexData);

                    uint[] instIB = new uint[indexData.Length * GFXShaderConstants.NUM_INSTANCES];
                    for (int k = 0; k < GFXShaderConstants.NUM_INSTANCES; k++)
                    {
                        for (int j = 0; j < indexData.Length; j++)
                        {
                            ulong index = (ulong)indexData[j] + (ulong)k * (ulong)vertexCount;
                            if (index > ulong.MaxValue)
                                Console.WriteLine("This is very very bad!");
                            instIB[k * indexData.Length + j] = (uint)(indexData[j] + k * vertexCount);
                        }
                    }

                    indexBufferInstanced = new IndexBuffer(GFX.Device, sizeof(uint) * instIB.Length, BufferUsage.None, elementSize);

                    indexBufferInstanced.SetData<uint>(instIB);
                }
                parts[i].renderElementInstanced.VertexDec = GFXVertexDeclarations.PNTTIDec;
                parts[i].renderElementInstanced.VertexStride = VertexPNTTI.SizeInBytes;
                parts[i].renderElementInstanced.IsAnimated = false;
                parts[i].renderElementInstanced.VertexBuffer = vertexBufferInstanced;
                parts[i].renderElementInstanced.IndexBuffer = indexBufferInstanced;
            }
        }

        void CreateImposter()
        {
            const int textureSize = 128;
            const int numViews = 4;

            int textureWidth = textureSize * numViews;
            int textureHeight = textureSize;

            imposterGeometry = new Imposter();
            imposterGeometry.BaseMap = new RenderTarget2D(GFX.Device, textureWidth, textureHeight, 1, SurfaceFormat.Color);
            imposterGeometry.NormalMap = new RenderTarget2D(GFX.Device, textureWidth, textureHeight, 1, SurfaceFormat.Vector2);
            
            ImposterRenderView renderViewImposter = new ImposterRenderView(Matrix.Identity, Matrix.Identity, Vector3.Zero, 1.0f, 1000.0f);

            Vector3 centerPos = (this.meshBounds.Min + this.meshBounds.Max)*0.5f;
            float rad = Math.Max(this.meshBounds.Min.Length(), this.meshBounds.Max.Length());

            renderViewImposter.SetNearPlane(1.0f);
            renderViewImposter.SetFarPlane(rad * rad);
            renderViewImposter.SetProjection(Matrix.CreateOrthographicOffCenter(-rad * 0.5f, rad * 0.5f, -rad * 0.5f, rad * 0.5f, 1.0f, rad * rad));

            Viewport oldViewport = GFX.Device.Viewport;
            DepthStencilBuffer oldDSBuffer = GFX.Device.DepthStencilBuffer;
            DepthStencilBuffer dsBufferImposter = new DepthStencilBuffer(GFX.Device, textureWidth, textureHeight, oldDSBuffer.Format);
            GFX.Device.DepthStencilBuffer = dsBufferImposter;

            float deltaTheta = MathHelper.TwoPi / (float)numViews;

            GFX.Device.SetRenderTarget(0, imposterGeometry.BaseMap);
            GFX.Device.SetRenderTarget(1, imposterGeometry.NormalMap);
            GFX.Device.Clear(Color.TransparentBlack);
            
            for (int i = 0; i < numViews; i++)
            {
                float theta = deltaTheta * i;
                Vector3 offset = new Vector3((float)Math.Cos(theta), 0, (float)Math.Sin(theta)) * rad;
                Vector3 camPos = centerPos + offset;

                renderViewImposter.SetPosition(camPos);
                renderViewImposter.SetView(Matrix.CreateLookAt(camPos, centerPos, Vector3.Up));

                Viewport newViewport = new Viewport();
                newViewport.X = i * textureSize;
                newViewport.Y = 0;
                newViewport.Width = textureSize;
                newViewport.Height = textureHeight;

                GFX.Device.Viewport = newViewport;
                this.RenderNoLOD(Matrix.Identity, renderViewImposter);
                renderViewImposter.Render();
            }

            GFX.Device.SetRenderTarget(1, null);

            for (int i = 0; i < numViews; i++)
            {
                float theta = deltaTheta * i;
                Vector3 offset = new Vector3((float)Math.Cos(theta), 0, (float)Math.Sin(theta)) * rad;
                Vector3 camPos = centerPos + offset;

                renderViewImposter.SetPosition(camPos);
                renderViewImposter.SetView(Matrix.CreateLookAt(camPos, centerPos, Vector3.Up));

                Viewport newViewport = new Viewport();
                newViewport.X = i * textureSize;
                newViewport.Y = 0;
                newViewport.Width = textureSize;
                newViewport.Height = textureHeight;

                GFX.Device.Viewport = newViewport;
                this.RenderNoLOD(Matrix.Identity, renderViewImposter);
                renderViewImposter.RenderBlended();
            }

            GFX.Device.SetRenderTarget(0, null);

            GFX.Device.Viewport = oldViewport;
            GFX.Device.DepthStencilBuffer = oldDSBuffer;
            dsBufferImposter.Dispose();
            imposterGeometry.BaseMap.GetTexture().Save("BaseMapImposter.dds", ImageFileFormat.Dds);

            imposterGeometry.ImposterMaterial.SetShader(ResourceManager.Inst.GetShader("ImposterShader"));
            TextureResource baseMap = new TextureResource();
            baseMap.SetTexture(TextureResourceType.Texture2D, imposterGeometry.BaseMap.GetTexture());
            TextureResource normalMap = new TextureResource();
            normalMap.SetTexture(TextureResourceType.Texture2D, imposterGeometry.NormalMap.GetTexture());
            imposterGeometry.ImposterMaterial.SetTexture(0, baseMap);
            imposterGeometry.ImposterMaterial.SetTexture(1, normalMap);
            imposterGeometry.ImposterMaterial.SetName(name + "_IMPOSTER_MATERIAL");
            imposterGeometry.ImposterMaterial.IsFoliage = true;
        }

        void IResource.LoadFromXML(XmlNode node)
        {
            bool useImposter = false;
            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.Name.ToLower())
                {
                    case "filename":
                        LoadMS3D(attrib.Value);
                        break;
                    case "name":
                        name = attrib.Value;
                        break;
                    case "useimposter":
                        useImposter = bool.Parse(attrib.Value);
                        break;
                }
            }
            
            

            ModifyMesh();

            CreateInstanceData();

            if (useImposter)
            {
                CreateImposter();
            }

            /*
            if(useCollision)
                CreateCollisionMesh();
            */
        }

        void IResource.Destroy()
        {

        }
    }

    public class AnimationNode
    {
        public Vector3 Translation;
        public Vector3 Rotation;
        public Matrix Transform;
        public string Name;
        public List<AnimationNode> children = new List<AnimationNode>();

        public AnimationNode(string name, Vector3 translation, Vector3 rotation)
        {
            Name = name;
            Translation = translation;
            Rotation = rotation;
        }

        public void ApplyTransform(ref Matrix parentTransform)
        {
            Matrix tempTransform = Matrix.CreateRotationX(Rotation.X) * Matrix.CreateRotationY(Rotation.Y) * Matrix.CreateRotationZ(Rotation.Z);
            tempTransform.Translation = Translation;
            Transform = tempTransform * parentTransform;
            for (int i = 0; i < children.Count; i++)
                children[i].ApplyTransform(ref Transform);
        }

        public AnimationNode RecursiveCopy(SortedList<string, AnimationNode> nodeCollection)
        {
            AnimationNode node = new AnimationNode(this.Name, this.Translation, this.Rotation);
            nodeCollection.Add(node.Name, node);
            for (int i = 0; i < this.children.Count; i++)
            {
                AnimationNode child = this.children[i].RecursiveCopy(nodeCollection);
                node.children.Add(child);
            }
            return node;
        }
    }
}
