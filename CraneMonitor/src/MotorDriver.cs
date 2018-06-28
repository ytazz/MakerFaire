using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraneMonitor
{
    /// <summary>
    /// motor driver
    ///  light blinking
    /// </summary>
    public class MotorDriver
    {
        public SerialClient com;
        //public SocketClient ip;

        //public string ipAddress;
        //public int port;
        public string comPort;
        public double velMax;
        public int pwmMax;

        public int[] mode;        //< mode  0:direct pwm  1:position control
        public double[] vel_ref;    //< velocity reference
        public int[] pwm_ref;    //< velocity reference
        public int[] pos_ref;    //< position reference
        public int[] pos;  //< current position (encoder count)
        public int[] pwm;  //< pwm duty ratio [0,255]
        public int[] dir;  //< direction 0,1

        public MotorDriver()
        {
            comPort = "COM5";
            com = new SerialClient();

            velMax = 1.0;
            pwmMax = 255;

            mode    = new int[3] { 0, 0, 0 };
            vel_ref = new double[3] { 0.0, 0.0, 0.0 };
            pwm_ref = new int[3] { 0, 0, 0 };
            pos_ref = new int[3] { 0, 0, 0 };
            pos     = new int[3] { 0, 0, 0 };
            pwm     = new int[3] { 0, 0, 0 };
            dir     = new int[3] { 0, 0, 0 };
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

            com.Send("enable\n");

            return true;
            //ip.Init(ipAddress, port);
            //return ip.Connected();
        }

        public bool Disable()
        {
            if (!com.Connected())
                return false;

            com.Send("disable\n");

            return true;
            //ip.Close();
            //return !ip.Connected();
        }

        public void ReceiveHandler(object sender, string message)
        {
            System.Diagnostics.Debug.WriteLine(message);

            string[] tokens = message.Split(' ');
            if(tokens.Length != 9)
            {
                return;
            }

            pos[0] = int.Parse(tokens[0]);
            pos[1] = int.Parse(tokens[1]);
            pos[2] = int.Parse(tokens[2]);
            pwm[0] = int.Parse(tokens[3]);
            pwm[1] = int.Parse(tokens[4]);
            pwm[2] = int.Parse(tokens[5]);
            dir[0] = int.Parse(tokens[6]);
            dir[1] = int.Parse(tokens[7]);
            dir[2] = int.Parse(tokens[8]);

            //System.Diagnostics.Debug.WriteLine(
            //    String.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8}",
            //    pos[0], pos[1], pos[2], pwm[0], pwm[1], pwm[2], dir[0], dir[1], dir[2]));

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

        public void Update(double dt)
        {
            // update position reference
            for(int i = 0; i < 3; i++)
            {
                pos_ref[i] += (int)(vel_ref[i] * dt);
            }

            string cmd = String.Format("set {0} {1} {2} {3} {4} {5} {6} {7} {8}\n",
                mode[0], mode[1], mode[2],
                pos_ref[0], pos_ref[1], pos_ref[2],
                pwm_ref[0], pwm_ref[1], pwm_ref[2]
                );
            com.Send(cmd);
            //System.Diagnostics.Debug.WriteLine(cmd);

            //string commands = "";
            //if (Math.Abs(outX - prevX) > 1e-10)
            //{
            //    // ジョイスティックを離してセンターポジションに戻ってきたときに初期位置から数パーセントずれていることがある（ずれているとアイドルでモーターが動いてしまうのを防ぐため）
            //    if (Math.Abs(outX) < 0.05)
            //        outX = 0;
            //
            //    commands += String.Format("S1,{0:f3}\n", outX);
            //    prevX = outX;
            //}
            //if (Math.Abs(outY - prevY) > 1e-10)
            //{
            //    if (Math.Abs(outY) < 0.05)
            //        outY = 0;
            //
            //    commands += String.Format("S2,{0:f3}\n", outY);
            //    prevY = outY;
            //}
            //if (Math.Abs(outZ - prevZ) > 1e-10)
            //{
            //    if (Math.Abs(outZ) < 0.05)
            //        outZ = 0;
            //
            //    commands += String.Format("S3,{0:f3}\n", outZ);
            //    prevZ = outZ;
            //}
            //
            //// Wはライトなので不感帯を設けない
            //if (Math.Abs(outW - prevW) > 0)
            //{
            //    commands += String.Format("S4,{0:f3}\n", outW);
            //    prevW = outW;
            //}
            //
            //if (ip != null && commands.Length != 0)
            //{
            //    ip.Send(commands);
            //}
        }
    }
}
