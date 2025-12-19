using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KernelAutomata.Models
{
    [StructLayout(LayoutKind.Explicit, Size = 12)]
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
    }
}
