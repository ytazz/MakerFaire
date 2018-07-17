﻿using System;
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
            camera[0].id = param.UsbCameraId2;

            controller.comPort = param.ControllerComPort;

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
                    new MeasurePercent(new GetRealValue(delegate() {return (float)(motor.pwm_ref[0]) / (float)(motor.pwmMax); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)(motor.pwm_ref[1]) / (float)(motor.pwmMax); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)(motor.pwm_ref[2]) / (float)(motor.pwmMax); })),
                    null,
                    new MeasurePercent(new GetRealValue(delegate() {return (float)(motor.vel_ref[0]) / (float)(motor.velMax); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)(motor.vel_ref[1]) / (float)(motor.velMax); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)(motor.vel_ref[2]) / (float)(motor.velMax); })),
                    null,
                    new MeasurePercent(new GetRealValue(delegate() {return (float)(motor.pwm[0]) / (float)(motor.pwmMax); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)(motor.pwm[1]) / (float)(motor.pwmMax); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)(motor.pwm[2]) / (float)(motor.pwmMax); })),
                    null,
                    new MeasurePercent(new GetRealValue(delegate() {return (float)joystick.axis[0]; })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)joystick.axis[1]; })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)joystick.axis[2]; })),
                    null,
                    new MeasurePercent(new GetRealValue(delegate() {return (float)controller.axis[0]; })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)controller.axis[1]; })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)controller.axis[2]; })),
                    new MeasurePercent(new GetRealValue(delegate() {return (float)controller.axis[3]; })),
                });
            
            for (int i = 0; i < meters.meter_controls.Length; i++)
            {
                if(meters.meter_controls[i] != null)
                {
                    meters.meter_controls[i].ThresholdScaleLen = 0.0f;  // スケールは表示しない
                    SetMeterLimits(i, true, -85f, true, 85f);
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
            BtnCtrl.Enabled = true;
            
            // インターバルタイマ
            DispatcherTimer dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, param.UpdateInterval);  // in milliseconds
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Start();
        }

        // ------------------------------------------------------------

        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            double dt = 0.001 * param.UpdateInterval;
            light.Update(dt);

            if (ranking.RequestUpdateRanking) TextRanking.Text = ranking.GetRankingText();
            if (ranking.RequestUpdateStart || ranking.RequestUpdateStop) TextElapseTime.Text = ranking.GetElapseTimeText();

            joystick.Update();

            motor.vel_ref[0] = (int)(motor.velMax * joystick.axis[0]);

            //motor.pwm_ref[0] = (int)(motor.pwmMax * joystick.axis[0]);
            //motor.pwm_ref[1] = (int)(motor.pwmMax * joystick.axis[1]);
            //motor.pwm_ref[2] = (int)(motor.pwmMax * joystick.axis[2]);

            motor.Update(dt);
            
            meters.Update(false);
            TextPos0.Text = motor.pos[0].ToString();
            TextPos1.Text = motor.pos[1].ToString();
            TextPos2.Text = motor.pos[2].ToString();

#if false
            TextButton0 .Text = controller.button[ 0] ? "*" : "-";
            TextButton1 .Text = controller.button[ 1] ? "*" : "-";
            TextButton2 .Text = controller.button[ 2] ? "*" : "-";
            TextButton3 .Text = controller.button[ 3] ? "*" : "-";
            TextButton4 .Text = controller.button[ 4] ? "*" : "-";
            TextButton5 .Text = controller.button[ 5] ? "*" : "-";
            TextButton6 .Text = controller.button[ 6] ? "*" : "-";
            TextButton7 .Text = controller.button[ 7] ? "*" : "-";
            TextButton8 .Text = controller.button[ 8] ? "*" : "-";
            TextButton9 .Text = controller.button[ 9] ? "*" : "-";
            TextButton10.Text = controller.button[10] ? "*" : "-";
            TextButton11.Text = controller.button[11] ? "*" : "-";
#endif
            controller.Update();

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
            Param.Save(param);
        }

        private void BtnGetLog_Click(object sender, RoutedEventArgs e)
        {
            log.Visibility = Visibility.Visible;
        }

        private void BtnShutdown_Click(object sender, RoutedEventArgs e)
        {
            motor.Close();
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

        private void SetMeterLimits(int MeterIndex, bool LowerEnable, float Lower, bool UpperEnable, float Upper)
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
