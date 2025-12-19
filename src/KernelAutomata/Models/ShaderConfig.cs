using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KernelAutomata.Models
{
    [StructLayout(LayoutKind.Explicit, Size = 76)]
    public struct ShaderConfig
    {
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

        [FieldOffset(36)]
        public SpeciesConfig species_g;

        [FieldOffset(56)]
        public SpeciesConfig species_b;
    }

    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public struct SpeciesConfig
    {
        public SpeciesConfig()
        {
            velocity = 0.5f;
            turnSpeed = 0.5f;
            sensorAngle = 0.6f;
            sensorDistance = 4.5f;
            sensorSize = 1;
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
    }
}
