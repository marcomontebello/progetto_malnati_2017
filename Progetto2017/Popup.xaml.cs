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

namespace Progetto2017
{
    /// <summary>
    /// Logica di interazione per Popup.xaml
    /// </summary>
    public partial class Popup : Window
    {
        //mystaticbool true if private access
        public static bool isOnline { get; set; }
        public Popup()

        {
            InitializeComponent();
            Left = System.Windows.SystemParameters.WorkArea.Width - Width - 70;
            Top = System.Windows.SystemParameters.WorkArea.Height - Height;
            isOnline = Settings1.Default.isPrivate;
            if (isOnline)
                button1.Content = "Stato: \nOffline";
            else
                button1.Content = "Stato: \nOnline";
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            isOnline = !isOnline;
            Settings1.Default.isPrivate = isOnline;
            Settings1.Default.Save();
            if (button1.Content.Equals("Stato: \nOnline"))
            {
                button1.Content = "Stato: \nOffline";
            }
            else
            {
                button1.Content = "Stato: \nOnline";
            }
        }
    }
}
