using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
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
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing;
//using FileShellExtension;

namespace Progetto2017
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    /// prova
    public partial class MainWindow : Window
    {
        //parte nuova
        private string userName = null;

        private string userImage = null;

        private readonly Dispatcher _uiDispatcher;

        public MainWindow()
        {
            InitializeComponent();
            //inizializzazione variabili all'avvio della main Window
            userName = Environment.UserName;
            userImage = null;
            //System.Console.WriteLine(userImage);
            load_settings();
            //nuovo codice
            _uiDispatcher = Dispatcher.CurrentDispatcher;
            Task.Factory.StartNew(UDP_sender);
        }

        public void UDP_sender()
        {
            var Client = new UdpClient();
            while (true)
            {


                Message.Udp_message dp = new Message.Udp_message{ name = this.userName, image = new Bitmap(40,40) };

                byte[] bytes = null;

                using (var ms = new MemoryStream())
                {
                    var bf = new BinaryFormatter();
                    bf.Serialize(ms, dp);
                    bytes = ms.ToArray();
                }

                System.Console.WriteLine("n byte inviati {0}", bytes.Length);
                Client.EnableBroadcast = true;
                Client.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, 8888));

                Thread.Sleep(2000);
            }
            Client.Close();
        }

     

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofdPicture = new OpenFileDialog();
            ofdPicture.Filter =
                "Image files|*.bmp;*.jpg;*.gif;*.png;*.tif";
            ofdPicture.FilterIndex = 1;

            if (ofdPicture.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ImageBrush new_source = new ImageBrush();
                new_source.ImageSource = new BitmapImage(new Uri(ofdPicture.FileName));
                this.userImage = ofdPicture.FileName;
                imageShape.Fill = new_source;
            }     
        }

        private void ok_button_Click(object sender, RoutedEventArgs e)
        {

            Settings1.Default.automaticAccept = (bool)checkBox.IsChecked;
            Settings1.Default.useDefaultPath = (bool)checkBox2.IsChecked;
            Settings1.Default.defaultPath = textBox.Text;

            Settings1.Default.Save();

            this.Close();


        }

        private void open_directory_path(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fdb = new FolderBrowserDialog();
            if (fdb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox.Text = fdb.SelectedPath;
            }
        }

        private void ignore_button_Click(object sender, RoutedEventArgs e)
        {

            load_settings();
            this.Close();
        }

        private void reset_button_Click(object sender, RoutedEventArgs e)
        {

            Settings1.Default.Reset();
            load_settings();


        }

        private void load_settings()
        {
            //parte cambiata
            label.Content = this.userName;
            ImageBrush new_source = new ImageBrush();

            try
            {

                // new_source.ImageSource = new BitmapImage(new Uri(Settings1.Default.userImage));
                new_source.ImageSource = new BitmapImage(new Uri(userImage));

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

        }


    }
}
