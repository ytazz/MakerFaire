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
        public VideoCapture cap = null;
        public Mat          mat;
        public Bitmap       bitmap;
        public int          id;

        public CameraUsb()
        {
            id = 0;
        }

        public bool Init()
        {
            cap = new VideoCapture(id);
            //cap.Open(0);
            if (!cap.IsOpened())
            {
                cap = null;
                return false;
            }

            //cap.FrameWidth = 640;
            //cap.FrameHeight = 480;

            mat = new Mat();

            return true;
        }

        public bool Update()
        {
            if (cap == null) return false;

            cap.Read(mat);
            if (mat.Empty())
            {
                return false;
            }

            bitmap = BitmapConverter.ToBitmap(mat);

            return true;
        }

        public bool Close()
        {
            cap.Release();
            cap = null;
            return true;
        }
    }
}
