using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;

using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace CraneMonitor
{
    public class CameraUsb
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public VideoCapture cap = null;
        public Mat          mat;
        public int          id;
        public BitmapSource image;

        public CameraUsb()
        {
            id = 0;
            image = null;
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

            cap.FrameWidth = 640;
            cap.FrameHeight = 480;

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

            Bitmap bitmap = BitmapConverter.ToBitmap(mat);
            IntPtr hBitmap = bitmap.GetHbitmap();
            image = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(hBitmap);  // to avoid memory leaks

            return true;
        }

        public bool Close()
        {
            if (mat != null)
                mat.Dispose();
            cap.Release();
            cap = null;
            image = null;
            return true;
        }
    }
}
