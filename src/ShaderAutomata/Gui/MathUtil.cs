using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderAutomata.Gui
{
    public static class MathUtil
    {
        public static void Normalize(float[] array, float decay)
        {
            var sum = array.Sum();
            for(int i=0; i<array.Length;i++)
            {
                array[i] = decay * array[i] / sum;
            }
        }
    }
}
