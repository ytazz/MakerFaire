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

        public LogWindow     log;
        public Joystick      joystick;
        public Controller    controller;
        public CameraUsb     camera;
        public MotorDriver   motor;
        //public CourseControl course;
        
        public MainWindow()
        {
            InitializeComponent();

            log        = new LogWindow();            
            motor      = new MotorDriver();
            joystick   = new Joystick();
            controller = new Controller();

            //course     = new CourseControl();
            //course.SetLog(log);
            //course.AutoStartHandler = AutoStart;

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
            // 左列：出力プラス、右列：出力マイナス
            meters.UpdateDisplayLayout(
                new MeasureObj[] {
                    new MeasurePercent(new GetRealValue(delegate() {return (motor.outX < 0) ? 0 : (float)Math.Min(motor.outX / param.MotorGainX,  1.0); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (motor.outX > 0) ? 0 : (float)Math.Max(motor.outX / param.MotorGainX, -1.0); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (motor.outY < 0) ? 0 : (float)Math.Min(motor.outY / param.MotorGainY,  1.0); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (motor.outY > 0) ? 0 : (float)Math.Max(motor.outY / param.MotorGainY, -1.0); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (motor.outZ < 0) ? 0 : (float)Math.Min(motor.outZ / param.MotorGainZ,  1.0); })),
                    new MeasurePercent(new GetRealValue(delegate() {return (motor.outZ > 0) ? 0 : (float)Math.Max(motor.outZ / param.MotorGainZ, -1.0); })) }
                );
            string[] titles = new string[] { "+ X", "- X", "+ Y", "- Y", "+ Z", "- Z" };
            //new MeasurePercent(new GetRealValue(delegate () { return (MotorOutW > 0) ? (float)MotorOutW : 0; })),

            for (int i = 0; i < 6; i++)
            {
                meters.meter_controls[i].ThresholdScaleLen = 0.0f;  // スケールは表示しない
                SetMeterLimits(i, false, 0, true, 95f);
            }

            for (int i = 0; i < 6; i++)
                meters.labels[i].Content = titles[i];

            param = Param.Load();

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

            joystick.Init();
            controller.Init();

            DispatcherTimer dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, param.UpdateInterval);  // in milliseconds
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Start();
        }

        // ------------------------------------------------------------

        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            controller.Update();
            
            if (BtnLight.Enabled)
            {
                motor.UpdateLight();
            }

            //course.Update();

            joystick.Update();

            // ------------------------------------------------------------
            // 疑似操作卓の状態の取得

            if (BtnHalt.Enabled)
            {
                motor.Halt();
            }
            else
            {
                motor.outX = param.MotorGainX * joystick.outX;
                motor.outY = param.MotorGainY * joystick.outY;
                motor.outZ = param.MotorGainZ * joystick.outZ;

                motor.Update();
            }
            
            meters.Update(false);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            motor.Close();

            log.CloseAsHide = false;
            log.Close();

            //camera.Close();

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

        private bool MotorStart() { return motor.Start(); }
        private bool MotorStop() { return motor.Stop(); }

        //private bool CameraStart() { return camera.Start(); }
        //private bool CameraStop() { return camera.Stop(); }

        //private bool ImuStart(){ return imu.Start(); }
        //private bool ImuStop() { return imu.Stop(); }
        //private bool JoystickStart() { return joystick.Start(); }
        //private bool JoystickStop () { return joystick.Stop(); }

        private bool ControllerStart(){ return controller.Start(); }
        private bool ControllerStop (){ return controller.Stop(); }

        //private void BtnRegister_Click(object sender, RoutedEventArgs e) { Course.GameRegister(); }
        //private bool GameRegister() { return course.GameRegister(); }
        //private bool DisplayStart() { return Course.DisplayStart(param.DisplayIp, param.DisplayPort); }
        //private bool DisplayStop() { return Course.DisplayStop(); }
        //private bool GameStart() { return Course.GameStart(); }
        //private bool GameStop() { if (Course.GameStop()) BtnRegister.Enabled = false; return true; }
        //private bool GameStartPause() { return Course.GameStartPause(); }
        //private bool GameStopPause() { return Course.GameStopPause(); }
        //public bool Contact1Start() { return Course.Contact1Start(param.Contact1Ip); }
        //public bool Contact2Start() { return Course.Contact2Start(param.Contact2Ip); }
        //public bool Contact3Start() { return Course.Contact3Start(param.Contact3Ip); }
        //public bool Contact1Stop() { return Course.Contact1Stop(); }
        //public bool Contact2Stop() { return Course.Contact2Stop(); }
        //public bool Contact3Stop() { return Course.Contact3Stop(); }
        //public void AutoStart(bool IsStartEvent) { Dispatcher.BeginInvoke((Action)(() => { if (BtnAutoStart.Enabled) BtnStart.Enabled = IsStartEvent; })); }

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

    }
}
