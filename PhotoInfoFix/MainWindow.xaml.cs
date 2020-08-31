using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
using System.Drawing.Imaging;
using Ookii.Dialogs.Wpf;
using PhotoInfoFix.Services;

namespace PhotoInfoFix
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //FixExifDateTime(@"D:\pic\20170326-JP-精简", "2017:03:20 10:25:00", "2014:10:02 10:58:54");



        }

        private void selectButton_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Please select a folder.";
            dialog.UseDescriptionForTitle = true; // This applies to the Vista style dialog only, not the old dialog.
            if (!VistaFolderBrowserDialog.IsVistaFolderDialogSupported)
            {
                MessageBox.Show(this, "Because you are not using Windows Vista or later, the regular folder browser dialog will be used. Please use Windows Vista to see the new dialog.", "Sample folder browser dialog");
            }
            if ((bool)dialog.ShowDialog(this))
            {
                pathTextBox.Text = dialog.SelectedPath;
                UpdateFirstFileTime();
            }
        }

        private void UpdateFirstFileTime()
        {
            if (!string.IsNullOrWhiteSpace(pathTextBox.Text))
            {
                var files = Directory.GetFiles(pathTextBox.Text, "*.*", SearchOption.AllDirectories);

                foreach(string item in files)
                {
                    System.Drawing.Image image = ExifFix.GetImage(item);
                    if (image != null)
                    {
                        DateTime time = ExifFix.GetDateTimeOriginal(image);
                        dateTimePicker.SelectedDateTime = time;
                        break;
                    }
                }

            }
        }

    }
}
