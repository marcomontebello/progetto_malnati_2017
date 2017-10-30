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

        private int num_completed_transf=0;

        public Transfers()
        {
            InitializeComponent();
        }
        public Transfers(ObservableCollection<User> list,string path, string filename,bool is_dir) {


            InitializeComponent();

            foreach (User u in list)
                users.Add(u);
            this.userSelectedList.ItemsSource = users;

            this.send_path = path;
            this.filename = filename;
            this.is_dir = is_dir;

            transfer();


        }

      
        private void transfer() {

            try { 

                System.Console.WriteLine("path:" + send_path);
                System.Console.WriteLine("filename:" + filename);
                System.Console.WriteLine("isDir:" + is_dir.ToString());

                // senderTCP invio_file = new senderTCP(ip_graziano, send_path, filename);



                // Task sendTask = Task.Factory.StartNew(invio_file.sendFile);
                // sendTask.Wait();
                foreach (User user in userSelectedList.Items)
                {
             
                    user.Progress = 0;
                    System.Console.WriteLine("user:" + user.Name);
                    System.Console.WriteLine("user progress:" + user.Progress);

                    worker = new BackgroundWorker();
                    worker.WorkerSupportsCancellation = true;
                    worker.WorkerReportsProgress = true;
                    worker.DoWork += new DoWorkEventHandler(worker_DoWork);

                    worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
                    worker.RunWorkerCompleted += worker_RunWorkerCompleted;

                    worker.RunWorkerAsync(user);
                   // this.user.Progress = 1000;

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

            try
            {


                var timer = new System.Timers.Timer(1000D);
                var snapshots = new Queue<long>(30);

                int bufferSize = 65536;
                byte[] buffer = null;
                byte[] header = null;


                FileStream fs = new FileStream(this.send_path, FileMode.Open);
                int bufferCount = Convert.ToInt32(Math.Ceiling((double)fs.Length / (double)bufferSize));

                System.Console.WriteLine("ip to which send:" + ipAddr);

                TcpClient tcpClient = new TcpClient(ipAddr, 11000);
                tcpClient.SendTimeout = 600000;
                tcpClient.ReceiveTimeout = 600000;

                string headerStr = "Content-length:" + fs.Length.ToString() + "\r\nFilename:" + this.filename + "\r\n";
                header = new byte[bufferSize];
                Array.Copy(Encoding.ASCII.GetBytes(headerStr), header, Encoding.ASCII.GetBytes(headerStr).Length);

                int filesize = Convert.ToInt32((double)fs.Length);
                tcpClient.Client.Send(header);
                double percentage = 0;

                DateTime started = DateTime.Now;


                for (int i = 0; i < bufferCount; i++)
                {

                    buffer = new byte[bufferSize];
                    int size = fs.Read(buffer, 0, bufferSize);

                    if (i == (bufferCount - 1))
                        percentage += ((double)filesize);

                    percentage = (double)(((i + 1) * bufferSize))/(double)fs.Length;
                    tcpClient.Client.Send(buffer, size, SocketFlags.Partial);
                    System.Console.WriteLine(percentage);
                    TimeSpan elapsedTime = DateTime.Now - started;
                    TimeSpan estimatedTime =
                            TimeSpan.FromSeconds(
                                (fs.Length - ((i + 1) * bufferSize)) /
                                ((double)((i + 1) * bufferSize) / elapsedTime.TotalSeconds));
                    user.Label_time = Convert.ToInt32((estimatedTime.TotalSeconds)).ToString();
                    (sender as BackgroundWorker).ReportProgress((int)(percentage*100),user);
                    filesize -= size;
                    Thread.Sleep(10);

                    if ((sender as BackgroundWorker).CancellationPending)
                    {
                        e.Cancel = true;
                        percentage = 0;
                        user.Time_left = -1;
                        (sender as BackgroundWorker).ReportProgress((int)(percentage), user);
                        return;
                    }
                }

                Console.WriteLine("File " + send_path + " inviato a " + ipAddr);

                tcpClient.Client.Close();
                fs.Close();
                FileAttributes attr = File.GetAttributes(send_path);

                if (is_dir == true)
                {
                    File.Delete(send_path);
                    Console.WriteLine("File " + send_path + " cancellato.");
                }


            }
            catch (Exception ex)
            {

                System.Console.WriteLine(ex.StackTrace);

            }

        }


        public void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            User user = e.UserState as User;
            user.Progress = e.ProgressPercentage;
            //user.Name = e.ProgressPercentage.ToString();
            System.Console.WriteLine("user progress after:" + user.Progress);
            System.Console.WriteLine("tiem left:" + user.Time_left);

        }

        static void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            if (e.Cancelled)
            {
                MessageBox.Show("The task has been cancelled");
               
            }
            else if (e.Error != null)
            {
                MessageBox.Show("Error. Details: " + (e.Error as Exception).ToString());
            }
            else
            {
              //  MessageBox.Show("The task has been completed. Results: " + e.Result.ToString());
            }
           
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            worker.CancelAsync();

        }
    }
}
