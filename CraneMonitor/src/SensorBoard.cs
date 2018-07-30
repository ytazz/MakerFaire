using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraneMonitor
{
    public class SensorBoard
    {
        public SerialClient com;
        public string comPort;
        
        public int[] pot;  //< potentiometer [0, 1024)
        public int[] sw;   //< switch {0, 1}

        public SensorBoard()
        {
            comPort = "COM5";
            com = new SerialClient();

            pot = new int[4] { 0, 0, 0, 0 };
            sw  = new int[4] { 0, 0, 0, 0 };
        }

        public bool Init()
        {
            com.ReceiveHandler = ReceiveHandler;
            com.Init(comPort);

            return com.Connected();
            //ip.LogHandler = log.WriteLogWarning;
            //ip.ErrorLogHandler = log.WriteLogError;
            //ip.ReceiveHandler = ReceiveHandler;
        }

        public void Close()
        {
            com.Close();
            //ip.Send("SHUTDOWN\n");
            //ip.Close();
        }

        public bool Enable()
        {
            if (!com.Connected())
                return false;

            return true;
            //ip.Init(ipAddress, port);
            //return ip.Connected();
        }

        public bool Disable()
        {
            if (!com.Connected())
                return false;

            return true;
            //ip.Close();
            //return !ip.Connected();
        }

        public void ReceiveHandler(object sender, string message)
        {
            //System.Diagnostics.Debug.WriteLine(message);

            string[] tokens = message.Split(' ');
            if(tokens.Length != 8)
            {
                return;
            }

            try
            {
                pot[0] = int.Parse(tokens[0]);
                pot[1] = int.Parse(tokens[1]);
                pot[2] = int.Parse(tokens[2]);
                pot[3] = int.Parse(tokens[3]);
                sw[0] = int.Parse(tokens[4]);
                sw[1] = int.Parse(tokens[5]);
                sw[2] = int.Parse(tokens[6]);
                sw[3] = int.Parse(tokens[7]);
            }
            catch (FormatException) { }
            
            //System.Diagnostics.Debug.WriteLine(
            //    String.Format("{0} {1} {2} {3} {4} {5} {6} {7}",
            //    pot[0], pot[1], pot[2], pot[3],
            //    sw[0], sw[1], sw[2], sw[3]));

            //if (message.Length < 3) return;
            //string prefix = message.Substring(0, 2);
            //if (prefix == "A,")
            //{
            //    string[] tokens = message.Split(',');
            //    posX = double.Parse(tokens[1]);
            //    posY = double.Parse(tokens[2]);
            //    posZ = double.Parse(tokens[3]);
            //}
        }
    }
}
