using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeterDisplay;

namespace CraneMonitor
{
    public class Controller
    {
        public SerialClient com;

        public string comPort;
        public double[] axis;  //< axis position [-1.0, 1.0]
        public bool[] button;   //< button state {0, 1}
        //public bool           initialized = true;
        //public bool[]         swRequests = new bool[] { false, false, false, false };
        //public bool[]         ledSwRequests = new bool[] { false, false, false, false };
        //public bool[]         ledSwPrevHardRequests = new bool[] { false, false, false, false };
        //public bool[]         ledSwPrevSoftRequests = new bool[] { false, false, false, false };
        //public EnableButton[] SwButtons;
        //public EnableButton[] LedSwButtons;
        //public int zeroPosX = -1;
        //public int zeroPosY = -1;
        //public int zeroPosZ = -1;
        //public double outX = 0;
        //public double outY = 0;
        //public double outZ = 0;

        public Controller(){
            comPort = "COM1";
            axis = new double[4];
            button = new bool[8];
        }

        public bool Init()
        {
            com = new SerialClient();
            //// 操作卓のLEDボタン機能の割り当てはここで行う
            //LedSwButtons = new EnableButton[] { BtnStart, BtnPause, BtnAutoStart, BtnRegister };
            ////SwButtons = new EnableButton[] { BtnHalt, BtnLight, BtnAdjust, null };
            //SwButtons = new EnableButton[] { null, BtnLight, BtnHalt, null };

            com.ReceiveHandler = ReceiveHandler;
            com.Init(comPort);
            return com.Connected();
        }

        /*
        public bool Start()
        {
            //com.LogHandler      = log.WriteLogWarning;
            //com.ErrorLogHandler = log.WriteLogError;
            com.ReceiveHandler  = ReceiveHandler;

            com.Init(comPort);
            return com.Connected();
        }

        public bool Stop()
        {
            com.Close();
            return !com.Connected();
        }

        public void Update()
        {
            if (com.Connected())
            {
                //InterlockHardSwitch(swRequests, swButtons);
                InterlockLedSwitch(ledSwRequests, ledSwPrevHardRequests, ledSwPrevSoftRequests, LedSwButtons);
            }
        }
        */

        public void ReceiveHandler(object sender, string message)
        {
            string[] resMsgList = message.Split(' ');
            if (resMsgList.Length != 12)
            {
                //log.WriteLogError(sender, "[Illegal message format] " + message); // illegal format
                return;
            }
            for (int i = 0; i < 12; i++) if (resMsgList[i].Length == 0) resMsgList[i] = "0";

            axis[0] = (double)(int.Parse(resMsgList[0]) - 512) / 100.0;
            axis[1] = (double)(int.Parse(resMsgList[1]) - 512) / 100.0;
            axis[2] = (double)(int.Parse(resMsgList[2]) - 512) / 100.0;
            axis[3] = (double)(int.Parse(resMsgList[3]) - 512) / 100.0;

            button[0] = (int.Parse(resMsgList[ 4]) != 0);
            button[1] = (int.Parse(resMsgList[ 5]) != 0);
            button[2] = (int.Parse(resMsgList[ 6]) != 0);
            button[3] = (int.Parse(resMsgList[ 7]) != 0);
            button[4] = (int.Parse(resMsgList[ 8]) != 0);
            button[5] = (int.Parse(resMsgList[ 9]) != 0);
            button[6] = (int.Parse(resMsgList[10]) != 0);
            button[7] = (int.Parse(resMsgList[11]) != 0);

            //int sw1 = int.Parse(resMsgList[4]);
            //int sw2 = int.Parse(resMsgList[5]);
            //int sw3 = int.Parse(resMsgList[6]);
            //int sw4 = int.Parse(resMsgList[7]);
            //
            //int ledsw1 = int.Parse(resMsgList[8]);
            //int ledsw2 = int.Parse(resMsgList[9]);
            //int ledsw3 = int.Parse(resMsgList[10]);
            //int ledsw4 = int.Parse(resMsgList[11]);
            //
            //if (zeroPosX < 0) zeroPosX = joy1a;
            //outX = (double)(joy1a - zeroPosX) / 400;
            //
            //if (zeroPosY < 0) zeroPosY = joy1b;
            //outY = (double)(joy1b - zeroPosY) / 400;
            //
            //if (zeroPosZ < 0) zeroPosZ = joy2b;
            //outZ = (double)(joy2b - zeroPosZ) / 400;
            //
            //ledSwRequests[0] = ledsw1 != 0;
            //ledSwRequests[1] = ledsw2 != 0;
            //ledSwRequests[2] = ledsw3 != 0;
            //ledSwRequests[3] = ledsw4 != 0;
            //
            //swRequests[0] = sw1 == 0;
            //swRequests[1] = sw2 == 0;
            //swRequests[2] = sw3 == 0;
            //swRequests[3] = sw4 == 0;
            //
            //for (int i = 0; i < ledSwRequests.Length; i++)
            //    ledSwPrevHardRequests[i] = ledSwRequests[i];

        }

        // 疑似操作卓スイッチとの同期（疑似操作卓のスイッチ状態を優先し常に強制的に同期させる/非LEDスイッチ用）
        void InterlockHardSwitch(bool[] Requests, EnableButton[] Buttons)
        {
            for (int i = 0; i < Requests.Length; i++)
            {
                if (Buttons[i] != null && Buttons[i].Enabled != Requests[i])
                    Buttons[i].Enabled = Requests[i];
            }
        }

        // 疑似操作卓LEDスイッチとの同期（疑似操作卓とアプリの双方の操作のスイッチ操作を許し状態をLEDに反映させる/疑似操作卓のハードウェアスイッチ状態とソフトウェア上の状態(=LED表示)が一致しない場合を許す）
        void InterlockLedSwitch(bool[] Requests, bool[] PrevHardRequests, bool[] PrevSoftRequests, EnableButton[] Buttons)
        {
            bool LedUpdated = false;
            for (int i = 0; i < Requests.Length; i++)
            {
                if (Buttons[i] == null) continue;
                if (Buttons[i].Enabled != PrevSoftRequests[i])
                {
                    // ソフト側で操作された⇒LEDに反映
                    PrevSoftRequests[i] = Buttons[i].Enabled;
                    LedUpdated = true;
                }
                if (Requests[i] != PrevHardRequests[i])
                {
                    // ハード側で操作された⇒LEDに反映
                    PrevHardRequests[i] = Requests[i];
                    LedUpdated = true;
                    // ハード側の要求操作とソフト側の現在の状態が違う⇒ソフト側に反映
                    if (Buttons[i].Enabled != Requests[i]) Buttons[i].Enabled = Requests[i];
                }

            }
            if (LedUpdated)
            {
                string message = "";
                for (int i = 0; i < 4; i++)
                    message += string.Format("{0} ", (Buttons[i].Enabled ? 1 : 0));
                com.Send(message);
            }
        }
    }
}
