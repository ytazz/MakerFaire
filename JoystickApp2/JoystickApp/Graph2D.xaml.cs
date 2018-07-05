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

namespace MeterDisplay
{
    /// <summary>
    /// Interaction logic for Graph2D.xaml
    /// </summary>
    public partial class Graph2D : UserControl
    {
        public Graph2D()
        {
            InitializeComponent();

            AxisX.AxisValue = 0;
            AxisX.AxisScale = 2.2;
            AxisX.AxisPosition = 0.5;
            AxisX.ScaleSmallInterval = 0.1;
            AxisX.ScaleLargeInterval = 5;   // 5 means 0.5 (=5 * ScaleSmallInterval)

            AxisY.AxisValue = 0;
            AxisY.AxisScale = 2.2;
            AxisY.AxisPosition = 0.5;
            AxisY.ScaleSmallInterval = 0.1;
            AxisY.ScaleLargeInterval = 5;   // 5 means 0.5 (=5 * ScaleSmallInterval)
            AxisY.Direction = false;

            UpdateAxis();
        }

        double org_fig_size_x = 1;
        double org_fig_size_y = 1;

        public double FigureWidth
        {
            set { main_canvas.Width = value; }
            get { return main_canvas.Width; }
        }
        public double FigureHeight
        {
            set { main_canvas.Height = value; }
            get { return main_canvas.Height; }
        }
        public int NumCursors { get; set; } = 1;
        public Brush[] CursorBrushList = null;

        public void UpdateAxis()
        {
            DrawAxis();
            DrawCursors();

            org_fig_size_x = main_canvas.Width;
            org_fig_size_y = main_canvas.Height;
        }
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double x_ratio = RenderSize.Width / org_fig_size_x;
            double y_ratio = RenderSize.Height / org_fig_size_y;
            Matrix mat = new Matrix();
            if (y_ratio > x_ratio)
            {
                double ratio = 0;
                switch (ResizeMode)
                {
                    case ResizeModeType.SquareSmaller:
                        mat.Scale(x_ratio, x_ratio);
                        ratio = x_ratio;
                        break;
                    case ResizeModeType.SquareLarger:
                        mat.Scale(y_ratio, y_ratio);
                        ratio = y_ratio;
                        break;
                    case ResizeModeType.RectangleStretch:
                        mat.Scale(x_ratio, y_ratio);
                        ratio = y_ratio;
                        break;
                }
                switch (VerticalContentAlignment)
                {
                    case VerticalAlignment.Top:
                        mat.Translate(0, 0);
                        break;
                    case VerticalAlignment.Bottom:
                        mat.Translate(0, RenderSize.Height - ratio * org_fig_size_y);
                        break;
                    case VerticalAlignment.Center:
                    default:
                        mat.Translate(0, (RenderSize.Height - ratio * org_fig_size_y) / 2);
                        break;
                }
            }
            else
            {
                double ratio = 0;
                switch (ResizeMode)
                {
                    case ResizeModeType.SquareSmaller:
                        mat.Scale(y_ratio, y_ratio);
                        ratio = y_ratio;
                        break;
                    case ResizeModeType.SquareLarger:
                        mat.Scale(x_ratio, x_ratio);
                        ratio = x_ratio;
                        break;
                    case ResizeModeType.RectangleStretch:
                        mat.Scale(x_ratio, y_ratio);
                        ratio = x_ratio;
                        break;
                }
                switch (HorizontalContentAlignment)
                {
                    case HorizontalAlignment.Left:
                        mat.Translate(0, 0);
                        break;
                    case HorizontalAlignment.Right:
                        mat.Translate(RenderSize.Width - ratio * org_fig_size_x, 0);
                        break;
                    case HorizontalAlignment.Center:
                    default:
                        mat.Translate((RenderSize.Width - ratio * org_fig_size_x) / 2, 0);
                        break;
                }
            }
            canvas_transform.Matrix = mat;
        }

