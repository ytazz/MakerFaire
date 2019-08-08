using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
//using System.Windows.Navigation;
//using System.Windows.Shapes;

using System.Windows.Threading;
using MeterDisplay;

using System.IO;
using System.Xml.Serialization;


namespace CraneMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static public Param param = new Param();

        public LogWindow      log;
        public Joystick       joystick;
        public Controller     controller;
        public CameraUsb[]    camera;
        public MotorDriver[]  motor;
        public SensorBoard    sensor;
        public Light          light;
        public RankingControl ranking;

        private MeasurePos mpos1 = null, mpos2 = null, mpos3 = null;

        public MainWindow()
        {
            InitializeComponent();

            log        = new LogWindow();            
            motor      = new MotorDriver[2];
            motor[0]   = new MotorDriver();
            motor[1]   = new MotorDriver();
            sensor     = new SensorBoard();
            joystick   = new Joystick();
            controller = new Controller();
            light      = new Light();
            camera     = new CameraUsb[2];
            camera[0]  = new CameraUsb();
            camera[1]  = new CameraUsb();

            ranking = new RankingControl();
            ranking.SetLog(log);

            // パラメータ読み込み ------------------------------------------------------------

            BtnLoadParam_Click(null, null);

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
            image1.Source = bi;
            image2.Source = bi;

            // --------------------------------------------------
            // メーター表示

            //meters.Background = System.Windows.Media.Brushes.Black.Clone();
            //meters.Background.Opacity = 0.5;

            float PwmVal(int n)
            {
                int i = meter_select[n];
                int k = i / 3;
                int j = i % 3;
                return (float)((motor[k].mode[j] == 0) ? motor[k].pwm_ref[j] : motor[k].pwm[j]) / (float)(motor[k].pwmMax);
            }

            float VelVal(int n)
            {
                int i = meter_select[n];
                int k = i / 3;
                int j = i % 3;
                return (float)((motor[k].mode[j] == 0) ? vel[i] : motor[k].vel_ref[j]) / (float)(motor[k].velMax);
            }

            float PosVal(int n)
            {
                int i = meter_select[n];
                int k = i / 3;
                int j = i % 3;
                return motor[k].pos[j];
            }

            meters.UpdateDisplayLayout(
                new MeasureObj[] {
                    new MeasurePercent(new GetRealValue(delegate() { return PwmVal(0); })),
                    new MeasurePercent(new GetRealValue(delegate() { return PwmVal(1); })),
                    new MeasurePercent(new GetRealValue(delegate() { return PwmVal(2); })),
                    new MeasurePercent(new GetRealValue(delegate() { return VelVal(0); })),
                    new MeasurePercent(new GetRealValue(delegate() { return VelVal(1); })),
                    new MeasurePercent(new GetRealValue(delegate() { return VelVal(2); })),
                    mpos1 = new MeasurePos(new GetRealValue(delegate() { return PosVal(0); })),
                    mpos2 = new MeasurePos(new GetRealValue(delegate() { return PosVal(1); })),
                    mpos3 = new MeasurePos(new GetRealValue(delegate() { return PosVal(2); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)(sensor.pot[0] / (float)(sensor.potMax)); })),
                    null,
                    null
                });
            
            for (int i = 0; i < meters.meter_controls.Length; i++)
            {
                if(meters.meter_controls[i] != null)
                {
                    meters.meter_controls[i].ThresholdScaleLen = 0.0f;  // スケールは表示しない
                    if (i < 6)
                        SetMeterLimitsSigned(i, true, -0.85f, true, 0.85f);
                    else
                        SetMeterLimitsUnsigned(i, true, 0.10f, true, 0.90f);
                }
            }

#if false
            string[] titles = new string[] { "+ X", "- X", "+ Y", "- Y", "+ Z", "- Z" };
            for (int i = 0; i < titles.Length; i++)
                meters.labels[i].Content = titles[i];
