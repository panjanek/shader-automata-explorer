using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KernelAutomata.Models
{
    [StructLayout(LayoutKind.Explicit, Size = 100)]
    public struct ShaderConfig
    {
        public ShaderConfig()
        {
            species_r = new SpeciesConfig();
            species_g = new SpeciesConfig();
            species_b = new SpeciesConfig();
        }

        [FieldOffset(0)]
        public int agentsCount;

        [FieldOffset(4)]
        public int width;

        [FieldOffset(8)]
        public int height;

        [FieldOffset(12)]
        public int time;

        [FieldOffset(16)]
        public SpeciesConfig species_r;

        [FieldOffset(44)]
        public SpeciesConfig species_g;

        [FieldOffset(72)]
        public SpeciesConfig species_b;
    }

    [StructLayout(LayoutKind.Explicit, Size = 28)]
    public struct SpeciesConfig
    {
        public SpeciesConfig()
        {
            velocity = 0.5f;
            turnSpeed = 2f;
            sensorAngle = 0.6f;
            sensorDistance = 4.5f;
            sensorSize = 1;
            attractionTreshold = 0.2f;
            repulsionTreshold = 0.1f;
        }

        [FieldOffset(0)]
        public float velocity;

        [FieldOffset(4)]
        public float turnSpeed;

        [FieldOffset(8)]
        public float sensorAngle;

        [FieldOffset(12)]
        public float sensorDistance;

        [FieldOffset(16)]
        public int sensorSize;

        [FieldOffset(20)]
        public float attractionTreshold;

        [FieldOffset(24)]
        public float repulsionTreshold;
    }
}
