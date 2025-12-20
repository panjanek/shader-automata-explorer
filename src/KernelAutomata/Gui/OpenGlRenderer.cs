using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using KernelAutomata.Models;
using OpenTK;
using OpenTK.Compute.OpenCL;
using OpenTK.GLControl;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using Application = System.Windows.Application;
using Panel = System.Windows.Controls.Panel;

namespace KernelAutomata.Gui
{
    public class OpenGlRenderer
    {
        public int FrameCounter => frameCounter;

        private int frameCounter;

        private Panel placeholder;

        private System.Windows.Forms.Integration.WindowsFormsHost host;

        private GLControl glControl;

        public int width;

        public int height;

        private int displayProgram;

        private int updateProgram;

        private int stateTexA;

        private int stateTexB;

        private int fboA;

        private int fboB;

        private int vao;

        private int vbo;

        private readonly int ubo;

        private readonly int agentsBuffer;

        private readonly int maxGroupsX;

        private int computeProgram;

        private int prevStateLocation;

        private int texelSizeLocation;

        private int stateLocation;

        private int kernelLocation;

        private ShaderConfig shaderConfig;

        private float[] blurKernel = new float[25] {
            0.0f, 0.0f, 0.0f, 0.0f, 0.0f,
            0.0f, 1.0f, 1.0f, 1.0f, 0.0f,
            0.1f, 2.0f, 30.0f, 2.0f, 0.0f,
            0.0f, 1.0f, 2.0f, 1.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 0.0f, 0.0f 
        };
  

        private Random rnd = new Random(123);

        public OpenGlRenderer(Panel placeholder)
        {
            this.placeholder = placeholder;
            width = (int)placeholder.ActualWidth / 1;
            height = (int)placeholder.ActualHeight / 1;

            MathUtil.Normalize(blurKernel, 0.96f);
            shaderConfig = new ShaderConfig();
            shaderConfig.agentsCount = 1000000;
            shaderConfig.width = width;
            shaderConfig.height = height;
            shaderConfig.species_r = new SpeciesConfig();
            /*
            shaderConfig.species_g = new SpeciesConfig();
            shaderConfig.species_g.velocity = 0.2f;
            shaderConfig.species_g.sensorDistance = 5.0f;
            shaderConfig.species_g.sensorSize = 2;
            shaderConfig.species_g.turnSpeed = 2.8f;
            shaderConfig.species_g.sensorAngle = 0.4f;

            shaderConfig.species_b = new SpeciesConfig();
            shaderConfig.species_b.velocity = 2.0f;
            shaderConfig.species_b.sensorDistance = 10.0f;
            shaderConfig.species_b.sensorSize = 2;
            shaderConfig.species_b.turnSpeed = 3.2f;
            shaderConfig.species_b.sensorAngle = 0.3f;
            */
            host = new System.Windows.Forms.Integration.WindowsFormsHost();
            host.Visibility = Visibility.Visible;
            host.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            host.VerticalAlignment = VerticalAlignment.Stretch;

            glControl = new GLControl(new GLControlSettings
            {
                API = OpenTK.Windowing.Common.ContextAPI.OpenGL,
                APIVersion = new Version(3, 3), // OpenGL 3.3
                Profile = ContextProfile.Compatability,
                Flags = ContextFlags.Default,
                IsEventDriven = false,
            });

            glControl.Dock = DockStyle.Fill;
            host.Child = glControl;
            placeholder.Children.Add(host);
            glControl.Paint += GlControl_Paint;
            glControl.MakeCurrent();

            Console.WriteLine(GL.GetString(StringName.ShadingLanguageVersion));

            //setup required features
            GL.Enable(EnableCap.ProgramPointSize);
            GL.Disable(EnableCap.Blend);
            GL.BlendEquation(OpenTK.Graphics.OpenGL.BlendEquationMode.FuncAdd);
            GL.Enable(EnableCap.PointSprite);


            string version = GL.GetString(StringName.Version);
            string renderer = GL.GetString(StringName.Renderer);
            string glsl = GL.GetString(StringName.ShadingLanguageVersion);

            Console.WriteLine(version);
            Console.WriteLine(glsl);

            // allocate space for ShaderConfig passed to each compute shader
            ubo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ubo);
            int configSizeInBytes = Marshal.SizeOf<ShaderConfig>();
            GL.BufferData(BufferTarget.ShaderStorageBuffer, configSizeInBytes, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, ubo);
            GL.GetInteger((OpenTK.Graphics.OpenGL.GetIndexedPName)All.MaxComputeWorkGroupCount, 0, out maxGroupsX);
             
