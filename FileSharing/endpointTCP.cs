using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FileSharing
{
    class senderTCP
    {
        //IPHostEntry ipHost;
        IPAddress ipAddr;
        IPEndPoint ipEndPoint;
        string path;
        string filename;
        // Establish the local endpoint for the socket.

        public senderTCP(string ip,string path,string name) {

            this.ipAddr = IPAddress.Parse(ip);
            this.ipEndPoint = new IPEndPoint(ipAddr, 11000);
            this.path = path;
            this.filename = name;
        }

        /// <summary>
        /// 
        
        public void sendFile()
        {
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            try
            {


                int bufferSize = 1024;
                byte[] buffer = null;
                byte[] header = null;


                FileStream fs = new FileStream(this.path, FileMode.Open);
                bool read = true;

                int bufferCount = Convert.ToInt32(Math.Ceiling((double)fs.Length / (double)bufferSize));



                TcpClient tcpClient = new TcpClient(ipAddr.ToString(), 11000);
                tcpClient.SendTimeout = 600000;
                tcpClient.ReceiveTimeout = 600000;

                string headerStr = "Content-length:" + fs.Length.ToString() + "\r\nFilename:" + this.filename + "\r\n";
                header = new byte[bufferSize];
                Array.Copy(Encoding.ASCII.GetBytes(headerStr), header, Encoding.ASCII.GetBytes(headerStr).Length);

                tcpClient.Client.Send(header);

                for (int i = 0; i < bufferCount; i++)
                {
                    buffer = new byte[bufferSize];
                    int size = fs.Read(buffer, 0, bufferSize);

                    tcpClient.Client.Send(buffer, size, SocketFlags.Partial);

                }

                tcpClient.Client.Close();
                fs.Close();

            }
            catch (Exception e)
            {

                System.Console.WriteLine(e.StackTrace);

            }
             finally {

                FileAttributes attr = File.GetAttributes(path);

                if (attr.HasFlag(FileAttributes.Directory))
                {
                    File.Delete(this.path);
                    Console.WriteLine("File " + this.path + " cancellato.");
                }

            }
            
        }
    }
}
