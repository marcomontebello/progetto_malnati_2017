using System.Windows;


namespace Progetto2017
{
    /// <summary>
    /// Logica di interazione per Popup.xaml
    /// </summary>
    public partial class Popup : Window
    {
        public static bool isPrivate { get; set; }
        public Popup()
        {
            InitializeComponent();
            Left = System.Windows.SystemParameters.WorkArea.Width - Width - 70;
            Top = System.Windows.SystemParameters.WorkArea.Height - Height;
            isPrivate = Settings1.Default.isPrivate;
            if (isPrivate)
                status_button.Content = "Stato: \nOffline";
            else
                status_button.Content = "Stato: \nOnline";
        }

        private void exit_button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void status_button_Click(object sender, RoutedEventArgs e)
        {
            isPrivate = !isPrivate;
            Settings1.Default.isPrivate = isPrivate;
            Settings1.Default.Save();
            if (isPrivate)
                status_button.Content = "Stato: \nOffline";
            else
                status_button.Content = "Stato: \nOnline";
        }
    }
}
