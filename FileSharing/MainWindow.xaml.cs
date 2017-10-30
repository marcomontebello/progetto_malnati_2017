﻿using System;
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


        //string[] args = Environment.GetCommandLineArgs(); 
        private readonly Dispatcher _uiDispatcher;
        private int id = 0;
        string ip_graziano = "192.168.1.186";
        string temp_path = null;

        //diventerà lista quando gestiremo utenti multipli
        //lista di indirizzi ip identificativi degli utenti
        private ObservableCollection<User> onlineUsers=new ObservableCollection<User>();
        private ObservableCollection<User> selectedUsers = new ObservableCollection<User>();

        private bool listen=true;

        public MainWindow()
        {
            InitializeComponent();

              userOnlineList.ItemsSource = onlineUsers;
             _uiDispatcher = Dispatcher.CurrentDispatcher;
             Task.Factory.StartNew(UDP_listening_PI1);

        }


      private void send_file()
        {
            try
            {

                //string to_send = args[1];
                //string send_path = "C:\\Users\\Marco Montebello\\Desktop\\PROVA";
                string send_path = "C:\\Users\\Marco Montebello\\Desktop\\ArchitectVideo_512kb.mp4";

                FileAttributes attr = File.GetAttributes(send_path);
                bool is_dir=false;

                if (attr.HasFlag(FileAttributes.Directory))
                {
                    //label0.Content = ell.Name;
                    //to_send = to_send.Split('\\').Last();
                    is_dir = true;
                    //temp_path = System.IO.Path.GetTempPath()+"\\"+ send_path.Split('\\').Last() + ".zip";
                    ZipFile.CreateFromDirectory(send_path, "C:\\Users\\Marco Montebello\\Desktop\\prova.LAN_DIR");
                    send_path = "C:\\Users\\Marco Montebello\\Desktop\\prova.LAN_DIR";
                    DirectoryInfo dInfo = new DirectoryInfo(send_path);

                    DirectorySecurity dSecurity = dInfo.GetAccessControl();
                    dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl,
                                                 InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                                 PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                    dInfo.SetAccessControl(dSecurity);


                    Console.WriteLine("Ho creato il file zip:" + send_path);
                    // ZipFile.ExtractToDirectory("C:\\Users\\Marco Montebello\\Desktop\\prova.zip", "C:\\Users\\Marco Montebello\\Desktop\\CAZZO");


                }

                string filename = send_path.Split('\\').Last();

                //System.Console.WriteLine("path:" + send_path);
                //System.Console.WriteLine("filename:" + filename);

                // senderTCP invio_file = new senderTCP(ip_graziano, send_path, filename);



                // Task sendTask = Task.Factory.StartNew(invio_file.sendFile);
                // sendTask.Wait();

                Transfers transf_windows = new Transfers(selectedUsers,send_path,filename,is_dir);
                transf_windows.Show();

              /*  foreach (User sel in selectedUsers)
                {

               
                        UserControl uc = new UserControl(sel.Address, send_path, filename);
                        uc.Visibility = Visibility.Visible;
                        StackPanel stack = (StackPanel)this.FindName("stack" + onlineUsers.IndexOf(sel));
                        stack.Children.Add(uc);

                    }*/
                
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
          listener.Client.ReceiveTimeout = 3000;

            while (true)
            {
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
                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ImageBrush ib = new ImageBrush();

                    ib.ImageSource = Imaging.CreateBitmapSourceFromHBitmap(packet_content.image.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                    User act_user = new FileSharing.User(ClientEp.Address.ToString(), packet_content.name, DateTime.Now,packet_content.image,ib);


                        if (!onlineUsers.Contains(act_user))
                        {
                            onlineUsers.Add(act_user);
                           // System.Console.WriteLine("aggiunto elemento alla lista");
                        }
                        else
                        {
                            foreach (User user in onlineUsers)
                            {

                                // var found = onlineUsers.FirstOrDefault(c => c.Address == ClientEp.Address.ToString());
                                var diffInSeconds = (DateTime.Now - user.Timestamp).TotalSeconds;
                                if (diffInSeconds > 3)
                                    onlineUsers.Remove(user);
                                else
                                {
                                    if (user.Equals(act_user))
                                    {
                                        user.Timestamp = DateTime.Now;
                                        user.Image = packet_content.image;
                                    //    System.Console.WriteLine("trovato elemento nella lista, non necessaria aggiunta");
                                    }
                                }
                            }
                        }
                        //Do something here.
                    });
                }

                catch (SocketException ex) {
                    update_list();
                    //update_ui();
                    continue;

                }

                catch (Exception ex)
                {
                    System.Console.WriteLine(ex.StackTrace);
                    update_list();
                    //update_ui();
                    continue;

                }

            }
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
                        onlineUsers.Remove(user);
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
            foreach(User u in selectedUsers)
                System.Console.WriteLine(u.Address);


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
    }
    }


