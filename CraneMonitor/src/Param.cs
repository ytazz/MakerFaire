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

        public string MotorComPort1 { get; set; }
        public string MotorComPort2 { get; set; }
        public string SensorComPort { get; set; }
        public string ControllerComPort { get; set; }

        public bool[] MotorFbMode { get; set; }
        public bool[] MotorDirection { get; set; }
        public bool[] EncoderDirection { get; set; }

        public int[] UsbCameraId { get; set; }

        public double LightAmplitude { get; set; }
        public double LightFrequency { get; set; }

        public void SetInitValue()  // default parameters
        {
            UpdateInterval = 50 * 1;
            
            // serial port of motor driver (2018)
            MotorComPort1 = "COM5";
            MotorComPort2 = "COM6";
            SensorComPort = "COM7";

            // serial port of controller box
            ControllerComPort = "COM3";

            MotorFbMode = new bool[] { true, true, true, true, true, true };
            MotorDirection = new bool[] { true, true, true, true, true, true };
            EncoderDirection = new bool[] { true, true, true, true, true, true };

            UsbCameraId = new int[] { 0, 1 };

            LightAmplitude = 255;
            LightFrequency = 1.0;
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
            //String path = String.Format(
            //    "{0}\\{1}",
            //    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            //    "CraneMonitor\\settings.xml");
            String path = "..\\settings.xml";
            XmlSerializer serializer = new XmlSerializer(typeof(Param));

            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                serializer.Serialize(stream, param);
            }
        }
    }
}
