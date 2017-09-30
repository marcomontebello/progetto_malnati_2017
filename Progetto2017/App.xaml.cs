using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Windows.Interop;


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
                MainWindow =  new MainWindow();
                MainWindow.Closing += MainWindow_Closing;
                Popup _popupWindow = new Popup();

            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.Click += (s, args) => click_on_notifyIcon(_popupWindow);
            _notifyIcon.DoubleClick += (s, args) => ShowMainWindow();
            _notifyIcon.Icon = Progetto2017.Properties.Resources.MyIcon;
            _notifyIcon.Visible = true;


            string menuCommand = string.Format("\"{0}\" \"%L\"", Application.Current);

            //Creazione entry context menu per le cartelle
            FileShellExtension.Register("Folder","LANsharing","Condividi in LAN", menuCommand);

            //Creazione entry context menu per i file
            FileShellExtension.Register("*", "LANsharing", "Condividi in LAN", menuCommand);
            _popupWindow.button.Click += (s, args) => ExitApplication(_popupWindow);
            _popupWindow.textBlock.MouseLeftButtonDown += (s, args) => ShowMainWindow();

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
