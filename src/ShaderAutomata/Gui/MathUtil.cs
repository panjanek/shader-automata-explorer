using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShaderAutomata.Gui
{
    public static class MathUtil
    {
        public static float[] Normalize(float[] array, float decay)
        {
            float[] result = new float[array.Length];
            int n = (int)Math.Sqrt(array.Length);
            var sum = array.Sum();
            for(int i=0; i<array.Length;i++)
            {
                result[i] = decay * array[i] / sum;
                int x = i % n;
                int y = i / n;
                if (array[i] != array[y * n + (n - x - 1)] || array[i] != array[(n - y - 1) * n + x] || array[i] != array[(n - y - 1) * n + (n - x - 1)])
                    throw new Exception("Kernel not symmetric!");
            }

            return result;
        }
    }
}
