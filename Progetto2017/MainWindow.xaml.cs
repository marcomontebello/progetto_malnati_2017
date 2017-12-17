using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Media;


//using FileShellExtension;


namespace Progetto2017
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    /// prova
    public partial class MainWindow : Window
    {
       
        private string userName = null;
        private string userImage = null;
        //public readonly Dispatcher _uiDispatcher;
        public object Constants { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            this.userName = Environment.UserName;
            this.userImage = GetUserTilePath(userName);
            load_settings();

            Task.Factory.StartNew(UDP_sender);
            Task.Factory.StartNew(TCP_receiver);
        }

        public void UDP_sender()
        {
            UDP_Sender_Service.sender(userName, userImage);
        }

        public void TCP_receiver()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 11000);
            listener.Start();
            while (true)
            {
                Console.WriteLine("TEMP PATH {0}", System.IO.Path.GetTempPath());
                Socket newSocket = listener.AcceptSocket();
                Thread t = new Thread(new ParameterizedThreadStart(TCP_Receiver_Service.TCP_receiver2));
                t.Start(newSocket);
            }
        }


        [DllImport("shell32.dll", EntryPoint = "#261",
           CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void GetUserTilePath(
  string username,
  UInt32 whatever, // 0x80000000
  StringBuilder picpath, int maxLength);

        public static string GetUserTilePath(string username)
        {   // username: use null for current user
            var sb = new StringBuilder(1000);
            GetUserTilePath(username, 0x80000000, sb, sb.Capacity);
            return sb.ToString();
        }

        public static System.Drawing.Image GetUserTile(string username)
        {
            return System.Drawing.Image.FromFile(GetUserTilePath(username));
        }






        private void ok_button_Click(object sender, RoutedEventArgs e)
        {
            Settings1.Default.automaticAccept = (bool)checkBox.IsChecked;
            Settings1.Default.useDefaultPath = (bool)checkBox2.IsChecked;
            Settings1.Default.defaultPath = (string) textBox.Text;

            Settings1.Default.Save();

            this.Close();

        }

        private void open_directory_path(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fdb = new FolderBrowserDialog();
            if (fdb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox.Text = fdb.SelectedPath;
                if(ok_button.IsEnabled == false)
                {
                    ok_button.IsEnabled = true;
                }
            }
        }

        private void ignore_button_Click(object sender, RoutedEventArgs e)
        {
            load_settings();
            this.Close();
        }

        private void reset_button_Click(object sender, RoutedEventArgs e)
        {

            bool temp = Popup.isPrivate;

            Settings1.Default.Reset();

            Popup.isPrivate = temp;
            Settings1.Default.isPrivate = temp;
            Settings1.Default.Save();
            load_settings();

        }

        private void load_settings() {

            label.Content = this.userName;
            ImageBrush new_source = new ImageBrush();

            try
            {

                new_source.ImageSource = new BitmapImage(new Uri(this.userImage));

            }
            catch (Exception ex)
            {
                ex.ToString();
                new_source.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/user_profile_male.jpg"));
            }

            imageShape.Fill = new_source;
            checkBox.IsChecked = Settings1.Default.automaticAccept;
            checkBox2.IsChecked = Settings1.Default.useDefaultPath;
            textBox.Text = Settings1.Default.defaultPath;
            if ((checkBox2.IsChecked == false))
            {
                ok_button.IsEnabled = true;
            }
            else if ((((string)textBox.Text) == "") && (checkBox2.IsChecked == true))
            {
                ok_button.IsEnabled = false;
            }
        }

        private void checkBox2_Click(object sender, RoutedEventArgs e)
        {
            if ((checkBox2.IsChecked == false))
            {
                ok_button.IsEnabled = true;
            }
            else if ((((string)textBox.Text) == "") && (checkBox2.IsChecked == true))
            {
                ok_button.IsEnabled = false;
            }
        }
    }
}
