using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using WPFNotification.Services;
using WPFNotification.Model;

namespace Progetto2017
{
    /// <summary>
    /// Logica di interazione per App.xaml
    /// </summary>
    public partial class App : Application
    {

          private System.Windows.Forms.NotifyIcon _notifyIcon;
          private bool _isExit;



        protected override void OnStartup(StartupEventArgs e)
            {
            //creazione taskbar icon
                base.OnStartup(e);
            //nuovo codice
            
            MainWindow =  new MainWindow();


            MainWindow.Closing += MainWindow_Closing;
                Popup _popupWindow = new Popup();

            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.Click += (s, args) => click_on_notifyIcon(_popupWindow);
            _notifyIcon.DoubleClick += (s, args) => ShowMainWindow();
            _notifyIcon.Icon = Progetto2017.Properties.Resources.MyIcon;
            _notifyIcon.Visible = true;



            string menuCommand = string.Format("\"{0}\" \"%L\"", System.IO.Path.GetFullPath("..\\..\\..\\FileSharing\\bin\\Debug\\FileSharing.exe"));

            //Creazione entry context menu per le cartelle
            FileShellExtension.Register("Folder","LANsharing","Condividi in LAN", menuCommand);

            //Creazione entry context menu per i file
            FileShellExtension.Register("*", "LANsharing", "Condividi in LAN", menuCommand);
            _popupWindow.button.Click += (s, args) => ExitApplication(_popupWindow);
            _popupWindow.textBlock.Click += (s, args) => ShowMainWindow();

        }


        
        private void click_on_notifyIcon(Popup pw)
        {

           
            if (pw.IsVisible) {

                pw.Hide();
            }
            else pw.Show();

        }


       private void ExitApplication(Popup pw)

            {
                _isExit = true;
                pw.Close();
                //MainWindow.Close();
                _notifyIcon.Dispose();
                _notifyIcon = null;
                
                //disattivazione menu contestuale folder file explorer
                FileShellExtension.Unregister("Folder", "LANsharing");

                //disattivazione menu contestuale per i file
                FileShellExtension.Unregister("*", "LANsharing");
                App.Current.Shutdown();

        }

        private void ShowMainWindow()
            {
                if (MainWindow.IsVisible)
                {
                    if (MainWindow.WindowState == WindowState.Minimized)
                    {
                        MainWindow.WindowState = WindowState.Normal;
                    }
                    MainWindow.Activate();
                }
                else
                {
                    MainWindow.Show();
                }
            }

            private void MainWindow_Closing(object sender, CancelEventArgs e)
            {
                if (!_isExit)
                {
                    e.Cancel = true;
                    MainWindow.Hide(); // A hidden window can be shown again, a closed one not
                }
            }

       
    }
}
