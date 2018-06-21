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
    /// Interaction logic for Graph2D1D.xaml
    /// </summary>
    public partial class Graph2D1D : UserControl
    {
        double px, py, pz;
        enum AxisCombinationType { AxisXY, AxisXZ, AxisYZ }
        AxisCombinationType AxisCombination = AxisCombinationType.AxisXY;

        public int NumCursors { get; set; } = 1;
        public Brush[] CursorBrushList = { System.Windows.Media.Brushes.Red, System.Windows.Media.Brushes.Yellow };

        public Graph2D1D()
        {
            InitializeComponent();

            graph2d.Position2DEvent = new Graph2D.Position2DHandler(delegate (Point p)
            {
                switch (AxisCombination)
                {
                    case AxisCombinationType.AxisXY:
                        px = p.X;
                        py = p.Y;
                        break;
                    case AxisCombinationType.AxisXZ:
                        px = p.X;
                        pz = p.Y;
                        break;
                    case AxisCombinationType.AxisYZ:
                        py = p.X;
                        pz = p.Y;
                        break;
                }
                Position2D1DEvent(px, py, pz);
            });
            graph1d.Position2DEvent = new Graph2D.Position2DHandler(delegate (Point p)
            {
                switch (AxisCombination)
                {
                    case AxisCombinationType.AxisXY:
                        pz = p.Y;
                        break;
                    case AxisCombinationType.AxisXZ:
                        py = p.Y;
                        break;
                    case AxisCombinationType.AxisYZ:
                        px = p.Y;
                        break;
                }
                Position2D1DEvent(px, py, pz);
            });

            graph1d.AxisX.AxisScale = graph1d.AxisY.AxisScale * graph1d.FigureWidth / graph1d.FigureHeight;

            UpdateAxis();
        }
        public void MoveCursor(int n, double x, double y, double z)
        {
            switch (AxisCombination)
            {
                case AxisCombinationType.AxisXY:
                    graph2d.MoveCursor(n, new Point(x, y));
                    graph1d.MoveCursor(n, new Point(0, z));
                    break;
                case AxisCombinationType.AxisXZ:
                    graph2d.MoveCursor(n, new Point(x, z));
                    graph1d.MoveCursor(n, new Point(0, y));
                    break;
                case AxisCombinationType.AxisYZ:
                    graph2d.MoveCursor(n, new Point(y, z));
                    graph1d.MoveCursor(n, new Point(0, x));
                    break;
            }
        }

        public delegate void Position2D1DHandler(double x, double y, double z);
        public Position2D1DHandler Position2D1DEvent = new Position2D1DHandler((double x, double y, double z) => { });

        public void UpdateAxis()
        {
            switch (AxisCombination)
            {
                case AxisCombinationType.AxisXY:
                    graph2d.AxisTitleX = "+x";
                    graph2d.AxisTitleY = "+y";
                    graph1d.AxisTitleX = "";
                    graph1d.AxisTitleY = "+z";
                    break;
                case AxisCombinationType.AxisXZ:
                    graph2d.AxisTitleX = "+x";
                    graph2d.AxisTitleY = "+z";
                    graph1d.AxisTitleX = "";
                    graph1d.AxisTitleY = "+y";
                    break;
                case AxisCombinationType.AxisYZ:
                    graph2d.AxisTitleX = "+y";
                    graph2d.AxisTitleY = "+z";
                    graph1d.AxisTitleX = "";
                    graph1d.AxisTitleY = "+x";
                    break;
            }

            graph2d.NumCursors = NumCursors;
            graph1d.NumCursors = NumCursors;
            graph2d.CursorBrushList = CursorBrushList;
            graph1d.CursorBrushList = CursorBrushList;

            graph2d.UpdateAxis();
            graph1d.UpdateAxis();

            for (int i = 0; i < NumCursors; i++)
                MoveCursor(i, px, py, pz);

#if False
            title.Inlines.Clear();
            title.Inlines.Add(new Run("     "));
            if(AxisCombination == AxisCombinationType.AxisXY)
                title.Inlines.Add(new Run("x-y") { Foreground = System.Windows.Media.Brushes.Black, Background = System.Windows.Media.Brushes.White });
            else
                title.Inlines.Add("x-y");
            title.Inlines.Add(new Run("    "));
            if (AxisCombination == AxisCombinationType.AxisXZ)
                title.Inlines.Add(new Run("x-z") { Foreground = System.Windows.Media.Brushes.Black, Background = System.Windows.Media.Brushes.White });
            else
                title.Inlines.Add("x-z");
            title.Inlines.Add(new Run("    "));
            if (AxisCombination == AxisCombinationType.AxisYZ)
                title.Inlines.Add(new Run("y-z") { Foreground = System.Windows.Media.Brushes.Black, Background = System.Windows.Media.Brushes.White });
            else
                title.Inlines.Add("y-z");
#else
            title.Inlines.Clear();
            title.Inlines.Add(new Run("     "));
            if (AxisCombination == AxisCombinationType.AxisXY)
                title.Inlines.Add(new Run("x - y") { TextDecorations = TextDecorations.Underline });
            else
                title.Inlines.Add("x - y");
            title.Inlines.Add(new Run("    "));
            if (AxisCombination == AxisCombinationType.AxisXZ)
                title.Inlines.Add(new Run("x - z") { TextDecorations = TextDecorations.Underline });
            else
                title.Inlines.Add("x - z");
            title.Inlines.Add(new Run("    "));
            if (AxisCombination == AxisCombinationType.AxisYZ)
                title.Inlines.Add(new Run("y - z") { TextDecorations = TextDecorations.Underline });
            else
                title.Inlines.Add("y - z");
#endif
        }

        private void title_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (AxisCombination)
            {
                case AxisCombinationType.AxisXY:
                    AxisCombination = AxisCombinationType.AxisXZ;
                    break;
                case AxisCombinationType.AxisXZ:
                    AxisCombination = AxisCombinationType.AxisYZ;
                    break;
                case AxisCombinationType.AxisYZ:
                    AxisCombination = AxisCombinationType.AxisXY;
                    break;
            }
            UpdateAxis();
        }

        public bool WithinDisplayRange { get { return graph2d.WithinDisplayRange && graph1d.WithinDisplayRange; } }

        public GraphAxis AxisX
        {
            get
            {
                switch (AxisCombination)
                {
                    case AxisCombinationType.AxisXY:
                        return graph2d.AxisX;
                    case AxisCombinationType.AxisXZ:
                        return graph2d.AxisX;
                    case AxisCombinationType.AxisYZ:
                        return graph2d.AxisY;
                    default:
                        return null;
                }
            }
        }
        public GraphAxis AxisY
        {
            get
            {
                switch (AxisCombination)
                {
                    case AxisCombinationType.AxisXY:
                        return graph2d.AxisY;
                    case AxisCombinationType.AxisXZ:
                        return graph1d.AxisY;
                    case AxisCombinationType.AxisYZ:
                        return graph1d.AxisY;
                    default:
                        return null;
                }
            }
        }
        public GraphAxis AxisZ
        {
            get
            {
                switch (AxisCombination)
                {
                    case AxisCombinationType.AxisXY:
                        return graph1d.AxisY;
                    case AxisCombinationType.AxisXZ:
                        return graph2d.AxisY;
                    case AxisCombinationType.AxisYZ:
                        return graph2d.AxisY;
                    default:
                        return null;
                }
            }
        }
    }
}
