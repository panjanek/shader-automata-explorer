using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using KernelAutomata.Gui;
using KernelAutomata.Models;

namespace KernelAutomata
{
    public partial class MainWindow : Window
    {
        private OpenGlRenderer renderer;

        private bool uiPending;

        private DateTime lastCheckTime;

        private long lastCheckFrameCount;

        private Simulation sim;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void parent_Loaded(object sender, RoutedEventArgs e)
        {
            sim = new Simulation(1920, 1080);
            renderer = new OpenGlRenderer(placeholder, sim);
            System.Timers.Timer systemTimer = new System.Timers.Timer() { Interval = 10 };
            systemTimer.Elapsed += SystemTimer_Elapsed;
            systemTimer.Start();
            DispatcherTimer infoTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1.0) };
            infoTimer.Tick += InfoTimer_Tick;
            infoTimer.Start();
        }

        private void SystemTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (!uiPending)
            {
                uiPending = true;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        renderer.Draw();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        uiPending = false;
                    }

                    uiPending = false;
                }), DispatcherPriority.Render);
            }
        }

        private void InfoTimer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            var timespan = now - lastCheckTime;
            double frames = renderer.FrameCounter - lastCheckFrameCount;
            if (timespan.TotalSeconds >= 0.0001 && renderer.FrameCounter>500)
            {
                double fps = frames / timespan.TotalSeconds;
                Title = $"ShaderExplorer. " +
                        $"fps:{fps.ToString("0.0")} ";

                lastCheckFrameCount = renderer.FrameCounter;
                lastCheckTime = now;
            }
        }
    }
}