            //allocate agents buffer
            GL.GenBuffers(1, out agentsBuffer);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, agentsBuffer);
            int shaderAgentStrideSize = Marshal.SizeOf<Agent>();
            GL.BufferData(BufferTarget.ShaderStorageBuffer, shaderConfig.agentsCount * shaderAgentStrideSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, agentsBuffer);

            //upload initial agents
            var agents = new Agent[shaderConfig.agentsCount];
            for(int i=0; i<agents.Length; i++)
            {
                agents[i].species = rnd.Next(2);
                var angle = rnd.NextDouble()*Math.PI*2;
                var r = 0.08 * Math.Min(width, height)* rnd.NextDouble();
                agents[i].position = new Vector2((float)(width/2 + (agents[i].species*150) + r * Math.Cos(angle)), (float)(height/2 + r*Math.Sin(angle)));
                agents[i].angle = (float)(Math.PI + angle);

                //agents[i].position = new Vector2((float)(width * rnd.NextDouble()), (float)(height * rnd.NextDouble()));
                //agents[i].angle = (float)(rnd.NextDouble() * 2 * Math.PI);

            }
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, agentsBuffer);
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, 0, shaderConfig.agentsCount * shaderAgentStrideSize, agents);

            computeProgram = ShaderUtil.CompileAndLinkComputeShader("agents.comp");
            updateProgram = ShaderUtil.CreateRenderProgram("fullscreen.vert", "update.frag");
            displayProgram = ShaderUtil.CreateRenderProgram("fullscreen.vert", "display.frag");

            stateTexA = TextureUtil.CreateStateTexture(width, height);
            stateTexB = TextureUtil.CreateStateTexture(width, height);

            //upload initial texture - empty
            float[] initialState = new float[width * height * 4]; 
            GL.BindTexture(TextureTarget.Texture2D, stateTexA);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, width, height, PixelFormat.Rgba, PixelType.Float, initialState);

            fboA = TextureUtil.CreateFboForTexture(stateTexA);
            fboB = TextureUtil.CreateFboForTexture(stateTexB);

            // Initialize uniforms
            GL.UseProgram(updateProgram);
            prevStateLocation = GL.GetUniformLocation(updateProgram, "uPrevState");
            texelSizeLocation = GL.GetUniformLocation(updateProgram, "uTexelSize");
            kernelLocation = GL.GetUniformLocation(updateProgram, "uKernel");
            GL.Uniform1(prevStateLocation, 0);
            GL.UseProgram(displayProgram);
            stateLocation = GL.GetUniformLocation(displayProgram, "uState");
            GL.Uniform1(stateLocation, 0);

            if (prevStateLocation == -1)
                throw new Exception("Uniform uPrevState not found.");
            if (texelSizeLocation == -1)
                throw new Exception("Uniform uTexelSize not found.");
            if (stateLocation == -1)
                throw new Exception("Uniform uState not found.");

            (vao, vbo) = PolygonUtil.CreateQuad();
        }

        public void Draw()
        {
            if (Application.Current.MainWindow.WindowState == System.Windows.WindowState.Minimized)
                return;

            //upload config
            shaderConfig.time++;
            int configSizeInBytes = Marshal.SizeOf<ShaderConfig>();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ubo);
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, Marshal.SizeOf<ShaderConfig>(), ref shaderConfig);

            //run compute
            GL.UseProgram(computeProgram);
            GL.BindImageTexture(3, stateTexA, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);
            int dispatchGroupsX = (shaderConfig.agentsCount + ShaderUtil.LocalSizeX - 1) / ShaderUtil.LocalSizeX;
            if (dispatchGroupsX > maxGroupsX)
                dispatchGroupsX = maxGroupsX;
            GL.DispatchCompute(dispatchGroupsX, 1, 1);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);

            //run update
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboB);
            GL.Viewport(0, 0, width, height);
            GL.UseProgram(updateProgram);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, stateTexA);
            GL.Uniform2(texelSizeLocation, 1.0f / width, 1.0f / height);
            GL.Uniform1(kernelLocation, 25, blurKernel);
            PolygonUtil.RenderTriangles(vao);

            // Swap
            (stateTexA, stateTexB) = (stateTexB, stateTexA);
            (fboA, fboB) = (fboB, fboA);
            glControl.Invalidate();
        }


        private void GlControl_Paint(object? sender, PaintEventArgs e)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, glControl.Width, glControl.Height);
            GL.UseProgram(displayProgram);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, stateTexA);
            PolygonUtil.RenderTriangles(vao);
            glControl.SwapBuffers();
            frameCounter++;
        }
    }
}
