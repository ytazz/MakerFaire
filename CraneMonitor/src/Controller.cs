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
        public bool[] PushButtonState;   //< button state {0, 1}
        public bool[] SyncButtonState;   //< button state {0, 1}
        public bool[] PrevPushButtonState;   //< button state {0, 1}
        //public bool           initialized = true;
        //public bool[]         swRequests = new bool[] { false, false, false, false };
        //public bool[]         ledSwRequests = new bool[] { false, false, false, false };
        //public bool[]         ledSwPrevHardRequests = new bool[] { false, false, false, false };
        //public bool[]         ledSwPrevSoftRequests = new bool[] { false, false, false, false };
        public EnableButton[] PushButtons;
        public EnableButton[] SyncButtons;
        //public EnableButton[] LedSwButtons;
        //public int zeroPosX = -1;
        //public int zeroPosY = -1;
        //public int zeroPosZ = -1;
        //public double outX = 0;
        //public double outY = 0;
        //public double outZ = 0;

        public int NumValues = 4;
        public int NumPushButtons = 1;
        public int NumSyncButtons = 10;

        public Controller(){
            comPort = "COM1";
            axis = new double[NumValues];
            PushButtonState = new bool[NumPushButtons];
            PrevPushButtonState = new bool[NumPushButtons];
            SyncButtonState = new bool[NumSyncButtons];
        }

        public bool Init()
        {
            com = new SerialClient();
            com.ReceiveHandler = ReceiveHandler;
            com.Init(comPort);
            return com.Connected();
        }

        public bool Close()
        {
            com.Close();
            return !com.Connected();
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
        */

        public void Update()
        {
            if (com.Connected())
            {
                // PUSHボタン
                GenEventHardSwitch(PushButtonState, PrevPushButtonState, PushButtons);
                for (int j = 0; j < NumPushButtons; j++) PrevPushButtonState[j] = PushButtonState[j];

                // SYNCボタン（状態を保持するボタン）
                InterlockHardSwitch(SyncButtonState, SyncButtons);

                // LED付きSYNCボタン
                //InterlockLedSwitch(ledSwRequests, ledSwPrevHardRequests, ledSwPrevSoftRequests, LedSwButtons);
            }
        }

        public void ReceiveHandler(object sender, string message)
        {
            string[] tok = message.Split(' ');

            if (tok.Length < NumValues + NumPushButtons + NumSyncButtons) return;

            int i = 0;

#if false   // 操作卓の送信側のコードを変えることができるならtrueにした方が分かりやすい
            for (int j = 0; j < NumValues; j++, i++) axis[j] = (double)(int.Parse(tok[i]) - 512) / 100.0;
            for (int j = 0; j < NumPushButtons; j++, i++) PushButtonState[j] = (int.Parse(tok[i]) != 0);
            for (int j = 0; j < NumSyncButtons; j++, i++) SyncButtonState[j] = (int.Parse(tok[i]) != 0);
#else
            for (int j = 0; j < NumValues; j++, i++) axis[j] = (double)(int.Parse(tok[i]) - 512) / 100.0;
            for (int j = 0; j < NumSyncButtons; j++, i++)
            {
                SyncButtonState[j] = (int.Parse(tok[i]) != 0);
                if (i == 10) i += 2;
            }
            i = 11;
            for (int j = 0; j < NumPushButtons; j++, i++) PushButtonState[j] = (int.Parse(tok[i]) != 0);
#endif
        }

        // プッシュボタンのイベント発生
        void GenEventHardSwitch(bool[] Requests, bool[] PrevRequests, EnableButton[] Buttons)
        {
            for (int i = 0; i < Requests.Length; i++)
            {
                if (Buttons[i] != null && PrevRequests[i] == false && Requests[i] == true)
                    Buttons[i].Enabled = !Buttons[i].Enabled;
            }
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
