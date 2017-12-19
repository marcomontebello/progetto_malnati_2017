using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Threading;
using System.Timers;

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
        private readonly int REFRESH_TIMEOUT = 12;
        private readonly String NO_USERS_MSG = "Nessun utente connesso, attendere...";
        private readonly String MANY_USERS_MSG = "Seleziona uno o più utenti con cui condividere:";
        private readonly String SELECT_WARNING = "Seleziona almeno un utente.";



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

            System.Timers.Timer updateUITimer = new System.Timers.Timer();
            updateUITimer.Elapsed += new ElapsedEventHandler(update_list);
            updateUITimer.Enabled = true;

        }


        public void UDP_listening_PI1()
        {
            UdpClient listener = new UdpClient(8889);
 
            listener.Client.ReceiveTimeout = 10000;
            while (listen)
            {
                if (onlineUsers.Count == 0)
                    _uiDispatcher.Invoke(new Action(() =>
                    {
                        label.Content = NO_USERS_MSG;
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

                    //deserializzazione del pacchetto ricevuto.
                    MemoryStream ms = new MemoryStream();
                    BinaryFormatter bf = new BinaryFormatter();
                    ms.Write(ClientRequestData, 0, ClientRequestData.Length);
                    ms.Seek(0, SeekOrigin.Begin);
                    packet_content = (Message.Udp_message)bf.Deserialize(ms);
                    //System.Console.WriteLine(packet_content.name+" "+packet_content.image.Size);

                    _uiDispatcher.Invoke(new Action(() =>
                    {

                        ImageBrush ib = new ImageBrush();

                        ib.ImageSource = Imaging.CreateBitmapSourceFromHBitmap(packet_content.image.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                        User act_user = new FileSharing.User(ClientEp.Address.ToString(), packet_content.name, DateTime.Now,packet_content.image,ib);
                        //Console.WriteLine(act_user.Address);

                        //GESTIONE CONNESSIONE NUOVO UTENTE
                        if (!onlineUsers.Contains(act_user) && !act_user.Address.Equals(GetLocalIPAddress()))
                        {
         
                                onlineUsers.Add(act_user);
                                button_invia.IsEnabled = true;
                                label.Content = MANY_USERS_MSG;
                                // System.Console.WriteLine("aggiunto utente alla lista");

                        }
                        //GESTIONE UTENTE GIA CONNESSO: CHECK DELLA LISTA PER UPDATE UI
                        else
                        {
                            foreach (User user in onlineUsers)
 
                                    if (user.Equals(act_user))
                                    {
                                        user.Timestamp = DateTime.Now;
                                        user.Image = packet_content.image;
                                        //System.Console.WriteLine("trovato utente nella lista, aggiornato timestamp");
                                    }
                                }

                    }));
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex.StackTrace);
                    update_list(this,null);
                    continue;

                }
            }

            listener.Close();
        }

        private void update_list(object source, ElapsedEventArgs e) {

            try
            {

                foreach (User user in new System.Collections.ArrayList(onlineUsers))
                {

                    var diffInSeconds = (DateTime.Now - user.Timestamp).TotalSeconds;
                    if (diffInSeconds >= REFRESH_TIMEOUT)
                    {
                            _uiDispatcher.Invoke(new Action(() =>
                            {

                                onlineUsers.Remove(user);
                                //System.Console.WriteLine("rimosso utente" + user.Name);

                                if (onlineUsers.Count == 0)
                                {

                                    label.Content = NO_USERS_MSG;
                                    button_invia.IsEnabled = false;

                                }
                            }));
                    }
                }
            }
            catch (Exception ex) {

                System.Console.WriteLine(ex.ToString());
            
            }
            Thread.Sleep(100);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            //PASSA IN MODALITA TRANSFERTS E DISABILITA LISTENER

            foreach (User u in userOnlineList.SelectedItems)

                selectedUsers.Add(u);

            if (selectedUsers.Count == 0) { 

                string msg = SELECT_WARNING;
                MessageBoxResult result =
               
                MessageBox.Show(
                msg,
                "Attenzione",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

                if (result == MessageBoxResult.OK)
                {

                    listen = false;
                    return;
                        // If user doesn't want to close, cancel closure
                }

             }

            this.Hide();

            prepare_transfer();

            this.Close();


        }

        private void button_annulla_Click(object sender, RoutedEventArgs e)
        {

            App.Current.Shutdown();

        }

        private void userOnlineList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void prepare_transfer()
        {
            try
            {

                string filePath = args[1];
                FileAttributes attr = File.GetAttributes(filePath);
                bool is_dir = false;

                if (attr.HasFlag(FileAttributes.Directory))
 
                    is_dir = true;
                 

                string filename = filePath.Split('\\').Last();
                FileToShare fileToTransfer = new FileToShare(filePath, filename, is_dir);

                listen = false;

                Transfers transfer_windows = new Transfers(selectedUsers, fileToTransfer);
                transfer_windows.Show();

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


