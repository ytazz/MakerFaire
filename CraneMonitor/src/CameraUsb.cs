using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;

using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace CraneMonitor
{
    public class CameraUsb
    {
        public VideoCapture cap;
        public Mat mat;
        public Bitmap bitmap;

        public bool Init()
        {
            cap = new VideoCapture(0);
            //cap.Open(0);
            //if (!cap.IsOpened())
            //{
            //    return false;
            //}

            //cap.FrameWidth = 640;
            //cap.FrameHeight = 480;

            mat = new Mat();

            return true;
        }

        public bool Update()
        {
            cap.Read(mat);
            if (mat.Empty())
            {
                return false;
            }

            bitmap = BitmapConverter.ToBitmap(mat);

            return true;
        }
    }
}
