//#define ENABLE_POS_LIMITS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using SharpDX.DirectInput;
using System.Windows.Threading;
using MjpegProcessor;
using MeterDisplay;

using System.IO;
using System.Xml.Serialization;

namespace JoystickApp
{
    public class Parameters
    {
        public Parameters() { SetInitValue(); }

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
        public string ControllerIp { get; set; }
        public string Contact1Ip { get; set; }
        public string Contact2Ip { get; set; }
        public string Contact3Ip { get; set; }

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

        public void SetInitValue()  // default parameters
        {
            UpdateInterval = 50*1;
            LightUpdateInterval = 1000;
            SliderGain = 3;

            MotorGainX = 1;
            MotorGainY = 1;
            MotorGainZ = 1;

            MotorIp = "192.168.11.15";
            MotorPort = 8888;
            CameraIp = "192.168.11.15";
            CameraPort = 8080;
            DisplayIp = "localhost";
            DisplayPort = 65002;
            ControllerIp = "COM3";
            Contact1Ip = "COM4";
            Contact2Ip = "COM5";
            Contact3Ip = "COM6";

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
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static public Parameters param = new Parameters();
        public LogWindow log = null;

        public delegate float GetRealValue();

        public class MeasurePercent : MeasureObj
        {
            GetRealValue method;
            public MeasurePercent(GetRealValue x) { method = x; }
            public override void InitializeMeterFormat(BoeingMeter meter)
            {
                meter.ValueFormat = "{0:0}%";
            }
            public override float GetMax()
            {
                return 100;
            }
            public override float GetValue()
            {
                return 100.0f * Math.Abs(method());
            }
        };
        public class MeasurePos : MeasureObj
        {
            GetRealValue method;
            public MeasurePos(GetRealValue x) { method = x; }
            public override void InitializeMeterFormat(BoeingMeter meter)
            {
                meter.ValueFormat = "{0:0}";
            }
            public override float GetMax()
            {
                return 100;
            }
            public override float GetValue()
            {
                return 100.0f * Math.Abs(method());
            }
        };

        public MainWindow()
        {
            InitializeComponent();
            log = new LogWindow();
            Course.SetLog(log);
            Course.AutoStartHandler = AutoStart;

            // --------------------------------------------------
            // カメラ画像にダミーを表示

            System.Reflection.Assembly myAssembly = System.Reflection.Assembly.GetEntryAssembly();
            string[] SplPath = myAssembly.Location.Split('\\');
            SplPath[SplPath.Length - 1] = "bird3.jpg";
            string path = string.Join("\\", SplPath);
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(path, UriKind.Absolute);
            bi.EndInit();
            image.Source = bi;

            // --------------------------------------------------
#if false
            //meters.Background = System.Windows.Media.Brushes.Transparent;
#else
            meters.Background = System.Windows.Media.Brushes.Black.Clone();
            meters.Background.Opacity = 0.5;
#endif
#if ENABLE_POS_LIMITS
            // 左列：出力絶対値、右列：位置
            meters.UpdateDisplayLayout(
                new MeasureObj[] {
                    new MeasurePercent(new GetRealValue(delegate() {return (float)MotorOutX; })),
                    new MeasurePos(new GetRealValue(delegate() {return (float)MotorPosX; })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)MotorOutY; })),
                    new MeasurePos(new GetRealValue(delegate() {return (float)MotorPosY; })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)MotorOutZ; })),
                    new MeasurePos(new GetRealValue(delegate() {return (float)MotorPosZ; })) }
                );
            string[] titles = new string[] { "Power X", "Pos. X", "Power Y", "Pos. Y", "Power Z", "Pos. Z" };
#else
            // 左列：出力プラス、右列：出力マイナス
            meters.UpdateDisplayLayout(
                new MeasureObj[] {
                    new MeasurePercent(new GetRealValue(delegate() {return (MotorOutX > 0) ? (float)Math.Min(MotorOutX / param.MotorGainX, 1.0) : 0; })),
                    new MeasurePercent(new GetRealValue(delegate() {return (MotorOutX > 0) ? 0 : (float)Math.Max(MotorOutX / param.MotorGainX, -1.0); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (MotorOutY > 0) ? (float)Math.Min(MotorOutY / param.MotorGainY, 1.0) : 0; })),
                    new MeasurePercent(new GetRealValue(delegate() {return (MotorOutY > 0) ? 0 : (float)Math.Max(MotorOutY / param.MotorGainY, -1.0); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (MotorOutZ > 0) ? (float)Math.Min(MotorOutZ / param.MotorGainZ, 1.0) : 0; })),
                    new MeasurePercent(new GetRealValue(delegate() {return (MotorOutZ > 0) ? 0 : (float)Math.Max(MotorOutZ / param.MotorGainZ, -1.0); })) }
                );
            string[] titles = new string[] { "+ X", "- X", "+ Y", "- Y", "+ Z", "- Z" };
            //new MeasurePercent(new GetRealValue(delegate () { return (MotorOutW > 0) ? (float)MotorOutW : 0; })),

            for (int i = 0; i < 6; i++)
            {
                meters.meter_controls[i].ThresholdScaleLen = 0.0f;  // スケールは表示しない
                SetMeterLimits(i, false, 0, true, 95f);
            }
