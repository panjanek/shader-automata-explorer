using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using KernelAutomata.Gui;

namespace KernelAutomata.Models
{
    public class Simulation
    {
        public ShaderConfig shaderConfig;

        public float[] blurKernel = new float[25] {
            0.0f, 0.0f, 0.0f, 0.0f, 0.0f,
            0.0f, 1.0f, 1.0f, 1.0f, 0.0f,
            0.1f, 2.0f, 30.0f, 2.0f, 0.0f,
            0.0f, 1.0f, 2.0f, 1.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 0.0f, 0.0f
        };

        public Simulation(int width, int height)
        {
            MathUtil.Normalize(blurKernel, 0.98f);
            shaderConfig = new ShaderConfig();
            shaderConfig.agentsCount = 1000000;
            shaderConfig.width = width;
            shaderConfig.height = height;

            shaderConfig.species_g.velocity *= 1.3f;
            shaderConfig.species_g.turnSpeed *= 1.5f;
            shaderConfig.species_g.turnBackTreshold = 1.0f;
        }
    }
}
