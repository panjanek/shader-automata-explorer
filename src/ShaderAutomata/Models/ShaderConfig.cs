using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ShaderAutomata.Models
{
    [StructLayout(LayoutKind.Explicit, Size = 136)]
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

        [FieldOffset(56)]
        public SpeciesConfig species_g;

        [FieldOffset(96)]
        public SpeciesConfig species_b;
    }
}