        public enum ResizeModeType { RectangleStretch, SquareSmaller, SquareLarger };
        public ResizeModeType ResizeMode { get; set; } = ResizeModeType.SquareSmaller;

        public GraphAxis AxisX = new GraphAxis();
        public GraphAxis AxisY = new GraphAxis();

        Point GraphCoords(Point p)
        {
            return new Point(AxisX.TransformToDrawCoord(p.X), AxisY.TransformToDrawCoord(p.Y));
        }

        Point InvGraphCoords(Point p)
        {
            return new Point(AxisX.TransformFromDrawCoord(p.X), AxisY.TransformFromDrawCoord(p.Y));
        }

        System.Collections.ArrayList AdditionalList = new System.Collections.ArrayList();

        public void RemoveScales()
        {
            foreach (Object obj in AdditionalList)
                main_canvas.Children.Remove(obj as System.Windows.UIElement);
            AdditionalList.Clear();
        }

        public Brush BrushAxis { get; set; } = System.Windows.Media.Brushes.LightGreen;
        public Brush BrushScaleSmall { get; set; } = System.Windows.Media.Brushes.Green;
        public Brush BrushScaleLarge { get; set; } = System.Windows.Media.Brushes.Green;
        public Brush BrushScaleText { get; set; } = System.Windows.Media.Brushes.White;
        public string AxisTitleX = "+x";
        public string AxisTitleY = "+y";

