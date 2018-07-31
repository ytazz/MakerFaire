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

            meters.Background = System.Windows.Media.Brushes.Black.Clone();
            //meters.Background.Opacity = 0.5;

            meters.UpdateDisplayLayout(
                new MeasureObj[] {
                    new MeasurePercent(new GetRealValue(delegate() {return (float)((motor[0].mode[0] == 0) ? motor[0].pwm_ref[0] : motor[0].pwm[0]) / (float)(motor[0].pwmMax); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)((motor[0].mode[1] == 0) ? motor[0].pwm_ref[1] : motor[0].pwm[1]) / (float)(motor[0].pwmMax); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)((motor[0].mode[2] == 0) ? motor[0].pwm_ref[2] : motor[0].pwm[2]) / (float)(motor[0].pwmMax); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)((motor[0].mode[0] == 0) ? vel[0] : motor[0].vel_ref[0]) / (float)(motor[0].velMax); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)((motor[0].mode[1] == 0) ? vel[1] : motor[0].vel_ref[1]) / (float)(motor[0].velMax); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)((motor[0].mode[2] == 0) ? vel[2] : motor[0].vel_ref[2]) / (float)(motor[0].velMax); })),
#if true
                    mpos1 = new MeasurePos(new GetRealValue(delegate() {return motor[0].pos[0]; })),
                    mpos2 = new MeasurePos(new GetRealValue(delegate() {return motor[0].pos[1]; })),
                    mpos3 = new MeasurePos(new GetRealValue(delegate() {return motor[0].pos[2]; })),
#else   // for debug
                    mpos1 = new MeasurePos(new GetRealValue(delegate() {return (float)controller.axis[0]; })),
                    mpos2 = new MeasurePos(new GetRealValue(delegate() {return (float)controller.axis[1]; })),
                    mpos3 = new MeasurePos(new GetRealValue(delegate() {return (float)controller.axis[2]; })),
