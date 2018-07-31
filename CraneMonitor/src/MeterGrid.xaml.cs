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
    /// Interaction logic for MeterGrid.xaml
    /// </summary>
    public partial class MeterGrid : UserControl
    {
        public BoeingMeter[]   meters;
        public MeterControl[]  meter_controls;
        public Label[]         labels;
        public RowDefinition[] rows;

        public MeterGrid()
        {
            InitializeComponent();

            int nrow = MainPanel.RowDefinitions.Count();
            int ncol = MainPanel.ColumnDefinitions.Count();
            meters         = new BoeingMeter[nrow*ncol];
            meter_controls = new MeterControl[nrow*ncol];
            labels         = new Label[nrow*ncol];
            rows           = new RowDefinition[nrow];

            meters[ 0] = Meter0;
            meters[ 1] = Meter1;
            meters[ 2] = Meter2;
            meters[ 3] = Meter3;
            meters[ 4] = Meter4;
            meters[ 5] = Meter5;
            meters[ 6] = Meter6;
            meters[ 7] = Meter7;
            meters[ 8] = Meter8;
            meters[ 9] = Meter9;
            meters[10] = Meter10;
            meters[11] = Meter11;

            labels[ 0] = label0;
            labels[ 1] = label1;
            labels[ 2] = label2;
            labels[ 3] = label3;
            labels[ 4] = label4;
            labels[ 5] = label5;
            labels[ 6] = label6;
            labels[ 7] = label7;
            labels[ 8] = label8;
            labels[ 9] = label9;
            labels[10] = label10;
            labels[11] = label11;

            for (int i = 0; i < MainPanel.RowDefinitions.Count(); i++)
            {
                rows[i] = MainPanel.RowDefinitions.ElementAt(i);
            }
        }

        public void UpdateDisplayLayout(MeasureObj[] Measures)
        {
            int n = Measures.Length;

            for (int i = 0; i < n; i++)
            {
                meter_controls[i] = new MeterControl()
                {
                    Meter = meters[i],
                    MeasureObj = Measures[i],
                    MaxTimeIndex = timer_count_max
                };
                meters[i].Visibility = (Measures[i] == null) ? Visibility.Hidden : Visibility.Visible;
            }
        }

        public int timer_count_max = 1;
        int timer_count = 0;

        public void Update(bool smooth)
        {
            if (meter_controls == null) return;

            if (smooth)
            {
                if (timer_count == 0)
                {
                    foreach (var x in meter_controls)
                    {
                        if (x == null) continue;
                        x.UpdateData();
                    }
                }
                foreach (var x in meter_controls)
                {
                    if (x == null) continue;
                    x.UpdateSmooth(timer_count);
                }
                timer_count++;
                if (timer_count == timer_count_max) timer_count = 0;
            }
            else
            {
                foreach (var x in meter_controls)
                {
                    if (x == null) continue;
                    x.UpdateData();
                    x.UpdateDirect();
                }
            }
        }
    }

}
