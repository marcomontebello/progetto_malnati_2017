using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
using System.Windows.Threading;

namespace FileSharing
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
            //You need to listen to the UDP port in a different background thread and then,
            //pump back the message to the main UI thread if and when required.
            //You probably need something like below.
        private readonly Dispatcher _uiDispatcher;

        public MainWindow()
        {
            InitializeComponent();
            _uiDispatcher = Dispatcher.CurrentDispatcher;
            Task.Factory.StartNew(UDP_listening_PI1);
        }

        public void UDP_listening_PI1()
        {
            UdpClient listener = new UdpClient(8888);
            //var timeToWait = TimeSpan.FromSeconds(10);
            listener.Client.ReceiveTimeout = 5000;
            while (true || listener.Client.ReceiveTimeout==10)
            {
                var ClientEp = new IPEndPoint(IPAddress.Any, 8888);
                try
                {
                    var ClientRequestData = listener.Receive(ref ClientEp);
                    var ClientRequest = Encoding.ASCII.GetString(ClientRequestData);
                    set(ClientRequest, ClientEp);

                }
                catch (SocketException ex) {

                    reset();
                    continue;

                }
              
            }
        }

        private void reset()
        {
            _uiDispatcher.BeginInvoke(new Action(() =>
            {
                label1.Content = " ";
                imageShape1.Fill.Opacity=0;
            }));

        }

        private void set(string ClientRequest, IPEndPoint ClientEp)
        {
            _uiDispatcher.BeginInvoke(new Action(() =>
            {
                label1.Content = ClientRequest;
                ImageBrush new_source = new ImageBrush();

                try
                {

                    new_source.ImageSource = new BitmapImage(new Uri(null));

                }
                catch (Exception ex)
                {
                    ex.ToString();
                    new_source.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/user_profile_male.jpg"));
                }

                imageShape1.Fill = new_source;
            }));
        }
    
        }
    }


