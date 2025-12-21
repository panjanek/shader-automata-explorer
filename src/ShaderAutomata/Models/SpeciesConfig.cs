using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ShaderAutomata.Models
{
    [StructLayout(LayoutKind.Explicit, Size = 36)]
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
            turnBackTreshold = 0.0f;
            strayForce = 0.1f;
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

        [FieldOffset(28)]
        public float turnBackTreshold;

        [FieldOffset(32)]
        public float strayForce;
    }
}