#endif
            for (int i = 0; i < 6; i++)
                meters.labels[i].Content = titles[i];

            Load();
            SetInitParams();

            // --------------------------------------------------

            // 操作卓のLEDボタン機能の割り当てはここで行う
            LedSwButtons = new EnableButton[] { BtnStart, BtnPause, BtnAutoStart, BtnRegister };
            //SwButtons = new EnableButton[] { BtnHalt, BtnLight, BtnAdjust, null };
            SwButtons = new EnableButton[] { null, BtnLight, BtnHalt, null };

            // --------------------------------------------------

            DispatcherTimer dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, param.UpdateInterval);  // in milliseconds
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Start();
        }

        // --------------------------------------------------

        public Joystick joystick = null;

        Joystick InitJoystick()
        {
            // Initialize DirectInput
            var directInput = new DirectInput();

            // Find a Joystick Guid
            var joystickGuid = Guid.Empty;

            foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
                joystickGuid = deviceInstance.InstanceGuid;

            // If Gamepad not found, look for a Joystick
            if (joystickGuid == Guid.Empty)
                foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
                    joystickGuid = deviceInstance.InstanceGuid;

            // If Joystick not found, throws an error
            if (joystickGuid == Guid.Empty)
            {
                log.MsgWriteLine(LogWindow.MsgTypes.Error, "No joystick/Gamepad found.");
                return null;
            }

            // Instantiate the joystick
            Joystick joystick = new Joystick(directInput, joystickGuid);

            log.MsgWriteLine(LogWindow.MsgTypes.Normal, "Found Joystick/Gamepad with GUID: {0}", joystickGuid);

            // Query all suported ForceFeedback effects
            var allEffects = joystick.GetEffects();
            foreach (var effectInfo in allEffects)
                log.MsgWriteLine(LogWindow.MsgTypes.Normal, "Effect available {0}", effectInfo.Name);

            // Set BufferSize in order to use buffered data.
            joystick.Properties.BufferSize = 128;

            // Acquire the joystick
            joystick.Acquire();

            return joystick;
        }

        private bool JoystickStart()
        {
            this.joystick = InitJoystick();
            if (joystick == null)
                log.Visibility = Visibility.Visible;
            return joystick != null;
        }

        // For debug
        private void BtnGetJoystick_Click(object sender, RoutedEventArgs e)
        {
            log.MsgClear();
            Joystick local_joystick = InitJoystick();
            if (joystick == null)
                log.Visibility = Visibility.Visible;

            // Poll events from joystick
            local_joystick.Poll();
            var datas = local_joystick.GetBufferedData();
            foreach (var state in datas)
            {
                log.MsgWriteLine(LogWindow.MsgTypes.Normal, state.ToString());
            }

            log.Visibility = Visibility.Visible;
            local_joystick.Unacquire();
        }

        enum JoystickDirection { Up, Down, Right, Left, Stop };
        JoystickDirection PoVCtrl0 = JoystickDirection.Stop;
        int JoystickZeroPosX = -1;
        int JoystickZeroPosY = -1;
        int JoystickZeroPosZ = -1;
        double OutX = 0;
        double OutY = 0;
        double OutZ = 0;
        double MotorOutX = 0;
        double MotorOutY = 0;
        double MotorOutZ = 0;
        double MotorOutW = 0;   // Wは点滅ライト
        double PrevMotorOutX = 10;
        double PrevMotorOutY = 10;
        double PrevMotorOutZ = 10;
        double PrevMotorOutW = 10;
        bool updated = true;
        bool stop = false;

        double MotorPosX = 0.5; // Todo : replace to zero
        double MotorPosY = 0.6;
        double MotorPosZ = 0.7;


        private void GetJoystickOp()
        {
            // Poll events from joystick
            joystick.Poll();
            var datas = joystick.GetBufferedData();
            foreach (var state in datas)
            {
                switch (state.Offset)
                {
                    case JoystickOffset.X:
                        if (JoystickZeroPosX < 0) JoystickZeroPosX = state.Value;
                        OutX = (double)(state.Value - JoystickZeroPosX) / (2 << 14);
                        updated = true;
                        break;
                    case JoystickOffset.Y:
                        if (JoystickZeroPosY < 0) JoystickZeroPosY = state.Value;
                        OutY = (double)(state.Value - JoystickZeroPosY) / (2 << 14);
                        OutY = -OutY;
                        updated = true;
                        break;
                    case JoystickOffset.Sliders0:
                        param.MotorGainX = 1.0f - (float)state.Value / (2 << 16);
                        param.MotorGainX *= param.SliderGain;
                        param.MotorGainY = param.MotorGainX;
                        updated = true;
                        break;
                    case JoystickOffset.PointOfViewControllers0:
                        switch (state.Value)
                        {
                            case -1:
                                PoVCtrl0 = JoystickDirection.Stop;
                                break;
                            case 0:
                                PoVCtrl0 = JoystickDirection.Up;
                                break;
                            case 9000:
                                PoVCtrl0 = JoystickDirection.Right;
                                break;
                            case 18000:
                                PoVCtrl0 = JoystickDirection.Down;
                                break;
                            case 27000:
                                PoVCtrl0 = JoystickDirection.Left;
                                break;
                        }

                        if (PoVCtrl0 == JoystickDirection.Stop)
                        {
                            OutZ = 0;
                            updated = true;
                        }
                        break;
                    case JoystickOffset.Buttons0:
                        stop = true;
                        break;
                    case JoystickOffset.Buttons6:
                        MotorClient.Send("SHUTDOWN\n");
                        break;

                }
            }
        }

        // ------------------------------------------------------------

        float LightDelta = 0;

        private bool SetLightInterval()
        {
            DivRatioLight = (int)Math.Round((double)param.LightUpdateInterval / 2 / param.UpdateInterval);
            if(DivRatioLight == 0)
            {
                LightDelta = 0;
                DivRatioLight = -1;
                return false;
            }
            else
            {
                LightDelta = 2.0f / DivRatioLight;
                TimeIndexLight = 0;
                return true;
            }
        }

        // ------------------------------------------------------------

        const int DivRatioAdc = -1; // 現在ADCは使ってない
        int TimeIndexAdc = 0;

        const int DivRatioBtnChk = 3;
        int TimeIndexBtnChk = 0;

        int DivRatioLight = -1;
        int TimeIndexLight = 0;

        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            //ImuClient.Send("POS\n");  // send command to obtain IMU measurements

            // ------------------------------------------------------------
            // ADC値の取得リクエスト発行（毎回の更新は必要ない）

            if (TimeIndexAdc == DivRatioAdc)
            {
                MotorClient.Send("ADC\n");  // send command to obtain ADC measurements
                TimeIndexAdc = 0;
            }
            TimeIndexAdc++;

            // ------------------------------------------------------------
            // ボタン動作（毎回の更新は必要ない）

            if (TimeIndexBtnChk == DivRatioBtnChk)
            {
                if (ControllerClient.Connected())
                {
                    InterlockHardSwitch(SwRequests, SwButtons);
                    InterlockLedSwitch(LedSwRequests, LedSwPrevHardRequests, LedSwPrevSoftRequests, LedSwButtons);
                }

                TimeIndexBtnChk = 0;
            }
            TimeIndexBtnChk++;

            // ------------------------------------------------------------
            // 点滅ライト

            if (BtnLight.Enabled)
            {
                if (TimeIndexLight == DivRatioLight)
                {
                    LightDelta = -LightDelta;
                    TimeIndexLight = 0;
                }
                TimeIndexLight++;

                MotorOutW += LightDelta;
                updated = true;
            }
            else
            {
                MotorOutW = 0;
            }

            // ------------------------------------------------------------
            // タイマー表示

            Course.UpdateDisplay();

            // ------------------------------------------------------------
            // モーター指令値の算出と送信

            MotorOutputGenerator();

            // ------------------------------------------------------------
            // メーター表示

            //label.Content = String.Format("X={0:f3}, Y={1:f3}, Scale={2:f2}", MotorOutX, MotorOutY, GainScale);
            //label.Content = String.Format("ADC1={0:f3}, ADC2={1:f3}, ADC3={2:f2}", MotorPosX, MotorPosY, MotorPosZ);
            meters.Update(false);
        }

        void MotorOutputGenerator()
        {
            // ------------------------------------------------------------
            // USBジョイスティック状態の取得

            if (joystick != null)
            {
                GetJoystickOp();

                switch (PoVCtrl0)
                {
                    case JoystickDirection.Up:
                        OutZ += 0.25;
                        updated = true;
                        break;
                    case JoystickDirection.Down:
                        OutZ -= 0.25;
                        updated = true;
                        break;
                }
            }

            // ------------------------------------------------------------
            // 疑似操作卓の状態の取得

            if (BtnHalt.Enabled)
            {
                OutX = 0;
                OutY = 0;
                OutZ = 0;
                MotorOutW = 0;
                updated = true;
            }

            // ------------------------------------------------------------
            // モーター指令値の算出と送信

            if (updated)
            {
                MotorOutX = param.MotorGainX * OutX;
                MotorOutY = param.MotorGainY * OutY;
                MotorOutZ = param.MotorGainZ * OutZ;

                if (MotorOutX < -1) MotorOutX = -1;
                if (MotorOutY < -1) MotorOutY = -1;
                if (MotorOutZ < -1) MotorOutZ = -1;
                if (MotorOutW < 0) MotorOutW = 0;   // Wはライトなので極性の反転なし
                if (MotorOutX > 1) MotorOutX = 1;
                if (MotorOutY > 1) MotorOutY = 1;
                if (MotorOutZ > 1) MotorOutZ = 1;
                if (MotorOutW > 1) MotorOutW = 1;

                if (param.DirectionX) MotorOutX = -MotorOutX;
                if (param.DirectionY) MotorOutY = -MotorOutY;
                if (param.DirectionZ) MotorOutZ = -MotorOutZ;

                string commands = "";
                if (Math.Abs(MotorOutX - PrevMotorOutX) > 1e-10)
                {
                    if (Math.Abs(MotorOutX) < 0.05) MotorOutX = 0; // ジョイスティックを離してセンターポジションに戻ってきたときに初期位置から数パーセントずれていることがある（ずれているとアイドルでモーターが動いてしまうのを防ぐため）
                    commands += String.Format("S1,{0:f3}\n", MotorOutX);
                    PrevMotorOutX = MotorOutX;
                }
                if (Math.Abs(MotorOutY - PrevMotorOutY) > 1e-10)
                {
                    if (Math.Abs(MotorOutY) < 0.05) MotorOutY = 0;
                    commands += String.Format("S2,{0:f3}\n", MotorOutY);
                    PrevMotorOutY = MotorOutY;
                }
                if (Math.Abs(MotorOutZ - PrevMotorOutZ) > 1e-10)
                {
                    if (Math.Abs(MotorOutZ) < 0.05) MotorOutZ = 0;
                    commands += String.Format("S3,{0:f3}\n", MotorOutZ);
                    PrevMotorOutZ = MotorOutZ;
                }
                if (Math.Abs(MotorOutW - PrevMotorOutW) > 0)    // Wはライトなので不感帯を設けない
                {
                    commands += String.Format("S4,{0:f3}\n", MotorOutW);
                    PrevMotorOutW = MotorOutW;
                }
                if (stop)
                {
                    commands += "STOP\n";
                }

                if (commands.Length != 0)
                {
                    MotorClient.Send(commands);
                }

                updated = false;
                stop = false;
            }
        }

        // ------------------------------------------------------------

        public void CommonReceiveHandler(object sender, string message)
        {
            if (message[1] != ',')
            {
                log.WriteLogError(sender, "[Unknown message] " + message); // Unknown message
                return;
            }
            switch (message[0])
            {
                case 'L':
                    log.WriteLogNormal(sender, message.Substring(2));
                    break;
                case 'E':
                    log.WriteLogError(sender, message.Substring(2));
                    break;
                case 'W':
                    log.WriteLogWarning(sender, message.Substring(2));
                    break;
                default:
                    log.WriteLogError(sender, "[Unknown message] " + message); // Unknown message
                    break;
            }
        }

        public void MotorCtrlReceiveHandler(object sender, string message)
        {
            if (message.Length < 3) return;
            string prefix = message.Substring(0, 2);
            if(prefix == "A,")
            {
                string[] tokens = message.Split(',');
                MotorPosX = double.Parse(tokens[1]);
                MotorPosY = double.Parse(tokens[2]);
                MotorPosZ = double.Parse(tokens[3]);
            }else
            {
                CommonReceiveHandler(sender, message);
            }
        }

        public void ControllerReceiveHandler(object sender, string message)
        {
            string[] resMsgList = message.Split(' ');
            if (resMsgList.Length != 12)
            {
                log.WriteLogError(sender, "[Illegal message format] " + message); // illegal format
                return;
            }
            for (int i = 0; i < 12; i++) if (resMsgList[i].Length == 0) resMsgList[i] = "0";

            int joy1a = int.Parse(resMsgList[0]);
            int joy1b = int.Parse(resMsgList[1]);
            int joy2a = int.Parse(resMsgList[2]);
            int joy2b = int.Parse(resMsgList[3]);

            int sw1 = int.Parse(resMsgList[4]);
            int sw2 = int.Parse(resMsgList[5]);
            int sw3 = int.Parse(resMsgList[6]);
            int sw4 = int.Parse(resMsgList[7]);

            int ledsw1 = int.Parse(resMsgList[8]);
            int ledsw2 = int.Parse(resMsgList[9]);
            int ledsw3 = int.Parse(resMsgList[10]);
            int ledsw4 = int.Parse(resMsgList[11]);

            if (JoystickZeroPosX < 0) JoystickZeroPosX = joy1a;
            OutX = (double)(joy1a - JoystickZeroPosX) / 400;

            if (JoystickZeroPosY < 0) JoystickZeroPosY = joy1b;
            OutY = (double)(joy1b - JoystickZeroPosY) / 400;

            if (JoystickZeroPosZ < 0) JoystickZeroPosZ = joy2b;
            OutZ = (double)(joy2b - JoystickZeroPosZ) / 400;

            LedSwRequests[0] = ledsw1 != 0;
            LedSwRequests[1] = ledsw2 != 0;
            LedSwRequests[2] = ledsw3 != 0;
            LedSwRequests[3] = ledsw4 != 0;

            SwRequests[0] = sw1 == 0;
            SwRequests[1] = sw2 == 0;
            SwRequests[2] = sw3 == 0;
            SwRequests[3] = sw4 == 0;

            if (ControllerInitial)
            {
                for (int i = 0; i < LedSwRequests.Length; i++)
                    LedSwPrevHardRequests[i] = LedSwRequests[i];
                ControllerInitial = false;
            }

            updated = true;
        }

        bool ControllerInitial = true;
        bool[] SwRequests = new bool[] { false, false, false, false };
        bool[] LedSwRequests = new bool[] { false, false, false, false };
        bool[] LedSwPrevHardRequests = new bool[] { false, false, false, false };
        bool[] LedSwPrevSoftRequests = new bool[] { false, false, false, false };
        EnableButton[] SwButtons;
        EnableButton[] LedSwButtons;

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
                ControllerClient.Send(message);
            }
        }

        // ------------------------------------------------------------

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MotorClient.Close();

            log.CloseAsHide = false;
            log.Close();

            if (mjpeg != null)
            {
                mjpeg.FrameReady += (object _sender, MjpegProcessor.FrameReadyEventArgs _e) => { };
                mjpeg = null;
            }

            // 以下コメントアウトを有効にすると、前回に設定したパラメータが次回起動時に初期設定される
            Save();
        }

        private void BtnGetLog_Click(object sender, RoutedEventArgs e)
        {
            log.Visibility = Visibility.Visible;
        }

        private void BtnShutdown_Click(object sender, RoutedEventArgs e)
        {
            MotorClient.Send("SHUTDOWN\n");
        }

        // ------------------------------------------------------------

        private bool DummyEnableHandler()
        {
            return true;
        }

        // ------------------------------------------------------------

        SocketClient MotorClient = new JoystickApp.SocketClient();

        private bool MotorStart()
        {
            MotorClient.LogHandler = log.WriteLogWarning;
            MotorClient.ErrorLogHandler = log.WriteLogError;
            MotorClient.ReceiveHandler = MotorCtrlReceiveHandler;

            MotorClient.Init(param.MotorIp, param.MotorPort);
            return MotorClient.Connected();
        }

        private bool MotorStop()
        {
            MotorClient.Close();
            return !MotorClient.Connected();
        }

        // ------------------------------------------------------------

        SocketClient ImuClient = new JoystickApp.SocketClient();

        private bool ImuStart()
        {
            ImuClient.LogHandler = log.WriteLogWarning;
            ImuClient.ErrorLogHandler = log.WriteLogError;
            //ImuClient.ReceiveHandler = ImuReceiveHandler;

            //ImuClient.Init(param.ImuIp, param.ImuPort);
            return ImuClient.Connected();
        }

        private bool ImuStop()
        {
            ImuClient.Close();
            return !ImuClient.Connected();
        }

        // ------------------------------------------------------------

        SerialClient ControllerClient = new JoystickApp.SerialClient();

        private bool ControllerStart()
        {
            ControllerClient.LogHandler = log.WriteLogWarning;
            ControllerClient.ErrorLogHandler = log.WriteLogError;
            ControllerClient.ReceiveHandler = ControllerReceiveHandler;

            ControllerClient.Init(param.ControllerIp);
            ControllerInitial = true;
            return ControllerClient.Connected();
        }

        private bool ControllerStop()
        {
            ControllerClient.Close();
            return !ControllerClient.Connected();
        }

        // ------------------------------------------------------------

        CourseControl Course = new CourseControl();

        //private void BtnRegister_Click(object sender, RoutedEventArgs e) { Course.GameRegister(); }
        private bool GameRegister() { return Course.GameRegister(); }
        private bool DisplayStart() { return Course.DisplayStart(param.DisplayIp, param.DisplayPort); }
        private bool DisplayStop() { return Course.DisplayStop(); }
        private bool GameStart() { return Course.GameStart(); }
        private bool GameStop() { if (Course.GameStop()) BtnRegister.Enabled = false; return true; }
        private bool GameStartPause() { return Course.GameStartPause(); }
        private bool GameStopPause() { return Course.GameStopPause(); }
        public bool Contact1Start() { return Course.Contact1Start(param.Contact1Ip); }
        public bool Contact2Start() { return Course.Contact2Start(param.Contact2Ip); }
        public bool Contact3Start() { return Course.Contact3Start(param.Contact3Ip); }
        public bool Contact1Stop() { return Course.Contact1Stop(); }
        public bool Contact2Stop() { return Course.Contact2Stop(); }
        public bool Contact3Stop() { return Course.Contact3Stop(); }
        public void AutoStart(bool IsStartEvent) { Dispatcher.BeginInvoke((Action)(() => { if (BtnAutoStart.Enabled) BtnStart.Enabled = IsStartEvent; })); }

        // ------------------------------------------------------------

        private string GetImageUrl(int n)
        {
            return String.Format("http://{0}:{1}/?action=snapshot&{2}", param.CameraIp, param.CameraPort, n);
        }

        private int ImageIndex = 0;

        private void ImageOneShot()
        {
            BitmapImage imageSource = new BitmapImage();
            imageSource.BeginInit();
            imageSource.UriSource = new Uri(GetImageUrl(ImageIndex++));
            imageSource.EndInit();
            image.Source = imageSource;
            //imageSource.DownloadCompleted += new EventHandler(LoadNextImage);
        }

        MjpegProcessor.MjpegDecoder mjpeg;

        private bool CameraStart()
        {
            mjpeg = new MjpegDecoder();
            mjpeg.ParseStream(new Uri(String.Format("http://{0}:{1}/?action=stream", param.CameraIp, param.CameraPort)));
            mjpeg.FrameReady += (object _sender, MjpegProcessor.FrameReadyEventArgs _e) =>
            {
                image.Source = _e.BitmapImage;
            };
            return true;
        }

        private bool CameraStop()
        {
            mjpeg.StopStream();
            return true;
        }

        // ------------------------------------------------------------

        //設定をファイルから読み込む
        public static void Load()
        {
            //ユーザ毎のアプリケーションデータディレクトリに保存する
            String appPath = String.Format(
                "{0}\\{1}",
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "JoystickApp\\settings.xml");

            if (File.Exists(appPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Parameters));

                using (FileStream stream = new FileStream(appPath, FileMode.Open))
                {
                    Parameters temp = serializer.Deserialize(stream) as Parameters;
                    if (temp == null)
                        param.SetInitValue();
                    else
                        param = (Parameters)temp;
                }
            }
            else
            {
                String folderPath = String.Format(
                    "{0}\\{1}",
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "JoystickApp");
                System.IO.Directory.CreateDirectory(folderPath);
                param.SetInitValue();
            }
        }

        //設定をファイルに保存する
        public static void Save()
        {
            String appPath = String.Format(
                "{0}\\{1}",
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "JoystickApp\\settings.xml");
            XmlSerializer serializer = new XmlSerializer(typeof(Parameters));

            using (FileStream stream = new FileStream(appPath, FileMode.Create))
            {
                serializer.Serialize(stream, param);
            }
        }

        // ------------------------------------------------------------

        public void SetInitParams()
        {
            MotorIp.Text = param.MotorIp + ":" + param.MotorPort;
            CameraIp.Text = param.CameraIp + ":" + param.CameraPort;
            ControllerIp.Text = param.ControllerIp;
            DisplayIp.Text = param.DisplayIp + ":" + param.DisplayPort;
            Contact1.Text = param.Contact1Ip;
            Contact2.Text = param.Contact2Ip;
            Contact3.Text = param.Contact3Ip;

            LowerX.Text = param.EnableLimitLowerX ? param.LimitLowerX.ToString() : "disabled";
            LowerY.Text = param.EnableLimitLowerY ? param.LimitLowerY.ToString() : "disabled";
            LowerZ.Text = param.EnableLimitLowerZ ? param.LimitLowerZ.ToString() : "disabled";
            UpperX.Text = param.EnableLimitUpperX ? param.LimitUpperX.ToString() : "disabled";
            UpperY.Text = param.EnableLimitUpperY ? param.LimitUpperY.ToString() : "disabled";
            UpperZ.Text = param.EnableLimitUpperZ ? param.LimitUpperZ.ToString() : "disabled";

            MotorGainX.Text = param.MotorGainX.ToString();
            MotorGainY.Text = param.MotorGainY.ToString();
            MotorGainZ.Text = param.MotorGainZ.ToString();

            param.DirectionX = (bool)directionX.IsChecked;
            param.DirectionY = (bool)directionY.IsChecked;
            param.DirectionZ = (bool)directionZ.IsChecked;

            UpdateInterval.Text = param.UpdateInterval.ToString();
            LightInterval.Text = param.LightUpdateInterval.ToString();
            SetLightInterval();
        }

        private bool GetIpAddress(TextBox textbox, ref string ip, ref int port)
        {
            string[] token = textbox.Text.Split(':');
            if ((token.Length != 2) || (token[0].Length == 0) || (token[1].Length == 0))
            {
                textbox.Background = System.Windows.Media.Brushes.Orange;
                return false;
            }
            else
            {
                textbox.Background = System.Windows.Media.Brushes.LightBlue;
                ip = token[0];
                port = int.Parse(token[1]);
                return true;
            }
        }

        private void Ip_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textbox = sender as TextBox;
            string ip = "";
            int port = 0;
            if (textbox == MotorIp)
            {
                ip = param.MotorIp;
                port = param.MotorPort;
            }
            else if (textbox == CameraIp)
            {
                ip = param.CameraIp;
                port = param.CameraPort;
            }
            else if (textbox == DisplayIp)
            {
                ip = param.DisplayIp;
                port = param.DisplayPort;
            }
            else if (textbox == ControllerIp)
            {
                ip = param.ControllerIp;
            }

            if (GetIpAddress(textbox, ref ip, ref port))
            {
                // IPアドレスのフォーマットである場合
                if (textbox == MotorIp)
                {
                    param.MotorIp = ip;
                    param.MotorPort = port;
                }
                else if (textbox == CameraIp)
                {
                    param.CameraIp = ip;
                    param.CameraPort = port;
                }
                else if (textbox == DisplayIp)
                {
                    param.DisplayIp = ip;
                    param.DisplayPort = port;
                }
            }
            else
            {
                // IPアドレスのフォーマットではない場合
                if (textbox.Text.Length != 0)
                {
                    textbox.Background = System.Windows.Media.Brushes.LightGreen;
                    if (textbox == ControllerIp)
                    { param.ControllerIp = textbox.Text; }
                    else if (textbox == Contact1)
                    { param.Contact1Ip = textbox.Text; }
                    else if (textbox == Contact2)
                    { param.Contact2Ip = textbox.Text; }
                    else if (textbox == Contact3)
                    { param.Contact3Ip = textbox.Text; }
                }

            }
        }

        private void SetMeterLimits(int MeterIndex, bool LowerEnable, float Lower, bool UpperEnable, float Upper)
        {
            MeterControl ctrl = meters.meter_controls[MeterIndex];

            if (LowerEnable && UpperEnable)
            {
                if(ctrl.NumThresholds != 2)
                {
                    ctrl.NumThresholds = 2;
                    Brush sbr = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0x44, 0x44));
                    ctrl.SetThresholdScaleBrush(0, sbr);
                    ctrl.SetThresholdScaleBrush(1, sbr);
                    Brush obr = new SolidColorBrush(Color.FromArgb(0x66, 0xff, 0x44, 0x44));
                    ctrl.SetThresholdBrush(0, obr);
                    ctrl.SetThresholdBrush(2, obr);
                    Brush normal = new SolidColorBrush(Color.FromArgb(0x44, 0xff, 0xff, 0xff));
                    ctrl.SetThresholdBrush(1, normal);
                }
                ctrl.SetThreshold(0, Lower);
                ctrl.SetThreshold(1, Upper);
            }else if (LowerEnable || UpperEnable)
            {
                if (ctrl.NumThresholds != 1)
                {
                    ctrl.NumThresholds = 1;
                    Brush sbr = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0x44, 0x44));
                    ctrl.SetThresholdScaleBrush(0, sbr);
                    Brush obr = new SolidColorBrush(Color.FromArgb(0x66, 0xff, 0x44, 0x44));
                    ctrl.SetThresholdBrush(LowerEnable ? 0 : 1, obr);
                    Brush normal = new SolidColorBrush(Color.FromArgb(0x44, 0xff, 0xff, 0xff));
                    ctrl.SetThresholdBrush(LowerEnable ? 1 : 0, normal);
                }
                ctrl.SetThreshold(0, LowerEnable ? Lower : Upper);
            }
            else
            {
                ctrl.NumThresholds = 0;
            }

            ctrl.RedrawScales();
        }

        private void Limits_TextChanged_Each(int MeterIndex, TextBox Lower, TextBox Upper, out bool enb_l, out bool enb_u, out float lv, out float uv)
        {
            enb_l = float.TryParse(Lower.Text, out lv);
            enb_u = float.TryParse(Upper.Text, out uv);
#if ENABLE_POS_LIMITS
            SetMeterLimits(MeterIndex, enb_l, lv, enb_u, uv);
#endif
            Lower.Background = enb_l ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.Orange;
            Upper.Background = enb_u ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.Orange;
        }

        private void Limits_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool enb_l, enb_u;
            float lv, uv;
            TextBox textbox = sender as TextBox;
            if (textbox == LowerX || textbox == UpperX)
            {
                Limits_TextChanged_Each(1, LowerX, UpperX, out enb_l, out enb_u, out lv, out uv);
                if (enb_l) param.LimitLowerX = lv;
                if (enb_u) param.LimitUpperX = uv;
                param.EnableLimitLowerX = enb_l;
                param.EnableLimitUpperX = enb_u;
            }
            else if (textbox == LowerY || textbox == UpperY)
            {
                Limits_TextChanged_Each(3, LowerY, UpperY, out enb_l, out enb_u, out lv, out uv);
                if (enb_l) param.LimitLowerY = lv;
                if (enb_u) param.LimitUpperY = uv;
                param.EnableLimitLowerY = enb_l;
                param.EnableLimitUpperY = enb_u;
            }
            else if (textbox == LowerZ || textbox == UpperZ)
            {
                Limits_TextChanged_Each(5, LowerZ, UpperZ, out enb_l, out enb_u, out lv, out uv);
                if (enb_l) param.LimitLowerZ = lv;
                if (enb_u) param.LimitUpperZ = uv;
                param.EnableLimitLowerZ = enb_l;
                param.EnableLimitUpperZ = enb_u;
            }
        }

        private void Interval_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textbox = sender as TextBox;
            int val;
            bool enb = int.TryParse(textbox.Text, out val);
            if (val <= 0) enb = false;
            textbox.Background = enb ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.Orange;
            if (enb)
            {
                if (textbox == UpdateInterval)
                    param.UpdateInterval = val;
                if (textbox == LightInterval)
                    param.LightUpdateInterval = val;

                SetLightInterval();
            }
        }

        private void Gain_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textbox = sender as TextBox;
            float val;
            bool enb = float.TryParse(textbox.Text, out val);
            if (val < 0) enb = false;
            textbox.Background = enb ? System.Windows.Media.Brushes.LightGreen : System.Windows.Media.Brushes.Orange;
            if (enb)
            {
                if (textbox == MotorGainX)
                    param.MotorGainX = val;
                if (textbox == MotorGainY)
                    param.MotorGainY = val;
                if (textbox == MotorGainZ)
                    param.MotorGainZ = val;
            }
        }

        private void direction_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox check = sender as CheckBox;
            if (check == directionX) param.DirectionX = (bool)check.IsChecked;
            if (check == directionY) param.DirectionY = (bool)check.IsChecked;
            if (check == directionZ) param.DirectionZ = (bool)check.IsChecked;
        }
    }
}