#endif
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
            controller.SyncButtons = new EnableButton[] { BtnStart, BtnPause, BtnRsv2, BtnMag, BtnRsv3, BtnRsv4, BtnHalt, BtnGrapL, BtnRsv1, BtnGrapR };

            // 初期自動接続
            BtnJoystick.Enabled = true;
            BtnMotor.Enabled = true;
            //BtnCtrl.Enabled = true;
            BtnLight.Enabled = true;

            motor[0].Init();
            motor[1].Init();
            sensor  .Init();

            OnMotorChanged();

            // インターバルタイマ
            DispatcherTimer dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, param.UpdateInterval);  // in milliseconds
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Start();
        }

        // ------------------------------------------------------------

        private int freqdiv_count = 0;
        private double[] axis = new double[3];
        private double[] vel = new double[3];
        private int[] prev_pos = new int[3];

        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            double dt = 0.001 * param.UpdateInterval;

            if (BtnJoystick.Enabled)
            {
                joystick.Update();
                axis[0] = joystick.axis[0];
                axis[1] = joystick.axis[1];
                axis[2] = joystick.axis[2];
            }
            else
            {
                axis[0] = controller.axis[0];
                axis[1] = controller.axis[1];
                axis[2] = controller.axis[2];
            }

            // motor[0]:
            // ch0 : axis[0]
            // ch1 : axis[1]
            // ch2 : axis[2]
            for (int j = 0; j < 3; j++)
            {
                // haltが押されていたら指令値を0
                if (BtnHalt.Enabled)
                {
                    motor[0].pwm_ref[j] = 0;
                    motor[0].vel_ref[j] = 0;
                }
                else
                {
                    if (motor[0].mode[j] == 0)
                        motor[0].pwm_ref[j] = (int)(motor[0].pwmMax * axis[j]);
                    else
                        motor[0].vel_ref[j] = (int)(motor[0].velMax * axis[j]);

                    vel[j] = (motor[0].pos[j] - prev_pos[j]) * 1000 / param.UpdateInterval;
                    prev_pos[j] = motor[0].pos[j];
                }
            }
            motor[0].Update(dt);

            // motor[1]:
            // ch0 : light
            if (BtnLight.Enabled)
            {
                light.Update(dt);
            }
            else
            {
                light.output = 0;
            }
            motor[1].mode[0] = 0;
            motor[1].mode[1] = 0;
            motor[1].mode[2] = 0;
            motor[1].pwm_ref[0] = light.output;
            motor[1].pwm_ref[1] = 0;
            motor[1].pwm_ref[2] = 0;
            motor[1].Update(dt);

            meters.Update(false);


            // カメラ動画 ------------------------------

            camera[0].Update();
            if(camera[0].bitmap != null)
            {
                image1.Source = Imaging.CreateBitmapSourceFromHBitmap(camera[0].bitmap.GetHbitmap(),
                    IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            camera[1].Update();
            if(camera[1].bitmap != null)
            {
                image2.Source = Imaging.CreateBitmapSourceFromHBitmap(camera[1].bitmap.GetHbitmap(),
                    IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }

            // 頻度の低い更新（分周比 1/5） ------------------------------

            if (freqdiv_count == 0)
            {
                controller.Update();

                if (ranking.RequestUpdateRanking) TextRanking.Text = ranking.GetRankingText();
                if (ranking.RequestUpdateStart || ranking.RequestUpdateStop) TextElapseTime.Text = ranking.GetElapseTimeText();

                freqdiv_count = 5;
            }

            freqdiv_count--;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            motor[0].Close();
            motor[1].Close();
            sensor.Close();

            log.CloseAsHide = false;
            log.Close();

            //camera.Close();
            BtnJoystick.Enabled = false;
            BtnMotor.Enabled = false;
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

            // set camera id
            camera[0].id = param.UsbCameraId[0];
            camera[1].id = param.UsbCameraId[1];

            BtnFbMode0.Enabled = param.MotorFbMode[0];
            BtnFbMode1.Enabled = param.MotorFbMode[1];
            BtnFbMode2.Enabled = param.MotorFbMode[2];

            BtnJ0.Enabled = param.MotorDirection[0];
            BtnJ1.Enabled = param.MotorDirection[1];
            BtnJ2.Enabled = param.MotorDirection[2];

            BtnP0.Enabled = param.EncoderDirection[0];
            BtnP1.Enabled = param.EncoderDirection[1];
            BtnP2.Enabled = param.EncoderDirection[2];

            light.amplitude = param.LightAmplitude;
            light.frequency = param.LightFrequency;
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

            param.MotorDirection[0] = BtnJ0.Enabled;
            param.MotorDirection[1] = BtnJ1.Enabled;
            param.MotorDirection[2] = BtnJ2.Enabled;

            param.EncoderDirection[0] = BtnP0.Enabled;
            param.EncoderDirection[1] = BtnP1.Enabled;
            param.EncoderDirection[2] = BtnP2.Enabled;

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
            motor[1].mot_pol[0] = 0;
            motor[1].mot_pol[1] = 0;
            motor[1].mot_pol[2] = 0;

            motor[0].enc_pol[0] = BtnP0.Enabled ? 1 : 0;
            motor[0].enc_pol[1] = BtnP1.Enabled ? 1 : 0;
            motor[0].enc_pol[2] = BtnP2.Enabled ? 1 : 0;
            motor[1].enc_pol[0] = 0;
            motor[1].enc_pol[1] = 0;
            motor[1].enc_pol[2] = 0;

            motor[0].SetMode(0, BtnFbMode0.Enabled ? 1 : 0);
            motor[0].SetMode(1, BtnFbMode1.Enabled ? 1 : 0);
            motor[0].SetMode(2, BtnFbMode2.Enabled ? 1 : 0);
            motor[1].SetMode(0, 0);
            motor[1].SetMode(1, 0);
            motor[1].SetMode(2, 0);
        }

        private bool MotorEnable () {
            return motor[0].Enable() && motor[1].Enable() && sensor.Enable();
        }
        private bool MotorDisable() {
            return motor[0].Disable() && motor[1].Disable() && sensor.Disable();
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
