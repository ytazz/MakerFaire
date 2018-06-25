using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraneMonitor
{
    public class Light
    {
        public float delta = 0;
        public int divRatio = -1;
        public int timeIndex = 0;

        private bool SetInterval()
        {
            //divRatioLight = (int)Math.Round((double)param.LightUpdateInterval / 2 / param.UpdateInterval);
            if (divRatio == 0)
            {
                delta = 0;
                divRatio = -1;
                return false;
            }
            else
            {
                delta = 2.0f / divRatio;
                timeIndex = 0;
                return true;
            }
        }

        public void Update()
        {
            if (timeIndex == divRatio)
            {
                delta = -delta;
                timeIndex = 0;
            }
            timeIndex++;

            //outW += delta;
        }
    }
}
