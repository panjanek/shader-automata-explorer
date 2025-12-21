using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using ShaderAutomata.Gui;
using OpenTK.Mathematics;

namespace ShaderAutomata.Models
{
    public class Simulation
    {
        public float decay = 0.98f;

        public string kernelName = "Default";

        public ShaderConfig shaderConfig;

        public float[] kernel;

        public static Dictionary<string, float[]> AvailableKernels { get; set; } = new()
        {
            ["Default"] = [
                  0.0f, 0.0f, 0.0f, 0.0f, 0.0f,
                  0.0f, 1.0f, 2.0f, 1.0f, 0.0f,
                  0.1f, 2.0f, 30.0f, 2.0f, 0.1f,
                  0.0f, 1.0f, 2.0f, 1.0f, 0.0f,
                  0.0f, 0.0f, 0.0f, 0.0f, 0.0f
            ],
            ["Mild"] = [
                  0,  0,  1,  0,  0,
                  0,  2,  4,  2,  0,
                  1,  4, 20,  4,  1,
                  0,  2,  4,  2,  0,
                  0,  0,  1,  0,  0
                ],
            ["Moderate"] = [
                  1,  2,  3,  2,  1,
                  2,  4,  6,  4,  2,
                  3,  6, 10,  6,  3,
                  2,  4,  6,  4,  2,
                  1,  2,  3,  2,  1
                ],
            ["Strong"] = [
                  1,  2,  4,  2,  1,
                  2,  4,  8,  4,  2,
                  4,  8,  4,  8,  4,
                  2,  4,  8,  4,  2,
                  1,  2,  4,  2,  1
                ],
            ["Edge-biased"] = [
                  0,  1,  2,  1,  0,
                  1,  2,  4,  2,  1,
                  2,  4,  2,  4,  2,
                  1,  2,  4,  2,  1,
                  0,  1,  2,  1,  0
                ],
            ["Uniform"] = [
                  1,  1,  1,  1,  1,
                  1,  1,  1,  1,  1,
                  1,  1,  1,  1,  1,
                  1,  1,  1,  1,  1,
                  1,  1,  1,  1,  1
                ],
            ["Anisotropic"] = [
                  0,  0,  1,  0,  0,
                  0,  1,  2,  1,  0,
                  0,  2,  6,  2,  0,
                  0,  1,  2,  1,  0,
                  0,  0,  1,  0,  0
                ]
        };    


        private Random rnd = new Random(123);

        public bool r;

        public bool g;

        public bool b;

        public StartingPosition startR = StartingPosition.DiskInward;

        public StartingPosition startG = StartingPosition.DiskInward;

        public StartingPosition startB = StartingPosition.Random;

        public Simulation()
        {
            RestoreDefaults();
        }

        public void RestoreDefaults()
        {
            kernelName = "Default";
            decay = 0.98f;
            ApplyKernel();
            shaderConfig = new ShaderConfig();
            shaderConfig.agentsCount = 1000000;
            shaderConfig.width = 1920;
            shaderConfig.height = 1080;

            shaderConfig.species_g.velocity *= 1.3f;
            shaderConfig.species_g.turnSpeed *= 1.5f;
            shaderConfig.species_g.turnBackTreshold = 1.0f;

            r = true;
            g = false;
            b = false;
            startR = StartingPosition.DiskInward;
            startG = StartingPosition.DiskOutward;
            startB = StartingPosition.Random;
            CreateAgents();
        }

        public Agent[] CreateAgents()
        {
            var agents = new Agent[shaderConfig.agentsCount];
            var active = new bool[] { r, g, b };
            for (int i = 0; i < agents.Length; i++)
            {
                agents[i].species = SelectRandomly(active);
                switch (agents[i].species)
                {
                    case 0:
                        SetStartingPosition(agents, i, startR);
                        break;
                    case 1:
                        SetStartingPosition(agents, i, startG);
                        break;
                    case 2:
                        SetStartingPosition(agents, i, startB);
                        break;
                }

            }

            return agents;
        }

        public void ApplyKernel()
        {
            kernel = MathUtil.Normalize(AvailableKernels[kernelName], decay);
        }

        private int SelectRandomly(bool[] active)
        {
            if (!active.Any(a => a))
                return 0;

            int idx = rnd.Next(active.Length);
            while (!active[idx])
                idx = rnd.Next(active.Length);
            return idx;
        }

        private void SetStartingPosition(Agent[] agents, int i, StartingPosition type)
        {
            if (type == StartingPosition.DiskInward || type == StartingPosition.DiskOutward)
            {
                var angle = rnd.NextDouble() * Math.PI * 2;
                var r = 0.4 * Math.Min(shaderConfig.width, shaderConfig.height) * rnd.NextDouble();
                var dx = agents[i].species == 0 ? 0 : (agents[i].species == 1 ? -300 : 300);
                agents[i].position = new Vector2((float)(shaderConfig.width / 2 + dx + r * Math.Cos(angle)), (float)(shaderConfig.height / 2 + r * Math.Sin(angle)));
                agents[i].angle = type == StartingPosition.DiskInward ? (float)(Math.PI + angle) : (float)(2 * Math.PI + angle);
            }
            else if (type == StartingPosition.CircleInward || type == StartingPosition.CircleOutward)
            {
                var angle = rnd.NextDouble() * Math.PI * 2;
                var r = 0.4 * Math.Min(shaderConfig.width, shaderConfig.height);
                var dx = agents[i].species == 0 ? 0 : (agents[i].species == 1 ? -300 : 300);
                agents[i].position = new Vector2((float)(shaderConfig.width / 2 + dx + r * Math.Cos(angle)), (float)(shaderConfig.height / 2 + r * Math.Sin(angle)));
                agents[i].angle = type == StartingPosition.CircleInward ? (float)(Math.PI + angle) : (float)(2 * Math.PI + angle);
            }
            else if (type == StartingPosition.Ring)
            {
                var angle = rnd.NextDouble() * Math.PI * 2;
                var r = (0.15 + 0.1 * agents[i].species + rnd.NextDouble() * 0.075) * Math.Min(shaderConfig.width, shaderConfig.height);
                agents[i].position = new Vector2((float)(shaderConfig.width / 2 + r * Math.Cos(angle)), (float)(shaderConfig.height / 2 + r * Math.Sin(angle)));
                agents[i].angle = (float)(rnd.NextDouble() * 2 * Math.PI);
            }
            else if (type == StartingPosition.Random)
            {
                agents[i].position = new Vector2((float)(shaderConfig.width * rnd.NextDouble()), (float)(shaderConfig.height * rnd.NextDouble()));
                agents[i].angle = (float)(rnd.NextDouble() * 2 * Math.PI);
            }
        }
    }

    public enum StartingPosition
    {
        DiskInward,
        DiskOutward,
        CircleInward,
        CircleOutward,
        Ring,
        Random
    }
}
