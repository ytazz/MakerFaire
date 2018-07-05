using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MeterDisplay
{
    /// <summary>
    /// Interaction logic for BoeingMeter.xaml
    /// </summary>
    public partial class BoeingMeter : UserControl
    {
        public BoeingMeter()
        {
            InitializeComponent();

            PathGeometry path = arc_path_fill.Data as PathGeometry;
            arc_fill_seg = path.Figures.ElementAt(0).Segments.ElementAt(1) as ArcSegment;

            path = arc_path_index.Data as PathGeometry;
            meter_index_seg = path.Figures.ElementAt(0).Segments.ElementAt(0) as LineSegment;

            UpdateScale();

            SizeChanged += delegate {
                double x_ratio = RenderSize.Width / org_fig_size_x;
                double y_ratio = RenderSize.Height / org_fig_size_y;
                Matrix mat = new Matrix();
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
                canvas_transform.Matrix = mat;
            };

            // set initial value
            prev_ratio = -1; // in order to draw at the 1st time, prev_ratio should not be less than -update_threshold
            meter_val = 0;
            meter_max = 100;
        }

        const int margin = 5;
        const int org_fig_size_x = 2 * margin + 155 + 190;
        const int org_fig_size_y = 2 * margin + 85 + 15 + 155 + 5;
        const int meter_radius = 155;
        const double update_threshold = 0.005;   // do not update the differences less than 0.5%

        ArcSegment arc_fill_seg;
        LineSegment meter_index_seg;
        private float meter_val;
        private float meter_max;
        private double prev_ratio;

        private void CircularCoordArc(ArcSegment arc, double ratio, double radius)
        {
            double angle = CircularConverter.CalcScaleAngle(ratio);
            arc.Point = CircularConverter.CircularCoord(angle, radius);
            arc.IsLargeArc = (angle > System.Math.PI);
        }
        private void CircularCoordLine(LineSegment line, double ratio, double radius)
        {
            double angle = CircularConverter.CalcScaleAngle(ratio);
            line.Point = CircularConverter.CircularCoord(angle, radius);
        }
        private void UpdateMeter()
        {
            double ratio = meter_val / meter_max;
            // do not update the differences less than the specified threshold
            if (System.Math.Abs(ratio - prev_ratio) >= update_threshold)
            {
                CircularCoordArc(arc_fill_seg, ratio, meter_radius);
                CircularCoordLine(meter_index_seg, ratio, meter_radius + 5);
                meter_label.Content = String.Format(ValueFormat, meter_val);
                prev_ratio = ratio;
            }
        }
        private void UpdateScale()
        {
            PathGeometry path = arc_path_scale.Data as PathGeometry;
            ArcSegment arc_seg = path.Figures.ElementAt(0).Segments.ElementAt(1) as ArcSegment;
            CircularCoordArc(arc_seg, 1.0, meter_radius);
            LineSegment line_seg = path.Figures.ElementAt(0).Segments.ElementAt(2) as LineSegment;
            CircularCoordLine(line_seg, 1.0, meter_radius - 55);
        }

        public float MeterValue 
        {
            get { return meter_val; }
            set { meter_val = value; UpdateMeter(); }
        }
        public float MeterMax
        {
            get { return meter_max; }
            set { meter_max = value; /* UpdateMeter(); */ }
        }
        public string ValueFormat { get; set; }
        public Brush FillBrush
        {
            get { return arc_path_fill.Fill; }
            set { arc_path_fill.Fill = value; }
        }

        // ------------------------------------------------------------

        System.Collections.ArrayList AdditionalList = new System.Collections.ArrayList();

        public void AddScale(float value, Brush stroke, float begin_radius, float end_radius)
        {
            double ratio = value / meter_max;
            double angle = CircularConverter.CalcScaleAngle(ratio);
            Point p1 = CircularConverter.CircularCoord(angle, begin_radius * meter_radius);
            Point p2 = CircularConverter.CircularCoord(angle, end_radius * meter_radius);
            Line line = new Line();
            line.Stroke = stroke;
            line.StrokeThickness = 6;
            line.Margin = arc_path_index.Margin;
            line.HorizontalAlignment = arc_path_index.HorizontalAlignment;
            line.VerticalAlignment = arc_path_index.VerticalAlignment;
            line.X1 = p1.X;
            line.Y1 = p1.Y;
            line.X2 = p2.X;
            line.Y2 = p2.Y;
            main_canvas.Children.Add(line);
            AdditionalList.Add(line);
        }
        public void RemoveScales()
        {
            foreach (Object obj in AdditionalList)
                main_canvas.Children.Remove(obj as System.Windows.UIElement);
            AdditionalList.Clear();
        }
    }

    [ValueConversion(typeof(Point), typeof(string))]
    public class CircularConverter : IValueConverter
    {
        const int center_x = 155;
        const int center_y = 100;
        const double meter_rate = 0.6116;

        static public double CalcScaleAngle(double ratio)
        {
            return meter_rate * 2.0 * System.Math.PI * ratio;
        }
        static public Point CircularCoord(double angle, double radius)
        {
            return new Point
            {
                X = center_x + radius * System.Math.Cos(angle),
                Y = center_y + radius * System.Math.Sin(angle)
            };
        }

        public object Convert(object value, System.Type targetType,
          object parameter, System.Globalization.CultureInfo culture)
        {
            Point p = (Point)value;
            double angle = CalcScaleAngle(p.X);
            return CircularCoord(angle, p.Y);
        }

        public object ConvertBack(object value, System.Type targetType,
          object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
