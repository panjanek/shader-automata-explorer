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

        private ShaderConfig shaderConfig;

        private Random rnd = new Random(123);

        public OpenGlRenderer(Panel placeholder)
        {
            this.placeholder = placeholder;
            width = (int)placeholder.ActualWidth / 1;
            height = (int)placeholder.ActualHeight / 1;

            shaderConfig.agentsCount = 1000000;
            shaderConfig.width = width;
            shaderConfig.height = height;
            shaderConfig.species_r = new SpeciesConfig();

            shaderConfig.species_g = new SpeciesConfig();
            shaderConfig.species_g.velocity = 1.0f;
            shaderConfig.species_g.sensorDistance = 10.0f;
            shaderConfig.species_g.sensorSize = 2;
            shaderConfig.species_g.turnSpeed = 0.8f;
            shaderConfig.species_g.sensorAngle = 0.3f;

            shaderConfig.species_b = new SpeciesConfig();
            shaderConfig.species_b.velocity = 1.0f;
            shaderConfig.species_b.sensorDistance = 10.0f;
            shaderConfig.species_b.sensorSize = 2;
            shaderConfig.species_b.turnSpeed = 0.8f;
            shaderConfig.species_b.sensorAngle = 0.3f;

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
                var angle = rnd.NextDouble()*Math.PI*2;
                var r = 0.48 * Math.Min(width, height)* rnd.NextDouble();
                agents[i].position = new Vector2((float)(width/2 + r * Math.Cos(angle)), (float)(height/2 + r*Math.Sin(angle)));
                agents[i].angle = -(float)(Math.PI + angle);
                agents[i].species = rnd.Next(3);
            }
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, agentsBuffer);
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, 0, shaderConfig.agentsCount * shaderAgentStrideSize, agents);

            computeProgram = ShaderUtil.CompileAndLinkComputeShader("agents.comp");
            updateProgram = ShaderUtil.CreateRenderProgram("fullscreen.vert", "update.frag");
            displayProgram = ShaderUtil.CreateRenderProgram("fullscreen.vert", "display.frag");

            stateTexA = CreateStateTexture();
            stateTexB = CreateStateTexture();

            float[] initialState = new float[width * height * 4];
            /*
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = (y * width + x) * 4;

                    float v = (float)Random.Shared.NextDouble();
                    if (Math.Sqrt((x - width / 3) * (x - width / 3) + (y - height / 2) * (y - height / 2)) > Math.Min(width, height)/2)
                        v = 0;
                    initialState[i + 0] = v;   // R
                    initialState[i + 1] = 0f;  // G
                    initialState[i + 2] = 0f;  // B
                    initialState[i + 3] = 0f;  // A
                }
            }*/
            UploadInitialState(stateTexA, initialState);

            fboA = CreateFboForTexture(stateTexA);
            fboB = CreateFboForTexture(stateTexB);

            // Initialize uniforms
            GL.UseProgram(updateProgram);
            prevStateLocation = GL.GetUniformLocation(updateProgram, "uPrevState");
            texelSizeLocation = GL.GetUniformLocation(updateProgram, "uTexelSize");
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

            InitQuad();



        }

        private void UploadInitialState(int texture, float[] data)
        {
            GL.BindTexture(TextureTarget.Texture2D, texture);

            GL.TexSubImage2D(
                TextureTarget.Texture2D,
                0,
                0, 0,
                width,
                height,
                PixelFormat.Rgba,
                PixelType.Float,
                data
            );
        }

        private void InitQuad()
        {
            float[] quad =
                {
                    -1f, -1f,
                     1f, -1f,
                     1f,  1f,
                    -1f, -1f,
                     1f,  1f,
                    -1f,  1f
                };

            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();

            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, quad.Length * sizeof(float), quad, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);

            GL.BindVertexArray(0);
        }
        private int CreateStateTexture()
        {
            int tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);

            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba32f,
                width,
                height,
                0,
                PixelFormat.Rgba,
                PixelType.Float,
                IntPtr.Zero
            );

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            // IMPORTANT: no mipmaps
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);

            return tex;
        }

        public int CreateFboForTexture(int texture)
        {
            int fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

            GL.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D,
                texture,
                0
            );

            GL.DrawBuffers(1, new[] { DrawBuffersEnum.ColorAttachment0 });

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
                throw new Exception($"FBO incomplete: {status}");

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            return fbo;
        }

        private void GlControl_Paint(object? sender, PaintEventArgs e)
        {

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, glControl.Width, glControl.Height);

            GL.UseProgram(displayProgram);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, stateTexA);

            RenderQuad();


            glControl.SwapBuffers();
            frameCounter++;
        }

        private void RenderQuad()
        {
            GL.BindVertexArray(vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.BindVertexArray(0);
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
            RenderQuad();

            // Swap
            (stateTexA, stateTexB) = (stateTexB, stateTexA);
            (fboA, fboB) = (fboB, fboA);

            glControl.Invalidate();
        }
    }
}
