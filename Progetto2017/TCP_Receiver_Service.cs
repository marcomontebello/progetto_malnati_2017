using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Progetto2017
{
    class TCP_Receiver_Service
    {
        public static void TCP_receiver2(object obj)
        {


            Socket socket = (Socket)obj;
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
            long filesize = 0;
            string userSender = "";

            header = new byte[bufferSize];
            string isDirectory = "";
            bool isDir = false;
            FileStream fs = null;
            bool flagD = false;
            bool request_ACCEPTED = false;
            try
            {
                socket.ReceiveTimeout = 5000;
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
                filesize = long.Parse(headers["Content-length"]);
                //Get filename from header
                filename = headers["Filename"];
                userSender = headers["User"];
                isDirectory = headers["IsDir"];
                if (isDirectory.Equals("True"))
                {
                    isDir = true;
                }
                Console.WriteLine("filename " + filename);
                Console.WriteLine("filesize " + filesize);
                Console.WriteLine("Sender " + userSender);


                //codice modificato

                if (Settings1.Default.automaticAccept == false)
                {
                    if (!requestAccept(filename, filesize, userSender))
                    {
                        Console.WriteLine("trasferimento rifiutato");
                        byte[] msg = Encoding.ASCII.GetBytes("no\r\n");

                        socket.Send(msg);
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                        return;
                    }
                    else
                    {
                        request_ACCEPTED = true;
                    }

                }


                //controllo default path
                if (Settings1.Default.useDefaultPath == false)
                {
                    selectedPathFile = findPath(filename, filesize, userSender);
                    if (selectedPathFile == null)
                    {
                        request_ACCEPTED = false;

                        byte[] msg = Encoding.ASCII.GetBytes("no\r\n");

                        socket.Send(msg);
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                        return;
                    } else
                    {
                        request_ACCEPTED = true;
                    }
                    Console.WriteLine("PERCORSO SELEZIONATO: {0}", selectedPathFile);

                }
                else
                {
                    selectedPathFile = Settings1.Default.defaultPath;
                    Console.WriteLine("PERCORSO DEFAULT: {0}", selectedPathFile);
                }

                Console.WriteLine("trasferimento accettato");
                byte[] msg2 = Encoding.ASCII.GetBytes("ok\r\n");
                socket.Send(msg2);


                selectedPathFile = selectedPathFile.Replace("\\", "\\\\");
                selectedPathFile = selectedPathFile + "\\\\";
                Console.WriteLine("Percorso directory IMPOSTATO O SELEZIONATO: " + selectedPathFile);


                if (isDir)
                {
                    Console.WriteLine("e' una directory");
                    //filename = filename.Replace(".LAN_DIR", ".zip");
                    filename = filename + ".zip";

                    Console.WriteLine("il nuovo file si chiama: {0}", filename);
                    string nameDir = filename.Substring(0, filename.Length-4);
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
                            nameDir = nameDir + "_" + k.ToString();
                        else
                        {
                            if (k < 11)
                            {
                                string lastPartOfnameDir = nameDir.Substring(nameDir.Length - 1).Replace((k - 1).ToString(), k.ToString());
                                nameDir = nameDir.Substring(0, nameDir.Length - 1) + lastPartOfnameDir;
                            } else
                            {
                                string lastPartOfnameDir = nameDir.Substring(nameDir.Length - 2).Replace((k - 1).ToString(), k.ToString());
                                nameDir = nameDir.Substring(0, nameDir.Length - 2) + lastPartOfnameDir;
                            }
                        }
                        fullPathDir = selectedPathFile + nameDir;
                        Console.WriteLine(Directory.Exists(fullPathDir) ? "Dir {0} exists." : "Dir {0} does not exist.", fullPathDir);
                        k++;
                    }
                    filename = nameDir + ".zip";
                    Console.WriteLine("FINAL DIRECTORY NAME: {0}", selectedPathFile + nameDir);
                    Console.WriteLine("FINAL DIRECTORY NAME (with zip ext): {0}", selectedPathFile + filename);

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

                    string nameF = filename.Substring(0, filename.Length-extFile.Length);
                    Console.WriteLine("CurFile no ext: {0}", nameF);

                    Console.WriteLine(File.Exists(curFile) ? "File {0} exists." : "File {0} does not exist.", curFile);
                    int i = 1;
                    while (File.Exists(curFile))
                    {
                        if (i == 1)
                            nameF = nameF + "_" + i.ToString();
                        else
                        {
                            if (i < 11)
                            {
                                string lastPartOfnameF = nameF.Substring(nameF.Length - 1).Replace((i - 1).ToString(), i.ToString());
                                nameF = nameF.Substring(0, nameF.Length - 1) + lastPartOfnameF;
                            }
                            else
                            {
                                string lastPartOfnameF = nameF.Substring(nameF.Length - 2).Replace((i - 1).ToString(), i.ToString());
                                nameF = nameF.Substring(0, nameF.Length - 2) + lastPartOfnameF;
                            }
                        }
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
                if (!isDir)
                {

                    fs = new FileStream(selectedPathFile + filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

                }
                else
                {
                    fs = new FileStream(System.IO.Path.GetTempPath() + filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                }
                while (filesize > 0)
                {
                    buffer = new byte[bufferSize];

                    int size = socket.Receive(buffer, SocketFlags.Partial);
                    if (size <= 0)
                    {
                        throw new Exception();
                    }
                    fs.Write(buffer, 0, size);

                    filesize -= size;

                }

                fs.Dispose();
                flagD = true;

                if (isDir)
                {
                    Console.WriteLine("Sto estraendo {0} ", selectedPathFile + filename);

                    // provare con 
                    // string dir = Path.GetFileNameWithoutExtension(filename) + "\\\\";
                    string dir = filename.Substring(0, filename.Length - 4) + "\\\\";
                    Console.WriteLine("Sto estraendo IN {0} ", selectedPathFile + dir);

                    ZipFile.ExtractToDirectory(System.IO.Path.GetTempPath() + filename, selectedPathFile + dir);
                    // filename = filename.Split('.').First();

                }
            }
            catch (Exception e)
            {
                if ((fs != null) && (flagD==false) )
                {
                    fs.Dispose();
                }
                Console.WriteLine("ERRORE DURANTE IL TRASFERIMENTO: "+e.StackTrace);
                if ((!isDir) && (File.Exists(selectedPathFile + filename)))
                {

                    File.Delete(selectedPathFile + filename);
                }
                if ((isDir) && (File.Exists(System.IO.Path.GetTempPath() + filename)))
                    File.Delete(System.IO.Path.GetTempPath() + filename);
                // FAR COMPARIRE LA NOTIFICA DI ERRORE SE C'E' UN ERRORE E:
                // ACCETT AUTOM E HO ACCETT
                // IMMETTERE PERCORSO E HO GIA' INSERITO
                // ENTRAMBI I DUE CASI PREC INSIEME
                if (request_ACCEPTED)
                {
                    App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Window4 errWindow = new Window4();
                        if (isDir)
                            filename = filename.Substring(0, filename.Length - 4);

                        errWindow.textBlock.Text = "Errore trasferimento: " + filename + " da " + userSender + " non ricevuto!";
                        errWindow.Show();
                        SystemSounds.Hand.Play();
                        Thread.Sleep(5000);
                        errWindow.Close();
                    });
                }
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                return;
            }
            App.Current.Dispatcher.InvokeAsync(() =>
            {
                Window3 okWindow = new Window3();
                if ((isDir) && (File.Exists(System.IO.Path.GetTempPath() + filename)))
                    File.Delete(System.IO.Path.GetTempPath() + filename);
                if (isDir)
                    filename = filename.Substring(0, filename.Length - 4);
                okWindow.textBlock.Text = "Ricevuto " + filename + " da " + userSender + "!";
                okWindow.Show();
                SystemSounds.Hand.Play();
                Thread.Sleep(5000);
                okWindow.Close();
            });


            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        private static bool requestAccept(string filename, long filesize, string userSender)
        {
            bool flag = false;
            ManualResetEvent oSignalEvent = new ManualResetEvent(false);
            App.Current.Dispatcher.InvokeAsync(() =>
            {
                Window2 fileAcceptWindow = new Window2();
                //nuovo file in arrivo; accetti automaticamente? controllare gli ultimi setting salvati

                Console.WriteLine("Non salva automaticamente, chiedere se accettare");
                double dim = (double)filesize / 1024;
                string unity = "KB";
                if (dim > 1024)
                {
                    dim = dim / 1024;
                    unity = unity.Replace("KB", "MB");
                    if(dim > 1024)
                    {
                        dim = dim / 1024;
                        unity = unity.Replace("MB", "GB");
                    }

                }
                fileAcceptWindow.textBlock.Text = "Vuoi accettare " + filename 
                    + " (" + String.Format("{0:0.00}", dim) + " "+ unity +") da " + userSender + "?";
                fileAcceptWindow.Show();
                SystemSounds.Asterisk.Play();
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

        private static string findPath(string filename, long filesize, string userSender)
        {

            ManualResetEvent oSignalEvent = new ManualResetEvent(false);

            string selectedPathFile = null;

            App.Current.Dispatcher.InvokeAsync(() =>
            {
                Window1 filePathWindow = new Window1();
                double dim = (double)filesize / 1024;
                string unity = "KB";
                if (dim > 1024)
                {
                    dim = dim / 1024;
                    unity = unity.Replace("KB", "MB");
                    if (dim > 1024)
                    {
                        dim = dim / 1024;
                        unity = unity.Replace("MB", "GB");
                    }

                }
                
                filePathWindow.textBlock.Text = "Inserisci la cartella di destinazione per "
                    + filename + " (" + String.Format("{0:0.00}", dim) + " " + unity + ") da " + userSender;

                filePathWindow.Show();
                if (Settings1.Default.automaticAccept == true)
                {
                    SystemSounds.Asterisk.Play();
                }
                filePathWindow.Topmost = true;

                //cLICK OK
                filePathWindow.button1.Click += (s, args) =>
                {
                    selectedPathFile = filePathWindow.textBox2.Text.ToString();
                    Console.WriteLine("PERCORSO selezionato: {0}", selectedPathFile);
                    filePathWindow.Close();
                    oSignalEvent.Set();
                };

                //cLICK ANNULLA
                filePathWindow.button.Click += (s, args) =>
                {
                    Console.WriteLine("Hai cliccato annulla. Uscita");
                    filePathWindow.Close();
                    oSignalEvent.Set();
                };
            });

            oSignalEvent.WaitOne();
            return selectedPathFile;

        }

    }
}
