using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FileSharing
{
    /// <summary>
    /// Logica di interazione per Transfers.xaml
    /// </summary>
    public partial class Transfers : Window
    {
        private ObservableCollection<User> users = new ObservableCollection<User>();
        private string send_path;
        private string filename;
        private bool is_dir;
        BackgroundWorker worker;
        List<BackgroundWorker> bgws = new List<BackgroundWorker>();

        public string Filename {

            get { return (String)GetValue(Transfers.TitleProperty); }
            set
            {
               
                filename = value;
                SetValue(Transfers.TitleProperty, value);
            }

        }


        public Transfers()
        {
            InitializeComponent();
            this.button_ok.IsEnabled = false;
        }
        public Transfers(ObservableCollection<User> list,string path, string filename,bool is_dir) {


            InitializeComponent();

            foreach (User u in list)
                users.Add(u);
            this.userSelectedList.ItemsSource = users;

            this.send_path = path;
            Filename = filename;
            this.is_dir = is_dir;


            transfer(bgws);


        }

     
        private void transfer(List<BackgroundWorker> bgws) {



            try
            { 

                System.Console.WriteLine("path:" + send_path);
                System.Console.WriteLine("filename:" + filename);
                System.Console.WriteLine("isDir:" + is_dir.ToString());

                // senderTCP invio_file = new senderTCP(ip_graziano, send_path, filename);



                // Task sendTask = Task.Factory.StartNew(invio_file.sendFile);
                // sendTask.Wait();
                foreach (User user in userSelectedList.Items)
                {
             
                    user.Progress = 0;
                    user.Annullable = false;
                    System.Console.WriteLine("user:" + user.Name);
                    System.Console.WriteLine("user progress:" + user.Progress);

                    worker = new BackgroundWorker();
                    worker.WorkerSupportsCancellation = true;
                    worker.WorkerReportsProgress = true;
                    worker.DoWork += new DoWorkEventHandler(worker_DoWork);

                    worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
                    worker.RunWorkerCompleted += worker_RunWorkerCompleted;

                    bgws.Add(worker);
                    System.Console.WriteLine("list bg worker"+bgws.Count);


                    worker.RunWorkerAsync(user);

        
                }

            }
            catch (Exception e)
            {

                System.Console.WriteLine(e.StackTrace);
            }

        }


        public void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            User user = e.Argument as User;

            string ipAddr = user.Address;
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            string responseString=null;

            try
            {


                var timer = new System.Timers.Timer(1000D);
                var snapshots = new Queue<long>(30);

                byte[] buffer = null;
                byte[] header = null;

                FileStream fs = new FileStream(this.send_path, FileMode.Open, FileAccess.Read, FileShare.Read);

                System.Console.WriteLine("ip to which send:" + ipAddr);

                TcpClient tcpClient = new TcpClient(ipAddr, 11000);
                tcpClient.SendTimeout = 600000;
                tcpClient.ReceiveTimeout = 600000;


                string headerStr = "Content-length:" + fs.Length.ToString() + "\r\nFilename:" + this.filename + "\r\nUser:" +Environment.UserName+"\r\nIsDir:"+this.is_dir.ToString()+"\r\n";
                Console.WriteLine(headerStr);
                int HeaderbufferSize = Encoding.ASCII.GetBytes(headerStr).Length;

                header = new byte[HeaderbufferSize];
                Array.Copy(Encoding.ASCII.GetBytes(headerStr), header, Encoding.ASCII.GetBytes(headerStr).Length);

                int filesize = Convert.ToInt32((double)fs.Length);
                tcpClient.Client.Send(header);
                double percentage = 0;

                DateTime started = DateTime.Now;

              
                {
                    // If user doesn't want to close, cancel closure

                    // tcpClient.Close();
                    //e.Cancel = false;
                    percentage = 0;
                    user.Label_time = "In attesa che il destinatario accetti il trasferimento...";
                    //user.TransferStatus = "#FFB80202";
                    user.Annullable = false;
                    (sender as BackgroundWorker).ReportProgress((int)(percentage), user);

                }


                //ottengo socket da tcpClient
                Socket s = tcpClient.Client;

                byte[] response = new byte[10];
                s.Receive(response);
                responseString = Encoding.ASCII.GetString(response);
                Console.WriteLine(responseString);

                if (responseString.StartsWith("no"))
                {

                    throw new Exception();
                }

                    if (responseString.StartsWith("ok"))
                {
                    int bufferSize = 65536;
                    if (fs.Length < bufferSize)
                        bufferSize =(int) fs.Length;

                    int bufferCount = Convert.ToInt32(Math.Ceiling(((double)(fs.Length)) / ((double)(bufferSize))));

                    for (int i = 0; i < bufferCount; i++)
                    {

                        buffer = new byte[bufferSize];
                        int size = fs.Read(buffer, 0, bufferSize);

                        if (i == (bufferCount - 1))
                            percentage += ((double)filesize);

                        percentage = (double)(((i + 1) * bufferSize)) / (double)fs.Length;
                        tcpClient.Client.Send(buffer, size, SocketFlags.Partial);
                        user.Annullable = true;
                        System.Console.WriteLine(percentage);
                        TimeSpan elapsedTime = DateTime.Now - started;
                        TimeSpan estimatedTime =
                                TimeSpan.FromSeconds(
                                    (fs.Length - ((i + 1) * bufferSize)) /
                                    ((double)((i + 1) * bufferSize) / elapsedTime.TotalSeconds));
                        user.Label_time = Convert.ToInt32((estimatedTime.TotalSeconds)).ToString();

                       
                        (sender as BackgroundWorker).ReportProgress((int)(percentage * 100), user);
                        filesize -= size;
                        Thread.Sleep(10);

                        if ((sender as BackgroundWorker).CancellationPending == true)
                        {

                            string msg = "Hai annullato il trasferimento.";
                            MessageBoxResult result =
                              MessageBox.Show(
                                msg,
                                "Attenzione",
                                MessageBoxButton.OK,
                                MessageBoxImage.Exclamation);

                            if (result == MessageBoxResult.OK)
                            {
                                // If user doesn't want to close, cancel closure
                                tcpClient.Close();
                                e.Cancel = true;
                                percentage = 0;
                                user.Label_time = "Trasferimento annullato dal mittente";
                                user.TransferStatus = "#FFB80202";
                                user.Annullable = false;
                                (sender as BackgroundWorker).ReportProgress((int)(percentage), user);
                                return;

                            }


                        }

                    }

                    ////////////////////////////
                    e.Result = user;
                    ///////////////////////////////////////////////////////////
                    Console.WriteLine("File " + send_path + " inviato a " + ipAddr);

                    fs.Close();
                    tcpClient.Client.Close();

                    FileAttributes attr = File.GetAttributes(send_path);


                    if (is_dir == true)
                    {
                        if (Directory.Exists(send_path)) { 
                            File.Delete(send_path);
                            Console.WriteLine("File " + send_path + " cancellato.");
                        }
                    }

                }


            }
            catch (Exception ex)
            {

                double percentage = 0;
                user.TransferStatus = "#FFB80202";
                user.Annullable = false;

                if (responseString.StartsWith("no"))
                {

                    Console.WriteLine("GRAZIANO NON HA ACCETTATO CAZZO");
                    // If user doesn't want to close, cancel closure
                    user.Label_time = "Trasferimento non accettato dal destinatario";
                }

                else
                  user.Label_time = "Errore di rete durante il trasferimento";
              
                (sender as BackgroundWorker).ReportProgress((int)(percentage), user);
                e.Result = user;
                throw;
            }

        }


        public void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            User user = e.UserState as User;

            if (e.ProgressPercentage > 100)
                user.Progress = 100;
            else
                user.Progress = e.ProgressPercentage;
            //user.Name = e.ProgressPercentage.ToString();
            System.Console.WriteLine("user progress after:" + user.Progress);
            System.Console.WriteLine("tiem left:" + user.Time_left);

        }

        public void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            BackgroundWorker bgw = (BackgroundWorker)sender;

            if (e.Error != null)
            {
                //User user = e.Result as User;
                //user.Label_time = "Trasferimento probabilmente cancellato dal destinatario";


            }

            else if (e.Cancelled)
            {
                //User user = e.Result as User;
                //user.Label_time = "Trasferimento annullato";

            }

            else
            {
                User user = e.Result as User;
                user.Annullable = false;
                user.Label_time = "Trasferimento concluso con successo";
            }

            bgws.Remove(bgw);

            System.Console.WriteLine("list bg worker" + bgws.Count);
            if (bgws.Count == 0) 
            this.button_ok.IsEnabled = true;
        



        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            worker.CancelAsync();

        }

        private void button_ok_Click(object sender, RoutedEventArgs e)
        {

            System.Windows.Application.Current.Shutdown();

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {

            if (bgws.Count > 0)
            {
                string msg = "Ci sono trasferimenti in corso. Annullarli ed uscire?";
                MessageBoxResult result =
                  MessageBox.Show(
                    msg,
                    "Attenzione",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    // If user doesn't want to close, cancel closure
                    e.Cancel = true;
                }

                else {

                    foreach (BackgroundWorker bgw in bgws)
                        bgw.CancelAsync();

                    System.Windows.Application.Current.Shutdown();


                }

            }

        }
    }
}
