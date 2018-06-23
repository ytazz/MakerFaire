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

namespace CraneMonitor
{
    /// <summary>
    /// RegisterUser.xaml の相互作用ロジック
    /// </summary>
    public partial class RegisterUser : Window
    {
        public RegisterUser()
        {
            InitializeComponent();
        }

        public string PlayerName
        {
            set
            {
                UserName.Text = value;
                // 開いたときにフォーカス＆全選択
                UserName.Focus();
                UserName.SelectAll();
            }
            get { return UserName.Text; }
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                DialogResult = true;
        }
    }
}
