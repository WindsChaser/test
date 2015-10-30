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

namespace ArrestForever.Role
{
    /// <summary>
    /// CreatGameDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CreatGameDialog : Window
    {
        public delegate void btn1_click();
        public event btn1_click btn1;
        public delegate void btn2_click();
        public event btn2_click btn2;
        public delegate void btn3_click();
        public event btn3_click btn3;
        public CreatGameDialog()
        {
            InitializeComponent();
        }

        private void CreatServer_Click(object sender, RoutedEventArgs e)
        {
            if (btn1 != null)
                btn1();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (btn2 != null)
                btn2();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (btn3 != null)
                btn3();
        }
    }
}
