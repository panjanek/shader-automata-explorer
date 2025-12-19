using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace KernelAutomata.Gui
{
    public static class ShaderUtil
    {
        // 16, 32, 64, 128, 256 - depending on GPU architecture/vendor. Can be set as first commandline parameter
        public static int LocalSizeX = 64;
        public static int CompileAndLinkComputeShader(string compFile)
        {
            // Compile compute shader
            string source = LoadShaderCode(compFile);
            source = source.Replace("{LocalSizeX}", LocalSizeX.ToString());
            int computeShader = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(computeShader, source);
            GL.CompileShader(computeShader);
            GL.GetShader(computeShader, ShaderParameter.CompileStatus, out int status);
            if (status != (int)All.True)
            {
                var log = GL.GetShaderInfoLog(computeShader);
                throw new Exception(log);
            }

            int program = GL.CreateProgram();
            GL.AttachShader(program, computeShader);
            GL.LinkProgram(program);
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out status);
            if (status != (int)All.True)
            {
                throw new Exception(GL.GetProgramInfoLog(program));
            }

            return program;
        }
        public static int CreateRenderProgram(string vertSource, string fragSource)
        {
            int vert = CompileShader(ShaderType.VertexShader, vertSource);
            int frag = CompileShader(ShaderType.FragmentShader, fragSource);

            int program = GL.CreateProgram();
            GL.AttachShader(program, vert);
            GL.AttachShader(program, frag);
            GL.LinkProgram(program);

            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int status);
            if (status == 0)
            {
                var log = GL.GetProgramInfoLog(program);
                throw new Exception(log);
            }

            GL.DeleteShader(vert);
            GL.DeleteShader(frag);

            return program;
        }
        public static int CompileShader(ShaderType type, string resourceName)
        {
            string source = LoadShaderCode(resourceName);
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
            {
                var log = GL.GetShaderInfoLog(shader);
                throw new Exception(log);
            }

            return shader;
        }

        public static string LoadShaderCode(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var a = assembly.GetManifestResourceNames();
            var resourceName = $"KernelAutomata.shaders.{name}";
            using Stream stream = assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Resource not found: {resourceName}");
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
