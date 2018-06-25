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
        public int output;

        public void Update(double dt)
        {
            time += dt;
            output = (int)(amplitude * Math.Sin((frequency/(2.0*Math.PI)) * dt));
        }
    }
}
