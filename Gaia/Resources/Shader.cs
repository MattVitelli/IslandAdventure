using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gaia.Rendering;

namespace Gaia.Resources
{
    public class Shader : IResource
    {
        string name;
        public string Name { get { return name; } }

        PixelShader ps;
        VertexShader vs;
        bool compiled = false;

        public int VSTarget = 2;
        public int PSTarget = 3;
        string errorMessage = null;

        void IResource.Destroy()
        {
            if (ps != null)
                ps.Dispose();
            ps = null;
            if (vs != null)
                vs.Dispose();
            vs = null;
        }

        void IResource.LoadFromXML(XmlNode node)
        {
            string psFileName = "";
            string vsFileName = "";
            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.Name.ToLower())
                {
                    case "psfilename":
                        psFileName = attrib.Value;
                        break;
                    case "vsfilename":
                        vsFileName = attrib.Value;
                        break;
                    case "pstarget":
                        PSTarget = int.Parse(attrib.Value);
                        break;
                    case "vstarget":
                        VSTarget = int.Parse(attrib.Value);
                        break;
                    case "name":
                        name = attrib.Value;
                        break;
                }
            }
            CompileFromFiles(psFileName, vsFileName);
        }

        public void CompileFromFiles(string psFileName, string vsFileName)
        {
            ShaderProfile psProf = GFX.Device.GraphicsDeviceCapabilities.MaxPixelShaderProfile;
            ShaderProfile vsProf = GFX.Device.GraphicsDeviceCapabilities.MaxVertexShaderProfile;
            CompiledShader psShader = ShaderCompiler.CompileFromFile(psFileName, null, null, CompilerOptions.PackMatrixRowMajor, "main", psProf, TargetPlatform.Windows);
            CompiledShader vsShader = ShaderCompiler.CompileFromFile(vsFileName, null, null, CompilerOptions.PackMatrixRowMajor, "main", vsProf, TargetPlatform.Windows);
            errorMessage = null;
            if (vsShader.ErrorsAndWarnings.Length > 1)
                errorMessage = "Vertex Shader: " + vsShader.ErrorsAndWarnings;
            if (psShader.ErrorsAndWarnings.Length > 1)
            {
                if (errorMessage == null)
                    errorMessage = "Pixel Shader: " + psShader.ErrorsAndWarnings;
                else
                    errorMessage = errorMessage + "\n Pixel Shader: " + psShader.ErrorsAndWarnings;
                Console.WriteLine(errorMessage);
            }
            if (psShader.Success && vsShader.Success)
            {
                ps = new PixelShader(GFX.Device, psShader.GetShaderCode());
                vs = new VertexShader(GFX.Device, vsShader.GetShaderCode());
                compiled = true;
            }
        }

        public void SetupShader()
        {
            if (!compiled)
            {
                if (errorMessage != null)
                    Console.WriteLine(errorMessage);
                return;
            }
            GFX.Device.PixelShader = ps;
            GFX.Device.VertexShader = vs;
        }
    }
}
