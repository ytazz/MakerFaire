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

namespace CraneMonitor
{
    /// <summary>
    /// Interaction logic for AzimuthMeter.xaml
    /// </summary>
    public partial class AzimuthMeter : UserControl
    {
        private const double radius = 200;
        private double org_fig_size_x = 0;
        private double org_fig_size_y = 0;
        private int div_phi = 180;

        private int meter_val = 0;

        public int MeterValue
        {
            get { return meter_val; }
            set { meter_val = value; meter_label.Content = meter_val; canvas_transform.Matrix = CalcTransMat(); }
        }

        private Matrix CalcTransMat()
        {
            org_fig_size_x = this.main_canvas.Width;
            org_fig_size_y = this.main_canvas.Height;
            double x_ratio = RenderSize.Width / org_fig_size_x;
            double y_ratio = RenderSize.Height / org_fig_size_y;
            Matrix mat_total = new Matrix();
            Matrix mat = new Matrix();
            mat.Translate(-org_fig_size_x / 2, -org_fig_size_y / 2);
            mat_total.Append(mat);
            mat = new Matrix();
            mat.Rotate(meter_val);
            mat_total.Append(mat);
            mat = new Matrix();
            mat.Translate(org_fig_size_x / 2, org_fig_size_y / 2);
            mat_total.Append(mat);
            mat = new Matrix();
            if (y_ratio > x_ratio)
            {
                mat.Scale(x_ratio, x_ratio);
                switch (VerticalContentAlignment)
                {
                    case VerticalAlignment.Top:
                        mat.Translate(0, 0);
                        break;
                    case VerticalAlignment.Bottom:
                        mat.Translate(0, RenderSize.Height - x_ratio * org_fig_size_y);
                        break;
                    case VerticalAlignment.Center:
                    default:
                        mat.Translate(0, (RenderSize.Height - x_ratio * org_fig_size_y) / 2);
                        break;
                }
            }
            else
            {
                mat.Scale(y_ratio, y_ratio);
                switch (HorizontalContentAlignment)
                {
                    case HorizontalAlignment.Left:
                        mat.Translate(0, 0);
                        break;
                    case HorizontalAlignment.Right:
                        mat.Translate(RenderSize.Width - y_ratio * org_fig_size_x, 0);
                        break;
                    case HorizontalAlignment.Center:
                    default:
                        mat.Translate((RenderSize.Width - y_ratio * org_fig_size_x) / 2, 0);
                        break;
                }
            }
            mat_total.Append(mat);
            return mat_total;
        }

        public AzimuthMeter()
        {
            InitializeComponent();

            Render();

            SizeChanged += delegate {
                canvas_transform.Matrix = CalcTransMat();
            };

        }

        public void Render()
        {
            main_canvas.Children.Clear();

            for (int i = 0; i < div_phi; i++)
            {
                bool scale = i % (div_phi / 4) == 0;
                double ratio1 = scale ? 1.0 : 0.98;
                double ratio2 = scale ? 0.8 : 0.85;
                double thick = scale ? 6 : 2;
                // Add a Line Element
                Line line = new Line();
                double angle = Math.PI * 2 * i / div_phi - Math.PI / 2;
                Point r1 = Pol2Cart(radius * ratio1, angle);
                line.X1 = r1.X;
                line.Y1 = r1.Y;
                Point r2 = Pol2Cart(radius * ratio2, angle);
                line.X2 = r2.X;
                line.Y2 = r2.Y;
                line.Stroke = (i != 0) ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Red;
                line.StrokeThickness = thick;
                line.HorizontalAlignment = HorizontalAlignment.Center;
                line.VerticalAlignment = VerticalAlignment.Center;
                //Canvas.SetLeft(line, this.main_canvas.Width / 2);
                //Canvas.SetTop(line, this.main_canvas.Height / 2);
                main_canvas.Children.Add(line);
            }

        }

        private Point Pol2Cart(double radius, double angle)
        {
            return new Point(this.main_canvas.Width / 2 + radius * Math.Cos(angle), this.main_canvas.Height / 2 + radius * Math.Sin(angle));
        }

    }
}