        void DrawAxis()
        {
            RemoveScales();

            AxisX.DrawSize = main_canvas.Width;
            AxisY.DrawSize = main_canvas.Height;
            AxisX.Init();
            AxisY.Init();

            Point p1 = new Point();
            Point p2 = new Point();

            // ------------------------------------------------------------

            DoubleCollection dbl = new DoubleCollection();
            dbl.Add(2);
            dbl.Add(2);
            dbl.Add(2);
            dbl.Add(2);

            if (AxisX.ScaleSmallVal != null)
            {
                for (int i = 0; i < AxisX.ScaleSmallVal.Length; i++)
                {
                    Line line = new Line();
                    line.Stroke = BrushScaleSmall;
                    line.HorizontalAlignment = HorizontalAlignment.Left;
                    line.VerticalAlignment = VerticalAlignment.Top;
                    line.StrokeThickness = 1;
                    line.StrokeDashArray = dbl;

                    p1.X = p2.X = AxisX.ScaleSmallVal[i];
                    p1.Y = AxisY.ValMin;
                    p2.Y = AxisY.ValMax;

                    Point op1 = GraphCoords(p1);
                    Point op2 = GraphCoords(p2);
                    line.X1 = op1.X;
                    line.Y1 = op1.Y;
                    line.X2 = op2.X;
                    line.Y2 = op2.Y;

                    main_canvas.Children.Add(line);
                    AdditionalList.Add(line);
                }
            }
            if (AxisY.ScaleSmallVal != null)
            {
                for (int i = 0; i < AxisY.ScaleSmallVal.Length; i++)
                {
                    Line line = new Line();
                    line.Stroke = BrushScaleSmall;
                    line.HorizontalAlignment = HorizontalAlignment.Left;
                    line.VerticalAlignment = VerticalAlignment.Top;
                    line.StrokeThickness = 1;
                    line.StrokeDashArray = dbl;

                    p1.Y = p2.Y = AxisY.ScaleSmallVal[i];
                    p1.X = AxisX.ValMin;
                    p2.X = AxisX.ValMax;

                    Point op1 = GraphCoords(p1);
                    Point op2 = GraphCoords(p2);
                    line.X1 = op1.X;
                    line.Y1 = op1.Y;
                    line.X2 = op2.X;
                    line.Y2 = op2.Y;

                    main_canvas.Children.Add(line);
                    AdditionalList.Add(line);
                }
            }

            // ------------------------------------------------------------

            if (AxisX.ScaleLargeVal != null)
            {
                for (int i = 0; i < AxisX.ScaleLargeVal.Length; i++)
                {
                    Line line = new Line();
                    line.Stroke = BrushScaleLarge;
                    line.HorizontalAlignment = HorizontalAlignment.Left;
                    line.VerticalAlignment = VerticalAlignment.Top;
                    line.StrokeThickness = 2;

                    p1.X = p2.X = AxisX.ScaleLargeVal[i];
                    p1.Y = AxisY.ValMin;
                    p2.Y = AxisY.ValMax;

                    Point op1 = GraphCoords(p1);
                    Point op2 = GraphCoords(p2);
                    line.X1 = op1.X;
                    line.Y1 = op1.Y;
                    line.X2 = op2.X;
                    line.Y2 = op2.Y;

                    main_canvas.Children.Add(line);
                    AdditionalList.Add(line);

                    AddScaleLabel(AxisLabelAlign.Center, AxisLabelAlign.TopLeft, op2.X, AxisY.TransformToDrawCoord(AxisY.AxisValue), String.Format("{0:g}", AxisX.ScaleLargeVal[i]));
                }
            }
            if (AxisY.ScaleLargeVal != null)
            {
                for (int i = 0; i < AxisY.ScaleLargeVal.Length; i++)
                {
                    Line line = new Line();
                    line.Stroke = BrushScaleLarge;
                    line.HorizontalAlignment = HorizontalAlignment.Left;
                    line.VerticalAlignment = VerticalAlignment.Top;
                    line.StrokeThickness = 2;

                    p1.Y = p2.Y = AxisY.ScaleLargeVal[i];
                    p1.X = AxisX.ValMin;
                    p2.X = AxisX.ValMax;

                    Point op1 = GraphCoords(p1);
                    Point op2 = GraphCoords(p2);
                    line.X1 = op1.X;
                    line.Y1 = op1.Y;
                    line.X2 = op2.X;
                    line.Y2 = op2.Y;

                    main_canvas.Children.Add(line);
                    AdditionalList.Add(line);

                    AddScaleLabel(AxisLabelAlign.BottomRight, AxisLabelAlign.Center, AxisX.TransformToDrawCoord(AxisX.AxisValue), op2.Y, String.Format("{0:g}", AxisY.ScaleLargeVal[i]));
                }
            }

            // ------------------------------------------------------------

            for (int i = 0; i < 2; i++)
            {
                Line line = new Line();
                line.Stroke = BrushAxis;
                line.HorizontalAlignment = HorizontalAlignment.Left;
                line.VerticalAlignment = VerticalAlignment.Top;
                line.StrokeThickness = 4;
                if (i == 0)
                {
                    p1.X = p2.X = AxisX.AxisValue;
                    p1.Y = AxisY.ValMin;
                    p2.Y = AxisY.ValMax;
                }
                else
                {
                    p1.Y = p2.Y = AxisY.AxisValue;
                    p1.X = AxisX.ValMin;
                    p2.X = AxisX.ValMax;
                }
                Point op1 = GraphCoords(p1);
                Point op2 = GraphCoords(p2);
                line.X1 = op1.X;
                line.Y1 = op1.Y;
                line.X2 = op2.X;
                line.Y2 = op2.Y;

                main_canvas.Children.Add(line);
                AdditionalList.Add(line);

                if (i == 0)
                    AddScaleLabel(AxisLabelAlign.TopLeft, AxisLabelAlign.TopLeft, op2.X, op2.Y, AxisTitleY);
                else
                    AddScaleLabel(AxisLabelAlign.BottomRight, AxisLabelAlign.BottomRight, op2.X, op2.Y, AxisTitleX);
            }
        }

