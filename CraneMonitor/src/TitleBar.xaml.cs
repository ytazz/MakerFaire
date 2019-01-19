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
    /// TitleBar.xaml の相互作用ロジック
    /// </summary>
    public partial class TitleBar : UserControl
    {
        public TitleBar()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register("Label", typeof(object), typeof(TitleBar), new UIPropertyMetadata("Label"));

        [System.ComponentModel.BindableAttribute(true)]
        public object Label
        {
            get
            {
                return (object)GetValue(LabelProperty);
            }
            set
            {
                SetValue(LabelProperty, value);
            }
        }
    }
}
