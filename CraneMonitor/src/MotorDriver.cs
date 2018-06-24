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
        public SocketClient ip;

        public string ipAddress;
        public int port;
        public string comPort;

        public double outX  = 0;
        public double outY  = 0;
        public double outZ  = 0;
        public double outW  = 0;   // Wは点滅ライト
        public double prevX = 10;
        public double prevY = 10;
        public double prevZ = 10;
        public double prevW = 10;
        public bool   polarityX = false;
        public bool   polarityY = false;
        public bool   polarityZ = false;
        public bool   polarityW = false;
        public bool updated = true;
        public bool stop = false;

        public double posX = 0.5; // Todo : replace to zero
        public double posY = 0.6;
        public double posZ = 0.7;

        public float lightDelta = 0;
        public int   divRatioLight = -1;
        public int   timeIndexLight = 0;

        //public const int DivRatioAdc = -1; // 現在ADCは使ってない
        //public int TimeIndexAdc = 0;

        public bool Init()
        {
            //ip.LogHandler = log.WriteLogWarning;
            //ip.ErrorLogHandler = log.WriteLogError;
            ip.ReceiveHandler = ReceiveHandler;
            return true;
        }

        public void Close()
        {
            if (com != null)
            {
                com.Close();
            }
            if (ip != null)
            {
                ip.Send("SHUTDOWN\n");
                ip.Close();
            }
        }

        public bool Start()
        {
            if (ip != null)
            {
                ip.Init(ipAddress, port);
                return ip.Connected();
            }
            return false;
        }

        public bool Stop()
        {
            if (ip != null)
            {
                ip.Close();
                return !ip.Connected();
            }
            return false;
        }

        public void ReceiveHandler(object sender, string message)
        {
            if (message.Length < 3) return;
            string prefix = message.Substring(0, 2);
            if (prefix == "A,")
            {
                string[] tokens = message.Split(',');
                posX = double.Parse(tokens[1]);
                posY = double.Parse(tokens[2]);
                posZ = double.Parse(tokens[3]);
            }
        }

        private bool SetLightInterval()
        {
            //divRatioLight = (int)Math.Round((double)param.LightUpdateInterval / 2 / param.UpdateInterval);
            if (divRatioLight == 0)
            {
                lightDelta    = 0;
                divRatioLight = -1;
                return false;
            }
            else
            {
                lightDelta = 2.0f / divRatioLight;
                timeIndexLight = 0;
                return true;
            }
        }

        public void UpdateLight()
        {
            if (timeIndexLight == divRatioLight)
            {
                lightDelta = -lightDelta;
                timeIndexLight = 0;
            }
            timeIndexLight++;

            outW += lightDelta;
            updated = true;
        }

        public void Halt()
        {
            outX = 0;
            outY = 0;
            outZ = 0;
            outW = 0;
        }

        public void Update()
        {
            // clip values
            if (outX < -1) outX = -1;
            if (outY < -1) outY = -1;
            if (outZ < -1) outZ = -1;
            if (outW <  0) outW =  0;   // Wはライトなので極性の反転なし
            if (outX >  1) outX =  1;
            if (outY >  1) outY =  1;
            if (outZ >  1) outZ =  1;
            if (outW >  1) outW =  1;

            if (polarityX) outX = -outX;
            if (polarityY) outY = -outY;
            if (polarityZ) outZ = -outZ;

            string commands = "";
            if (Math.Abs(outX - prevX) > 1e-10)
            {
                // ジョイスティックを離してセンターポジションに戻ってきたときに初期位置から数パーセントずれていることがある（ずれているとアイドルでモーターが動いてしまうのを防ぐため）
                if (Math.Abs(outX) < 0.05)
                    outX = 0;

                commands += String.Format("S1,{0:f3}\n", outX);
                prevX = outX;
            }
            if (Math.Abs(outY - prevY) > 1e-10)
            {
                if (Math.Abs(outY) < 0.05)
                    outY = 0;

                commands += String.Format("S2,{0:f3}\n", outY);
                prevY = outY;
            }
            if (Math.Abs(outZ - prevZ) > 1e-10)
            {
                if (Math.Abs(outZ) < 0.05)
                    outZ = 0;

                commands += String.Format("S3,{0:f3}\n", outZ);
                prevZ = outZ;
            }

            // Wはライトなので不感帯を設けない
            if (Math.Abs(outW - prevW) > 0)
            {
                commands += String.Format("S4,{0:f3}\n", outW);
                prevW = outW;
            }

            if (ip != null && commands.Length != 0)
            {
                ip.Send(commands);
            }
        }
    }
}