#endif
            foreach (Label label in meters.labels) label.Visibility = Visibility.Hidden;

            // --------------------------------------------------

            //// 操作卓のLEDボタン機能の割り当てはここで行う
            controller.PushButtons = new EnableButton[] { BtnRegister };
            controller.SyncButtons = new EnableButton[] { null, BtnPause, null, BtnStart, null, null, BtnHalt, null, null, null };

            OnMotorChanged();

            // 初期自動接続
            BtnJoystick.Enabled = true;
            BtnMotor1.Enabled = true;
            BtnMotor2.Enabled = true;
            BtnSensor.Enabled = true;
            //BtnCtrl.Enabled = true;
            BtnLight.Enabled = true;

            BtnDispRank.Enabled = true;
            BtnDispAzim.Enabled = true;

            // インターバルタイマ
            DispatcherTimer dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, param.UpdateInterval);  // in milliseconds
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Start();
        }

        // ------------------------------------------------------------

        private const int NUM_SOURCE = 6;
        private int freqdiv_count = 0;
        private double[] vel = new double[NUM_SOURCE];
        private int[] prev_pos = new int[NUM_SOURCE];
        private double prev_azimuth = -1;
        private const int SOURCE_INVALID = -1;
        private const int SOURCE_LIGHT = -2;
        private int[] source_select = new int[NUM_SOURCE] { 0, 1, 2, SOURCE_INVALID, SOURCE_INVALID, SOURCE_INVALID };
        private const int NUM_METER = 3;
        private int[] meter_select = new int[NUM_METER] { 0, 1, 2 };

        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            double dt = 0.001 * param.UpdateInterval;

            double select(double[] axis, int select_id)
            {
                switch (select_id)
                {
                    case SOURCE_LIGHT:
                        return light.output;
                    default:
                        return (select_id >= 0) ? axis[select_id] : 0;
                }
            }

            for (int i = 0; i < NUM_SOURCE; i++)
            {
                double source;
                if (BtnJoystick.Enabled)
                {
                    joystick.Update();
                    source = select(joystick.axis, source_select[i]);
                }
                else
                {
                    source = select(controller.axis, source_select[i]);
                }
                int j = i % 3;
                int k = i / 3;
                // haltが押されていたら指令値を0
                if (BtnHalt.Enabled)
                {
                    motor[k].pwm_ref[j] = 0;
                    motor[k].vel_ref[j] = 0;
                }
                else
                {
                    if (motor[k].mode[j] == 0)
                        motor[k].pwm_ref[j] = (int)(motor[k].pwmMax * source);
                    else
                        motor[k].vel_ref[j] = (int)(motor[k].velMax * source);

                    vel[j] = (motor[k].pos[j] - prev_pos[i]) * 1000 / param.UpdateInterval;
                    prev_pos[i] = motor[k].pos[j];
                }
            }
            motor[0].Update(dt);
            motor[1].Update(dt);

            // Update Light
            if (BtnLight.Enabled)
            {
                light.Update(dt);
            }
            else
            {
                light.output = 0;
            }

            // Update Meter
            meters.Update(false);

            // カメラ動画 ------------------------------
            if (camera[0].Update()) image1.Source = camera[0].image;
            if (camera[1].Update()) image2.Source = camera[1].image;

            // 頻度の低い更新（分周比 1/5） ------------------------------

            if (freqdiv_count == 0)
            {
                controller.Update();

                if (ranking.RequestUpdateRanking) TextRanking.Text = ranking.GetRankingText();
                if (ranking.RequestUpdateStart || ranking.RequestUpdateStop) TextElapseTime.Text = ranking.GetElapseTimeText();

                freqdiv_count = 5;
            }

            freqdiv_count--;

#if true
            if (prev_azimuth != 0)
            {
                Azimuth.MeterValue = 0;
                prev_azimuth = Azimuth.MeterValue;
            }
