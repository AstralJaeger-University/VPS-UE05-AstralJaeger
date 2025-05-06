using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.OpenGL.Extensions.ImGui;

using ImGuiNET;
using System.Threading.Tasks;

using Quandl.API;
using System.Diagnostics;

namespace Quandl.UI
{
    class Program
    {
        private static Tuple<float, float> GetMinMax(float[] values) {
            var vmin = values[0];
            var vmax = values[0];
            foreach(var v in values) {
                vmin = Math.Min(v, vmin);
                vmax = Math.Max(v, vmax);
            }
            return Tuple.Create(vmin, vmax);
        }

        unsafe static void Main(string[] args)
        {
            // Create a Silk.NET window
            using var window = Window.Create(WindowOptions.Default);

            // these must be initialized after we have a window (in Load)
            IInputContext inputContext = null;
            GL gl = null;
            ImGuiController controller = null;

            List<Tuple<string, float[], float[]>> data = null;
            double responseTime = 0;

            EventHandler<QuandlEventArgs> requestCompleted = (sender, args) => {
                data = args.Series;
                responseTime = args.Duration.TotalMilliseconds / 1000.0;
            };

            var clients = new IQuandlClient[] {
                new SequentialClient(),
                new TaskClient(),
                new AsyncClient()
            };

            foreach(var client in clients) {
                client.RequestCompleted += requestCompleted;
            }

            int currentItem = 0;
            var clientNames = new string[] { "Sequential", "Task", "Async" };
            IQuandlClient currentClient = clients[currentItem];

            // Our loading function
            window.Load += () =>
            {
                gl = window.CreateOpenGL();
                inputContext = window.CreateInput();
                controller = new ImGuiController(gl, window, inputContext);
                var io = ImGuiNET.ImGui.GetIO();
                io.ConfigWindowsMoveFromTitleBarOnly = true;
            };

            // Handle resizes
            window.FramebufferResize += s =>
            {
                // Adjust the viewport to the new window size
                gl.Viewport(s);
            };

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
                ImGui.ShowMetricsWindow();

                ImGui.Begin("Quandl.UI");
                if (ImGui.Combo("Client: ", ref currentItem, clientNames, clientNames.Length)) {
                    currentClient = clients[currentItem];
                }

                if (ImGui.Button("Display data"))
                {
                    Task.Run(() => currentClient.Request());
                }
                ImGui.SameLine();
                ImGui.Text($"Response time: {responseTime} seconds");

                if (data != null) {
                    foreach(var (name, values, trend) in data) {
                        var (vmin, vmax) = GetMinMax(values);
                        var (tmin, tmax) = GetMinMax(trend);
                        ImGui.Text(name);
                        ImGui.PlotLines("", ref values[0], values.Length, 0, "Values", vmin, vmax, new System.Numerics.Vector2(600, 200));
                        ImGui.SameLine();
                        ImGui.PlotLines("", ref trend[0], trend.Length, 0, "Trend", tmin, tmax, new System.Numerics.Vector2(600, 200));
                    }
                }
                ImGui.End();

                // Make sure ImGui renders too!
                controller.Render();
            };

            // The closing function
            window.Closing += () =>
            {
                // Clear event handler
                foreach(var client in clients) {
                    client.RequestCompleted -= requestCompleted;
                }

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
