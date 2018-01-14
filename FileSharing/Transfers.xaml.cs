using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FileSharing
{
    /// <summary>
    /// Logica di interazione per Transfers.xaml
    /// </summary>
    public partial class Transfers : Window
    {
        //LISTA DEI POSSIBILI MESSAGGI OTTENIBILI
        private readonly string WAIT_RECEIVER_MSG = "In attesa che il destinatario accetti il trasferimento...";
        private readonly string TRANSF_CANCELED_MSG = "Hai annullato il trasferimento.";
        private readonly string TRANSFER_NOT_ACCEPTED_MSG = "Trasferimento non accettato dal destinatario";
        private readonly string NETWORK_ERROR_MSG = "Errore di rete durante il trasferimento";
        private readonly string SUCCESS_MSG = "Trasferimento concluso con successo";
        private readonly string CLOSE_QUESTION_MSG = "Ci sono trasferimenti in corso. Annullarli ed uscire?";
        private readonly string DIRECTORY_PROCESSING = "Preparazione contenuto in corso - Potrebbe richiedere diversi minuti";
        private bool isZipping = false;

        ObservableCollection<User> users = new ObservableCollection<User>();
        private List<User> userSupportList = new List<User>();
        FileToShare file;
        BackgroundWorker worker;
        private List<BackgroundWorker> bgws = new List<BackgroundWorker>();
        private string title;

        public string Filename
        {

            get { return (String)GetValue(Transfers.TitleProperty); }
            set
            {

                this.title = value;
                SetValue(Transfers.TitleProperty, value);
            }

        }


        public Transfers()
        {
            InitializeComponent();
            this.button_ok.IsEnabled = false;
        }

        public Transfers(ObservableCollection<User> list, FileToShare file)
        {

            foreach (User u in list)
            {
                users.Add(u);
                userSupportList.Add(u);
            }

            InitializeComponent();
            Filename = file.Filename;
            userSelectedList.ItemsSource = users;


            this.Show();

            var dispatcher = this.Dispatcher;
            var loadingTask = Task.Run(
                async () =>
                {
                    await Task.Delay(200);
                    if (file.IsDir)
                    {
                        dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { label_directory.Content = DIRECTORY_PROCESSING; }));

                        string temp_path = null;

                        temp_path = System.IO.Path.GetTempPath() + file.Filename;


                        if (!(File.Exists(temp_path)))
                        {
                            isZipping = true;
                            ZipFile.CreateFromDirectory(file.Path, temp_path, CompressionLevel.NoCompression, false);
                        }
                        file.Path = temp_path;
                        System.Console.WriteLine(file.Path);

                        DirectoryInfo dInfo = new DirectoryInfo(file.Path);

                        DirectorySecurity dSecurity = dInfo.GetAccessControl();
                        dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl,
                                                     InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                                                     PropagationFlags.NoPropagateInherit, AccessControlType.Allow));

                        dInfo.SetAccessControl(dSecurity);

                        dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { label_directory.Content = " "; }));
                    }


                    await Task.Delay(200);
                });

            while (!loadingTask.IsCompleted)
            {
                dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
            }


            isZipping = false;
            this.file = new FileToShare(file.Path, file.Filename, file.IsDir);
            transfer(bgws);

        }


        private void transfer(List<BackgroundWorker> bgws)
        {

            try
            {

                System.Console.WriteLine("path:" + file.Path + " filename:" + file.Filename + " isDir:" + file.IsDir.ToString());

                int i = 0;
                foreach (User user in userSelectedList.Items)
                {

                    user.Code = i;
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
                    System.Console.WriteLine("list bg worker" + bgws.Count);


                    worker.RunWorkerAsync(user);
                    i++;
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
            string responseString = null;
            double percentage = 0;
            FileStream fs = new FileStream(this.file.Path, FileMode.Open, FileAccess.Read, FileShare.Read);

            try
            {
                byte[] buffer = null;
                byte[] header = null;

                System.Console.WriteLine("ip to which send:" + ipAddr);
                TcpClient tcpClient = new TcpClient(ipAddr, 11000);
                tcpClient.SendTimeout = 60000;
                tcpClient.ReceiveTimeout = 60000;


                string headerStr = "Content-length:" + fs.Length.ToString() + "\r\nFilename:" + this.file.Filename + "\r\nUser:" + Environment.UserName + "\r\nIsDir:" + this.file.IsDir.ToString() + "\r\n";
                Console.WriteLine(headerStr);
                int HeaderbufferSize = Encoding.ASCII.GetBytes(headerStr).Length;

                header = new byte[HeaderbufferSize];
                Array.Copy(Encoding.ASCII.GetBytes(headerStr), header, Encoding.ASCII.GetBytes(headerStr).Length);

                long filesize = fs.Length;
                tcpClient.Client.Send(header);

                DateTime started = DateTime.Now;

                {

                    percentage = 0;
                    user.Label_time = WAIT_RECEIVER_MSG;
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
                    if (fs != null)
                        fs.Dispose();

                    Thread.Sleep(100);
                    throw new Exception();
                }

                if (responseString.StartsWith("ok"))
                {
                    long bufferSize = 65536;
                    if (fs.Length < bufferSize)
                        bufferSize = fs.Length;

                    int bufferCount = Convert.ToInt32(Math.Ceiling(((double)(fs.Length)) / ((double)(bufferSize))));

                    for (int i = 0; i < bufferCount; i++)
                    {

                        buffer = new byte[bufferSize];
                        long size = fs.Read(buffer, 0, (int)bufferSize);

                        if (i == (bufferCount - 1))
                            percentage += filesize;

                        percentage = (double)(((i + 1) * bufferSize)) / fs.Length;
                        tcpClient.Client.Send(buffer, (int)size, SocketFlags.Partial);
                        user.Annullable = true;
                        System.Console.WriteLine(percentage);
                        TimeSpan elapsedTime = DateTime.Now - started;
                        TimeSpan estimatedTime =
                                TimeSpan.FromSeconds(
                                    (fs.Length - ((i + 1) * bufferSize)) /
                                    ((long)((i + 1) * bufferSize) / elapsedTime.TotalSeconds));
                        user.Label_time = Convert.ToInt64((estimatedTime.TotalSeconds)).ToString();

                        (sender as BackgroundWorker).ReportProgress((int)(percentage * 100), user);
                        filesize -= size;
                        Thread.Sleep(10);

                        if ((sender as BackgroundWorker).CancellationPending == true)
                        {

                            string msg = TRANSF_CANCELED_MSG;
                            MessageBoxResult result =
                              MessageBox.Show(
                                msg,
                                "Attenzione",
                                MessageBoxButton.OK,
                                MessageBoxImage.Exclamation);

                            if (result == MessageBoxResult.OK)
                            {
                                // If user doesn't want to close, cancel closure
                                if (fs!=null)
                                    fs.Dispose();

                                tcpClient.Close();
                                e.Cancel = true;
                                percentage = 0;
                                user.Label_time = TRANSF_CANCELED_MSG;
                                user.TransferStatus = "#FFB80202";
                                user.Annullable = false;
                                (sender as BackgroundWorker).ReportProgress((int)(percentage), user);
                                return;
                            }
                        }
                    }

                    Console.WriteLine("File " + this.file.Path + " inviato a " + ipAddr);

                    e.Result = user;
                    fs.Close();
                    tcpClient.Client.Close();

                }

            }
            catch (Exception ex)
            {
                if((file.IsDir==true) && (fs != null))
                    fs.Dispose();

                Console.WriteLine(ex.StackTrace);
                percentage = 0;
                user.TransferStatus = "#FFB80202";
                user.Annullable = false;

                if (ex is SocketException)
                    user.Label_time = NETWORK_ERROR_MSG;

                else if (responseString != null)
                    if (responseString.StartsWith("no"))
                        user.Label_time = TRANSFER_NOT_ACCEPTED_MSG;

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

        }

        public void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            BackgroundWorker bgw = (BackgroundWorker)sender;

            if (e.Error != null || e.Cancelled)
            {   //don't do nothing more  
            }
            else
            {
                User user = e.Result as User;
                user.Annullable = false;
                user.Label_time = SUCCESS_MSG;
            }

            int i = bgws.IndexOf(bgw);
            bgws.Remove(bgw);
            userSupportList.RemoveAt(i);

            foreach (User user_1 in users)
                foreach(User user_2 in userSupportList)
                    if(user_1.Equals(user_2))
                        user_1.Code = userSupportList.IndexOf(user_2);

            if (bgws.Count == 0)
            {
                this.button_ok.IsEnabled = true;

                if (this.file.IsDir && File.Exists(file.Path))
                { 

                    File.Delete(file.Path);
                    Console.WriteLine("dir cancellata da temp\n");
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            int myValue = (int)((System.Windows.Controls.Button)sender).Tag;

            bgws[myValue].CancelAsync();

        }

        private void button_ok_Click(object sender, RoutedEventArgs e)
        {

            System.Windows.Application.Current.Shutdown();

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {

            if (bgws.Count > 0 || isZipping)
            {
                string msg = CLOSE_QUESTION_MSG;
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

                else
                {
                    foreach (BackgroundWorker bgw in bgws)
                        bgw.CancelAsync();

                    System.Windows.Application.Current.Shutdown();
                }
            }
        }
    }
}
