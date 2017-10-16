using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Security.Principal;
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


        //string[] args = Environment.GetCommandLineArgs(); 
        private readonly Dispatcher _uiDispatcher;
        private int id = 0;
        string ip_graziano = "192.168.1.186";
        string temp_path = null;

        //diventerà lista quando gestiremo utenti multipli
        private List<string> onlineUsers=new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            _uiDispatcher = Dispatcher.CurrentDispatcher;
            // Task.Factory.StartNew(UDP_listening_PI1);




            ///////////////prova update immagini manwindow
            for (int i = 0; i < 10; i++)
            {
                Label label = (Label)this.FindName("label" + i);
                //label1.Content = packet.name;
                label.Content = "prova" + i;

                ImageBrush new_source = new ImageBrush();


                new_source.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/user_profile_male.jpg"));


                Ellipse ellipse = (Ellipse)this.FindName("ellipse" + i);

                ellipse.Fill = new_source;
                ellipse.Fill.Opacity = 100;




            }
            /////prova invio file

        }


        private void send_file(/*object o*/)
        {
            try
            {

                //Ellipse ell=(Ellipse)o;

                //string to_send = args[1];
                string send_path = "C:\\Users\\Marco Montebello\\Desktop\\PROVA";
                FileAttributes attr = File.GetAttributes(send_path);

                if (attr.HasFlag(FileAttributes.Directory))
                {
                    //label0.Content = ell.Name;
                    //to_send = to_send.Split('\\').Last();

                    //temp_path = System.IO.Path.GetTempPath()+"\\"+ send_path.Split('\\').Last() + ".zip";
                    ZipFile.CreateFromDirectory(send_path, "C:\\Users\\Marco Montebello\\Desktop\\prova.zip");
                    send_path = "C:\\Users\\Marco Montebello\\Desktop\\prova.zip";
                    DirectoryInfo dInfo = new DirectoryInfo(send_path);

                    DirectorySecurity dSecurity = dInfo.GetAccessControl();
                    dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl,
                                                 InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                                 PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                    dInfo.SetAccessControl(dSecurity);


                    Console.WriteLine("Ho creato il file zip:" + send_path);
                    ZipFile.ExtractToDirectory("C:\\Users\\Marco Montebello\\Desktop\\prova.zip", "C:\\Users\\Marco Montebello\\Desktop\\CAZZO");


                }

                string filename = send_path.Split('\\').Last();

                System.Console.WriteLine("path:" + send_path);
                System.Console.WriteLine("filename:" + filename);

                senderTCP invio_file = new senderTCP(ip_graziano, send_path, filename);

                Task.Factory.StartNew(invio_file.sendFile);
            }
            catch (Exception e)
            {

                System.Console.WriteLine(e.StackTrace);
            }

        }
    

        public void UDP_listening_PI1()
        {
            UdpClient listener = new UdpClient(8889);
            //var timeToWait = TimeSpan.FromSeconds(10);
            //
          listener.Client.ReceiveTimeout = 15000;

            while (true)
            {
                var ClientEp = new IPEndPoint(IPAddress.Any, 8888);

                Message.Udp_message packet_content = null;

                try
                {
                    //ricezione pacchetto udp da endpoint
                    System.Console.WriteLine("in attesa di un pacchetto udp");
                    var ClientRequestData = listener.Receive(ref ClientEp);
                    System.Console.WriteLine(ClientRequestData.Length);
                    //deserializzazione del pacchetto ricevuto.
                    MemoryStream ms = new MemoryStream();
                    BinaryFormatter bf = new BinaryFormatter();
                    ms.Write(ClientRequestData, 0, ClientRequestData.Length);
                    ms.Seek(0, SeekOrigin.Begin);
                    packet_content = (Message.Udp_message)bf.Deserialize(ms);
                    System.Console.WriteLine(packet_content.name+" "+packet_content.image.Size);
                    // var ClientRequest = Encoding.ASCII.GetString(ClientRequestData);
                    if (!onlineUsers.Contains(packet_content.name))
                    {
                        onlineUsers.Add(packet_content.name);
                    }
                    set(packet_content, ClientEp);

                }
                catch (SocketException ex) {

                    reset(packet_content);
                    continue;

                }

                catch (Exception ex)
                {
                    System.Console.WriteLine(ex.StackTrace);
                    reset(packet_content);
                    continue;

                }

            }
        }


        private void reset(Message.Udp_message packet)
        {
            _uiDispatcher.BeginInvoke(new Action(() =>
            {
                Label label;
                Ellipse ellipse;
                try
                {
                    onlineUsers.Remove(packet.name);
                    label = (Label)this.FindName("label" + onlineUsers.IndexOf(packet.name));
                    ellipse = (Ellipse)this.FindName("ellipse" + onlineUsers.IndexOf(packet.name));
               

                label.Content = " ";
                ellipse.Fill.Opacity=0;

                }
                catch (Exception e)
                {

                    return ;
                }
                //label0.Content = " ";
                //ellipse0.Fill.Opacity=0;

            }));

        }

        private void set(Message.Udp_message packet, IPEndPoint ClientEp)
        {
            _uiDispatcher.BeginInvoke(new Action(() =>
            {

                Label label=(Label) this.FindName("label"+onlineUsers.IndexOf(packet.name));
                //label1.Content = packet.name;
                label.Content= packet.name;

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

                Ellipse ellipse = (Ellipse)this.FindName("ellipse" + onlineUsers.IndexOf(packet.name));

                ellipse.Fill = new_source;
                ellipse.Fill.Opacity = 100;

                //ellipse0.Fill = new_source;
                //ellipse0.Fill.Opacity = 100;

            }));
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {

            for (int i = 0; i < 10; i++) {

                CheckBox check = (CheckBox)this.FindName("checkbox" + i);
                if (check.IsChecked == true)
                {
                    Label label = (Label)this.FindName("label" + i);
                    label.Content = "isCheked";
                }


            }

            HideAllControl();
            UserControl1 uc = new UserControl1();
            uc.Name = "progress";
            uc.Visibility = Visibility.Visible;
            StackPanel stack = new StackPanel { Orientation = Orientation.Horizontal };
            stack.VerticalAlignment = VerticalAlignment.Center;
            stack.HorizontalAlignment = HorizontalAlignment.Center;
            stack.Children.Add(uc);
            this.Content = stack;
            send_file();

        }




        private void HideAllControl()
        {
            /// casting the content into panel
            Panel mainContainer = (Panel)this.Content;

            /// GetAll UIElement
            UIElementCollection element = mainContainer.Children;

            /// casting the UIElementCollection into List
            List<FrameworkElement> lstElement = element.Cast<FrameworkElement>().ToList();

            /// Geting all Control from list
            var lstControl = lstElement.OfType<Control>();

            foreach (Control contol in lstControl)
            {
                ///Hide all Controls
                contol.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private void checkbox2_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
    }


