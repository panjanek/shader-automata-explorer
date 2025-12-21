using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using ShaderAutomata.Models;
using OpenTK;
using OpenTK.Compute.OpenCL;
using OpenTK.GLControl;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using Application = System.Windows.Application;
using Panel = System.Windows.Controls.Panel;

namespace ShaderAutomata.Gui
{
    public class OpenGlRenderer
    {
        public const double ZoomingSpeed = 0.0005;
        public int FrameCounter => frameCounter;

        private int frameCounter;

        private Panel placeholder;

        private System.Windows.Forms.Integration.WindowsFormsHost host;

        private GLControl glControl;

        private int displayProgram;

        private int updateProgram;

        private int stateTexA;

        private int stateTexB;

        private int fboA;

        private int fboB;

        private int vao;

        private int vbo;

        private int configBuffer;

        private int agentsBuffer;

        private int maxGroupsX;

        private int computeProgram;

        private int prevStateLocation;

        private int texelSizeLocation;

        private int stateLocation;

        private int kernelLocation;

        private int zoomLocation;

        private int centerLocation;

        private float zoom = 1.0f;

        private Vector2 center = new Vector2(0.5f, 0.5f);

        private Simulation sim;

        private DraggingHandler dragging;

        public OpenGlRenderer(Panel placeholder, Simulation simulation)
        {
            this.placeholder = placeholder;
            sim = simulation;
            host = new System.Windows.Forms.Integration.WindowsFormsHost();
            host.Visibility = Visibility.Visible;
            host.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            host.VerticalAlignment = VerticalAlignment.Stretch;
            placeholder.Children.Add(host);
            placeholder.SizeChanged += Placeholder_SizeChanged;
            CreateGlControl();
        }

        private void Placeholder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (glControl.Width <= 0 || glControl.Height <= 0)
                return;

            if (!glControl.Context.IsCurrent)
                glControl.MakeCurrent();

            //GL.Viewport(0, 0, glControl.Width, glControl.Height);
            GL.Viewport(0, 0, sim.shaderConfig.width, sim.shaderConfig.height);
            glControl.Invalidate();
        }

        public void Recreate()
        {
            DestroyGlControl();
            CreateGlControl();
        }

        public void ResetPanning()
        {
            zoom = 1.0f;
            center = new Vector2(0.5f, 0.5f);
        }

        private void CreateGlControl()
        {
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
            glControl.Paint += GlControl_Paint;
            glControl.MakeCurrent();

            Console.WriteLine(GL.GetString(StringName.ShadingLanguageVersion));

            //setup required features
            GL.Enable(EnableCap.ProgramPointSize);
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.PointSprite);


            string openglVer = GL.GetString(StringName.Version);
            string rendererVer = GL.GetString(StringName.Renderer);
            string glslVer = GL.GetString(StringName.ShadingLanguageVersion);
            string versionInfo = $"ShaderExplorer. GPU info: openglVer:{openglVer}, rendererVer:{rendererVer}, glslVer:{glslVer}";
            Application.Current.MainWindow.Title = versionInfo;

