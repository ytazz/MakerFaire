using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoystickApp
{
    class ImuDataProc
    {
        int TimeIndex = 0;
        int SizeRing;
        float[] RingX;
        float[] RingY;
        float[] RingZ;
        public float AveX, AveY, AveZ;
        public float RangeX, RangeY, RangeZ;

        public ImuDataProc(int n)
        {
            SizeRing = n;
            RingX = new float[n];
            RingY = new float[n];
            RingZ = new float[n];
            for (int i = 0; i < n; i++)
                RingX[i] = RingY[i] = RingZ[i] = 0;
        }

        public void Put(float x, float y, float z)
        {
            RingX[TimeIndex] = x;
            RingY[TimeIndex] = y;
            RingZ[TimeIndex] = z;
            TimeIndex++;
            if (TimeIndex == SizeRing) TimeIndex = 0;
        }

        public void CalcAverage()
        {
            float SumX = 0;
            float SumY = 0;
            float SumZ = 0;
            for (int i = 0; i < SizeRing; i++)
            {
                SumX += RingX[i];
                SumY += RingY[i];
                SumZ += RingZ[i];
            }
            AveX = SumX / SizeRing;
            AveY = SumY / SizeRing;
            AveZ = SumZ / SizeRing;
        }

        public void CalcWidth()
        {
            float MaxX = 0;
            float MaxY = 0;
            float MaxZ = 0;
            for (int i = 0; i < SizeRing; i++)
            {
                float dx = Math.Abs(RingX[i] - AveX);
                float dy = Math.Abs(RingY[i] - AveY);
                float dz = Math.Abs(RingZ[i] - AveZ);
                if (dx > MaxX) MaxX = dx;
                if (dy > MaxY) MaxY = dy;
                if (dz > MaxZ) MaxZ = dz;
            }
            RangeX = MaxX;
            RangeY = MaxY;
            RangeZ = MaxZ;
        }

        public bool WithinRange(float x, float y, float z)
        {
            float dx = Math.Abs(x - AveX);
            float dy = Math.Abs(y - AveY);
            float dz = Math.Abs(z - AveZ);
            return (dx < RangeX) && (dy < RangeY) && (dz < RangeZ);
        }
    }
}
