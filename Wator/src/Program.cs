// This file is part of Silk.NET.
//
// You may modify and distribute Silk.NET under the terms
// of the MIT license. See the LICENSE file for details.

using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.OpenGL.Extensions.ImGui;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Wator
{
  using System.Reflection;
    class Program
    {
        private static void ExecuteWorld(IWatorWorld world, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                world.ExecuteStep();
            }
        }

        private unsafe static void UpdateTexture(GL gl, Image<Rgba32> image)
        {
            image.ProcessPixelRows(accessor =>
            {
                // Color is pixel-agnostic, but it's implicitly convertible to the Rgba32 pixel type
                for (int y = 0; y < accessor.Height; y++)
                {
                    fixed (void* data = accessor.GetRowSpan(y))
                    {
                        //Loading the actual image.
                        gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, y, (uint)accessor.Width, 1, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                    }
                }
            });
        }

        unsafe static void Main(string[] args)
        {
            // Create a Silk.NET window
            using var window = Window.Create(WindowOptions.Default);

            // these must be initialized after we have a window (in Load)
            IInputContext inputContext = null;
            GL gl = null;
            ImGuiController controller = null;

            uint texture = 0;
            var settings = new Settings();
            settings.DisplayWorld = true;
            settings.Iterations = 1000;

            // Type worldType = Type.GetType("Wator.Original.OriginalWatorWorld");
            var type = Assembly.GetExecutingAssembly().GetTypes().
                  Where(x => x.GetInterfaces().Contains(typeof(IWatorWorld))).
                  First(x => x.Name == settings.Version.ToString()) ?? throw new ArgumentException("Could not determine world type.");

            var instance = Activator.CreateInstance(type, settings) ?? throw new ArgumentException("Could not create world instance.");
            var world = (IWatorWorld)instance;

            var source = new CancellationTokenSource();
            var token = source.Token;

            var signal = new ManualResetEventSlim(false);
            Image<Rgba32> image = new Image<Rgba32>(settings.Width, settings.Height);

            // some simple state
            int iterations = 0;
            int maxIterations = settings.Iterations;
            bool displayWorld = settings.DisplayWorld;

            var taskFactory = new TaskFactory(token);

            // Our loading function
            window.Load += () =>
            {
                gl = window.CreateOpenGL();
                inputContext = window.CreateInput();
                controller = new ImGuiController(gl, window, inputContext);

                texture = gl.GenTexture();
                gl.ActiveTexture(TextureUnit.Texture0);
                gl.BindTexture(TextureTarget.Texture2D, texture);

                gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)settings.Width,
                    (uint)settings.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);

                gl.GenerateMipmap(TextureTarget.Texture2D);
                gl.Enable(EnableCap.Blend);
                gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                var task = taskFactory.StartNew(() =>
                {
                    while(!token.IsCancellationRequested) {
                        world.ExecuteStep();
                        ++iterations;
                        if (iterations == maxIterations) { source.Cancel(); }
                        if (settings.DisplayWorld) {
                            image = world.GenerateImage();
                        }
                        signal.Set();
                    }
                }, token);
            };

            // Handle resizes
            window.FramebufferResize += s =>
            {
                // Adjust the viewport to the new window size
                gl.Viewport(s);
            };

            // The render function
            window.Render += delta =>
            {
                // Make sure ImGui is up-to-date
                controller.Update((float)delta);

                // This is where you'll do any rendering beneath the ImGui context
                // Here, we just have a blank screen.
                gl.ClearColor(System.Drawing.Color.FromArgb(255, (int)(.45f * 255), (int)(.55f * 255), (int)(.60f * 255)));
                gl.Clear((uint)ClearBufferMask.ColorBufferBit);

                // This is where you'll do all of your ImGUi rendering
                // Here, we're just showing the ImGui built-in demo window.
                ImGuiNET.ImGui.ShowMetricsWindow();

                if (iterations < maxIterations && signal.Wait(1000))
                {
                    UpdateTexture(gl, image);
                    signal.Reset();
                }

                ImGuiNET.ImGui.Begin(world.ToString());
                ImGuiNET.ImGui.Text($"Iterations: {iterations}");
                ImGuiNET.ImGui.Image(new IntPtr(texture), new System.Numerics.Vector2(settings.Width, settings.Height));
                ImGuiNET.ImGui.End();

                // Make sure ImGui renders too!
                controller.Render();
            };

            // The closing function
            window.Closing += () =>
            {
                // Dispose our controller first
                controller?.Dispose();

                // Dispose the input context
                inputContext?.Dispose();

                // Unload OpenGL
                gl?.Dispose();
            };

            // Now that everything's defined, let's run this bad boy!
            window.Run();

            window.Dispose();
        }
    }
}
