using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraneMonitor
{
    public class Light
    {
        public double time;
        public double amplitude;
        public double frequency;
        public double output;

        public void Update(double dt)
        {
            time += dt;
            
            // [0, amplitude]の正弦波
            output = amplitude * 0.5 * (Math.Sin(2.0 * Math.PI * frequency * time) + 1.0);
            output = Math.Max(0, output);
        }
    }
}