#endif
        }
        
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            log.CloseAsHide = false;
            log.Close();

            BtnJoystick.Enabled = false;
            BtnMotor1.Enabled = false;
            BtnMotor2.Enabled = false;
            BtnSensor.Enabled = false;
            BtnCtrl.Enabled = false;
            BtnCamera1.Enabled = false;
            BtnCamera2.Enabled = false;

            // 以下コメントアウトを有効にすると、前回に設定したパラメータが次回起動時に初期設定される
            //Param.Save(param);
        }

        private void BtnGetLog_Click(object sender, RoutedEventArgs e)
        {
            log.Visibility = Visibility.Visible;
        }

        private void BtnLoadParam_Click(object sender, RoutedEventArgs e)
        {
            // パラメータ読み込み ------------------------------------------------------------

            param = Param.Load();

            motor[0].comPort = param.MotorComPort1;
            motor[1].comPort = param.MotorComPort2;
            sensor.comPort = param.SensorComPort;
            controller.comPort = param.ControllerComPort;

            LabMotor1.Text = param.MotorComPort1;
            LabMotor2.Text = param.MotorComPort2;
            LabSensor.Text = param.SensorComPort;
            LabCtrl.Text = param.ControllerComPort;

            // set camera id
            camera[0].id = param.UsbCameraId[0];
            camera[1].id = param.UsbCameraId[1];

            LabCamera1.Text = param.UsbCameraId[0].ToString();
            LabCamera2.Text = param.UsbCameraId[1].ToString();

            BtnFbMode0.Enabled = param.MotorFbMode[0];
            BtnFbMode1.Enabled = param.MotorFbMode[1];
            BtnFbMode2.Enabled = param.MotorFbMode[2];
            BtnFbMode3.Enabled = param.MotorFbMode[3];
            BtnFbMode4.Enabled = param.MotorFbMode[4];
            BtnFbMode5.Enabled = param.MotorFbMode[5];

            BtnJ0.Enabled = param.MotorDirection[0];
            BtnJ1.Enabled = param.MotorDirection[1];
            BtnJ2.Enabled = param.MotorDirection[2];
            BtnJ3.Enabled = param.MotorDirection[3];
            BtnJ4.Enabled = param.MotorDirection[4];
            BtnJ5.Enabled = param.MotorDirection[5];

            BtnP0.Enabled = param.EncoderDirection[0];
            BtnP1.Enabled = param.EncoderDirection[1];
            BtnP2.Enabled = param.EncoderDirection[2];
            BtnP3.Enabled = param.EncoderDirection[3];
            BtnP4.Enabled = param.EncoderDirection[4];
            BtnP5.Enabled = param.EncoderDirection[5];

            LabAxisCh0.Text = param.Source[0];
            LabAxisCh1.Text = param.Source[1];
            LabAxisCh2.Text = param.Source[2];
            LabAxisCh3.Text = param.Source[3];
            LabAxisCh4.Text = param.Source[4];
            LabAxisCh5.Text = param.Source[5];

            LabMeter0.Text = param.MeterLabel[0];
            LabMeter1.Text = param.MeterLabel[1];
            LabMeter2.Text = param.MeterLabel[2];

            light.amplitude = param.LightAmplitude;
            light.frequency = param.LightFrequency;

            LabLight.Text = param.LightFrequency.ToString();

            sensor.potLower[0] = param.PotentioLower;
            sensor.potUpper[0] = param.PotentioUpper;

            for (int i = 0, j = 0; i < NUM_SOURCE; i++)
            {
                string src = param.Source[i];
                if (src.Length == 0 || src.Length > 2)
                    source_select[i] = SOURCE_INVALID;
                else
                {
                    string src_num = src.Substring(0, 1);
                    source_select[i] = (src_num[0] == 'L' || src_num[0] == 'l') ? SOURCE_LIGHT : int.Parse(src_num);
                    if(src.Length == 2 && j < NUM_METER) meter_select[j++] = i;
                }
            }
        }

        private void BtnSaveParam_Click(object sender, RoutedEventArgs e)
        {
            // パラメータ書き込み ------------------------------------------------------------

            param.MotorComPort1 = motor[0].comPort;
            param.MotorComPort2 = motor[1].comPort;
            param.SensorComPort = sensor.comPort;
            param.ControllerComPort = controller.comPort;

            // set camera id
            param.UsbCameraId[0] = camera[0].id;
            param.UsbCameraId[1] = camera[1].id;

            param.MotorFbMode[0] = BtnFbMode0.Enabled;
            param.MotorFbMode[1] = BtnFbMode1.Enabled;
            param.MotorFbMode[2] = BtnFbMode2.Enabled;
            param.MotorFbMode[3] = BtnFbMode3.Enabled;
            param.MotorFbMode[4] = BtnFbMode4.Enabled;
            param.MotorFbMode[5] = BtnFbMode5.Enabled;

            param.MotorDirection[0] = BtnJ0.Enabled;
            param.MotorDirection[1] = BtnJ1.Enabled;
            param.MotorDirection[2] = BtnJ2.Enabled;
            param.MotorDirection[3] = BtnJ3.Enabled;
            param.MotorDirection[4] = BtnJ4.Enabled;
            param.MotorDirection[5] = BtnJ5.Enabled;

            param.EncoderDirection[0] = BtnP0.Enabled;
            param.EncoderDirection[1] = BtnP1.Enabled;
            param.EncoderDirection[2] = BtnP2.Enabled;
            param.EncoderDirection[3] = BtnP3.Enabled;
            param.EncoderDirection[4] = BtnP4.Enabled;
            param.EncoderDirection[5] = BtnP5.Enabled;

            Param.Save(param);
        }

        private void BtnPosReset_Click(object sender, RoutedEventArgs e)
        {
            if (mpos1 != null) mpos1.Reset();
            if (mpos2 != null) mpos2.Reset();
            if (mpos3 != null) mpos3.Reset();
        }

        private void OnMotorChanged()
        {
            motor[0].mot_pol[0] = BtnJ0.Enabled ? 1 : 0;
            motor[0].mot_pol[1] = BtnJ1.Enabled ? 1 : 0;
            motor[0].mot_pol[2] = BtnJ2.Enabled ? 1 : 0;
            motor[1].mot_pol[0] = BtnJ3.Enabled ? 1 : 0;
            motor[1].mot_pol[1] = BtnJ4.Enabled ? 1 : 0;
            motor[1].mot_pol[2] = BtnJ5.Enabled ? 1 : 0;

            motor[0].enc_pol[0] = BtnP0.Enabled ? 1 : 0;
            motor[0].enc_pol[1] = BtnP1.Enabled ? 1 : 0;
            motor[0].enc_pol[2] = BtnP2.Enabled ? 1 : 0;
            motor[1].enc_pol[0] = BtnP3.Enabled ? 1 : 0;
            motor[1].enc_pol[1] = BtnP4.Enabled ? 1 : 0;
            motor[1].enc_pol[2] = BtnP5.Enabled ? 1 : 0;

            motor[0].SetMode(0, BtnFbMode0.Enabled ? 1 : 0);
            motor[0].SetMode(1, BtnFbMode1.Enabled ? 1 : 0);
            motor[0].SetMode(2, BtnFbMode2.Enabled ? 1 : 0);
            motor[1].SetMode(0, BtnFbMode3.Enabled ? 1 : 0);
            motor[1].SetMode(1, BtnFbMode4.Enabled ? 1 : 0);
            motor[1].SetMode(2, BtnFbMode5.Enabled ? 1 : 0);
        }

        private bool Motor1Enable()
        {
            motor[0].Init();    // connect serial
            return motor[0].Enable();
        }
        private bool Motor1Disable()
        {
            if (!motor[0].Disable()) return false;
            motor[0].Close();    // disconnect serial
            return true;
        }

        private bool Motor2Enable()
        {
            motor[1].Init();    // connect serial
            return motor[1].Enable();
        }
        private bool Motor2Disable()
        {
            if (!motor[1].Disable()) return false;
            motor[1].Close();    // disconnect serial
            return true;
        }

        private bool SensorEnable()
        {
            sensor.Init();    // connect serial
            return sensor.Enable();
        }
        private bool SensorDisable()
        {
            if (!sensor.Disable()) return false;
            sensor.Close();    // disconnect serial
            return true;
        }

        private bool Camera1Start() { return camera[0].Init(); }
        private bool Camera1Stop() { return camera[0].Close(); }
        private bool Camera2Start() { return camera[1].Init(); }
        private bool Camera2Stop() { return camera[1].Close(); }

        //private bool ImuStart(){ return imu.Start(); }
        //private bool ImuStop() { return imu.Stop(); }
        private bool JoystickStart() { return joystick.Init(); }
        private bool JoystickStop () { return joystick.Close(); }

        private bool ControllerStart(){ return controller.Init(); }
        private bool ControllerStop (){ return controller.Close(); }

        private bool GameRegister() { bool x = ranking.GameRegister(); if (x) { PlayerName.Text = ranking.GetPlayerNameText(); return x; } else return false; }
        private bool GameStart() { return ranking.GameStart();  }
        private bool GameStop() { PlayerName.Text = ranking.GetPlayerNameText(); if (ranking.GameStop()) BtnRegister.Enabled = false; return true; }
        private bool GameStartPause() { return ranking.GameStartPause(); }
        private bool GameStopPause() { return ranking.GameStopPause(); }

        private void OnDispRankChanged()
        {
            PanelRanking.Visibility = BtnDispRank.Enabled ? Visibility.Visible : Visibility.Collapsed;
            PanelPlayer.Visibility = BtnDispRank.Enabled ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnDispAzimChanged()
        {
            PanelAzimuth.Visibility = BtnDispAzim.Enabled ? Visibility.Visible : Visibility.Collapsed;
        }

        private int CollapseMode = 0;

        private void Window_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CollapseMode = (CollapseMode + 1) % 3;
            switch (CollapseMode)
            {
                case 0:
                    PanelFunc.Visibility = Visibility.Visible;
                    PanelEnable.Visibility = Visibility.Visible;
                    PanelMotor.Visibility = Visibility.Visible;
                    PanelMeter.Visibility = Visibility.Visible;
                    break;
                case 1:
                    PanelFunc.Visibility = Visibility.Collapsed;
                    PanelEnable.Visibility = Visibility.Collapsed;
                    PanelMotor.Visibility = Visibility.Collapsed;
                    break;
                case 2:
                    PanelMeter.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        //public void AutoStart(bool IsStartEvent) { Dispatcher.BeginInvoke((Action)(() => { if (BtnAutoStart.Enabled) BtnStart.Enabled = IsStartEvent; })); }

        // ------------------------------------------------------------

        private void SetMeterLimitsSigned(int MeterIndex, bool LowerEnable, float Lower, bool UpperEnable, float Upper)
        {
            MeterControl ctrl = meters.meter_controls[MeterIndex];

            if (LowerEnable && UpperEnable)
            {
                if(ctrl.NumThresholds != 3)
                {
                    ctrl.NumThresholds = 3;
                    Brush sbr = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0x44, 0x44));
                    ctrl.SetThresholdScaleBrush(0, sbr);
                    ctrl.SetThresholdScaleBrush(1, sbr);
                    ctrl.SetThresholdScaleBrush(2, sbr);
                    Brush obr1 = new SolidColorBrush(Color.FromArgb(0x88, 0xff, 0x44, 0xff));
                    ctrl.SetThresholdBrush(0, obr1);
                    Brush obr2 = new SolidColorBrush(Color.FromArgb(0x88, 0xff, 0x44, 0x44));
                    ctrl.SetThresholdBrush(3, obr2);
                    Brush normal = new SolidColorBrush(Color.FromArgb(0x44, 0xff, 0xff, 0xff));
                    ctrl.SetThresholdBrush(2, normal);
                    Brush minus = new SolidColorBrush(Color.FromArgb(0x99, 0x44, 0x44, 0xff));
                    ctrl.SetThresholdBrush(1, minus);
                }
                ctrl.SetThreshold(0, Lower);
                ctrl.SetThreshold(1, 0);
                ctrl.SetThreshold(2, Upper);
            }
            else
            {
                throw new NotImplementedException();
            }

            ctrl.RedrawScales();
        }

        private void SetMeterLimitsUnsigned(int MeterIndex, bool LowerEnable, float Lower, bool UpperEnable, float Upper)
        {
            MeterControl ctrl = meters.meter_controls[MeterIndex];

            if (LowerEnable && UpperEnable)
            {
                if (ctrl.NumThresholds != 2)
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
            }
            else if (LowerEnable || UpperEnable)
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
    }
}
