using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MjpegProcessor;

namespace CraneMonitor
{
    /// <summary>
    /// HTTP-base network camera
    /// </summary>
    /// 
    class CameraIp
    {
        public string ipAddress;
        public int port;
        public MjpegProcessor.MjpegDecoder mjpeg;
        //private int ImageIndex = 0;
        public BitmapImage frame;

        public CameraIp()
        {
            ipAddress = "localhost";
            port = 0;
        }

        private string GetImageUrl(int n)
        {
            return String.Format("http://{0}:{1}/?action=snapshot&{2}", ipAddress, port, n);
        }

        //private void ImageOneShot()
        //{
        //    BitmapImage imageSource = new BitmapImage();
        //    imageSource.BeginInit();
        //    imageSource.UriSource = new Uri(GetImageUrl(ImageIndex++));
        //    imageSource.EndInit();
        //    image.Source = imageSource;
        //    //imageSource.DownloadCompleted += new EventHandler(LoadNextImage);
        //}

        private bool CameraStart()
        {
            mjpeg = new MjpegDecoder();
            mjpeg.ParseStream(new Uri(String.Format("http://{0}:{1}/?action=stream", ipAddress, port)));
            mjpeg.FrameReady += (object _sender, MjpegProcessor.FrameReadyEventArgs _e) =>
            {
                frame = _e.BitmapImage;
            };
            return true;
        }

        private bool CameraStop()
        {
            mjpeg.StopStream();
            return true;
        }

        public void Close()
        {
            if (mjpeg != null)
            {
                mjpeg.FrameReady += (object _sender, MjpegProcessor.FrameReadyEventArgs _e) => { };
                mjpeg = null;
            }
        }
    }
}
