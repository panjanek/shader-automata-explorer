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
        public double Test;

        public ShaderConfig shaderConfig;

        public float[] blurKernel = new float[25] {
            0.0f, 0.0f, 0.0f, 0.0f, 0.0f,
            0.0f, 1.0f, 1.0f, 1.0f, 0.0f,
            0.1f, 2.0f, 30.0f, 2.0f, 0.0f,
            0.0f, 1.0f, 2.0f, 1.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 0.0f, 0.0f
        };

        private Random rnd = new Random(123);

        public bool r;

        public bool g;

        public bool b;

        public StartingPosition startR = StartingPosition.Disk;

        public StartingPosition startG = StartingPosition.Disk;

        public StartingPosition startB = StartingPosition.Random;

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

            //shaderConfig.species_b.strayForce = 3.0f;

            r = true;
            startR = StartingPosition.Random;
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
            if (type == StartingPosition.Disk)
            {
                var angle = rnd.NextDouble() * Math.PI * 2;
                var r = 0.28 * Math.Min(shaderConfig.width, shaderConfig.height) * rnd.NextDouble();
                agents[i].position = new Vector2((float)(shaderConfig.width / 2 + (agents[i].species * 250) + r * Math.Cos(angle)), (float)(shaderConfig.height / 2 + r * Math.Sin(angle)));
                agents[i].angle = (float)(Math.PI + angle);
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
        Disk,
        Random
    }
}
