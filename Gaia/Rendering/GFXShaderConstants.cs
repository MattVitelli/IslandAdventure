using System;
using System.Collections.Generic;
using System.IO;

namespace Gaia.Rendering
{
    public static class GFXShaderConstants
    {
        public static int VC_MODELVIEW = 0;
        public static int VC_USERDEF0 = 4;
        public static int VC_WORLD = 16;
        public static int VC_TEXGEN = 8;
        public static int VC_EYEPOS = 12;
        public static int VC_INVTEXRES = 13;
        public static int VC_TIME = 14;

        public static int PC_AMBIENT = 0;
        public static int PC_DIFFUSE = 1;
        public static int PC_SPECULAR = 2;
        public static int PC_SPECPOWER = 3;

        public static int PC_EYEPOS = 8;
        public static int PC_TIME = 9;
        public static int PC_LIGHTPOS = 10;
        public static int PC_LIGHTCOLOR = 11;
        public static int PC_LIGHTPARAMS = 12;


        public static int GRASSFALLOFF = 200;

        public static int ALPHACUTOFF = 158;

        public static int NUM_SPLITS = 6;
        
        public static int NUM_INSTANCES = 60;

        public static int MAX_PARTICLES = 256*256; //That's a LOT of particles. Interestingly enough, this only takes up about a kilobyte

        public static int MAX_PARTICLECOLORS = 16;

        public static int MAX_PARTICLEFORCES = 4;

        public static int PC_PARTICLECOLORS = 2;

        public static int PC_PARTICLETIMES = PC_PARTICLECOLORS + MAX_PARTICLECOLORS;

        public static int PC_PARTICLEVARS = PC_PARTICLETIMES + MAX_PARTICLECOLORS;

        public static int PC_LIGHTMODELVIEW = 14;

        public static int PC_LIGHTCLIPPLANE = PC_LIGHTMODELVIEW + 4 * NUM_SPLITS;

        public static int PC_LIGHTCLIPPOS = PC_LIGHTCLIPPLANE + NUM_SPLITS;

        public static int PC_INVSHADOWRES = PC_LIGHTCLIPPOS + NUM_SPLITS;

        public static int PC_EYEPOSPHYSICS = 10 + MAX_PARTICLEFORCES;

        public static int PC_VIEWMATRIXPHYSICS = PC_EYEPOSPHYSICS + 1;

        static void WriteCommand(StreamWriter writer, string commandName, int index)
        {
            writer.Write("#define ");
            writer.Write(commandName);
            writer.Write(" C");
            writer.Write(index);
            writer.Write("\n");
        }

        static void WriteDefine(StreamWriter writer, string commandName, int index)
        {
            writer.Write("#define ");
            writer.Write(commandName);
            writer.Write(" ");
            writer.Write(index);
            writer.Write("\n");
        }

        static void WriteDefine(StreamWriter writer, string commandName, float value)
        {
            writer.Write("#define ");
            writer.Write(commandName);
            writer.Write(" ");
            writer.Write(value);
            writer.Write("\n");
        }

        public static void AuthorShaderConstantFile()
        {
            using (FileStream fs = new FileStream("Shaders/ShaderConst.h", FileMode.Create))
            {
                using (StreamWriter wr = new StreamWriter(fs))
                {
                    WriteDefine(wr, "MAX_PARTICLES", MAX_PARTICLES);
                    WriteDefine(wr, "MAX_PARTICLECOLORS", MAX_PARTICLECOLORS);
                    WriteDefine(wr, "MAX_PARTICLEFORCES", MAX_PARTICLEFORCES);
                    WriteDefine(wr, "NUM_INSTANCES", NUM_INSTANCES); //Instancing
                    WriteDefine(wr, "NUM_SPLITS", NUM_SPLITS); //Cascade shadow maps
                    WriteDefine(wr, "GRASSFALLOFF", GRASSFALLOFF);
                    WriteDefine(wr, "ALPHACUTOFF", (float)ALPHACUTOFF / 255.0f);

                    WriteCommand(wr, "VC_MODELVIEW", VC_MODELVIEW);
                    WriteCommand(wr, "VC_WORLD", VC_WORLD);
                    WriteCommand(wr, "VC_EYEPOS", VC_EYEPOS);
                    WriteCommand(wr, "VC_INVTEXRES", VC_INVTEXRES);
                    WriteCommand(wr, "VC_TEXGEN", VC_TEXGEN);
                    WriteCommand(wr, "VC_TIME", VC_TIME);
                    WriteCommand(wr, "VC_USERDEF0", VC_USERDEF0);

                    WriteCommand(wr, "PC_AMBIENT", PC_AMBIENT);
                    WriteCommand(wr, "PC_DIFFUSE", PC_DIFFUSE);
                    WriteCommand(wr, "PC_SPECULAR", PC_SPECULAR);
                    WriteCommand(wr, "PC_SPECPOWER", PC_SPECPOWER);
                    WriteCommand(wr, "PC_EYEPOS", PC_EYEPOS);
                    WriteCommand(wr, "PC_TIME", PC_TIME);
                    WriteCommand(wr, "PC_LIGHTPOS", PC_LIGHTPOS);
                    WriteCommand(wr, "PC_LIGHTCOLOR", PC_LIGHTCOLOR);
                    WriteCommand(wr, "PC_LIGHTPARAMS", PC_LIGHTPARAMS);
                    WriteCommand(wr, "PC_LIGHTMODELVIEW", PC_LIGHTMODELVIEW);
                    WriteCommand(wr, "PC_LIGHTCLIPPLANE", PC_LIGHTCLIPPLANE);
                    WriteCommand(wr, "PC_LIGHTCLIPPOS", PC_LIGHTCLIPPOS);
                    WriteCommand(wr, "PC_INVSHADOWRES", PC_INVSHADOWRES);
                    WriteCommand(wr, "PC_PARTICLECOLORS", PC_PARTICLECOLORS);
                    WriteCommand(wr, "PC_PARTICLETIMES", PC_PARTICLETIMES);
                    WriteCommand(wr, "PC_PARTICLEVARS", PC_PARTICLEVARS);
                    WriteCommand(wr, "PC_EYEPOSPHYSICS", PC_EYEPOSPHYSICS);
                    WriteCommand(wr, "PC_VIEWMATRIXPHYSICS", PC_VIEWMATRIXPHYSICS);
                }
            }

        }
    }
}
