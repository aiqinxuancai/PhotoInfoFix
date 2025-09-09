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
using PhotoInfoFix.Services;

namespace PhotoInfoFix
{
    public partial class MainWindow : Window
    {
        private List<string> imageFiles = new List<string>();
        private int currentImageIndex = -1;
        private TimeSpan calculatedTimeOffset;

        public MainWindow()
        {
            InitializeComponent();
            actualDatePicker.SelectedDateChanged += ActualDateTime_Changed;
            actualTimeTextBox.TextChanged += ActualDateTime_Changed;
        }

        private void selectButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "请选择包含照片的目录";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    pathTextBox.Text = dialog.SelectedPath;
                    LoadImages(dialog.SelectedPath);
                }
            }
        }

        private void LoadImages(string directoryPath)
        {
            var extensions = new string[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            imageFiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly)
                                .Where(f => extensions.Contains(System.IO.Path.GetExtension(f).ToLower()))
                                .ToList();

            if (imageFiles.Any())
            {
                currentImageIndex = 0;
                UpdatePreviewImage();
                applyFixButton.IsEnabled = true;
            }
            else
            {
                currentImageIndex = -1;
                imagePreview.Source = null;
                exifTimeTextBlock.Text = "";
                timeOffsetTextBlock.Text = "";
                imageCounterTextBlock.Text = "0 / 0";
                applyFixButton.IsEnabled = false;
                MessageBox.Show("目录中未找到支持的图片文件。", "提示");
            }
        }

        private void UpdatePreviewImage()
        {
            if (currentImageIndex < 0 || currentImageIndex >= imageFiles.Count)
                return;

            string filePath = imageFiles[currentImageIndex];
            try
            {
                // 使用BitmapImage加载以便在UI中显示
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // 确保文件加载后可以被其他进程访问
                bitmap.EndInit();
                imagePreview.Source = bitmap;

                // 使用System.Drawing.Image读取EXIF
                using (System.Drawing.Image image = ExifFix.GetImage(filePath))
                {
                    if (image != null)
                    {
                        DateTime time = ExifFix.GetDateTimeOriginal(image);
                        exifTimeTextBlock.Text = time.ToString("yyyy-MM-dd HH:mm:ss");
                        actualDatePicker.SelectedDate = time.Date;
                        actualTimeTextBox.Text = time.ToString("HH:mm:ss");
                    }
                }
            }
            catch (Exception ex)
            {
                exifTimeTextBlock.Text = "无法读取EXIF信息: " + ex.Message;
            }

            imageCounterTextBlock.Text = $"{currentImageIndex + 1} / {imageFiles.Count}";
            CalculateTimeOffset();
        }

        private void ActualDateTime_Changed(object sender, RoutedEventArgs e)
        {
            CalculateTimeOffset();
        }

        private void CalculateTimeOffset()
        {
            if (currentImageIndex < 0)
                return;

            try
            {
                DateTime exifTime = DateTime.Parse(exifTimeTextBlock.Text);
                DateTime actualTime = actualDatePicker.SelectedDate.Value.Date + TimeSpan.Parse(actualTimeTextBox.Text);

                calculatedTimeOffset = actualTime - exifTime;
                var time = calculatedTimeOffset.ToString();


                timeOffsetTextBlock.Text = $"{(calculatedTimeOffset < TimeSpan.Zero ? "-" : "+")}{time}";
            }
            catch (Exception ex)
            {
                timeOffsetTextBlock.Text = "无法计算时间差(格式错误)";
            }
        }

        private void prevButton_Click(object sender, RoutedEventArgs e)
        {
            if (imageFiles.Any())
            {
                currentImageIndex = (currentImageIndex - 1 + imageFiles.Count) % imageFiles.Count;
                UpdatePreviewImage();
            }
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            if (imageFiles.Any())
            {
                currentImageIndex = (currentImageIndex + 1) % imageFiles.Count;
                UpdatePreviewImage();
            }
        }

        private void applyFixButton_Click(object sender, RoutedEventArgs e)
        {
            string message = $"确认要将目录 \"{pathTextBox.Text}\" 内的所有照片应用以下时间偏移吗？\n\n偏移量: {timeOffsetTextBlock.Text}\n\n此操作将直接修改文件，请提前做好备份！";
            MessageBoxResult result = MessageBox.Show(message, "高风险操作确认", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    ExifFix.FixExifDateTime(pathTextBox.Text, calculatedTimeOffset);
                    MessageBox.Show("修正完成！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                { 
                    MessageBox.Show("修正过程中发生错误：\n" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}