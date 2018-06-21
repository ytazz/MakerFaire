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
    public delegate bool EnableEventHandler();

    public class EnableButton : Button
    {
        public Brush ForegroundEnabled { get; set; } = null;
        public Brush BackgroundEnabled { get; set; } = null;
        public Brush ForegroundDisabled { get; set; } = null;
        public Brush BackgroundDisabled { get; set; } = null;

        public EnableEventHandler OnEnable { get; set; } = new EnableEventHandler(() => { return true; });
        public EnableEventHandler OnDisable { get; set; } = new EnableEventHandler(() => { return true; });
        public static readonly DependencyProperty EnabledProperty = DependencyProperty.Register(
            "Enabled", // Property Name
            typeof(bool), // Property Type
            typeof(EnableButton), // type of the owner
            new PropertyMetadata(false, EnabledChanged, CoerceEnabledValue)); // default value
        public bool Enabled
        {
            get { return (bool)GetValue(EnabledProperty); }
            set { SetValue(EnabledProperty, value); }
        }

        private static object CoerceEnabledValue(DependencyObject d, object baseValue)
        {
            var obj = (EnableButton)d;
            var value = (bool)baseValue;

            if (value == obj.Enabled)
                return value;
            else if (value)
                return obj.OnEnable();
            else
                return !obj.OnDisable();
        }

        private static void EnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = (EnableButton)d;

            if (e.OldValue == e.NewValue) return;
            obj.Flip((bool)e.NewValue);
        }

        protected override void OnClick()
        {
            base.OnClick();
            Enabled = !Enabled;
        }

        private void Flip(bool enabled)
        {
            if (enabled)
            {
                Background = BackgroundEnabled;
                Foreground = ForegroundEnabled;
            }
            else
            {
                Background = BackgroundDisabled;
                Foreground = ForegroundDisabled;
            }
        }

        public EnableButton()
        {
            
            BorderThickness = new Thickness(0);
            HorizontalContentAlignment = HorizontalAlignment.Left;
            ForegroundEnabled = Foreground;
            ForegroundDisabled = Foreground;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            if (BackgroundEnabled == null && BackgroundDisabled == null)
            {
                if (Background == null)
                    SetDisableOpacity(System.Windows.Media.Brushes.Yellow);
                else if (BackgroundEnabled != null && BackgroundEnabled is SolidColorBrush)
                    SetDisableOpacity(BackgroundEnabled as SolidColorBrush);
                else if (Background != null && Background is SolidColorBrush)
                    SetDisableOpacity(Background as SolidColorBrush);
            }

            Flip(Enabled);
        }

        public void SetDisableOpacity(SolidColorBrush brush)
        {
            BackgroundEnabled = brush;
            Color color = brush.Color;
            color.A = 0x88;
            BackgroundDisabled = new SolidColorBrush(color);
        }



    }
}
