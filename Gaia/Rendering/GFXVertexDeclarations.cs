using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gaia.Rendering
{
    #region Custom Vertex Formats
    public struct VertexAnimation
    {
        public Vector4 Position;
        public Vector3 Normal;
        public Vector2 Texcoord;
        public Vector2 IndexCoord;
        public Vector3 Tangent;

        public static int SizeInBytes = (14) * sizeof(float);

        public static VertexElement[] VertexElements = new VertexElement[]
         {
             new VertexElement( 0, 0, VertexElementFormat.Vector4, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Position, 0),
             new VertexElement( 0, sizeof(float)*4, VertexElementFormat.Vector3, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Normal, 0),
             new VertexElement( 0, sizeof(float)*7, VertexElementFormat.Vector2, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.TextureCoordinate, 0),
             new VertexElement( 0, sizeof(float)*9, VertexElementFormat.Vector2, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.TextureCoordinate, 1),
             new VertexElement( 0, sizeof(float)*11, VertexElementFormat.Vector3, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Tangent, 0),
         };
        public VertexAnimation(Vector3 position, Vector3 normal, Vector2 texCoord, Vector2 indexCoord, Vector3 tangent)
        {
            Position = new Vector4(position, 1.0f);
            Normal = normal;
            Texcoord = texCoord;
            IndexCoord = indexCoord;
            Tangent = tangent;
        }
    }

    public struct VertexParticles
    {
        public Vector2 Index;

        public static int SizeInBytes = (2) * sizeof(float);

        public static VertexElement[] VertexElements = new VertexElement[]
         {
             new VertexElement( 0, 0, VertexElementFormat.Vector2, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Position, 0),
         };
        public VertexParticles(Vector2 index)
        {
            Index = index;
        }
    }

    public struct VertexPosition
    {
        public Vector4 Position;

        public static int SizeInBytes = (4) * sizeof(float);

        public static VertexElement[] VertexElements = new VertexElement[]
        {
             new VertexElement( 0, 0, VertexElementFormat.Vector4, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Position, 0),
        };
        public VertexPosition(Vector3 position)
        {
            Position = new Vector4(position, 1.0f);
        }
    }

    public struct VertexPTI
    {
        public Vector3 Position;
        public Vector2 TexCoord;
        public float Index;

        public static int SizeInBytes = 6 * sizeof(float);

        public static VertexElement[] VertexElements = new VertexElement[]
         {
             new VertexElement( 0, 0, VertexElementFormat.Vector3, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Position, 0),
             new VertexElement( 0, sizeof(float)*3, VertexElementFormat.Vector2, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.TextureCoordinate, 0),
             new VertexElement( 0, sizeof(float)*5, VertexElementFormat.Single, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.TextureCoordinate, 1),
         };
        public VertexPTI(Vector3 position, Vector2 texcoord, float index)
        {
            Position = position;
            TexCoord = texcoord;
            Index = index;
        }
    }

    public struct VertexPNTan
    {
        public Vector4 Position;
        public Vector3 Normal;
        public Vector3 Tangent;

        public static int SizeInBytes = (10) * sizeof(float);

        public static VertexElement[] VertexElements = new VertexElement[]
         {
             new VertexElement( 0, 0, VertexElementFormat.Vector4, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Position, 0),
             new VertexElement( 0, sizeof(float)*4, VertexElementFormat.Vector3, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Normal, 0),
             new VertexElement( 0, sizeof(float)*9, VertexElementFormat.Vector3, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Tangent, 0),
         };
        public VertexPNTan(Vector3 position, Vector3 normal, Vector3 tangent)
        {
            Position = new Vector4(position, 1.0f);
            Normal = normal;
            Tangent = tangent;
        }
    }

    public struct VertexPNTT
    {
        public Vector4 Position;
        public Vector3 Normal;
        public Vector2 Texcoord;
        public Vector3 Tangent;

        public static int SizeInBytes = (12) * sizeof(float);

        public static VertexElement[] VertexElements = new VertexElement[]
         {
             new VertexElement( 0, 0, VertexElementFormat.Vector4, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Position, 0),
             new VertexElement( 0, sizeof(float)*4, VertexElementFormat.Vector3, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Normal, 0),
             new VertexElement( 0, sizeof(float)*7, VertexElementFormat.Vector2, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.TextureCoordinate, 0),
             new VertexElement( 0, sizeof(float)*9, VertexElementFormat.Vector3, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Tangent, 0),
         };
        public VertexPNTT(Vector3 position, Vector3 normal, Vector2 texCoord, Vector3 tangent)
        {
            Position = new Vector4(position, 1.0f);
            Normal = normal;
            Texcoord = texCoord;
            Tangent = tangent;
        }
    }

    public struct VertexPNTTI
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Texcoord;
        public Vector3 Tangent;
        public float Index;

        public static int SizeInBytes = (12) * sizeof(float);

        public static VertexElement[] VertexElements = new VertexElement[]
         {
             new VertexElement( 0, 0, VertexElementFormat.Vector3, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Position, 0),
             new VertexElement( 0, sizeof(float)*3, VertexElementFormat.Vector3, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Normal, 0),
             new VertexElement( 0, sizeof(float)*6, VertexElementFormat.Vector2, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.TextureCoordinate, 0),
             new VertexElement( 0, sizeof(float)*8, VertexElementFormat.Vector3, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Tangent, 0),
             new VertexElement( 0, sizeof(float)*11, VertexElementFormat.Single,
                                      VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 1),
         };
        public VertexPNTTI(Vector3 position, Vector3 normal, Vector2 texCoord, Vector3 tangent, float index)
        {
            Position = position;
            Normal = normal;
            Texcoord = texCoord;
            Tangent = tangent;
            Index = index;
        }
    }

    public struct VertexPNTTB
    {
        public Vector4 Position;
        public Vector3 Normal;
        public Vector2 Texcoord;
        public Vector4 Bone;
        public Vector4 BoneWeights;
        public Vector3 Tangent;


        public static int SizeInBytes = (20) * sizeof(float);

        public static VertexElement[] VertexElements = new VertexElement[]
         {
             new VertexElement( 0, 0, VertexElementFormat.Vector4, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Position, 0),
             new VertexElement( 0, sizeof(float)*4, VertexElementFormat.Vector3, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Normal, 0),
             new VertexElement( 0, sizeof(float)*7, VertexElementFormat.Vector2, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.TextureCoordinate, 0),
             new VertexElement( 0, sizeof(float)*9, VertexElementFormat.Vector4, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.TextureCoordinate, 1),
             new VertexElement( 0, sizeof(float)*13, VertexElementFormat.Vector4, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.TextureCoordinate, 2),
             new VertexElement( 0, sizeof(float)*17, VertexElementFormat.Vector3, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Tangent, 0),
         };
        public VertexPNTTB(Vector3 position, Vector3 normal, Vector2 texCoord, Vector4 bones, Vector4 boneWeights, Vector3 tangent)
        {
            Position = new Vector4(position, 1.0f);
            Normal = normal;
            Texcoord = texCoord;
            Bone = bones;
            BoneWeights = boneWeights;
            Tangent = tangent;

        }
    }

    public struct VertexPN
    {
        public Vector3 Position;
        public Vector3 Normal;

        public static int SizeInBytes = (6) * sizeof(float);

        public static VertexElement[] VertexElements = new VertexElement[]
         {
             new VertexElement( 0, 0, VertexElementFormat.Vector3, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Position, 0),
             new VertexElement( 0, sizeof(float)*3, VertexElementFormat.Vector3, 
                                      VertexElementMethod.Default, 
                                      VertexElementUsage.Normal, 0),
         };
        public VertexPN(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;
        }
    }
    #endregion

    public static class GFXVertexDeclarations
    {
        public static VertexDeclaration PDec;
        public static VertexDeclaration PTDec;
        public static VertexDeclaration PTIDec;
        public static VertexDeclaration PNTTDec;
        public static VertexDeclaration PNTTIDec;
        public static VertexDeclaration PNTanDec;
        public static VertexDeclaration PNTTBDec;
        public static VertexDeclaration AnimDec;
        public static VertexDeclaration PNDec;
        public static VertexDeclaration ParticlesDec;

        public static void Initialize()
        {
            PDec = new VertexDeclaration(GFX.Device, VertexPosition.VertexElements);
            PNDec = new VertexDeclaration(GFX.Device, VertexPN.VertexElements);
            PNTTDec = new VertexDeclaration(GFX.Device, VertexPNTT.VertexElements);
            PNTTIDec = new VertexDeclaration(GFX.Device, VertexPNTTI.VertexElements);
            PNTanDec = new VertexDeclaration(GFX.Device, VertexPNTan.VertexElements);
            PNTTBDec = new VertexDeclaration(GFX.Device, VertexPNTTB.VertexElements);
            PTDec = new VertexDeclaration(GFX.Device, VertexPositionTexture.VertexElements);
            PTIDec = new VertexDeclaration(GFX.Device, VertexPTI.VertexElements);
            AnimDec = new VertexDeclaration(GFX.Device, VertexAnimation.VertexElements);
            ParticlesDec = new VertexDeclaration(GFX.Device, VertexParticles.VertexElements);
        }
    }
}
