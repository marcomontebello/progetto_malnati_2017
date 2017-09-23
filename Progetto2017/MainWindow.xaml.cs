using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
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
using System.Diagnostics;

//using FileShellExtension;

namespace Progetto2017
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    /// prova
    public partial class MainWindow : Window
    {

        private void AddOption_ContextMenu()
        {
   

            // Allow the current user to read and delete the key.
            //

      
            RegistryKey _key  = Registry.ClassesRoot.OpenSubKey("Folder\\Shell", true);
            
           // _key = Registry.ClassesRoot.OpenSubKey("Directory\\Background\\Shell", true);
            RegistryKey newkey = _key.CreateSubKey("Condividi3 con");
            RegistryKey subNewkey = newkey.CreateSubKey("Command");
            subNewkey.SetValue("\"{0}\" \"%L\"", System.Reflection.Assembly.GetExecutingAssembly().Location);
            subNewkey.Close();
            newkey.Close();
            _key.Close();


            // full path to self, %L is placeholder for selected file
          /*  string menuCommand = string.Format(
                "\"{0}\" \"%L\"", Application.Current);

            // register the context menu
            FileShellExtension.Register("Folder",
                Program.KeyName, Program.MenuText,
                menuCommand);*/
        }

        public MainWindow()
        {
            InitializeComponent();
            AddOption_ContextMenu();
        }

    


    }
}