        enum AxisLabelAlign { TopLeft, Center, BottomRight };
        void AddScaleLabel(AxisLabelAlign AlignX, AxisLabelAlign AlignY, double x, double y, string text)
        {
            Label label = new Label();
            label.Foreground = BrushScaleText;
            label.Content = text;
            label.HorizontalAlignment = HorizontalAlignment.Left;
            label.VerticalAlignment = VerticalAlignment.Top;
            var ft = new FormattedText(text, 
                System.Globalization.CultureInfo.CurrentCulture, 
                FlowDirection.LeftToRight, 
                new Typeface(label.FontFamily, label.FontStyle, label.FontWeight, label.FontStretch, label.FontFamily), 
                label.FontSize, 
                BrushScaleText);
            double sx = ft.Width + 10;
            double sy = ft.Height + 10;

            switch (AlignX)
            {
                case AxisLabelAlign.Center:
                    x -= sx / 2;
                    if (x < 0) x += sx / 2;
                    break;
                case AxisLabelAlign.BottomRight:
                    x -= sx;
                    if (x < 0) x += sx / 2;
                    break;
            }
            switch (AlignY)
            {
                case AxisLabelAlign.Center:
                    y -= sy / 2;
                    if (y < 0) y += sy / 2;
                    break;
                case AxisLabelAlign.BottomRight:
                    y -= sy;
                    if (y < 0) y += sy / 2;
                    break;
            }

            if (x >= 0 && x < main_canvas.Width && y >= 0 && y < main_canvas.Height)
            {
                label.Margin = new Thickness(x, y, 0, 0);
                main_canvas.Children.Add(label);
                AdditionalList.Add(label);
            }
        }

        private double CursorSize = 1;
        private Line[,] Cursors = null;
        private void DrawCursors()
        {
            if(Cursors != null)
            {
                for(int i = 0; i < NumCursors; i++)
                    for(int j = 0; j < 2; j++)
                        main_canvas.Children.Remove(Cursors[i, j]);
            }

            Point p1 = new Point();
            Point p2 = new Point();

            const int n = 20;
            CursorSize = Math.Max(AxisX.DrawSize, AxisY.DrawSize) / n;

            Cursors = new Line[NumCursors, 2];
            for(int i = 0; i < NumCursors; i++)
            {
                for(int j = 0; j < 2; j++)
                {
                    Line line = new Line();
                    line.Stroke = (CursorBrushList == null) ? System.Windows.Media.Brushes.White : CursorBrushList[i];
                    line.HorizontalAlignment = HorizontalAlignment.Left;
                    line.VerticalAlignment = VerticalAlignment.Center;
                    line.StrokeThickness = 3;

                    p1.X = p2.X = AxisX.AxisValue;
                    p1.Y = p2.Y = AxisY.AxisValue;

                    Point op1 = GraphCoords(p1);
                    Point op2 = GraphCoords(p2);

                    if (j == 0)
                    {
                        line.X1 = op1.X;
                        line.Y1 = op1.Y - CursorSize / 2;
                        line.X2 = op2.X;
                        line.Y2 = op2.Y + CursorSize / 2;
                    }
                    else
                    {
                        line.X1 = op1.X - CursorSize / 2;
                        line.Y1 = op1.Y;
                        line.X2 = op2.X + CursorSize / 2;
                        line.Y2 = op2.Y;
                    }

                    main_canvas.Children.Add(line);
                    Cursors[i, j] = line;
                }
            }

            CursorSize = (Cursors[0, 0].Y2 - Cursors[0, 0].Y1) / 2;
        }

        public bool WithinDisplayRange = true;

        public void MoveCursor(int n, Point p)
        {
            Point op = GraphCoords(p);

            for (int j = 0; j < 2; j++)
            {
                Line line = Cursors[n, j];
                if (j == 0)
                {
                    line.X1 = op.X;
                    line.Y1 = op.Y - CursorSize;
                    line.X2 = op.X;
                    line.Y2 = op.Y + CursorSize;
                }
                else
                {
                    line.X1 = op.X - CursorSize;
                    line.Y1 = op.Y;
                    line.X2 = op.X + CursorSize;
                    line.Y2 = op.Y;
                }
            }

            WithinDisplayRange = op.X >= 0 && op.X < main_canvas.Width && op.Y >= 0 && op.Y < main_canvas.Height;
        }

