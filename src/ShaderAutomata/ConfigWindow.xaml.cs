using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ShaderAutomata.Gui;
using ShaderAutomata.Models;
using Binding = System.Windows.Data.Binding;
using CheckBox = System.Windows.Controls.CheckBox;
using ToolTip = System.Windows.Controls.ToolTip;

namespace ShaderAutomata
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindow : Window
    {
        private Simulation simulation;

        private OpenGlRenderer renderer;
        public ConfigWindow(Simulation sim, OpenGlRenderer renderer)
        {
            InitializeComponent();
            simulation = sim;
            Closing += (s, e) => { e.Cancel = true; WindowState = WindowState.Minimized; };
            ContentRendered += (s, e) => UpdateControls();
            this.renderer = renderer;
        }

        public void UpdateControls()
        {
            foreach (var slider in WpfUtil.FindVisualChildren<Slider>(this))
            {
                if (slider.Tag is string)
                {
                    slider.Value = ReflectionUtil.GetObjectValue<float>(simulation, slider.Tag as string);

                    if (slider.ToolTip == null)
                    {
                        var toolTip = new ToolTip();

                        var binding = new Binding("Value")
                        {
                            Source = slider,
                            StringFormat = "F2"
                        };

                        toolTip.SetBinding(ContentControl.ContentProperty, binding);

                        slider.ToolTip = toolTip;
                    }
                }
            }

            foreach (var checkbox in WpfUtil.FindVisualChildren<CheckBox>(this))
            {
                if (checkbox.Tag is string)
                {
                    checkbox.IsChecked = ReflectionUtil.GetObjectValue<bool>(simulation, checkbox.Tag as string);
                }
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider)
            {
                var slider = (Slider)sender;
                if (slider.Tag is string)
                {
                    ReflectionUtil.SetObjectValue<float>(simulation, (string)slider.Tag, (float)slider.Value);
                }
            }
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox)
            {
                var checkbox = (CheckBox)sender;
                if (checkbox.Tag is string)
                {
                    ReflectionUtil.SetObjectValue<bool>(simulation, (string)checkbox.Tag, checkbox.IsChecked ?? false);
                    
                }
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            renderer.Recreate();
        }
    }
}
