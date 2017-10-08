using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
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
                    Message.Class1 packet_content = null;
                    //ricezione pacchetto udp da endpoint
                    var ClientRequestData = listener.Receive(ref ClientEp);
                    System.Console.WriteLine(ClientRequestData.Length);
                    //deserializzazione del pacchetto ricevuto.
                    MemoryStream ms = new MemoryStream();
                    BinaryFormatter bf = new BinaryFormatter();
                    ms.Write(ClientRequestData, 0, ClientRequestData.Length);
                    ms.Seek(0, SeekOrigin.Begin);
                    packet_content = (Message.Class1)bf.Deserialize(ms);
                    System.Console.WriteLine(packet_content.name+" "+packet_content.image.Size);
                    // var ClientRequest = Encoding.ASCII.GetString(ClientRequestData);
                    set(packet_content, ClientEp);

                }
                catch (SocketException ex) {

                    reset();
                    continue;

                }

                catch (Exception ex)
                {
                    System.Console.WriteLine(ex.StackTrace);
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

        private void set(Message.Class1 packet, IPEndPoint ClientEp)
        {
            _uiDispatcher.BeginInvoke(new Action(() =>
            {
                label1.Content = packet.name;
                ImageBrush new_source = new ImageBrush();

                try
                {

                    new_source.ImageSource = Imaging.CreateBitmapSourceFromHBitmap(packet.image.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                }
                catch (Exception ex)
                {
                    ex.ToString();
                    new_source.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/user_profile_male.jpg"));
                }

                imageShape1.Fill = new_source;
                imageShape1.Fill.Opacity = 100;

            }));
        }
    
        }
    }


