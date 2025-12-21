using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace ShaderAutomata.Models
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct Agent
    {
        [FieldOffset(0)]
        public Vector2 position;

        [FieldOffset(8)]
        public float angle;

        [FieldOffset(12)]
        public int species;
    }
}
