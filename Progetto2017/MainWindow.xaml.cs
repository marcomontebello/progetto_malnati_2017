using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using System.Drawing;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WPFNotification.Services;
using WPFNotification.Model;
using WPFNotification.Core.Configuration;

//using FileShellExtension;


namespace Progetto2017
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    /// prova
    public partial class MainWindow : Window
    {
       
        private string userName = null;
        private string userImage = null;
        const long MAX_PHOTO_SIZE = 10000;

        //nuovo codice 
        private readonly Dispatcher _uiDispatcher;

        public object Constants { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            this.userName = Environment.UserName;
            this.userImage = GetUserTilePath(userName);
            load_settings();

            _uiDispatcher = Dispatcher.CurrentDispatcher;

            //nuovo codice
            Task.Factory.StartNew(UDP_sender);
            Task.Factory.StartNew(TCP_receiver);
        }

        public void TCP_receiver()
        {
                TcpListener listener = new TcpListener(IPAddress.Any, 11000);
            listener.Start();
            while (true)
            {

                Socket newSocket = listener.AcceptSocket();
                Thread t = new Thread(new ParameterizedThreadStart(TCP_receiver2));
                t.Start(newSocket);
            }

        }

        public void TCP_receiver2(object obj)
        {

 
            Socket socket = (Socket) obj;
            Console.WriteLine("E' ARRIVATO UN NUOVO TRASFERIMENTO");

            int RemotePort = ((IPEndPoint)socket.RemoteEndPoint).Port;
            string RemoteIP = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
            Console.WriteLine("Nuova richiesta di trasferimento in arrivo da IP: {0}, PORT: {1}", RemoteIP, RemotePort);
            //String selectedPathFile = "C:";
            
            String selectedPathFile = null;
            //controllo accettazione file

                int bufferSize = 65536;
                byte[] buffer = null;
                byte[] header = null;
                string headerStr = "";
                string filename = "";
                int filesize = 0;
            string userSender = "";

                header = new byte[bufferSize];
            bool isDirectory = false;

            try
            {

                socket.Receive(header);

                headerStr = Encoding.ASCII.GetString(header);


                string[] splitted = headerStr.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                Dictionary<string, string> headers = new Dictionary<string, string>();
                foreach (string s in splitted)
                {
                    if (s.Contains(":"))
                    {
                        headers.Add(s.Substring(0, s.IndexOf(":")), s.Substring(s.IndexOf(":") + 1));
                    }

                }
                //Get filesize from header
                filesize = Convert.ToInt32(headers["Content-length"]);
                //Get filename from header
                filename = headers["Filename"];
                userSender = headers["User"];
                Console.WriteLine("filename "+filename);
                Console.WriteLine("filesize "+filesize);
                Console.WriteLine("Sender "+userSender);


                //codice modificato

                if (Settings1.Default.automaticAccept == false)
                {
                    if (!requestAccept(filename, filesize, userSender))
                    {
                        Console.WriteLine("trasferimento rifiutato");
                        socket.Close();
                        return;
                    }
                    Console.WriteLine("trasferimento accettato");
                }

                //controllo default path
                if (Settings1.Default.useDefaultPath == false)
                {
                    selectedPathFile = findPath(filename, filesize, userSender);
                    if (selectedPathFile == null)
                    {
                        socket.Close();
                        return;
                    }
                    Console.WriteLine("PERCORSO SELEZIONATO: {0}", selectedPathFile);
                }
                else
                {
                    selectedPathFile = Settings1.Default.defaultPath;
                    Console.WriteLine("PERCORSO DEFAULT: {0}", selectedPathFile);
                }




                selectedPathFile = selectedPathFile.Replace("\\", "\\\\");
                selectedPathFile = selectedPathFile + "\\\\";
                Console.WriteLine("Percorso directory IMPOSTATO O SELEZIONATO: " + selectedPathFile);
            

                if (filename.Split('.').Last().Equals("LAN_DIR"))
                {
                    Console.WriteLine("e' una directory");
                    filename = filename.Replace(".LAN_DIR", ".zip");
                    isDirectory = true;

                    Console.WriteLine("il nuovo file si chiama: {0}", filename);
                    string nameDir = Path.GetFileNameWithoutExtension(filename);
                    Console.WriteLine("Name Dir: {0}", nameDir);

                    string curZip = selectedPathFile + filename;
                    Console.WriteLine("FullPathFile no extracted: {0}", curZip);
                    string fullPathDir = curZip.Replace(".zip", "");
                    Console.WriteLine("Full Path Dir: {0}", fullPathDir);


                    Console.WriteLine(Directory.Exists(fullPathDir) ? "Dir {0} exists." : "Dir {0} does not exist.", fullPathDir);
                    int k = 1;
                    while (Directory.Exists(fullPathDir))
                    {
                        if (k == 1)
                            nameDir = nameDir + " - Copia_" + k.ToString();
                        else
                            nameDir = nameDir.Replace(" - Copia_" + (k - 1).ToString(), " - Copia_" + k.ToString());
                        fullPathDir = selectedPathFile + nameDir;
                        Console.WriteLine(Directory.Exists(fullPathDir) ? "Dir {0} exists." : "Dir {0} does not exist.", fullPathDir);
                        k++;
                    }
                    filename = nameDir + ".zip";
                    Console.WriteLine("FINAL DIRECTORY NAME: {0}", selectedPathFile+nameDir);
                    Console.WriteLine("FINAL DIRECTORY NAME (with zip ext): {0}", selectedPathFile+filename);

                }
                //ULTIMA PARTE
                else
                {
                    //controllo se c'è gia un file con lo stesso nome (LA VARIABILE FILENAME DEVE CONTENERE L'ESTENSIONE)
                    
                    string curFile = selectedPathFile + filename;
                    Console.WriteLine("FullPathFIle: {0}", curFile);

                //recupera anche il punto
                string extFile = Path.GetExtension(curFile);
                     Console.WriteLine("Estensione filename: {0}", extFile);

                string fullPathFile = curFile.Replace(extFile, "");
                Console.WriteLine("FullPathFIle no ext: {0}", fullPathFile);

                string nameF = Path.GetFileNameWithoutExtension(filename);
                Console.WriteLine("CurFile no ext: {0}", nameF);

                Console.WriteLine(File.Exists(curFile) ? "File {0} exists." : "File {0} does not exist.", curFile);
                    int i = 1;
                    while(File.Exists(curFile))
                    {
                        if (i == 1)
                            nameF = nameF + " - Copia_" + i.ToString();
                        else
                            nameF = nameF.Replace(" - Copia_" + (i-1).ToString(), " - Copia_" + i.ToString());
                        curFile = selectedPathFile + nameF + extFile;
                        Console.WriteLine(File.Exists(curFile) ? "File {0} exists." : "File {0} does not exist.", curFile);
                        i++;
                    }
                    filename = nameF + extFile;
                    Console.WriteLine("FINAL FILE NAME: {0}", selectedPathFile + nameF);
                    Console.WriteLine("FINAL FILE NAME (with ext): {0}", selectedPathFile + filename);
                }


                

                // string curDir = @"c:\temp\test2";
                // Console.WriteLine(Directory.Exists(curDir) ? "Dir exists." : "Dir does not exist.");
                int bufferCount = Convert.ToInt32(Math.Ceiling((double)filesize / (double)bufferSize));

                //controllo impostazioni di configurazione


                FileStream fs = new FileStream(selectedPathFile + filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                while (filesize > 0)
                {
                    buffer = new byte[bufferSize];

                    int size = socket.Receive(buffer, SocketFlags.Partial);

                    fs.Write(buffer, 0, size);

                    filesize -= size;

                } 
                fs.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("ERRORE DURANTE IL TRASFERIMENTO GENERARE NOTIFICA");
                socket.Close();
                return;
            }
            if (isDirectory)
            {
                Console.WriteLine("Sto estraendo {0} ", selectedPathFile + filename);
                try
                {
                    // provare con 
                    // string dir = Path.GetFileNameWithoutExtension(filename) + "\\\\";
                    string dir = filename.Split('.').First() + "\\\\";
                    ZipFile.ExtractToDirectory(selectedPathFile + filename, selectedPathFile + dir);
                    File.Delete(selectedPathFile + filename);
                    filename = filename.Split('.').First();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                    socket.Close();
                    return;
                }
            }
            socket.Close();
            // }
        }

        private bool requestAccept(string filename, int filesize, string userSender)
        {
            bool flag = false;
            ManualResetEvent oSignalEvent = new ManualResetEvent(false);
            _uiDispatcher.InvokeAsync(() =>
            { 
                Window2 fileAcceptWindow = new Window2();
                //nuovo file in arrivo; accetti automaticamente? controllare gli ultimi setting salvati

                Console.WriteLine("Non salva automaticamente, chiedere se accettare");
                fileAcceptWindow.textBlock.Text = "Vuoi accettare il file "+filename+" (dim: "+filesize+" B) da "+userSender+"?";
                fileAcceptWindow.Show();
                fileAcceptWindow.Topmost = true;
                //CLICK OK
                fileAcceptWindow.button1.Click += (s, args) =>
                {
                    //non invio nessun pacchetto finche non seleziono il path
                    flag = true;
                    fileAcceptWindow.Close();
                    oSignalEvent.Set();
                };
                //CLICK ANULLA
                fileAcceptWindow.button.Click += (s, args) =>
                {
                    //non invio nessun pacchetto finche non seleziono il path
                    fileAcceptWindow.Close();
                    oSignalEvent.Set();
                };
            });
            oSignalEvent.WaitOne();
            Console.WriteLine("Flag ritornato: {0}", flag);
            return flag;
        }
        
        private string findPath(string filename, int filesize, string userSender)
        {

            ManualResetEvent oSignalEvent = new ManualResetEvent(false);

            string selectedPathFile = null;
          
            _uiDispatcher.InvokeAsync(() =>
            {
                Window1 filePathWindow = new Window1();
                filePathWindow.textBlock.Text = "Inserisci la cartella di destinazione per il file: "+filename+" ricevuto da "+userSender;
                filePathWindow.Show();
                filePathWindow.Topmost = true;

                //cLICK OK
                filePathWindow.button1.Click += (s, args) =>
                {
                    selectedPathFile = filePathWindow.textBox2.Text.ToString();
                    Console.WriteLine("PERCORSOOOOOOO: {0}", selectedPathFile);
                    filePathWindow.Close();
                    oSignalEvent.Set();
                };

                //cLICK ANNULLA
                filePathWindow.button.Click += (s, args) =>
                {
                    //invio pacchetto per dire no non puoi mandare
                    //sendTCPPacket(RemotePort, RemoteIP, "non mandare");
                    Console.WriteLine("Hai cliccato annulla. Uscita");
                    filePathWindow.Close();
                    oSignalEvent.Set();
                };
                  });

                oSignalEvent.WaitOne();
            return selectedPathFile;
            
        }



        public void UDP_sender()
        {
            var Client = new UdpClient();
            //ASPETTO CHE CARICA LE IMPOSTAZIONI IN POPUP RIGUARDO STATO ONLINE/OFFLINE
            Thread.Sleep(1000);
            while (true)
            {

                //Console.WriteLine("isOffline? {0}", Popup.MyStaticBool);
                //Console.WriteLine("isOffline? {0}", Popup.MyStaticBool);


                //SE NON SONO OFFLINE INVIO UN PACCHETTO UDP OGNI SEC
                if (!Popup.isOnline)
                {
                    string path = userImage;
                    //Console.WriteLine("Percorso userimage: {0}", path);

                    //var photo = System.IO.Path.GetFileNameWithoutExtension(@"C:\Users\GRAZIANO\Desktop\crash.jpg");
                    var photo = System.IO.Path.GetFileNameWithoutExtension(path);


                    //var fi = new FileInfo(@"C:\Users\GRAZIANO\Desktop\crash.jpg");
                    var fi = new FileInfo(path);

                    //Console.WriteLine("Photo: " + photo);
                    //Console.WriteLine("Size: " + fi.Length);

                    ///* da qui si comprime la foto
                    if (fi.Length > MAX_PHOTO_SIZE)
                    {


                        using (var stream = DownscaleImage(System.Drawing.Image.FromFile(path)))
                        {
                            // salva in /..../<progetto>/bin/Debug
                            string newPath = photo + "-smaller.jpg";
                            string savePath = photo + "-RESIZED.jpg";
                            using (var file = File.Create(newPath))
                            {
                                stream.CopyTo(file);

                                Bitmap img = new Bitmap(file);
                                img.Save(savePath, System.Drawing.Imaging.ImageFormat.Jpeg);

                                // Console.WriteLine("File resized: {0} ; Size {1} Byte", savePath, file.Length);

                                //versione non compressa
                                //Bitmap img = new Bitmap(@"C:\Users\GRAZIANO\Desktop\sfondo.jpg", true);


                                Message.Udp_message dp = new Message.Udp_message { name = userName, image = img };
                                //System.Console.WriteLine("Sto inviando a {0}", userName);

                                byte[] bytes = null;

                                using (var ms = new MemoryStream())
                                {
                                    var bf = new BinaryFormatter();
                                    bf.Serialize(ms, dp);
                                    bytes = ms.ToArray();
                                }

                                //var RequestData = Encoding.ASCII.GetBytes(Settings1.Default.userName);
                                Client.EnableBroadcast = true;
                                Client.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, 8889));
                                //Client.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Parse("7.123.164.139"), 8889));
                                //System.Console.WriteLine("n byte inviati {0} verso IP: {1} e porta: {2}", bytes.Length, IPAddress.Broadcast, 8889);
                                //Client.Send(data, data.Length, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888));


                            }
                        }
                    }
                }

                Thread.Sleep(100);
            }
            Client.Close();
        }

        private static System.Drawing.Image resizeImage(System.Drawing.Image imgToResize, System.Drawing.Size size)
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)size.Width / (float)sourceWidth);
            nPercentH = ((float)size.Height / (float)sourceHeight);

            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((System.Drawing.Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();

            return (System.Drawing.Image)b;
        }
        private static MemoryStream DownscaleImage(System.Drawing.Image photo)
        {
            MemoryStream resizedPhotoStream = new MemoryStream();

            long resizedSize = 0;
            var quality = 95;
            long lastSizeDifference = 0;
            do
            {
                resizedPhotoStream.SetLength(0);

                EncoderParameters eps = new EncoderParameters(1);
                eps.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);
                ImageCodecInfo ici = GetEncoderInfo("image/jpeg");

                photo.Save(resizedPhotoStream, ici, eps);
                resizedSize = resizedPhotoStream.Length;

                long sizeDifference = resizedSize - MAX_PHOTO_SIZE;
                // Console.WriteLine(resizedSize + "(" + sizeDifference + " " + (lastSizeDifference - sizeDifference) + ")");
                lastSizeDifference = sizeDifference;
                quality--;

            } while (resizedSize > MAX_PHOTO_SIZE);

            resizedPhotoStream.Seek(0, SeekOrigin.Begin);
            //Console.WriteLine("Fine DownScale");
            return resizedPhotoStream;
        }

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }
        [DllImport("shell32.dll", EntryPoint = "#261",
                   CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void GetUserTilePath(
          string username,
          UInt32 whatever, // 0x80000000
          StringBuilder picpath, int maxLength);

        public static string GetUserTilePath(string username)
        {   // username: use null for current user
            var sb = new StringBuilder(1000);
            GetUserTilePath(username, 0x80000000, sb, sb.Capacity);
            return sb.ToString();
        }

        public static System.Drawing.Image GetUserTile(string username)
        {
            return System.Drawing.Image.FromFile(GetUserTilePath(username));
        }


  

   

        private void ok_button_Click(object sender, RoutedEventArgs e)
        {


            Settings1.Default.automaticAccept = (bool)checkBox.IsChecked;
            Settings1.Default.useDefaultPath = (bool)checkBox2.IsChecked;
            Settings1.Default.defaultPath = (string) textBox.Text;

            Settings1.Default.Save();

            this.Close();


        }

        private void open_directory_path(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fdb = new FolderBrowserDialog();
            if (fdb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox.Text = fdb.SelectedPath;
                if(ok_button.IsEnabled == false)
                {
                    ok_button.IsEnabled = true;
                }
            }
        }

        private void ignore_button_Click(object sender, RoutedEventArgs e)
        {

            load_settings();
            this.Close();
        }

        private void reset_button_Click(object sender, RoutedEventArgs e)
        {
           

            Settings1.Default.Reset();
            load_settings();


        }

        private void load_settings() {

            label.Content = this.userName;
            ImageBrush new_source = new ImageBrush();

            try
            {

                new_source.ImageSource = new BitmapImage(new Uri(this.userImage));

            }
            catch (Exception ex)
            {
                ex.ToString();
                new_source.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/user_profile_male.jpg"));
            }

            imageShape.Fill = new_source;
            checkBox.IsChecked = Settings1.Default.automaticAccept;
            checkBox2.IsChecked = Settings1.Default.useDefaultPath;
            textBox.Text = Settings1.Default.defaultPath;
            if ((checkBox2.IsChecked == false))
            {
                ok_button.IsEnabled = true;
            }
            else if ((((string)textBox.Text) == "") && (checkBox2.IsChecked == true))
            {
                ok_button.IsEnabled = false;
            }
        }

        private void checkBox2_Click(object sender, RoutedEventArgs e)
        {
            if ((checkBox2.IsChecked == false))
            {
                ok_button.IsEnabled = true;
            }
            else if ((((string)textBox.Text) == "") && (checkBox2.IsChecked == true))
            {
                ok_button.IsEnabled = false;
            }
        }
    }
}
