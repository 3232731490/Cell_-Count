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

namespace Cell__Count
{
    /// <summary>
    /// MyDialog.xaml 的交互逻辑
    /// </summary>
    public partial class MyDialog : Window
    {
        private Window topWindow;
        public MyDialog(Window topWindow)
        {
            InitializeComponent();
            this.topWindow = topWindow;
        }

        private void No_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Yes_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            topWindow.Close();
        }
    }
}
