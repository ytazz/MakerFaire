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
using System.Windows.Shapes;

namespace JoystickApp
{
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : Window
    {
        public LogWindow()
        {
            InitializeComponent();
            MsgClear();
        }

        // --------------------------------------------------

        public bool CloseAsHide = true;
        public enum MsgTypes { Normal, Error, Warning };

        public void MsgWriteLine(MsgTypes type, string format, params object[] args)
        {
            string line = String.Format(format, args);
            var r = new Run() { Text = line, FontSize = 12 };
            switch (type)
            {
                case MsgTypes.Normal:
                    break;
                case MsgTypes.Error:
                    r.Background = Brushes.Red;
                    r.Foreground = Brushes.White;
                    break;
                case MsgTypes.Warning:
                    r.Background = Brushes.Yellow;
                    r.Foreground = Brushes.Black;
                    break;
            }

            //text.Inlines.Add(r);
            //text.Inlines.Add(new LineBreak());

            //Paragraph p = new Paragraph();
            //p.Inlines.Add(r);
            Paragraph p = text.Document.Blocks.FirstBlock as Paragraph;
            p.Inlines.Add(r);
            p.Inlines.Add(new LineBreak());
        }

        public void MsgClear()
        {
            //text.Inlines.Clear();
            Paragraph p = text.Document.Blocks.FirstBlock as Paragraph;
            p.Inlines.Clear();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (CloseAsHide)
            {
                e.Cancel = true;
                Visibility = Visibility.Hidden;
            }
        }

        // For operations from the other threads

        public void WriteLogNormal(object sender, string message)
        {
            Dispatcher.BeginInvoke((Action)(() => { MsgWriteLine(MsgTypes.Normal, message); }));
        }
        public void WriteLogError(object sender, string message)
        {
            Dispatcher.BeginInvoke((Action)(() => { MsgWriteLine(MsgTypes.Error, message); }));
        }
        public void WriteLogWarning(object sender, string message)
        {
            Dispatcher.BeginInvoke((Action)(() => { MsgWriteLine(MsgTypes.Warning, message); }));
        }
    }
}
