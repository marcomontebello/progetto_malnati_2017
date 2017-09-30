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
using System.Windows.Forms;

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

      
        public MainWindow()
        {
            InitializeComponent();
            boxName.Text=Settings1.Default.userName;
            ImageBrush new_source = new ImageBrush();
            new_source.ImageSource = new BitmapImage(new Uri(Settings1.Default.userImage));
            imageShape.Fill = new_source;

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.userName = boxName.Text;
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofdPicture = new OpenFileDialog();
            ofdPicture.Filter =
                "Image files|*.bmp;*.jpg;*.gif;*.png;*.tif";
            ofdPicture.FilterIndex = 1;



            if (ofdPicture.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ImageBrush new_source = new ImageBrush();
                new_source.ImageSource = new BitmapImage(new Uri(ofdPicture.FileName));
                this.userImage = ofdPicture.FileName;
                imageShape.Fill = new_source;
            }     
        }

        private void ok_button_Click(object sender, RoutedEventArgs e)
        {

            Settings1.Default.userName = userName;
            Settings1.Default.userImage = userImage;

            Settings1.Default.Save();

            this.Close();


        }

        private void open_directory_path(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fdb = new FolderBrowserDialog();
            if (fdb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox.Text = fdb.SelectedPath;
            }
        }
        private void ignore_button_Click(object sender, RoutedEventArgs e)
        {
            boxName.Text = Settings1.Default.userName;
            ImageBrush new_source = new ImageBrush();
            new_source.ImageSource = new BitmapImage(new Uri(Settings1.Default.userImage));
            imageShape.Fill = new_source;

            this.Close();
        }
    }
}
