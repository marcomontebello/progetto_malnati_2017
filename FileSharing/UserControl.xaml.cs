using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

namespace FileSharing
{
    /// <summary>
    /// Logica di interazione per UserControl.xaml
    /// </summary>
    public partial class UserControl : ProgressBar
    {
        IPAddress ipAddr;
        IPEndPoint ipEndPoint;
        string path;
        string filename;

        public UserControl(string ip, string path, string name)
        {
            InitializeComponent();
            FileInfo f = new FileInfo(path);        
            this.Maximum= f.Length;
            this.ipAddr = IPAddress.Parse(ip);
            this.ipEndPoint = new IPEndPoint(ipAddr, 11000);
            this.path = path;
            this.filename = name;    
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;

            worker.RunWorkerAsync();
        }

        public void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            


                IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
                try
                {


                    int bufferSize = 1024;
                    byte[] buffer = null;
                    byte[] header = null;


                    FileStream fs = new FileStream(this.path, FileMode.Open);
                    int bufferCount = Convert.ToInt32(Math.Ceiling((double)fs.Length / (double)bufferSize));

                    TcpClient tcpClient = new TcpClient(this.ipAddr.ToString(), 11000);
                    tcpClient.SendTimeout = 600000;
                    tcpClient.ReceiveTimeout = 600000;

                    string headerStr = "Content-length:" + fs.Length.ToString() + "\r\nFilename:" + this.filename + "\r\n";
                    header = new byte[bufferSize];
                    Array.Copy(Encoding.ASCII.GetBytes(headerStr), header, Encoding.ASCII.GetBytes(headerStr).Length);

                    int filesize = Convert.ToInt32((double)fs.Length);
                    tcpClient.Client.Send(header);
                    double percentage=0;
                    for (int i = 0; i < bufferCount; i++)
                    {
                        buffer = new byte[bufferSize];
                        int size = fs.Read(buffer, 0, bufferSize);

                    if(i==(bufferCount-1))
                         percentage += ((double)filesize);

                     percentage = (double)((i+1)*bufferSize);
                     tcpClient.Client.Send(buffer, size, SocketFlags.Partial);
                     (sender as BackgroundWorker).ReportProgress((int)percentage);
                    filesize -= size;
                    }

                tcpClient.Client.Close();
                fs.Close();
                FileAttributes attr = File.GetAttributes(path);

                if (attr.HasFlag(FileAttributes.Directory))
                {
                    File.Delete(path);
                    Console.WriteLine("File " + path + " cancellato.");
                }

            }
                catch (Exception ex)
                {

                    System.Console.WriteLine(ex.StackTrace);

                }

            }
        
        public void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            pbstatus.Value = e.ProgressPercentage;
        }

      
    }
}