            // allocate space for ShaderConfig passed to each compute shader
            configBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, configBuffer);
            int configSizeInBytes = Marshal.SizeOf<ShaderConfig>();
            GL.BufferData(BufferTarget.ShaderStorageBuffer, configSizeInBytes, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, configBuffer);
            GL.GetInteger((OpenTK.Graphics.OpenGL.GetIndexedPName)All.MaxComputeWorkGroupCount, 0, out maxGroupsX);

            //allocate agents buffer
            GL.GenBuffers(1, out agentsBuffer);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, agentsBuffer);
            int shaderAgentStrideSize = Marshal.SizeOf<Agent>();
            GL.BufferData(BufferTarget.ShaderStorageBuffer, sim.shaderConfig.agentsCount * shaderAgentStrideSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, agentsBuffer);

            //upload initial agents
            var agents = sim.CreateAgents();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, agentsBuffer);
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, 0, sim.shaderConfig.agentsCount * shaderAgentStrideSize, agents);

            computeProgram = ShaderUtil.CompileAndLinkComputeShader("agents.comp");
            updateProgram = ShaderUtil.CreateRenderProgram("fullscreen.vert", "update.frag");
            displayProgram = ShaderUtil.CreateRenderProgram("fullscreen.vert", "display.frag");

            stateTexA = TextureUtil.CreateStateTexture(sim.shaderConfig.width, sim.shaderConfig.height);
            stateTexB = TextureUtil.CreateStateTexture(sim.shaderConfig.width, sim.shaderConfig.height);

            //upload initial texture - empty
            float[] initialState = new float[sim.shaderConfig.width * sim.shaderConfig.height * 4];
            GL.BindTexture(TextureTarget.Texture2D, stateTexA);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, sim.shaderConfig.width, sim.shaderConfig.height, PixelFormat.Rgba, PixelType.Float, initialState);
            GL.BindTexture(TextureTarget.Texture2D, stateTexB);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, sim.shaderConfig.width, sim.shaderConfig.height, PixelFormat.Rgba, PixelType.Float, initialState);

            fboA = TextureUtil.CreateFboForTexture(stateTexA);
            GL.ClearColor(0f, 0f, 0f, 0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            fboB = TextureUtil.CreateFboForTexture(stateTexB);
            GL.ClearColor(0f, 0f, 0f, 0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // Initialize uniforms
            GL.UseProgram(updateProgram);
            prevStateLocation = GL.GetUniformLocation(updateProgram, "uPrevState");
            texelSizeLocation = GL.GetUniformLocation(updateProgram, "uTexelSize");
            kernelLocation = GL.GetUniformLocation(updateProgram, "uKernel");
            GL.Uniform1(prevStateLocation, 0);
            GL.UseProgram(displayProgram);
            stateLocation = GL.GetUniformLocation(displayProgram, "uState");
            zoomLocation = GL.GetUniformLocation(displayProgram, "uZoom");
            centerLocation = GL.GetUniformLocation(displayProgram, "uZoomCenter");
            GL.Uniform1(stateLocation, 0);

            if (prevStateLocation == -1)
                throw new Exception("Uniform uPrevState not found.");
            if (texelSizeLocation == -1)
                throw new Exception("Uniform uTexelSize not found.");
            if (stateLocation == -1)
                throw new Exception("Uniform uState not found.");
            
            if (zoomLocation == -1)
                throw new Exception("uZoom");
            if (centerLocation == -1)
                throw new Exception("uZoomCenter");
            
            (vao, vbo) = PolygonUtil.CreateQuad();

            GL.ClearColor(0f, 0f, 0f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            glControl.SwapBuffers();

            dragging = new DraggingHandler(glControl, (pos, left) => true, (prev, curr) =>
            {
                var delta = prev - curr;
                center.X += delta.X / (sim.shaderConfig.width * zoom);
                center.Y -= delta.Y / (sim.shaderConfig.height * zoom);
            });

            glControl.MouseWheel += GlControl_MouseWheel;
            glControl.MouseDown += (s, e) => { if (e.Button == MouseButtons.Right) ResetPanning(); };
        }

        private void GlControl_MouseWheel(object? sender, MouseEventArgs e)
        {
            var pos = new Vector2(e.X, e.Y);
            float zoomRatio = (float)(1.0 + ZoomingSpeed * e.Delta);
            float newZoom = zoom * zoomRatio;
            float screenToTexX = (float)sim.shaderConfig.width / glControl.ClientSize.Width;
            float screenToTexY = (float)sim.shaderConfig.height / glControl.ClientSize.Height;
            Vector2 mouseUV = new Vector2((pos.X / glControl.ClientSize.Width) / screenToTexX, (1.0f - pos.Y / glControl.ClientSize.Height) / screenToTexY);
            Vector2 mouseTex = (mouseUV - new Vector2(0.5f, 0.5f)) / zoom + center;
            center = mouseTex - (mouseUV - new Vector2(0.5f)) / newZoom;
            zoom = newZoom;
        }

        public void Draw()
        {
            if (Application.Current?.MainWindow == null || Application.Current?.MainWindow?.WindowState == System.Windows.WindowState.Minimized)
                return;

            //upload config
            sim.shaderConfig.time++;
            int configSizeInBytes = Marshal.SizeOf<ShaderConfig>();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, configBuffer);
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, Marshal.SizeOf<ShaderConfig>(), ref sim.shaderConfig);

            //run compute
            GL.UseProgram(computeProgram);
            GL.BindImageTexture(3, stateTexA, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);
            int dispatchGroupsX = (sim.shaderConfig.agentsCount + ShaderUtil.LocalSizeX - 1) / ShaderUtil.LocalSizeX;
            if (dispatchGroupsX > maxGroupsX)
                dispatchGroupsX = maxGroupsX;
            GL.DispatchCompute(dispatchGroupsX, 1, 1);
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);

            //run update
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboB);
            GL.UseProgram(updateProgram);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, stateTexA);
            GL.Uniform2(texelSizeLocation, 1.0f / sim.shaderConfig.width, 1.0f / sim.shaderConfig.height);
            GL.Uniform1(kernelLocation, 25, sim.blurKernel);
            PolygonUtil.RenderTriangles(vao);

            // Swap
            (stateTexA, stateTexB) = (stateTexB, stateTexA);
            (fboA, fboB) = (fboB, fboA);

            glControl.Invalidate();
        }


        private void GlControl_Paint(object? sender, PaintEventArgs e)
        {
            //clear
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, sim.shaderConfig.width, sim.shaderConfig.height);
            GL.ClearColor(0f, 0f, 0f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            //render
            GL.UseProgram(displayProgram);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Uniform1(zoomLocation, zoom);                 
            GL.Uniform2(centerLocation, center.X, center.Y);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, stateTexA);
            PolygonUtil.RenderTriangles(vao);
            glControl.SwapBuffers();
            frameCounter++;
        }

        private void DestroyGlControl()
        {
            if (glControl == null || glControl.IsDisposed)
                return;

            glControl.MakeCurrent();

            // Programs
            if (updateProgram != 0) GL.DeleteProgram(updateProgram);
            if (displayProgram != 0) GL.DeleteProgram(displayProgram);
            if (computeProgram != 0) GL.DeleteProgram(computeProgram);

            // Buffers
            if (vbo != 0) GL.DeleteBuffer(vbo);
            if (vao != 0) GL.DeleteVertexArray(vao);
            if (agentsBuffer != 0) GL.DeleteBuffer(agentsBuffer);
            if (configBuffer != 0) GL.DeleteBuffer(configBuffer);

            // Textures
            if (stateTexA != 0) GL.DeleteTexture(stateTexA);
            if (stateTexB != 0) GL.DeleteTexture(stateTexB);

            // Framebuffers
            if (fboA != 0) GL.DeleteFramebuffer(fboA);
            if (fboB != 0) GL.DeleteFramebuffer(fboB);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            GL.Finish();
            glControl.Dispose();
            host.Child = null;
        }
    }
}
