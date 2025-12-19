using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KernelAutomata.Models
{
    [StructLayout(LayoutKind.Explicit, Size = 12)]
    public unsafe struct ShaderConfig
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

        [FieldOffset(48)]
        public SpeciesConfig species_g;

        [FieldOffset(80)]
        public SpeciesConfig species_b;
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
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

        // Padding required by std140
        [FieldOffset(20)] private int _pad0;
        [FieldOffset(24)] private int _pad1;
        [FieldOffset(28)] private int _pad2;
    }
}
