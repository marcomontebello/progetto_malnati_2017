using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

using System.Drawing;
using System.Collections.ObjectModel;

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


        string[] args = Environment.GetCommandLineArgs(); 
        private readonly Dispatcher _uiDispatcher;
       // private int id = 0;
        string temp_path = null;

        //diventerà lista quando gestiremo utenti multipli
        //lista di indirizzi ip identificativi degli utenti
        private ObservableCollection<User> onlineUsers=new ObservableCollection<User>();
        private ObservableCollection<User> selectedUsers = new ObservableCollection<User>();

        private bool listen=true;

        public MainWindow()
        {
            InitializeComponent();
           
            this.Title = "Condividi " + args[1].Substring(args[1].LastIndexOf('\\')+1)+ " con:";
            userOnlineList.ItemsSource = onlineUsers;
            _uiDispatcher = Dispatcher.CurrentDispatcher;
            Task.Factory.StartNew(UDP_listening_PI1);

        }
    

        public void UDP_listening_PI1()
        {
            UdpClient listener = new UdpClient(8889);
            //var timeToWait = TimeSpan.FromSeconds(10);
            //
          listener.Client.ReceiveTimeout = 10000;

            while (listen)
            {
                if (onlineUsers.Count == 0)
                    _uiDispatcher.Invoke(new Action(() =>
                    {
                        label.Content = "Nessun utente connesso in LAN, attendere.";
                        button_invia.IsEnabled = false;

                    }));
                //System.Console.WriteLine(onlineUsers);
                var ClientEp = new IPEndPoint(IPAddress.Any, 8889);

                Message.Udp_message packet_content = null;

                try
                {
                    //ricezione pacchetto udp da endpoint
                   // System.Console.WriteLine("in attesa di un pacchetto udp");
                    var ClientRequestData = listener.Receive(ref ClientEp);
                   // System.Console.WriteLine(ClientRequestData.Length);
                    //deserializzazione del pacchetto ricevuto.
                    MemoryStream ms = new MemoryStream();
                    BinaryFormatter bf = new BinaryFormatter();
                    ms.Write(ClientRequestData, 0, ClientRequestData.Length);
                    ms.Seek(0, SeekOrigin.Begin);
                    packet_content = (Message.Udp_message)bf.Deserialize(ms);
                    //System.Console.WriteLine(packet_content.name+" "+packet_content.image.Size);
                    // var ClientRequest = Encoding.ASCII.GetString(ClientRequestData);
                    _uiDispatcher.Invoke(new Action(() =>
                    {

                        ImageBrush ib = new ImageBrush();

                        ib.ImageSource = Imaging.CreateBitmapSourceFromHBitmap(packet_content.image.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                        User act_user = new FileSharing.User(ClientEp.Address.ToString(), packet_content.name, DateTime.Now,packet_content.image,ib);
                        Console.WriteLine(act_user.Address);

                        if (!onlineUsers.Contains(act_user) && !act_user.Address.Equals(GetLocalIPAddress()))
                        {
         
                                onlineUsers.Add(act_user);
                                button_invia.IsEnabled = true;
                                label.Content = "Scegli con chi condividere:";

                            // System.Console.WriteLine("aggiunto elemento alla lista");
                        }
                        else
                        {
                            foreach (User user in onlineUsers)
                            {

                                // var found = onlineUsers.FirstOrDefault(c => c.Address == ClientEp.Address.ToString());
                                var diffInSeconds = (DateTime.Now - user.Timestamp).TotalSeconds;
                                if (diffInSeconds > 10)
                                {

                                        _uiDispatcher.Invoke(new Action(() =>
                                        {
                                            onlineUsers.Remove(user);
                                            if (onlineUsers.Count == 0)
                                            {
                                                label.Content = "Nessun utente connesso in LAN, attendere.";
                                                button_invia.IsEnabled = false;
                                            }

                                        }));
                                }

                                else
                                {
                                    if (user.Equals(act_user))
                                    {
                                        user.Timestamp = DateTime.Now;
                                        user.Image = packet_content.image;
                                        //System.Console.WriteLine("trovato utente nella lista, non necessaria aggiunta");
                                    }
                                }
                            }
                        }
                        //Do something here.
                    }));
                }

                catch (SocketException ex) {

                    update_list();
                    continue;

                }

                catch (Exception ex)
                {
                    System.Console.WriteLine(ex.StackTrace);
                    update_list();
                    continue;

                }


            }

            listener.Close();
        }

        private void update_list() {

            try
            {

                foreach (User user in new System.Collections.ArrayList(onlineUsers))
                {
                    // var found = onlineUsers.FirstOrDefault(c => c.Address == ClientEp.Address.ToString());
                    var diffInSeconds = (DateTime.Now - user.Timestamp).TotalSeconds;
                    if (diffInSeconds >= 10)
                    {
                            _uiDispatcher.Invoke(new Action(() =>
                            {

                                onlineUsers.Remove(user);

                                if (onlineUsers.Count == 0)
                                    label.Content = "Nessun utente connesso in LAN, attendere.";
                                button_invia.IsEnabled = false;


                                /* Your code here */
                            }));
                        //System.Console.WriteLine("rimosso utente" + user.Name);
                    }
                }
            }
            catch (Exception ex) { System.Console.WriteLine(ex.ToString()); }
            }

        private void button_Click(object sender, RoutedEventArgs e)
        {

            listen = false;

            foreach (User u in userOnlineList.SelectedItems)
                selectedUsers.Add(u);

            // selectedUsers = userOnlineList.SelectedItems as ObservableCollection<User>;
            System.Console.WriteLine("###########################################################################################################################");

            System.Console.WriteLine(selectedUsers.Count);
            foreach (User u in selectedUsers)
                System.Console.WriteLine(u.Address);

            if (selectedUsers.Count == 0) { 
            string msg = "Seleziona almeno un utente";
            MessageBoxResult result =
              MessageBox.Show(
                msg,
                "Attenzione",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

                if (result == MessageBoxResult.OK)
                {
                        return;
                    // If user doesn't want to close, cancel closure
                }

             }

           System.Console.WriteLine("###########################################################################################################################");
            send_file();
            this.Close();
            //transf_windows.userSelectedList.ItemsSource = selectedUsers;    
            // UserControl uc = new UserControl();

        }

        private void button_annulla_Click(object sender, RoutedEventArgs e)
        {

            App.Current.Shutdown();

        }

        private void userOnlineList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void send_file()
        {
            try
            {

                string send_path = args[1];

                //string send_path = "C:\\Users\\Marco Montebello\\Desktop\\PROVA";
               // string send_path = "C:\\Users\\Marco Montebello\\Desktop\\ArchitectVideo_512kb.mp4";
                //string send_path = "C:\\Users\\GRAZIANO\\Desktop\\ArchitectVideo_512kb.mp4";

                FileAttributes attr = File.GetAttributes(send_path);
                bool is_dir = false;

                if (attr.HasFlag(FileAttributes.Directory))
                {
                 
                    is_dir = true;
                    temp_path = System.IO.Path.GetTempPath()+ send_path.Split('\\').Last();
                    if (!(File.Exists(temp_path)))
                        //   ZipFile.CreateFromDirectory(send_path, "C:\\Users\\Marco Montebello\\Desktop\\PROVA.LAN_DIR");
                        ZipFile.CreateFromDirectory(send_path,temp_path);

                    //send_path = "C:\\Users\\Marco Montebello\\Desktop\\prova.LAN_DIR";
                    send_path = temp_path;
                    System.Console.WriteLine( send_path);

                    DirectoryInfo dInfo = new DirectoryInfo(send_path);

                    DirectorySecurity dSecurity = dInfo.GetAccessControl();
                    dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl,
                                                 InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                                 PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                    dInfo.SetAccessControl(dSecurity);
                    Console.WriteLine("Ho creato il file zip:" + send_path);


                }

                string filename = send_path.Split('\\').Last();

                Transfers transf_windows = new Transfers(selectedUsers, send_path, filename, is_dir);
                transf_windows.Show();
                listen = false;

            }

            catch (Exception e)
            {

                System.Console.WriteLine(e.StackTrace);
            }

        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }


    }


