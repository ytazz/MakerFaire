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
        public MotorDriver    motor;
        public Light          light;
        public RankingControl ranking;

        public MainWindow()
        {
            InitializeComponent();

            log        = new LogWindow();            
            motor      = new MotorDriver();
            joystick   = new Joystick();
            controller = new Controller();
            light      = new Light();
            camera     = new CameraUsb[2];
            camera[0]  = new CameraUsb();
            camera[1]  = new CameraUsb();

            ranking = new RankingControl();
            ranking.SetLog(log);

            // パラメータ読み込み ------------------------------------------------------------

            param = Param.Load();

            // set camera id
            camera[0].id = param.UsbCameraId1;
            camera[1].id = param.UsbCameraId2;

            motor.comPort = param.MotorComPort;
            controller.comPort = param.ControllerComPort;

            BtnFbMode1.Enabled = param.MotorFbMode1;
            BtnFbMode2.Enabled = param.MotorFbMode2;
            BtnFbMode3.Enabled = param.MotorFbMode3;

            //MotorIp.Text = param.MotorIp + ":" + param.MotorPort;
            //CameraIp.Text = param.CameraIp + ":" + param.CameraPort;
            //ControllerIp.Text = param.ControllerIp;
            //DisplayIp.Text = param.DisplayIp + ":" + param.DisplayPort;
            //Contact1.Text = param.Contact1Ip;
            //Contact2.Text = param.Contact2Ip;
            //Contact3.Text = param.Contact3Ip;

            //LowerX.Text = param.EnableLimitLowerX ? param.LimitLowerX.ToString() : "disabled";
            //LowerY.Text = param.EnableLimitLowerY ? param.LimitLowerY.ToString() : "disabled";
            //LowerZ.Text = param.EnableLimitLowerZ ? param.LimitLowerZ.ToString() : "disabled";
            //UpperX.Text = param.EnableLimitUpperX ? param.LimitUpperX.ToString() : "disabled";
            //UpperY.Text = param.EnableLimitUpperY ? param.LimitUpperY.ToString() : "disabled";
            //UpperZ.Text = param.EnableLimitUpperZ ? param.LimitUpperZ.ToString() : "disabled";

            //MotorGainX.Text = param.MotorGainX.ToString();
            //MotorGainY.Text = param.MotorGainY.ToString();
            //MotorGainZ.Text = param.MotorGainZ.ToString();

            //param.DirectionX = (bool)directionX.IsChecked;
            //param.DirectionY = (bool)directionY.IsChecked;
            //param.DirectionZ = (bool)directionZ.IsChecked;

            //UpdateInterval.Text = param.UpdateInterval.ToString();
            //LightInterval.Text = param.LightUpdateInterval.ToString();
            //SetLightInterval();

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
            meters.Background.Opacity = 0.5;

            meters.UpdateDisplayLayout(
                new MeasureObj[] {
                    new MeasurePercent(new GetRealValue(delegate() {return (float)((motor.mode[0] == 0) ? motor.pwm_ref[0] : motor.pwm[0]) / (float)(motor.pwmMax); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)((motor.mode[1] == 0) ? motor.pwm_ref[1] : motor.pwm[1]) / (float)(motor.pwmMax); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)((motor.mode[2] == 0) ? motor.pwm_ref[2] : motor.pwm[2]) / (float)(motor.pwmMax); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)(motor.vel_ref[0]) / (float)(motor.velMax); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)(motor.vel_ref[1]) / (float)(motor.velMax); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)(motor.vel_ref[2]) / (float)(motor.velMax); })),
                    new MeasurePos(new GetRealValue(delegate() {return (pos_max[0] == pos_min[0]) ? 0 : (float)(motor.pos[0] - pos_min[0]) / (float)(pos_max[0] - pos_min[0]); })),
                    new MeasurePos(new GetRealValue(delegate() {return (pos_max[1] == pos_min[1]) ? 0 : (float)(motor.pos[1] - pos_min[1]) / (float)(pos_max[1] - pos_min[1]); })),
                    new MeasurePos(new GetRealValue(delegate() {return (pos_max[2] == pos_min[2]) ? 0 : (float)(motor.pos[2] - pos_min[2]) / (float)(pos_max[2] - pos_min[2]); })),
                });
            
            for (int i = 0; i < meters.meter_controls.Length; i++)
            {
                if(meters.meter_controls[i] != null)
                {
                    meters.meter_controls[i].ThresholdScaleLen = 0.0f;  // スケールは表示しない
                    if (i < 6)
                        SetMeterLimitsSigned(i, true, -85f, true, 85f);
                    else
                        SetMeterLimitsUnsigned(i, true, 10f, true, 90f);
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

            // インターバルタイマ
            DispatcherTimer dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, param.UpdateInterval);  // in milliseconds
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Start();
        }

        // ------------------------------------------------------------

        private int freqdiv_count = 0;
        private double[] axis = new double[3];
        private int[] pos_max = new int[3] { 0, 0, 0 };
        private int[] pos_min = new int[3] { 1 << 15, 1 << 15, 1 << 15 };

        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            double dt = 0.001 * param.UpdateInterval;

            if(BtnLight.Enabled) light.Update(dt);

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

            for(int i = 0; i < 3; i++)
            {
                if (motor.mode[i] == 0)
                    motor.pwm_ref[i] = (int)(motor.pwmMax * axis[i]);
                else
                    motor.vel_ref[i] = (int)(motor.velMax * axis[i]);

                if (pos_max[i] < motor.pos[i]) pos_max[i] = motor.pos[i];
                if (pos_min[i] > motor.pos[i]) pos_min[i] = motor.pos[i];
            }

            motor.Update(dt);
            
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

                TextPos0.Text = motor.pos[0].ToString();
                TextPos1.Text = motor.pos[1].ToString();
                TextPos2.Text = motor.pos[2].ToString();

                freqdiv_count = 5;
            }

            freqdiv_count--;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            motor.Close();

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

        private bool FbModeChanged()
        {
            motor.mode[0] = BtnFbMode1.Enabled ? 1 : 0;
            motor.mode[1] = BtnFbMode2.Enabled ? 1 : 0;
            motor.mode[2] = BtnFbMode3.Enabled ? 1 : 0;
            return true;
        }

        private bool DummyEnableHandler(){ return true; }

        private bool MotorEnable () { return motor.Enable (); }
        private bool MotorDisable() { return motor.Disable(); }

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
