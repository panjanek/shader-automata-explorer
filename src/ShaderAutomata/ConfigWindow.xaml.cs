using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using ShaderAutomata.Gui;
using ShaderAutomata.Models;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Binding = System.Windows.Data.Binding;
using CheckBox = System.Windows.Controls.CheckBox;
using ComboBox = System.Windows.Controls.ComboBox;
using Control = System.Windows.Controls.Control;
using ToolTip = System.Windows.Controls.ToolTip;
using Window = System.Windows.Window;

namespace ShaderAutomata
{
    /// <summary>
    /// Interaction logic for ConfigWindow.xaml
    /// </summary>
    public partial class ConfigWindow : Window
    {
        private Simulation simulation;

        private OpenGlRenderer renderer;

        private bool recreatingEnabled = false;
        public ConfigWindow(Simulation sim, OpenGlRenderer renderer)
        {
            InitializeComponent();
            simulation = sim;
            this.renderer = renderer;
            Closing += (s, e) => { e.Cancel = true; WindowState = WindowState.Minimized; };
            ContentRendered += (s, e) => { UpdateControls(); };
            Loaded += (s, e) => {  };
            foreach(var kernelName in Simulation.AvailableKernels.Keys)
                kernel.Items.Add(new ComboBoxItem() { IsSelected = kernelName.Equals("Default"), Content = kernelName });
            decay.Value = simulation.decay;
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
                        var textBlock = new TextBlock();

                        textBlock.SetBinding(TextBlock.TextProperty, new Binding("Value")
                        {
                            Source = slider,
                            StringFormat = "0.000"
                        });


                        var toolTip = new ToolTip
                        {
                            Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse,
                            StaysOpen = true,
                            Content = textBlock
                        };
 
                        slider.ToolTip = toolTip;
                        slider.PreviewMouseLeftButtonDown += (_, _) => toolTip.IsOpen = true;
                        slider.PreviewMouseLeftButtonUp += (_, _) => toolTip.IsOpen = false;
                    }
                }
            }

            foreach (var checkbox in WpfUtil.FindVisualChildren<CheckBox>(this))
            {
                if (checkbox.Tag is string)
                {
                    checkbox.IsChecked = ReflectionUtil.GetObjectValue<bool>(simulation, checkbox.Tag as string);
                    SetDisabledControls(checkbox.IsChecked ?? false, checkbox.Tag as string);
                }
            }

            foreach (var combo in WpfUtil.FindVisualChildren<ComboBox>(this))
            {
                if (combo.Tag is string)
                {
                    var tag = combo.Tag as string;
                    if (tag.StartsWith("start"))
                    {
                        if (combo.Items.Count == 0)
                        {
                            foreach (StartingPosition value in Enum.GetValues(typeof(StartingPosition)))
                            {
                                combo.Items.Add(value);
                            }
                        }

                        combo.SelectedItem = ReflectionUtil.GetObjectValue<StartingPosition>(simulation, tag);
                    }
                    else if (tag.EndsWith("sensorSize"))
                    {
                        if (combo.Items.Count == 0)
                        {
                            for(int i=0; i<10; i++)
                            {
                                combo.Items.Add(i);
                            }
                        }

     
                        combo.SelectedItem = ReflectionUtil.GetObjectValue<int>(simulation, tag);
                    }
                }
            }

            recreatingEnabled = true;
        }

        private void Reset()
        {
            if (recreatingEnabled)
            {
                simulation.CreateAgents();
                renderer.Recreate();
                renderer.ResetPanning();
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

        private void Value_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox)
            {
                var checkbox = (CheckBox)sender;
                if (checkbox.Tag is string)
                {
                    var checkboxTag = (string)checkbox.Tag;
                    bool isCheched = checkbox.IsChecked ?? false;
                    ReflectionUtil.SetObjectValue<bool>(simulation, checkboxTag, isCheched);
                    SetDisabledControls(isCheched, checkboxTag);
                    Reset();
                }
            }
        }

        private void SetDisabledControls(bool enabled, string color)
        {
            foreach (var control in WpfUtil.FindVisualChildren<Control>(this))
            {
                if (control.Tag is string)
                {
                    var controlTag = control.Tag as string;
                    if (controlTag.StartsWith($"shaderConfig.species_{color}") || controlTag.StartsWith($"start{color.ToUpper()}"))
                    {
                        control.IsEnabled = enabled;
                        control.Opacity = enabled ? 1 : 0.3;
                    }
                }
            }
        }


        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox)
            {
                var combo = (ComboBox)sender;
                if (combo.Tag is string)
                {
                    var tag = combo.Tag as string;
                    if (tag.StartsWith("start"))
                    {
                        ReflectionUtil.SetObjectValue<StartingPosition>(simulation, tag, (StartingPosition)combo.SelectedItem);
                        Reset();
                    }
                    else if (tag.EndsWith("sensorSize"))
                    {
                        ReflectionUtil.SetObjectValue<int>(simulation, tag, (int)combo.SelectedItem);
                    }
                }
            }
        }

        private void agentsCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectionStr = GetComboSelectionAsString(agentsCount);
            if (int.TryParse(selectionStr, out var newCount))
            {
                simulation.shaderConfig.agentsCount = newCount;
                simulation.CreateAgents();
                renderer.Recreate();
                renderer.ResetPanning();
            }
        }

        private void resolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var resolutionStr = GetComboSelectionAsString(resolution);
            if (!string.IsNullOrWhiteSpace(resolutionStr) && resolutionStr.Contains("x"))
            {
                var split = resolutionStr.Split('x');
                if (int.TryParse(split[0], out var newWidth) && int.TryParse(split[1], out var newHeight))
                {
                    simulation.shaderConfig.width = newWidth;
                    simulation.shaderConfig.height = newHeight;
                    simulation.CreateAgents();
                    renderer.Recreate();
                    renderer.ResetPanning();
                }
            }
        }

        private void kernel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var kernelName = GetComboSelectionAsString(kernel);
            if (!string.IsNullOrWhiteSpace(kernelName) && simulation != null)
            {
                simulation.kernelName = kernelName;
                simulation.ApplyKernel();
            }
        }

        private void decay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (simulation != null)
            {
                simulation.decay = (float)e.NewValue;
                simulation.ApplyKernel();
                decayLabel.Text = $"Decay {simulation.decay.ToString("0.000")}";
            }
        }

        private string GetComboSelectionAsString(ComboBox combo)
        {
            if (combo.SelectedItem is ComboBoxItem)
            {
                var item = (ComboBoxItem)combo.SelectedItem;
                return item.Content?.ToString();
            }

            return null;
        }

        private void SetComboStringSelection(ComboBox combo, string value)
        {
            foreach(var item in combo.Items)
            {
                if (item is ComboBoxItem)
                {
                    var comboItem = item as ComboBoxItem;
                    comboItem.IsSelected = comboItem.Content?.ToString() == value;
                }
            }
        }

        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            simulation.CreateAgents();
            renderer.Recreate();
            renderer.ResetPanning();
        }

        private void Default_Click(object sender, RoutedEventArgs e)
        {
            simulation.RestoreDefaults();
            renderer.Recreate();
            renderer.ResetPanning();
            UpdateControls();
            decay.Value = simulation.decay;
            SetComboStringSelection(kernel, simulation.kernelName);
            SetComboStringSelection(resolution, "1920x1080");
            SetComboStringSelection(agentsCount, simulation.shaderConfig.agentsCount.ToString());
        }
    }
}