        // ------------------------------------------------------------

        public delegate void Position2DHandler(Point pos);
        public Position2DHandler Position2DEvent;

        bool IsMouseLeftDown = false;

        private void main_canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
                IsMouseLeftDown = true;

            Point p = InvGraphCoords(e.GetPosition(main_canvas));
            Position2DEvent(p);
        }

        private void main_canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsMouseLeftDown)
            {
                Point p = InvGraphCoords(e.GetPosition(main_canvas));
                Position2DEvent(p);
            }
        }

        private void main_canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsMouseLeftDown)
            {
                Position2DEvent(new Point(0, 0));
                IsMouseLeftDown = false;
            }
        }
    }

    public class GraphAxis
    {
        public double DrawSize;

        public double AxisValue = 0;
        public double AxisScale = 1;
        public double AxisPosition = 0.5;
        public double ScaleSmallInterval = 0.1;
        public double ScaleLargeInterval = 5;   // 5 means 0.5 (=5 * ScaleSmallInterval)

        public double[] ScaleSmallVal;
        public double[] ScaleLargeVal;

        public double ValMin;
        public double ValMax;

        public bool Direction = true;

        public void Init()
        {
            ValMin = AxisValue - AxisPosition * AxisScale;
            ValMax = AxisValue + (1.0 - AxisPosition) * AxisScale;
            int IdxMin = (int)Math.Ceiling(ValMin / ScaleSmallInterval);
            int IdxMax = (int)Math.Ceiling(ValMax / ScaleSmallInterval);

            int CountScaleSmall = 0;
            int CountScaleLarge = 0;
            for (int i = IdxMin; i < IdxMax; i++)
            {
                if (ScaleSmallInterval * i == AxisValue) continue;
                if (i % ScaleLargeInterval == 0)
                    CountScaleLarge++;
                else
                    CountScaleSmall++;
            }
            if (CountScaleSmall > 0)
                ScaleSmallVal = new double[CountScaleSmall];
            else
                ScaleSmallVal = null;
            if (CountScaleLarge > 0)
                ScaleLargeVal = new double[CountScaleLarge];
            else
                ScaleLargeVal = null;

            CountScaleSmall = 0;
            CountScaleLarge = 0;
            for (int i = IdxMin; i < IdxMax; i++)
            {
                if (ScaleSmallInterval * i == AxisValue) continue;
                if (i % ScaleLargeInterval == 0)
                    ScaleLargeVal[CountScaleLarge++] = ScaleSmallInterval * i;
                else
                    ScaleSmallVal[CountScaleSmall++] = ScaleSmallInterval * i;
            }
        }
        public double TransformToDrawCoord(double x)
        {
            if (Direction)
            {
                return DrawSize * (x - ValMin) / AxisScale;
            }
            else
            {
                return DrawSize - DrawSize * (x - ValMin) / AxisScale;
            }

        }
        public double TransformFromDrawCoord(double x)
        {
            if (Direction)
            {
                return ValMin + AxisScale * x / DrawSize;
            }else
            {
                return ValMin + AxisScale * (DrawSize - x) / DrawSize;
            }
        }

        // ------------------------------------------------------------

        // dValueを小数点以下iDigitsに丸める
        public static double ToRoundDown(double dValue, int iDigits)
        {
            double dCoef = System.Math.Pow(10, iDigits);

            return dValue > 0 ? System.Math.Floor(dValue * dCoef) / dCoef :
                                System.Math.Ceiling(dValue * dCoef) / dCoef;
        }
        // dValueの有効数字iDigits桁目が小数点以下何桁目かを求める
        public static int ToRoundDownSigFig(double dValue, int iDigits)
        {
            if (dValue == 0) return 0;
            int p = (int)Math.Floor(Math.Log10(Math.Abs(dValue)));
            return -p + iDigits - 1;
        }
    }
}
