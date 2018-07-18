using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using System.Drawing;

namespace CraneMonitor
{
    public class Param
    {
        public Param() { SetInitValue(); }

        public int UpdateInterval { get; set; }
        public int LightUpdateInterval { get; set; }

        public Color BackColor { get; set; }
        public float WindowWidth { get; set; }
        public float WindowHeight { get; set; }

        public string MotorIp { get; set; }
        public int MotorPort { get; set; }
        public string CameraIp { get; set; }
        public int CameraPort { get; set; }
        public string DisplayIp { get; set; }
        public int DisplayPort { get; set; }
        public string MotorComPort { get; set; }
        public string ControllerComPort { get; set; }
        public string Contact1ComPort { get; set; }
        public string Contact2ComPort { get; set; }
        public string Contact3ComPort { get; set; }

        public bool DirectionX { get; set; }
        public bool DirectionY { get; set; }
        public bool DirectionZ { get; set; }

        public bool EnableLimitUpperX { get; set; }
        public bool EnableLimitUpperY { get; set; }
        public bool EnableLimitUpperZ { get; set; }
        public bool EnableLimitLowerX { get; set; }
        public bool EnableLimitLowerY { get; set; }
        public bool EnableLimitLowerZ { get; set; }

        public float LimitUpperX { get; set; }
        public float LimitUpperY { get; set; }
        public float LimitUpperZ { get; set; }
        public float LimitLowerX { get; set; }
        public float LimitLowerY { get; set; }
        public float LimitLowerZ { get; set; }

        public float MotorGainX { get; set; }
        public float MotorGainY { get; set; }
        public float MotorGainZ { get; set; }

        public float SliderGain { get; set; }

        public int UsbCameraId1 { get; set; }
        public int UsbCameraId2 { get; set; }

        public bool MotorFbMode1 { get; set; }
        public bool MotorFbMode2 { get; set; }
        public bool MotorFbMode3 { get; set; }

        public void SetInitValue()  // default parameters
        {
            UpdateInterval = 50 * 1;
            LightUpdateInterval = 1000;
            SliderGain = 3;

            MotorGainX = 1;
            MotorGainY = 1;
            MotorGainZ = 1;

            // ip address of motor controller (2017)
            MotorIp = "192.168.11.15";
            MotorPort = 8888;

            // ip address of network camera (2017)
            CameraIp = "192.168.11.15";
            CameraPort = 8080;

            // ip address of display program (2017)
            DisplayIp = "localhost";
            DisplayPort = 65002;

            // serial port of controller box
            ControllerComPort = "COM3";

            // serial port of motor driver (2018)
            MotorComPort = "COMX";

            // serial port of obstacle contact switch (2017)
            Contact1ComPort = "COM4";
            Contact2ComPort = "COM5";
            Contact3ComPort = "COM6";

            DirectionX = true;
            DirectionY = true;
            DirectionZ = true;

            EnableLimitLowerX = false;
            EnableLimitLowerY = false;
            EnableLimitLowerZ = false;
            EnableLimitUpperX = false;
            EnableLimitUpperY = false;
            EnableLimitUpperZ = false;

            LimitLowerX = 5;
            LimitLowerY = 10;
            LimitLowerZ = 15;
            LimitUpperX = 90;
            LimitUpperY = 85;
            LimitUpperZ = 80;

            UsbCameraId1 = 0;
            UsbCameraId2 = 1;
        }

        //設定をファイルから読み込む
        public static Param Load()
        {
            Param param = new Param();

            //ユーザ毎のアプリケーションデータディレクトリに保存する
            //String path = String.Format(
            //    "{0}\\{1}",
            //    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            //    "CraneMonitor\\settings.xml");
            String path = "..\\settings.xml"; 

            if (File.Exists(path))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Param));

                using (FileStream stream = new FileStream(path, FileMode.Open))
                {
                    param = serializer.Deserialize(stream) as Param;
                }
            }
            //else
            //{
            //    String folderPath = String.Format(
            //        "{0}\\{1}",
            //        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            //        "CraneMonitor");
            //    System.IO.Directory.CreateDirectory(folderPath);
            //}

            return param;
        }

        //設定をファイルに保存する
        public static void Save(Param param)
        {
            String appPath = String.Format(
                "{0}\\{1}",
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CraneMonitor\\settings.xml");
            XmlSerializer serializer = new XmlSerializer(typeof(Param));

            using (FileStream stream = new FileStream(appPath, FileMode.Create))
            {
                serializer.Serialize(stream, param);
            }
        }
    }
}
